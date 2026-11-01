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
    public async Task Display_PageInAFolderOfAnArchive_RecordsTheArchiveAndThePagesEntry()
    {
        // Its file is named after its folder, but the entry is the page's path in the archive
        var tab = ActiveTabOfTheActiveMainWindow();
        var zip = CreateZip(Path.Combine("Volume 01", "001.jpg"), Path.Combine("Volume 02", "001.jpg"));
        var extraction = tab.ArchiveExtractionService!;
        try
        {
            await extraction.PrepareArchiveAsync(zip, NoLocalSoftware, string.CompareOrdinal);
            var page = await extraction.ExtractEntryAsync(zip, "Volume 02/001.jpg", TestContext.Current.CancellationToken);

            tab.Display(Model(page!));

            Assert.Equal("Volume 02 - 001.jpg", Path.GetFileName(page));
            Assert.Equal(ViewHistoryEntry.ForArchivePage(zip, "Volume 02/001.jpg"), Assert.Single(tab.ViewHistory.Entries));
            Assert.Equal([zip], FileHistoryManager.AllEntries.Select(e => e.Path));
            Assert.Equal(zip, Settings.StartUp.LastFile);
        }
        finally
        {
            extraction.Cleanup();
        }
    }

    [Fact]
    public async Task Display_PageThatLocalSoftwareExtractedIntoAFolder_RecordsTheArchiveAndThePagesPath()
    {
        // Local software extracts a .7z whole, keeping its folders, so the page's path in the folder is its entry
        var tab = ActiveTabOfTheActiveMainWindow();
        var archive = PathOf("volumes.7z");
        await File.WriteAllBytesAsync(archive, [0x37, 0x7A], TestContext.Current.CancellationToken);
        var entry = Path.Combine("Volume 02", "001.jpg");
        var extraction = tab.ArchiveExtractionService!;
        try
        {
            var preparation = await extraction.PrepareArchiveAsync(archive, (_, folder) =>
            {
                WriteImage(Path.Combine(folder, "Volume 01", "001.jpg"));
                WriteImage(Path.Combine(folder, entry));
                return Task.FromResult(true);
            }, string.CompareOrdinal);

            tab.Display(Model(Path.Combine(preparation!.Value.TempDirectory, entry)));

            Assert.Equal(ViewHistoryEntry.ForArchivePage(archive, entry), Assert.Single(tab.ViewHistory.Entries));
            Assert.Equal([archive], FileHistoryManager.AllEntries.Select(e => e.Path));
            Assert.Equal(archive, Settings.StartUp.LastFile);
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
    public async Task Display_OnTwoThreadsAtOnce_RecordsEachModelsPathWithItsOwnPage()
    {
        // Loads finish on whichever thread they run on, so two can show their models at once
        var file = Model(PathOf("a.jpg"));
        var tiff = TiffModel(PathOf("t.tif"), pageCount: 3);
        tiff.TiffNavigation!.CurrentPage = 2;

        for (var round = 0; round < 20; round++)
        {
            var tab = new TabViewModel(null!);
            await Task.WhenAll(new[] { file, tiff }.Select(model => Task.Run(() =>
            {
                for (var i = 0; i < 500; i++)
                {
                    tab.Display(model);
                }
            })));

            Assert.All(tab.ViewHistory.Entries, entry => Assert.Equal(entry.Path == PathOf("t.tif") ? 2 : -1, entry.Page));
        }
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
    public async Task ReloadAsync_MultiPageTiffOnAPageAfterGoingBack_ShowsThatPageAndKeepsTheEntriesAfterIt()
    {
        var files = new[] { "a.jpg", "t.tif" }.Select(name => new FileInfo(PathOf(name))).ToList();
        // A step skips the entries whose file is gone, so the files are on disk
        foreach (var file in files)
        {
            await File.WriteAllBytesAsync(file.FullName, [0x49, 0x49, 0x2A, 0x00], TestContext.Current.CancellationToken);
        }
        // Each load decodes the TIFF anew, at its first page, as a reload does
        var cache = new SharedImageCache(file =>
            ValueTask.FromResult(file.Name == "t.tif" ? TiffModel(file.FullName, pageCount: 3) : Model(file.FullName)));
        var iterator = new ImageIterator(cache, new MockThumbnailCache(), new MockThumbnailLoader(), _tab);
        iterator.Initialize(files, 1);
        await iterator.IterateToIndexAsync(1, new CancellationTokenSource());
        _tab.ShowPage(1);
        _tab.ShowPage(2);
        var back = _tab.ViewHistory.BeginStep(forwards: false)!.Value;
        _tab.ShowPage(1);
        _tab.ViewHistory.EndStep(back);

        await iterator.ReloadAsync();

        Assert.Equal([0, 1, 2], _tab.ViewHistory.Entries.Select(e => e.Page));
        Assert.Equal(1, _tab.ViewHistory.Cursor);
        Assert.Equal(1, _tab.Model.TiffNavigation!.CurrentPage);
        Assert.Same(_tab.Model.TiffNavigation.Pages![1], _tab.Image.Value);
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

    // A zip of the images given, which may sit in folders
    private string CreateZip(params string[] images)
    {
        var source = Directory.CreateDirectory(Path.Combine(_folder, "zip_" + Guid.NewGuid().ToString("N"))).FullName;
        foreach (var image in images)
        {
            WriteImage(Path.Combine(source, image));
        }
        var zip = Path.Combine(_folder, Guid.NewGuid().ToString("N") + ".zip");
        ZipFile.CreateFromDirectory(source, zip);
        return zip;
    }

    private static void WriteImage(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, [0xFF, 0xD8, 0xFF]);
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
