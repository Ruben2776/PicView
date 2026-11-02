using ImageMagick;
using PicView.Core.ArchiveHandling;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.ArchiveHandling;

/// <summary>
/// The Archive Openings of a tab: each opening prepares its archive, extracts the start page and the nine after it,
/// extracts the rest in the background, and ends finished or stopped (ADR-0013).
/// </summary>
[Collection("Sequential")]
public class ArchiveOpeningsTests : IDisposable
{
    // Bounds each wait, so a failing test fails instead of hanging the run
    private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(10);

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "PicViewTests_" + Guid.NewGuid());
    private readonly List<TabViewModel> _tabs = [];

    public ArchiveOpeningsTests()
    {
        SetDefaults();
        Directory.CreateDirectory(_folder);
    }

    public void Dispose()
    {
        foreach (var tab in _tabs)
        {
            tab.Archives.Cleanup();
        }

        try
        {
            Directory.Delete(_folder, true);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    [Fact]
    public async Task OpenAsync_ExtractsTheStartPageAndTheNineAfterItFirst()
    {
        var tab = CreateTab();
        var zip = await CreateZipAsync("first_ten.zip", 15);

        var opening = await OpenAsync(tab, zip, "img_03.jpg");

        Assert.NotNull(opening);
        Assert.Equal("img_03.jpg", opening.StartFile.Name);
        Assert.Equal(Names(3, 12), opening.Files.Select(f => f.Name));
        Assert.Equal(zip, tab.Archives.OpenArchive);
        Assert.Equal(opening.Folder, tab.Archives.OpenFolder);
        // Nothing extracts until the seed shows and the rest is asked for
        Assert.Equal(ExtractionState.Finished, tab.Archives.State);
    }

    [Fact]
    public async Task ExtractRest_ExtractsEveryOtherPage_AndListsThoseTheTabDoesNotHaveWhenItEnds()
    {
        var tab = CreateTab();
        var zip = await CreateZipAsync("rest.zip", 15);
        var opening = (await OpenAsync(tab, zip, "img_03.jpg"))!;
        // The tab lists the seed, as it does once the start page shows. Nothing lists the rest for it
        tab.ImageIterator!.Initialize(opening.Files);

        opening.ExtractRest();
        var stateWhileExtracting = tab.Archives.State;
        await opening.Ended.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

        Assert.Equal(ExtractionState.Extracting, stateWhileExtracting);
        Assert.Equal(ExtractionState.Finished, tab.Archives.State);
        Assert.Equal(Names(1, 15), Directory.GetFiles(opening.Folder).Select(Path.GetFileName).Order());
        Assert.Equal(Names(1, 15), tab.ImageIterator.Files.Select(f => f.Name));
    }

    [Fact]
    public async Task ExtractRest_ListsEachPageInTheTabAsItIsWritten()
    {
        // The last page is written until the test lets it, so the extraction is still going once the page before it is
        // written
        var writer = new HeldEntryWriter("img_12.jpg");
        var tab = CreateTab(writer);
        var zip = await CreateZipAsync("as_written.zip", 12);
        var opening = (await OpenAsync(tab, zip, null))!;
        tab.ImageIterator!.Initialize(opening.Files);

        opening.ExtractRest();
        await writer.Holding("img_12.jpg").WaitAsync(WaitLimit, TestContext.Current.CancellationToken);
        await WaitUntil(() => tab.ImageIterator.Files.Any(f => f.Name == "img_11.jpg"));
        var stateWhenListed = tab.Archives.State;
        writer.Release("img_12.jpg");
        await opening.Ended.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

        Assert.Equal(ExtractionState.Extracting, stateWhenListed);
        Assert.Equal(Names(1, 12), tab.ImageIterator.Files.Select(f => f.Name));
    }

    [Fact]
    public async Task ExtractRest_ListsThePagesOffTheLowPriorityExtractionThread()
    {
        // A key press reads the list under the lock an insert takes, so no insert holds it on the low-priority thread
        var tab = CreateTab();
        var zip = await CreateZipAsync("priority.zip", 13);
        var opening = (await OpenAsync(tab, zip, null))!;
        tab.ImageIterator!.Initialize(opening.Files);
        var listedOn = new System.Collections.Concurrent.ConcurrentBag<ThreadPriority>();
        using var listing = tab.MaxIndex.Skip(1).Subscribe(_ => listedOn.Add(Thread.CurrentThread.Priority));

        opening.ExtractRest();
        await opening.Ended.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

        Assert.Equal(Names(1, 13), tab.ImageIterator.Files.Select(f => f.Name));
        Assert.NotEmpty(listedOn);
        Assert.All(listedOn, priority => Assert.Equal(ThreadPriority.Normal, priority));
    }

    [Fact]
    public async Task ExtractRest_PageWrittenAfterTheTabListsAnotherFolder_IsNotListed()
    {
        var writer = new HeldEntryWriter("img_11.jpg");
        var tab = CreateTab(writer);
        var zip = await CreateZipAsync("left_behind.zip", 12);
        var opening = (await OpenAsync(tab, zip, null))!;
        tab.ImageIterator!.Initialize(opening.Files);
        opening.ExtractRest();
        await writer.Holding("img_11.jpg").WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

        // The tab leaves the archive for a folder while the page is still being written
        var elsewhere = new FileInfo(Path.Combine(Directory.CreateDirectory(Path.Combine(_folder, "elsewhere")).FullName, "x.jpg"));
        await File.WriteAllBytesAsync(elsewhere.FullName, [0xFF, 0xD8, 0xFF], TestContext.Current.CancellationToken);
        tab.ImageIterator.Initialize([elsewhere]);
        writer.Release("img_11.jpg");
        await opening.Ended.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

        Assert.Equal(["x.jpg"], tab.ImageIterator.Files.Select(f => f.Name));
    }

    [Fact]
    public async Task OpenAsync_SideBySideAtTheLastPage_ExtractsItsPartnerTheFirstPageWithIt()
    {
        Settings.ImageScaling.ShowImageSideBySide = true;
        Settings.UIProperties.Looping = true;
        var tab = CreateTab();
        var zip = await CreateZipAsync("partner.zip", 12);

        try
        {
            var opening = await OpenAsync(tab, zip, "img_12.jpg");

            Assert.NotNull(opening);
            Assert.Equal("img_12.jpg", opening.StartFile.Name);
            Assert.Equal(["img_01.jpg", "img_12.jpg"], opening.Files.Select(f => f.Name));
        }
        finally
        {
            Settings.ImageScaling.ShowImageSideBySide = false;
            Settings.UIProperties.Looping = false;
        }
    }

    [Fact]
    public async Task OpenAsync_WhileANewerOpeningStarts_GivesUpAndLeavesTheNewerOneOpen()
    {
        // The first opening writes its start page until the test lets it, and a newer one opens meanwhile
        var writer = new HeldEntryWriter("img_01.jpg");
        var tab = CreateTab(writer);
        var older = await CreateZipAsync("older.zip", 3);
        var newer = await CreateZipAsync("newer.zip", 3, prefix: "page");

        var first = OpenAsync(tab, older, null).AsTask();
        await writer.Holding("img_01.jpg").WaitAsync(WaitLimit, TestContext.Current.CancellationToken);
        var second = await OpenAsync(tab, newer, null);
        writer.Release("img_01.jpg");
        var firstOpening = await first.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

        Assert.Null(firstOpening);
        Assert.NotNull(second);
        Assert.Equal(newer, tab.Archives.OpenArchive);
        Assert.Equal(second.Folder, tab.Archives.OpenFolder);
    }

    [Fact]
    public async Task ExtractRest_StoppedByANewerOpening_EndsStopped()
    {
        var writer = new HeldEntryWriter("img_11.jpg");
        var tab = CreateTab(writer);
        var older = await CreateZipAsync("stopped.zip", 12);
        var newer = await CreateZipAsync("stopper.zip", 2, prefix: "page");
        var opening = (await OpenAsync(tab, older, null))!;
        opening.ExtractRest();
        await writer.Holding("img_11.jpg").WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

        // Another archive opens, which stops this one's extraction before its own page shows
        await OpenAsync(tab, newer, null);
        writer.Release("img_11.jpg");
        await opening.Ended.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

        Assert.Equal(ExtractionState.Stopped, tab.Archives.State);
        Assert.Null(tab.Archives.ProgressText.Value);
    }

    [Fact]
    public async Task ExtractRest_ReplacedByANewerExtraction_LeavesTheTabExtractingForTheNewerOne()
    {
        var writer = new HeldEntryWriter("img_11.jpg", "page_11.jpg");
        var tab = CreateTab(writer);
        var older = await CreateZipAsync("replaced.zip", 12);
        var newer = await CreateZipAsync("replacer.zip", 12, prefix: "page");
        var first = (await OpenAsync(tab, older, null))!;
        first.ExtractRest();
        await writer.Holding("img_11.jpg").WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

        // A Back opened another archive, whose extraction replaced the first, and Next waits for its pages
        var second = (await OpenAsync(tab, newer, null))!;
        second.ExtractRest();
        await writer.Holding("page_11.jpg").WaitAsync(WaitLimit, TestContext.Current.CancellationToken);
        writer.Release("img_11.jpg");
        await first.Ended.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);
        var stateAfterTheFirstEnded = tab.Archives.State;
        var progressAfterTheFirstEnded = tab.Archives.ProgressText.Value;
        writer.Release("page_11.jpg");
        await second.Ended.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

        Assert.Equal(ExtractionState.Extracting, stateAfterTheFirstEnded);
        Assert.Equal("10/12", progressAfterTheFirstEnded);
        Assert.Equal(ExtractionState.Finished, tab.Archives.State);
        Assert.Null(tab.Archives.ProgressText.Value);
    }

    [Fact]
    public async Task ExtractRest_WritesThePagesAboveTheLowestPriority()
    {
        // The gallery's thumbnails keep every core busy at normal priority, so a lowest-priority extraction starves and a
        // Next waiting for the next page waits with it
        var writer = new PriorityRecordingWriter();
        var tab = CreateTab(writer);
        var opening = (await OpenAsync(tab, await CreateZipAsync("priority_rest.zip", 12), null))!;

        opening.ExtractRest();
        await opening.Ended.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

        Assert.NotEmpty(writer.Priorities);
        Assert.All(writer.Priorities, priority => Assert.NotEqual(ThreadPriority.Lowest, priority));
    }

    [Fact]
    public async Task ExtractRest_ShowsHowManyPagesAreExtracted_UntilTheExtractionEnds()
    {
        var writer = new HeldEntryWriter("img_11.jpg");
        var tab = CreateTab(writer);
        var opening = (await OpenAsync(tab, await CreateZipAsync("progress.zip", 12), null))!;
        var progressBeforeExtracting = tab.Archives.ProgressText.Value;

        opening.ExtractRest();
        await writer.Holding("img_11.jpg").WaitAsync(WaitLimit, TestContext.Current.CancellationToken);
        var progressWhileExtracting = tab.Archives.ProgressText.Value;
        writer.Release("img_11.jpg");
        await opening.Ended.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);
        // A progress report of the last pages written could otherwise land after the end cleared the text
        await Task.Delay(100, TestContext.Current.CancellationToken);

        Assert.Null(progressBeforeExtracting);
        Assert.Equal("10/12", progressWhileExtracting);
        Assert.Null(tab.Archives.ProgressText.Value);
    }

    [Fact]
    public async Task ExtractRest_WithNothingLeftWhileAnOlderExtractionStillEnds_IsFinished()
    {
        var writer = new HeldEntryWriter("img_11.jpg");
        var tab = CreateTab(writer);
        var older = (await OpenAsync(tab, await CreateZipAsync("older_ending.zip", 12), null))!;
        older.ExtractRest();
        await writer.Holding("img_11.jpg").WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

        // A small archive opens, whose first ten pages are all of it, while the older extraction is still ending
        var newer = (await OpenAsync(tab, await CreateZipAsync("small.zip", 3, prefix: "page"), null))!;
        newer.ExtractRest();
        var stateBeforeTheOlderEnded = tab.Archives.State;
        var progressBeforeTheOlderEnded = tab.Archives.ProgressText.Value;
        writer.Release("img_11.jpg");
        await older.Ended.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

        Assert.Equal(ExtractionState.Finished, stateBeforeTheOlderEnded);
        Assert.Null(progressBeforeTheOlderEnded);
        Assert.Equal(ExtractionState.Finished, tab.Archives.State);
    }

    [Fact]
    public async Task Shown_DeletesTheFoldersOfTheOpeningsAtOrBeforeTheMark_AndKeepsTheOneShown()
    {
        var tab = CreateTab();
        var left = (await OpenAsync(tab, await CreateZipAsync("left.zip", 2), null))!;
        // A load takes its mark as it begins, and the archive it opens comes after the mark
        var mark = tab.Archives.Mark();
        var shown = (await OpenAsync(tab, await CreateZipAsync("shown.zip", 2, prefix: "page"), null))!;

        tab.Archives.Shown(mark);

        Assert.False(Directory.Exists(left.Folder), "The folder of the archive the tab left was kept");
        Assert.True(Directory.Exists(shown.Folder), "The folder of the archive shown was deleted");
        Assert.Equal(shown.Folder, tab.Archives.OpenFolder);
    }

    [Fact]
    public async Task Abandon_DeletesTheOpeningsFolder_AndLeavesTheOpenArchiveAsItWas()
    {
        var tab = CreateTab();
        var read = (await OpenAsync(tab, await CreateZipAsync("read.zip", 2), null))!;
        var abandoned = (await OpenAsync(tab, await CreateZipAsync("abandoned.zip", 2, prefix: "page"), null))!;

        // The seed never showed, as a newer load of a file overtook it
        abandoned.Abandon();

        Assert.False(Directory.Exists(abandoned.Folder), "The abandoned opening's folder was kept");
        Assert.True(Directory.Exists(read.Folder), "The folder of the archive the tab reads was deleted");
        Assert.Equal(read.Archive, tab.Archives.OpenArchive);
    }

    [Fact]
    public async Task Cleanup_StopsTheExtraction_AndDeletesEveryFolder()
    {
        var writer = new HeldEntryWriter("img_11.jpg");
        var tab = CreateTab(writer);
        var opening = (await OpenAsync(tab, await CreateZipAsync("cleaned.zip", 12), null))!;
        opening.ExtractRest();
        await writer.Holding("img_11.jpg").WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

        tab.Archives.Cleanup();
        writer.Release("img_11.jpg");
        await opening.Ended.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

        Assert.Equal(ExtractionState.Stopped, tab.Archives.State);
        Assert.False(Directory.Exists(opening.Folder), "The folder was kept");
        Assert.False(tab.Archives.HasOpenArchive);
    }

    [Fact]
    public async Task OpenAsync_ArchiveWithNoImages_ReturnsNull_AndTheOpenArchiveStays()
    {
        var tab = CreateTab();
        var read = (await OpenAsync(tab, await CreateZipAsync("images.zip", 2), null))!;
        var noImages = Path.Combine(_folder, "readme.zip");
        var source = Directory.CreateDirectory(Path.Combine(_folder, "readme_source")).FullName;
        await File.WriteAllTextAsync(Path.Combine(source, "readme.txt"), "no images", TestContext.Current.CancellationToken);
        System.IO.Compression.ZipFile.CreateFromDirectory(source, noImages);

        var opening = await OpenAsync(tab, noImages, null);

        Assert.Null(opening);
        Assert.Equal(read.Archive, tab.Archives.OpenArchive);
        Assert.True(Directory.Exists(read.Folder));
    }

    // Records the priority of the thread each page is written on
    private sealed class PriorityRecordingWriter : IArchiveEntryWriter
    {
        private readonly System.Collections.Concurrent.ConcurrentBag<ThreadPriority> _priorities = [];

        public IReadOnlyCollection<ThreadPriority> Priorities => _priorities;

        public void Write(Stream entry, FileStream file)
        {
            _priorities.Add(Thread.CurrentThread.Priority);
            entry.CopyTo(file);
        }
    }

    private static async Task WaitUntil(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + WaitLimit;
        while (!condition())
        {
            Assert.True(DateTime.UtcNow < deadline, "Timed out waiting");
            await Task.Delay(20, TestContext.Current.CancellationToken);
        }
    }

    private static ValueTask<ArchiveOpening?> OpenAsync(TabViewModel tab, string zip, string? startEntry) =>
        tab.Archives.OpenAsync(zip, startEntry, (_, _) => Task.FromResult(false), string.CompareOrdinal,
            TestContext.Current.CancellationToken);

    private static IEnumerable<string> Names(int from, int to, string prefix = "img") =>
        Enumerable.Range(from, to - from + 1).Select(i => $"{prefix}_{i:D2}.jpg");

    private TabViewModel CreateTab(IArchiveEntryWriter? writer = null)
    {
        var tab = new TabViewModel(new MainWindowViewModel(null!, null!, null!, null!, null!), null, writer);
        var thumbnails = new MockThumbnailCache();
        tab.Initialize(new SharedImageCache(file => ValueTask.FromResult(new ImageModel { FileInfo = file })), thumbnails,
            new MockThumbnailLoader(), null, thumbnails);
        tab.ImageIterator!.Initialize([]);
        _tabs.Add(tab);
        return tab;
    }

    private async Task<string> CreateZipAsync(string name, int pages, string prefix = "img")
    {
        var source = Directory.CreateDirectory(
            Path.Combine(_folder, Path.GetFileNameWithoutExtension(name) + "_source")).FullName;
        foreach (var page in Names(1, pages, prefix))
        {
            await File.WriteAllBytesAsync(Path.Combine(source, page), [0xFF, 0xD8, 0xFF],
                TestContext.Current.CancellationToken);
        }

        var zip = Path.Combine(_folder, name);
        System.IO.Compression.ZipFile.CreateFromDirectory(source, zip);
        return zip;
    }

    private sealed class MockThumbnailLoader : IThumbnailLoader
    {
        public ValueTask<object?> GetThumbnailAsync(FileInfo file, CoreViewModel core) => ValueTask.FromResult<object?>(null);

        public ValueTask<object?> GetThumbnailAsync(FileInfo file, uint size, CoreViewModel? core = null,
            MagickImage? magick = null) => ValueTask.FromResult<object?>(null);

        public object? GetExifThumbnail(FileInfo file) => null;
        public object? GetThumbQuick(FileInfo file, CoreViewModel? core = null) => null;
    }

    private sealed class MockThumbnailCache : IThumbnailCache
    {
        public void Add(uint ownerId, string path, object thumbnail) { }
        public bool TryGet(string path, out object? thumbnail) { thumbnail = null; return false; }
        public void Remove(string path) { }
        public void RemoveOwner(uint ownerId) { }
        public void Clear() { }
        public bool IsEmpty => true;
    }
}
