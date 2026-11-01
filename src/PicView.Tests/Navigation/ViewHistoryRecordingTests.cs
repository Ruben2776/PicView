using System.IO.Compression;
using ImageMagick;
using PicView.Core.ArchiveHandling;
using PicView.Core.FileHistory;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.Navigation.Tiff;
using PicView.Core.ViewModels;
using PicView.Tests.FileHistory;
using R3;

namespace PicView.Tests.Navigation;

[Collection("Sequential")]
public class ViewHistoryRecordingTests : IDisposable
{
    private readonly FileHistoryFileBackup _fileHistoryBackup = new();
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "PicViewRecording_" + Guid.NewGuid().ToString("N"));
    private readonly TabViewModel _tab;

    public ViewHistoryRecordingTests()
    {
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
        SetDefaults();
        Settings.Navigation.IsFileHistoryEnabled = true;
        Directory.CreateDirectory(_folder);
        FileHistoryManager.Initialize(Path.Combine(_folder, "history.json"));
        _tab = new TabViewModel(null!);
    }

    public void Dispose()
    {
        _tab.ArchiveExtractionService!.Cleanup();
        Directory.Delete(_folder, true);
        _fileHistoryBackup.Dispose();
    }

    [Fact]
    public void Display_File_RecordsItsPathInViewHistoryAndFileHistory()
    {
        var path = PathOf("a.jpg");

        _tab.Display(Model(path));

        var entry = Assert.Single(_tab.ViewHistory.Entries);
        Assert.Equal(ViewHistoryKind.File, entry.Kind);
        Assert.Equal(path, entry.Path);
        Assert.Equal(path, Assert.Single(FileHistoryManager.AllEntries).Path);
    }

    [Fact]
    public void Display_SideBySide_RecordsOnlyTheFirst()
    {
        _tab.Display(Model(PathOf("a.jpg")), Model(PathOf("b.jpg")));

        Assert.Equal(PathOf("a.jpg"), Assert.Single(_tab.ViewHistory.Entries).Path);
        Assert.Single(FileHistoryManager.AllEntries);
    }

    [Fact]
    public void Display_SamePrimaryWithAnotherPartner_AddsNothing()
    {
        _tab.Display(Model(PathOf("a.jpg")), Model(PathOf("b.jpg")));

        _tab.Display(Model(PathOf("a.jpg")), Model(PathOf("c.jpg")));

        Assert.Single(_tab.ViewHistory.Entries);
    }

    [Fact]
    public void Display_SameFileAgain_AddsNothing()
    {
        // A reload, a crop or a file changed on disk shows the same file again
        _tab.Display(Model(PathOf("a.jpg")));

        _tab.Display(Model(PathOf("a.jpg")));

        Assert.Single(_tab.ViewHistory.Entries);
    }

    [Fact]
    public void Display_MultiPageTiff_RecordsItsPage_AndEachPageShowPageTurnsTo()
    {
        var model = TiffModel(PathOf("t.tif"), pageCount: 3);
        var pages = model.TiffNavigation!.Pages!;
        _tab.Display(model);

        _tab.ShowPage(1);
        // The window picks each page up from the model's image
        Assert.Same(pages[1], model.Image);
        _tab.ShowPage(2);
        Assert.Same(pages[2], model.Image);

        Assert.Equal([0, 1, 2], _tab.ViewHistory.Entries.Select(e => e.Page));
        Assert.Single(FileHistoryManager.AllEntries);
    }

    [Fact]
    public void Display_SinglePageTiff_RecordsNoPage()
    {
        _tab.Display(TiffModel(PathOf("t.tif"), pageCount: 1));

        Assert.Equal(-1, Assert.Single(_tab.ViewHistory.Entries).Page);
    }

    [Fact]
    public async Task Display_ExtractedArchivePage_RecordsTheArchiveAndItsEntry()
    {
        var zip = CreateZip("a.jpg", "b.jpg");
        await PrepareArchiveAsync(zip);
        var page = await _tab.ArchiveExtractionService!.ExtractEntryAsync(zip, "b.jpg", TestContext.Current.CancellationToken);

        _tab.Display(Model(page!));

        var entry = Assert.Single(_tab.ViewHistory.Entries);
        Assert.Equal(ViewHistoryKind.ArchivePage, entry.Kind);
        Assert.Equal(zip, entry.Path);
        Assert.Equal("b.jpg", entry.ArchiveEntry);
    }

    [Fact]
    public async Task Display_FirstFileAfterLeavingTheArchive_RecordsTheFile()
    {
        // Leaving an archive shows the next file before the extraction is cleaned up
        await PrepareArchiveAsync(CreateZip("a.jpg"));

        _tab.Display(Model(PathOf("outside.jpg")));

        var entry = Assert.Single(_tab.ViewHistory.Entries);
        Assert.Equal(ViewHistoryKind.File, entry.Kind);
        Assert.Equal(PathOf("outside.jpg"), entry.Path);
    }

    [Fact]
    public async Task Display_PageOfTheShownArchive_AfterAnotherArchiveFailedToOpen_RecordsItAsThatArchivesPage()
    {
        var tab = ActiveTabOfTheActiveMainWindow();
        var zip = CreateZip("a.jpg", "b.jpg");
        var extraction = tab.ArchiveExtractionService!;
        try
        {
            await extraction.PrepareArchiveAsync(zip, NoLocalSoftware, string.CompareOrdinal);
            var a = await extraction.ExtractEntryAsync(zip, "a.jpg", TestContext.Current.CancellationToken);
            var b = await extraction.ExtractEntryAsync(zip, "b.jpg", TestContext.Current.CancellationToken);
            tab.Display(Model(a!));
            // An archive with no images fails to open, so the tab still shows the first one
            Assert.Null(await extraction.PrepareArchiveAsync(CreateZip("readme.txt"), NoLocalSoftware, string.CompareOrdinal));

            tab.Display(Model(b!));

            Assert.Equal([ViewHistoryEntry.ForArchivePage(zip, "a.jpg"), ViewHistoryEntry.ForArchivePage(zip, "b.jpg")],
                tab.ViewHistory.Entries);
            Assert.Equal([zip], FileHistoryManager.AllEntries.Select(e => e.Path));
            Assert.Equal(zip, Settings.StartUp.LastFile);
        }
        finally
        {
            extraction.Cleanup();
        }
    }

    [Fact]
    public async Task Display_PageOfAnArchive_AfterAnotherArchiveStartedOpening_RecordsItAsThatArchivesPage()
    {
        var zip = CreateZip("a.jpg", "b.jpg");
        await PrepareArchiveAsync(zip);
        var page = await _tab.ArchiveExtractionService!.ExtractEntryAsync(zip, "b.jpg", TestContext.Current.CancellationToken);
        // A quick second step opens an archive before this page shows
        await PrepareArchiveAsync(CreateZip("c.jpg"));

        _tab.Display(Model(page!));

        Assert.Equal(ViewHistoryEntry.ForArchivePage(zip, "b.jpg"), Assert.Single(_tab.ViewHistory.Entries));
        Assert.Equal([zip], FileHistoryManager.AllEntries.Select(e => e.Path));
    }

    [Fact]
    public async Task IndexOfArchivePage_PageExtractedAndListed_IsItsIndexInTheTabsFiles()
    {
        var zip = CreateZip("a.jpg", "b.jpg");
        await OpenArchiveAsync(zip, extracted: ["a.jpg", "b.jpg"], listed: ["a.jpg", "b.jpg"]);

        Assert.Equal(1, _tab.IndexOfArchivePage(ViewHistoryEntry.ForArchivePage(zip, "b.jpg")));
    }

    [Fact]
    public async Task IndexOfArchivePage_PageExtractedButNotListedYet_IsNull()
    {
        // The folder watcher adds an extracted page to the tab's files a moment later
        var zip = CreateZip("a.jpg", "b.jpg");
        await OpenArchiveAsync(zip, extracted: ["a.jpg", "b.jpg"], listed: ["a.jpg"]);

        Assert.Null(_tab.IndexOfArchivePage(ViewHistoryEntry.ForArchivePage(zip, "b.jpg")));
    }

    [Fact]
    public async Task IndexOfArchivePage_PageNotExtracted_IsNull()
    {
        var zip = CreateZip("a.jpg", "b.jpg");
        await OpenArchiveAsync(zip, extracted: ["a.jpg"], listed: ["a.jpg"]);

        Assert.Null(_tab.IndexOfArchivePage(ViewHistoryEntry.ForArchivePage(zip, "b.jpg")));
    }

    [Fact]
    public async Task IndexOfArchivePage_PageOfAnotherArchive_IsNull()
    {
        var zip = CreateZip("a.jpg");
        await OpenArchiveAsync(zip, extracted: ["a.jpg"], listed: ["a.jpg"]);

        Assert.Null(_tab.IndexOfArchivePage(ViewHistoryEntry.ForArchivePage(PathOf("other.zip"), "a.jpg")));
    }

    [Fact]
    public async Task IndexOfArchivePage_AfterTheTabLeftTheArchive_IsNull()
    {
        var zip = CreateZip("a.jpg");
        await OpenArchiveAsync(zip, extracted: ["a.jpg"], listed: ["a.jpg"]);

        _tab.ArchiveExtractionService!.Cleanup();

        Assert.Null(_tab.IndexOfArchivePage(ViewHistoryEntry.ForArchivePage(zip, "a.jpg")));
    }

    [Fact]
    public async Task IndexOfArchivePage_AfterAnotherArchiveFailedToOpen_IsStillThePagesIndex()
    {
        var zip = CreateZip("a.jpg", "b.jpg");
        await OpenArchiveAsync(zip, extracted: ["a.jpg", "b.jpg"], listed: ["a.jpg", "b.jpg"]);

        Assert.Null(await PrepareArchiveAsync(CreateZip("readme.txt")));

        Assert.Equal(1, _tab.IndexOfArchivePage(ViewHistoryEntry.ForArchivePage(zip, "b.jpg")));
    }

    [Fact]
    public void DisplaySingleImage_ClipboardImage_RecordsTheImageButNotFileHistory()
    {
        var model = new ImageModel { Image = new object() };

        _tab.DisplaySingleImage(model, SingleImageType.Clipboard, "Clipboard image");

        var entry = Assert.Single(_tab.ViewHistory.Entries);
        Assert.Same(model, entry.Model);
        Assert.Equal("Clipboard image", entry.Title);
        Assert.Empty(FileHistoryManager.AllEntries);
    }

    [Fact]
    public void DisplaySingleImage_UrlDownload_RecordsTheUrlInBoth()
    {
        const string url = "https://example.com/a.png";
        _tab.SourceURL = url;

        _tab.DisplaySingleImage(Model(PathOf("download.png")), SingleImageType.Url, url);

        Assert.Equal(url, Assert.Single(_tab.ViewHistory.Entries).Path);
        Assert.Equal(url, Assert.Single(FileHistoryManager.AllEntries).Path);
    }

    [Fact]
    public void Display_EmptyModel_RecordsNothing()
    {
        // As when a tab becomes fresh again
        _tab.Display(new ImageModel());

        Assert.Equal(0, _tab.ViewHistory.Count);
    }

    [Fact]
    public void Display_WithFileHistoryOff_StillRecordsViewHistory()
    {
        Settings.Navigation.IsFileHistoryEnabled = false;

        _tab.Display(Model(PathOf("a.jpg")));

        Assert.Single(_tab.ViewHistory.Entries);
        Assert.Empty(FileHistoryManager.AllEntries);
    }

    [Fact]
    public async Task Display_InSeveralTabsAtOnce_WhileTheDropdownReads_NeverThrows()
    {
        var tabs = Enumerable.Range(0, 4).Select(_ => new TabViewModel(null!)).ToArray();
        var showing = tabs.Select((tab, t) => Task.Run(() =>
        {
            for (var i = 0; i < 500; i++)
            {
                tab.Display(Model(PathOf($"{t}-{i % 70}.jpg")));
            }
        })).ToArray();

        while (!showing.All(task => task.IsCompleted))
        {
            _ = FileHistoryManager.AllEntries;
            _ = FileHistoryManager.PinnedEntries;
        }

        await Task.WhenAll(showing);
        Assert.All(tabs, tab => Assert.Equal(ViewHistory.MaxEntries, tab.ViewHistory.Count));
    }

    [Fact]
    public async Task RepeatNavigation_RecordsEachImageItShows_AndNoneItPassedBeforeItLoaded()
    {
        var files = new[] { "a.jpg", "b.jpg", "c.jpg", "d.jpg" }.Select(name => new FileInfo(PathOf(name))).ToList();
        var slow = new TaskCompletionSource<ImageModel>();
        var cache = new SharedImageCache(file => file.Name == "b.jpg"
            ? new ValueTask<ImageModel>(slow.Task)
            : ValueTask.FromResult(new ImageModel { FileInfo = file }));
        var iterator = new ImageIterator(cache, new MockThumbnailCache(), new MockThumbnailLoader(), _tab);
        iterator.Initialize(files);
        var cts = new CancellationTokenSource();

        // b is still loading when the held key moves on
        var passed = iterator.RepeatNavigateAsync(NavigateTo.Next, TimeSpan.Zero, cts).AsTask();
        await iterator.RepeatNavigateAsync(NavigateTo.Next, TimeSpan.Zero, cts);
        await iterator.RepeatNavigateAsync(NavigateTo.Next, TimeSpan.Zero, cts);
        slow.SetResult(new ImageModel { FileInfo = files[1] });
        await passed;

        Assert.Equal(["c.jpg", "d.jpg"], _tab.ViewHistory.Entries.Select(e => Path.GetFileName(e.Path)));
    }

    [Fact]
    public void UpdateHistory_MarksTheEntryTheActiveTabsViewHistoryIsOn_IgnoringCase()
    {
        var window = new MainWindowViewModel(null!, null!, null!, null!, null!);
        FileHistoryManager.Add(PathOf("a.jpg"));
        FileHistoryManager.Add(PathOf("b.jpg"));
        window.WindowTabs.ActiveTab.Value.Display(Model(PathOf("A.JPG")));
        var history = new FileHistoryViewModel(window);

        history.UpdateHistory();

        Assert.Equal([PathOf("a.jpg")], CurrentPaths(history));
    }

    [Fact]
    public async Task UpdateHistory_WhileTheTabBrowsesAnArchive_MarksTheArchive()
    {
        var window = new MainWindowViewModel(null!, null!, null!, null!, null!);
        var tab = window.WindowTabs.ActiveTab.Value;
        var zip = CreateZip("a.jpg");
        var history = new FileHistoryViewModel(window);

        try
        {
            await tab.ArchiveExtractionService!.PrepareArchiveAsync(zip, (_, _) => Task.FromResult(false), string.CompareOrdinal);
            tab.Display(Model(Path.Combine(tab.ArchiveExtractionService.TempZipDirectory!, "a.jpg")));

            history.UpdateHistory();

            Assert.Equal([zip], CurrentPaths(history));
        }
        finally
        {
            tab.ArchiveExtractionService!.Cleanup();
        }
    }

    private static IEnumerable<string> CurrentPaths(FileHistoryViewModel history) =>
        history.Entries.Where(e => e.IsCurrentItem.CurrentValue).Select(e => e.FilePath.CurrentValue).ToArray();

    private string PathOf(string name) => Path.Combine(_folder, name);

    private static ImageModel Model(string path) => new() { FileInfo = new FileInfo(path), Image = new object() };

    private static ImageModel TiffModel(string path, int pageCount) => new()
    {
        FileInfo = new FileInfo(path),
        Image = new object(),
        TiffNavigation = new TiffNavigationInfo
        {
            PageCount = pageCount,
            Pages = Enumerable.Range(0, pageCount).Select(_ => new object()).ToArray()
        }
    };

    private string CreateZip(params string[] images)
    {
        var source = Directory.CreateDirectory(Path.Combine(_folder, "zip_" + Guid.NewGuid().ToString("N"))).FullName;
        foreach (var image in images)
        {
            File.WriteAllBytes(Path.Combine(source, image), [0xFF, 0xD8, 0xFF]);
        }
        var zip = Path.Combine(_folder, Guid.NewGuid().ToString("N") + ".zip");
        ZipFile.CreateFromDirectory(source, zip);
        return zip;
    }

    private Task<ArchiveExtractionService.ArchivePreparation?> PrepareArchiveAsync(string zip) =>
        _tab.ArchiveExtractionService!.PrepareArchiveAsync(zip, NoLocalSoftware, string.CompareOrdinal);

    // A zip opens without local software
    private static Task<bool> NoLocalSoftware(string archive, string folder) => Task.FromResult(false);

    // Opens the archive in the tab, extracts the entries given, and lists the files of some of them, as the folder
    // watcher does once it sees them
    private async Task OpenArchiveAsync(string zip, string[] extracted, string[] listed)
    {
        await PrepareArchiveAsync(zip);
        var files = new Dictionary<string, FileInfo>();
        foreach (var entry in extracted)
        {
            var file = await _tab.ArchiveExtractionService!.ExtractEntryAsync(zip, entry, TestContext.Current.CancellationToken);
            files[entry] = new FileInfo(file!);
        }

        var thumbnails = new MockThumbnailCache();
        _tab.Initialize(new SharedImageCache(file => ValueTask.FromResult(new ImageModel { FileInfo = file })), thumbnails,
            new MockThumbnailLoader(), null, thumbnails);
        _tab.ImageIterator!.Initialize(listed.Select(entry => files[entry]).ToList());
    }

    // The active tab of the Active Main Window, which is the tab that writes the Last File
    private static TabViewModel ActiveTabOfTheActiveMainWindow()
    {
        Settings.StartUp.LastFile = null;
        var core = new CoreViewModel(null, file => ValueTask.FromResult(new ImageModel { FileInfo = file }));
        var window = new MainWindowViewModel(core, null!, null!, null!, null!);
        core.MainWindows.MainWindows.Add(window);
        core.MainWindows.ActiveWindow.Value = window;
        return window.WindowTabs.ActiveTab.Value;
    }

    private class MockThumbnailLoader : IThumbnailLoader
    {
        public ValueTask<object?> GetThumbnailAsync(FileInfo file, CoreViewModel core) => ValueTask.FromResult<object?>(null);
        public ValueTask<object?> GetThumbnailAsync(FileInfo file, uint size, CoreViewModel? core = null, MagickImage? magick = null) => ValueTask.FromResult<object?>(null);
        public object? GetExifThumbnail(FileInfo file) => null;
        public object? GetThumbQuick(FileInfo file, CoreViewModel? core = null) => null;
    }

    private class MockThumbnailCache : IThumbnailCache
    {
        public void Add(uint ownerId, string path, object thumbnail) { }
        public bool TryGet(string path, out object? thumbnail) { thumbnail = null; return false; }
        public void Remove(string path) { }
        public void RemoveOwner(uint ownerId) { }
        public void Clear() { }
        public bool IsEmpty => true;
    }

    private class MockFrameProvider : FrameProvider
    {
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) => callback.MoveNext(0);
    }
}
