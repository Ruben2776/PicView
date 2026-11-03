using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using ImageMagick;
using PicView.Core.ArchiveHandling;
using PicView.Core.FileHistory;
using PicView.Core.Localization;
using PicView.Core.IPlatform;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.Titles;
using PicView.Core.ViewModels;
using R3;
using PicView.Tests.ArchiveHandling;
using PicView.Tests.FileHistory;

namespace PicView.Tests.Navigation;

[Collection("Sequential")]
public class TabLoadTests : IDisposable
{
    private readonly FileHistoryFileBackup _fileHistoryBackup = new();
    private readonly MockImageModelLoader _mockImageModelLoader;
    private readonly SharedImageCache _cache;
    private readonly MockFileWatcherService _mockFileWatcherService;
    private readonly MockThumbnailLoader _mockThumbnailLoader;
    private readonly string _testDirectory;

    // Bounds each wait on a held load, so a failing test fails instead of hanging the run
    private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(10);

    // How long a call gets to end before the file shows, which is the bug the held-load tests catch
    private static readonly TimeSpan EarlyEndWait = TimeSpan.FromMilliseconds(500);

    // A test holds the load of the file at _heldPath: the cache's loader waits for _held, and says when it is asked
    private string? _heldPath;
    private readonly TaskCompletionSource<ImageModel> _held = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _heldRequested = new(TaskCreationOptions.RunContinuationsAsynchronously);

    // More held loads, for a test that holds several files, by path
    private readonly ConcurrentDictionary<string, HeldLoad> _holds = new(StringComparer.Ordinal);

    // Every file the cache's loader was asked to decode, in order
    private readonly ConcurrentQueue<string> _decoded = new();

    public TabLoadTests()
    {
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
        SetDefaults();
        TranslationManager.LoadLanguage("en");
        FileHistoryManager.Initialize();

        _testDirectory = Path.Combine(Path.GetTempPath(), "PicViewTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_testDirectory);

        _mockImageModelLoader = new MockImageModelLoader();
        _cache = new SharedImageCache(file =>
        {
            _decoded.Enqueue(file.FullName);
            if (_holds.TryGetValue(file.FullName, out var hold))
            {
                hold.Requested.TrySetResult();
                return new ValueTask<ImageModel>(hold.Model.Task);
            }

            if (file.FullName != _heldPath)
            {
                return ValueTask.FromResult(new ImageModel { FileInfo = file });
            }

            _heldRequested.TrySetResult();
            return new ValueTask<ImageModel>(_held.Task);
        });
        _mockFileWatcherService = new MockFileWatcherService();
        _mockThumbnailLoader = new MockThumbnailLoader();
    }

    public void Dispose()
    {
        foreach (var hold in _holds.Values)
        {
            hold.Model.TrySetResult(new ImageModel());
        }

        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        _fileHistoryBackup.Dispose();
    }

    [Fact]
    public async Task LoadAsync_File_UnwatchesAndWatchesItsFolder()
    {
        // Arrange
        var tab = CreateTab(_testDirectory);
        var path = await WriteImageAsync("test.jpg");

        // Act
        await tab.Load.LoadAsync(path);

        // Assert
        Assert.True(_mockFileWatcherService.UnwatchCalled, "Unwatch should be called");
        Assert.True(_mockFileWatcherService.WatchCalled, "Watch should be called");
        Assert.Equal(_testDirectory, _mockFileWatcherService.WatchedDirectory);
        Assert.Equal(tab, _mockFileWatcherService.WatchedTab);
    }


    [Theory]
    [InlineData("")]
    [InlineData("not a path, url or base64 ¤")]
    public async Task LoadAsync_UnloadableSource_Fails(string source)
    {
        var tab = CreateTab(_testDirectory);

        var result = await tab.Load.LoadAsync(source);

        Assert.False(result.IsLoaded);
    }

    [Fact]
    public async Task LoadAsync_DirectoryWithoutImages_Fails()
    {
        var tab = CreateTab(_testDirectory);
        var empty = Directory.CreateDirectory(Path.Combine(_testDirectory, "empty")).FullName;

        var result = await tab.Load.LoadAsync(empty);

        Assert.False(result.IsLoaded);
    }

    [Fact]
    public async Task LoadAsync_UnreachableUrl_FailsWithMessage()
    {
        var tab = CreateTab(_testDirectory);

        // Port 9 (discard) refuses connections on a machine without that service
        var result = await tab.Load.LoadAsync("http://127.0.0.1:9/image.jpg");

        Assert.False(result.IsLoaded);
        Assert.False(string.IsNullOrWhiteSpace(result.Error));
    }

    [Fact]
    public async Task LoadAsync_UnreachableUrl_LeavesErrorTitle()
    {
        var tab = CreateTab(_testDirectory);

        await tab.Load.LoadAsync("http://127.0.0.1:9/image.jpg");

        // The Error phase holds until the next load, instead of being overwritten by a recompute
        Assert.Equal(TitlePhase.Error, tab.Titles.Phase);
        Assert.Equal(TranslationManager.Translation.ErrorLoadingImage ?? "Error", tab.Titles.Title);
    }

    [Fact]
    public async Task LoadAsync_CorruptArchive_EndsLoading()
    {
        var tab = CreateTab(_testDirectory);
        var corrupt = Path.Combine(_testDirectory, "bad.zip");
        await File.WriteAllTextAsync(corrupt, "not an archive", TestContext.Current.CancellationToken);

        var result = await tab.Load.LoadAsync(corrupt);

        // The load said Loading and showed nothing, so it ends the phase itself
        Assert.False(result.IsLoaded);
        Assert.Equal(TitlePhase.None, tab.Titles.Phase);
    }

    [Fact]
    public async Task LoadAsync_UrlCancelledBeforeAnyProgress_EndsItsOwnLoading()
    {
        // A server that takes the request and never answers, so the download waits for its headers until it is
        // cancelled, before any progress
        using var server = new TcpListener(IPAddress.Loopback, 0);
        server.Start();
        var port = ((IPEndPoint)server.LocalEndpoint).Port;
        var tab = CreateTab(_testDirectory);
        tab.SetDownloading("image.jpg 40%");

        var load = tab.Load.LoadAsync($"http://127.0.0.1:{port}/image.jpg").AsTask();
        // The Load reads the tab's own token, so a token cancelled before the call cannot be given: the tab cancels it
        // while the download waits
        tab.ResetNavigationCts();
        var result = await load.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

        // The Load entered Loading in place of the Downloading it found, which it never entered and holds no ticket of.
        // It showed nothing, so it ends its own phase
        Assert.False(result.IsLoaded);
        Assert.Equal(TitlePhase.None, tab.Titles.Phase);
    }

    [Fact]
    public async Task LoadAsync_Url_DropsImageIterator()
    {
        var tab = CreateTab(_testDirectory);

        await tab.Load.LoadAsync("http://127.0.0.1:9/image.jpg");

        // Opening a file afterwards recreates it and docks the gallery again, as after a clipboard image
        Assert.Null(tab.ImageIterator);
    }

    [Fact]
    public async Task LoadAsync_FileWithRootDirectory_ListsFilesFromRoot()
    {
        Settings.Sorting.IncludeSubDirectories = true;
        try
        {
            var sub = Directory.CreateDirectory(Path.Combine(_testDirectory, "sub")).FullName;
            var inRoot = Path.Combine(_testDirectory, "a.jpg");
            var inSub = Path.Combine(sub, "b.jpg");
            await File.WriteAllTextAsync(inRoot, "x", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(inSub, "x", TestContext.Current.CancellationToken);
            var tab = CreateTab(sub);

            var result = await tab.Load.LoadAsync(inSub, new LoadOptions(RootDirectory: _testDirectory));

            Assert.True(result.IsLoaded);
            Assert.Contains(tab.ImageIterator.Files, f => f.FullName == inRoot);
            Assert.Equal(inSub, tab.ImageIterator.Files[tab.ImageIterator.CurrentIndex].FullName);
        }
        finally
        {
            Settings.Sorting.IncludeSubDirectories = false;
        }
    }

    [Fact]
    public async Task LoadAsync_NewFileInShownFolder_WaitsForWatcherInsteadOfRepopulating()
    {
        var tab = CreateTab(_testDirectory);
        var a = new FileInfo(Path.Combine(_testDirectory, "a.jpg"));
        var c = new FileInfo(Path.Combine(_testDirectory, "c.jpg"));
        var copy = new FileInfo(Path.Combine(_testDirectory, "b.jpg"));
        await File.WriteAllTextAsync(a.FullName, "x", TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(c.FullName, "x", TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(copy.FullName, "x", TestContext.Current.CancellationToken);
        tab.Model = new ImageModel { FileInfo = a };
        tab.ImageIterator.Initialize([a, c]);

        // A duplicate is loaded as soon as it is written, before the folder watcher reports it
        var load = tab.Load.LoadAsync(copy.FullName).AsTask();
        await tab.OnFolderChangedAsync(new FolderChange(FolderChangeKind.Created, copy, null,
            new FileDetailsSource(copy, waitUntilReady: false), string.CompareOrdinal), TestContext.Current.CancellationToken);
        await load;

        Assert.False(_mockFileWatcherService.UnwatchCalled, "The file list should not be rebuilt");
        Assert.Equal([a.FullName, copy.FullName, c.FullName], tab.ImageIterator.Files.Select(f => f.FullName));
        Assert.Equal(1, tab.ImageIterator.CurrentIndex);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LoadAsync_FileInAFolderTheTabIsNotBrowsing_FinishesOnlyOnceTheTabShowsIt(bool tabHasFiles)
    {
        var (tab, elsewhere) = await CreateTabAndFileElsewhereAsync(tabHasFiles);
        _heldPath = elsewhere.FullName;

        var load = tab.Load.LoadAsync(elsewhere.FullName).AsTask();
        await _heldRequested.Task.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);
        // A call that ends before the file shows ends within moments of the load being asked for
        await Task.WhenAny(load, Task.Delay(EarlyEndWait, TestContext.Current.CancellationToken));
        var endedEarly = load.IsCompleted;
        _held.SetResult(new ImageModel { FileInfo = elsewhere });
        await load.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

        Assert.False(endedEarly, "The call ended while the file was still loading");
        Assert.Equal(elsewhere.FullName, tab.Model.FileInfo?.FullName);
    }

    // The loads that clean up the extraction of the archive they leave
    public enum LoadSite
    {
        File,
        Directory,
        FileWithRootDirectory
    }

    [Theory]
    [InlineData(LoadSite.File, false)]
    [InlineData(LoadSite.File, true)]
    [InlineData(LoadSite.Directory, false)]
    [InlineData(LoadSite.Directory, true)]
    [InlineData(LoadSite.FileWithRootDirectory, false)]
    [InlineData(LoadSite.FileWithRootDirectory, true)]
    public async Task LoadAsync_FileOrDirectoryWhileANewerArchiveOpens_KeepsTheNewerExtraction(LoadSite site,
        bool tabShowsAnArchive)
    {
        var (tab, elsewhere) = await CreateTabAndFileElsewhereAsync(tabHasFiles: true);
        var left = tabShowsAnArchive ? await PrepareExtractionAsync(tab, "left.zip") : null;
        _heldPath = elsewhere.FullName;
        try
        {
            var load = StartLoad(site, tab, elsewhere);
            await _heldRequested.Task.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);
            // A quick second step can open an archive in the tab before the file shows
            var newer = await PrepareExtractionAsync(tab, "newer.zip");
            _held.SetResult(new ImageModel { FileInfo = elsewhere });
            await load.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

            Assert.True(Directory.Exists(newer), "The load deleted the extraction of the archive that opened meanwhile");
            Assert.Equal(newer, tab.Archives.OpenFolder);
            if (left is not null)
            {
                Assert.False(Directory.Exists(left), "The load left the extraction of the archive it left behind");
            }
        }
        finally
        {
            tab.Archives.Cleanup();
        }
    }

    [Theory]
    [InlineData(LoadSite.File)]
    [InlineData(LoadSite.Directory)]
    [InlineData(LoadSite.FileWithRootDirectory)]
    public async Task LoadAsync_FileOrDirectoryLeavingAnArchive_CleansUpItsExtraction(LoadSite site)
    {
        var (tab, elsewhere) = await CreateTabAndFileElsewhereAsync(tabHasFiles: true);
        var left = await PrepareExtractionAsync(tab, "left.zip");
        try
        {
            await StartLoad(site, tab, elsewhere).WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

            Assert.False(Directory.Exists(left), "The load left the archive's extraction behind");
            Assert.False(tab.Archives.HasOpenArchive);
        }
        finally
        {
            tab.Archives.Cleanup();
        }
    }

    [Fact]
    public async Task LoadAsync_Archive_ShowsTheStartPageBeforeTheRestIsExtracted()
    {
        var tempZipPath = Path.Combine(_testDirectory, "test_archive.zip");
        var archiveSourceDir = Path.Combine(_testDirectory, "archive_source");
        Directory.CreateDirectory(archiveSourceDir);

        for (var i = 1; i <= 15; i++)
        {
            await File.WriteAllBytesAsync(Path.Combine(archiveSourceDir, $"img_{i:D2}.jpg"), [0xFF, 0xD8, 0xFF], TestContext.Current.CancellationToken);
        }

        System.IO.Compression.ZipFile.CreateFromDirectory(archiveSourceDir, tempZipPath);

        var tab = CreateTab(_testDirectory);

        try
        {
            var result = await tab.Load.LoadAsync(tempZipPath);

            Assert.True(result.IsLoaded);
            Assert.True(tab.Archives.HasOpenArchive);
            Assert.NotNull(tab.ImageIterator.Files);
            // The start page is listed before anything else is extracted
            Assert.True(tab.ImageIterator.Files.Count >= 1);
            Assert.NotNull(tab.Model);
            Assert.NotNull(tab.Model.FileInfo);
            Assert.True(File.Exists(tab.Model.FileInfo.FullName));
        }
        finally
        {
            tab.Archives.Cleanup();
        }
    }

    [Fact]
    public async Task LoadAsync_ArchiveWithImagesInSeveralFolders_OpensFirstFolderAndKeepsFolderOrder()
    {
        var tempZipPath = Path.Combine(_testDirectory, "volumes.zip");
        var archiveSourceDir = Path.Combine(_testDirectory, "volumes_source");

        foreach (var volume in new[] { "Volume 01", "Volume 02" })
        {
            Directory.CreateDirectory(Path.Combine(archiveSourceDir, volume));
            for (var i = 1; i <= 3; i++)
            {
                await File.WriteAllBytesAsync(Path.Combine(archiveSourceDir, volume, $"{i:D3}.jpg"), [0xFF, 0xD8, 0xFF], TestContext.Current.CancellationToken);
            }
        }

        System.IO.Compression.ZipFile.CreateFromDirectory(archiveSourceDir, tempZipPath);

        var tab = CreateTab(_testDirectory);

        try
        {
            var result = await tab.Load.LoadAsync(tempZipPath);
            // The start page shows first, and the rest are listed as they are extracted
            await WaitUntil(() => tab.Archives.State.Value is not ExtractionState.Extracting,
                () => string.Join(", ", tab.ImageIterator.Files.Select(f => f.Name)));

            Assert.True(result.IsLoaded);
            Assert.Equal("Volume 01 - 001.jpg", tab.Model?.FileInfo?.Name);
            Assert.Equal(
                [
                    "Volume 01 - 001.jpg", "Volume 01 - 002.jpg", "Volume 01 - 003.jpg",
                    "Volume 02 - 001.jpg", "Volume 02 - 002.jpg", "Volume 02 - 003.jpg"
                ],
                tab.ImageIterator.Files.Select(f => f.Name));
        }
        finally
        {
            tab.Archives.Cleanup();
        }
    }

    [Fact]
    public async Task LoadAsync_Archive_RecordsTheArchiveAndNoneOfItsTempFiles()
    {
        Settings.Navigation.IsFileHistoryEnabled = true;
        FileHistoryManager.Initialize(Path.Combine(_testDirectory, "history.json"));
        var zip = await CreateZipAsync("recorded.zip", 3);
        var tab = CreateTab(_testDirectory);

        try
        {
            await tab.Load.LoadAsync(zip);

            Assert.Equal([zip], FileHistoryManager.AllEntries.Select(e => e.Path));
        }
        finally
        {
            tab.Archives.Cleanup();
        }
    }

    [Fact]
    public async Task LoadAsync_ArchiveWithStartEntry_ShowsThatPageFirst()
    {
        var zip = await CreateZipAsync("start.zip", 5);
        var tab = CreateTab(_testDirectory);

        try
        {
            var result = await tab.Load.LoadAsync(zip, new LoadOptions(ArchiveEntry: "img_03.jpg"));

            Assert.True(result.IsLoaded);
            Assert.Equal("img_03.jpg", tab.Model.FileInfo!.Name);
            var entry = Assert.Single(tab.ViewHistory.Entries);
            Assert.Equal(ViewHistoryKind.ArchivePage, entry.Kind);
            Assert.Equal("img_03.jpg", entry.ArchiveEntry);
        }
        finally
        {
            tab.Archives.Cleanup();
        }
    }

    [Theory]
    // The start page comes first, then those either side of it take turns, nearest first
    [InlineData(5, new[] { "img_03.jpg" }, new[] { "img_04.jpg", "img_02.jpg", "img_05.jpg", "img_01.jpg" })]
    [InlineData(15, new[] { "img_03.jpg" }, new[]
    {
        "img_04.jpg", "img_02.jpg", "img_05.jpg", "img_01.jpg", "img_06.jpg", "img_07.jpg", "img_08.jpg", "img_09.jpg",
        "img_10.jpg", "img_11.jpg", "img_12.jpg", "img_13.jpg", "img_14.jpg", "img_15.jpg"
    })]
    public async Task LoadAsync_ArchiveWithStartEntry_ExtractsTheNearestPagesFirst(int pages, string[] withTheStart,
        string[] rest)
    {
        var zip = await CreateZipAsync($"order_{pages}.zip", pages);
        // Records each page as it is written, in order
        var writer = new RecordingEntryWriter();
        var tab = CreateTab(_testDirectory, writer: writer);

        try
        {
            await tab.Load.LoadAsync(zip, new LoadOptions(ArchiveEntry: "img_03.jpg"));
            await WaitUntil(() => tab.Archives.State.Value is not ExtractionState.Extracting && writer.Written.Length >= pages,
                () => string.Join(", ", writer.Written));

            // The pages extracted together with the start entry come in the archive's order, so they are compared sorted
            Assert.Equal(withTheStart, writer.Written.Take(withTheStart.Length).Order(StringComparer.Ordinal));
            Assert.Equal(rest, writer.Written.Skip(withTheStart.Length));
        }
        finally
        {
            tab.Archives.Cleanup();
        }
    }

    [Fact]
    public async Task LoadAsync_TwoQuickBacksIntoAnArchiveTheTabLeft_KeepTheEntriesAfterThem()
    {
        Settings.Navigation.IsFileHistoryEnabled = true;
        FileHistoryManager.Initialize(Path.Combine(_testDirectory, "history.json"));
        // Local software opens a .7z: it writes the pages into the archive's folder, and the second opening waits for the
        // test to let it finish
        var archive = Path.Combine(_testDirectory, "pages.7z");
        await File.WriteAllBytesAsync(archive, [0x37, 0x7A], TestContext.Current.CancellationToken);
        var folders = new ConcurrentQueue<string>();
        var secondOpening = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondOpens = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var platform = new MockPlatformSpecificService
        {
            LocalSoftware = async (_, folder) =>
            {
                folders.Enqueue(folder);
                File.WriteAllBytes(Path.Combine(folder, "1.jpg"), [0xFF, 0xD8, 0xFF]);
                File.WriteAllBytes(Path.Combine(folder, "2.jpg"), [0xFF, 0xD8, 0xFF]);
                if (folders.Count == 2)
                {
                    secondOpening.SetResult();
                    await secondOpens.Task;
                }
                return true;
            }
        };
        // The page the first Back opens the archive at loads until the test lets it
        var firstPage = new TaskCompletionSource<ImageModel>(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstPageAsked = new TaskCompletionSource<FileInfo>(TaskCreationOptions.RunContinuationsAsynchronously);
        var isFirstPageAsked = 0;
        var cache = new SharedImageCache(file =>
        {
            if (file.Name != "2.jpg" || Interlocked.Exchange(ref isFirstPageAsked, 1) == 1)
            {
                return ValueTask.FromResult(new ImageModel { FileInfo = file });
            }

            firstPageAsked.SetResult(file);
            return new ValueTask<ImageModel>(firstPage.Task);
        });
        var tab = CreateTab(_testDirectory, cache: cache, platform: platform);
        var x = await WriteImageAsync("x.jpg");
        var y = await WriteImageAsync("y.jpg");
        // The tab showed two pages of the archive and two files, then went back to the first file
        ViewHistoryEntry[] shown =
        [
            ViewHistoryEntry.ForArchivePage(archive, "1.jpg"), ViewHistoryEntry.ForArchivePage(archive, "2.jpg"),
            ViewHistoryEntry.ForFile(x), ViewHistoryEntry.ForFile(y)
        ];
        foreach (var entry in shown)
        {
            tab.ViewHistory.Record(entry);
        }
        var backToX = tab.ViewHistory.BeginStep(forwards: false)!.Value;
        tab.ViewHistory.Record(ViewHistoryEntry.ForFile(x));
        tab.ViewHistory.EndStep(backToX);

        try
        {
            // Two quick Backs, which open the archive again at their pages, the second before the first one's page shows
            var toSecondPage = tab.ViewHistory.BeginStep(forwards: false)!.Value;
            var first = tab.Load.LoadAsync(archive, new LoadOptions(ArchiveEntry: toSecondPage.Target.ArchiveEntry))
                .AsTask();
            var firstPageFile = await firstPageAsked.Task.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);
            var toFirstPage = tab.ViewHistory.BeginStep(forwards: false)!.Value;
            var second = tab.Load.LoadAsync(archive, new LoadOptions(ArchiveEntry: toFirstPage.Target.ArchiveEntry))
                .AsTask();
            await secondOpening.Task.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);
            firstPage.SetResult(new ImageModel { FileInfo = firstPageFile });
            await first.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);
            tab.ViewHistory.EndStep(toSecondPage);
            secondOpens.SetResult();
            await second.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);
            tab.ViewHistory.EndStep(toFirstPage);

            Assert.Equal(shown, tab.ViewHistory.Entries);
            Assert.Equal(0, tab.ViewHistory.Cursor);
            Assert.Equal([archive], FileHistoryManager.AllEntries.Select(e => e.Path));
            // The first opening showed nothing, so its folder goes
            Assert.False(Directory.Exists(folders.First()), "The first opening's folder was left behind");
            Assert.Equal(folders.Last(), tab.Archives.OpenFolder);
        }
        finally
        {
            secondOpens.TrySetResult();
            firstPage.TrySetResult(new ImageModel());
            tab.Archives.Cleanup();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LoadAsync_AfterAnOvertakenArchiveLoad_CleansUpTheArchiveTheTabWasReadingWhenItShows(bool loadsAnArchive)
    {
        // Local software opens b.7z, and its page loads until the test lets it
        var b = Path.Combine(_testDirectory, "b.7z");
        await File.WriteAllBytesAsync(b, [0x37, 0x7A], TestContext.Current.CancellationToken);
        var platform = new MockPlatformSpecificService
        {
            LocalSoftware = (_, folder) =>
            {
                File.WriteAllBytes(Path.Combine(folder, "b.jpg"), [0xFF, 0xD8, 0xFF]);
                return Task.FromResult(true);
            }
        };
        var heldPage = new TaskCompletionSource<ImageModel>(TaskCreationOptions.RunContinuationsAsynchronously);
        var heldPageAsked = new TaskCompletionSource<FileInfo>(TaskCreationOptions.RunContinuationsAsynchronously);
        var cache = new SharedImageCache(file =>
        {
            if (file.Name != "b.jpg")
            {
                return ValueTask.FromResult(new ImageModel { FileInfo = file });
            }

            heldPageAsked.TrySetResult(file);
            return new ValueTask<ImageModel>(heldPage.Task);
        });
        var tab = CreateTab(_testDirectory, cache: cache, platform: platform);
        var c = await CreateZipAsync("c.zip", 2);
        var file = new FileInfo(await WriteImageAsync("x.jpg"));
        // The tab reads archive a
        await tab.Load.LoadAsync(await CreateZipAsync("a.zip", 2));
        var read = tab.Archives.OpenFolder;

        try
        {
            // Two quick Backs: the first opens b, and the second begins once b's folder is published, before b's page
            // shows, so the first is overtaken
            var first = tab.Load.LoadAsync(b).AsTask();
            var page = await heldPageAsked.Task.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);
            var second = loadsAnArchive
                ? tab.Load.LoadAsync(c).AsTask()
                : tab.Load.LoadAsync(file.FullName).AsTask();
            await second.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);
            heldPage.SetResult(new ImageModel { FileInfo = page });
            await first.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

            Assert.False(Directory.Exists(read), "The folder of the archive the tab was reading was left behind");
            Assert.Equal(loadsAnArchive ? c : null, tab.Archives.OpenArchive);
            Assert.Equal(loadsAnArchive, tab.Archives.HasOpenArchive);
        }
        finally
        {
            heldPage.TrySetResult(new ImageModel());
            tab.Archives.Cleanup();
        }
    }

    [Fact]
    public async Task LoadAsync_ArchiveWithACorruptPage_LeavesItOutAndListsTheRest()
    {
        // The corrupt page is the start page's side-by-side partner, which is extracted with it: the start shows alone,
        // and the page after the corrupt one is listed as the rest is extracted
        Settings.ImageScaling.ShowImageSideBySide = true;
        var zip = await CreateZipAsync("corrupt_partner.zip", 3);
        TestZips.CorruptEntry(zip, "img_02.jpg");
        var tab = CreateTab(_testDirectory);

        try
        {
            var result = await tab.Load.LoadAsync(zip);
            await WaitUntil(() => tab.Archives.State.Value is not ExtractionState.Extracting,
                () => string.Join(", ", tab.ImageIterator.Files.Select(f => f.Name)));

            Assert.True(result.IsLoaded);
            Assert.Equal("img_01.jpg", tab.Model.FileInfo?.Name);
            Assert.Equal(["img_01.jpg", "img_03.jpg"], tab.ImageIterator.Files.Select(f => f.Name));
        }
        finally
        {
            tab.Archives.Cleanup();
            Settings.ImageScaling.ShowImageSideBySide = false;
        }
    }

    [Fact]
    public async Task LoadAsync_ArchiveWithStartEntryInFolders_ExtractsThePagesNearestOnScreenFirst()
    {
        // The tab lists the extracted files by name, and "(" sorts before "-", so the color volume's files come first,
        // although its folder's name comes after the other one's. The start page on screen comes first, then the pages
        // either side of it take turns
        string[] withTheStart = ["Vol 1 (color) - 002.jpg"];
        string[] rest = ["Vol 1 - 001.jpg", "Vol 1 (color) - 001.jpg", "Vol 1 - 002.jpg"];
        var zip = await CreateZipWithEntriesAsync("folders.zip",
            "Vol 1/001.jpg", "Vol 1/002.jpg", "Vol 1 (color)/001.jpg", "Vol 1 (color)/002.jpg");
        // Records each page as it is written, in order
        var writer = new RecordingEntryWriter();
        var tab = CreateTab(_testDirectory, writer: writer);

        try
        {
            await tab.Load.LoadAsync(zip, new LoadOptions(ArchiveEntry: "Vol 1 (color)/002.jpg"));
            await WaitUntil(() => tab.Archives.State.Value is not ExtractionState.Extracting && writer.Written.Length >= 4,
                () => string.Join(", ", writer.Written));

            Assert.Equal("Vol 1 (color) - 002.jpg", tab.Model.FileInfo?.Name);
            // The pages extracted together with the start entry come in the archive's order, so they are compared sorted
            Assert.Equal(withTheStart, writer.Written.Take(withTheStart.Length).Order(StringComparer.Ordinal));
            Assert.Equal(rest, writer.Written.Skip(withTheStart.Length));
        }
        finally
        {
            tab.Archives.Cleanup();
        }
    }

    private static readonly string[] TwoVolumes =
    [
        "Volume 01/001.jpg", "Volume 01/002.jpg", "Volume 01/003.jpg",
        "Volume 02/001.jpg", "Volume 02/002.jpg", "Volume 02/003.jpg"
    ];

    [Theory]
    [InlineData(true)]
    // With the folders left out of the names, the pages of both volumes share names
    [InlineData(false)]
    public async Task LoadAsync_ArchiveBackToAPageInAFolderAfterTheTabLeft_OpensTheArchiveAtThatPage(
        bool isRespectingArchiveStructure)
    {
        Settings.Navigation.IsRespectingArchiveStructure = isRespectingArchiveStructure;
        var zip = await CreateZipWithEntriesAsync($"back_{isRespectingArchiveStructure}.zip", TwoVolumes);
        var tab = CreateTab(_testDirectory);
        var page = ViewHistoryEntry.ForArchivePage(zip, "Volume 02/002.jpg");
        var left = ViewHistoryEntry.ForFile(await WriteImageAsync("left.jpg"));
        // The tab showed the page, then left the archive for a file
        tab.ViewHistory.Record(page);
        tab.ViewHistory.Record(left);

        try
        {
            var back = tab.ViewHistory.BeginStep(forwards: false)!.Value;
            await tab.Load.LoadAsync(zip, new LoadOptions(ArchiveEntry: back.Target.ArchiveEntry));
            tab.ViewHistory.EndStep(back);

            Assert.Equal("Volume 02/002.jpg", tab.Archives.EntryOf(tab.Model.FileInfo!.FullName));
            Assert.Equal([page, left], tab.ViewHistory.Entries);
            Assert.Equal(0, tab.ViewHistory.Cursor);
        }
        finally
        {
            tab.Archives.Cleanup();
            Settings.Navigation.IsRespectingArchiveStructure = true;
        }
    }

    [Fact]
    public async Task LoadAsync_ArchiveBackToAPageLocalSoftwareExtractedIntoAFolder_OpensTheArchiveAtThatPage()
    {
        // Local software extracts a .7z whole, keeping its folders, so the page's path in the folder is its entry
        var archive = Path.Combine(_testDirectory, "volumes.7z");
        await File.WriteAllBytesAsync(archive, [0x37, 0x7A], TestContext.Current.CancellationToken);
        var platform = new MockPlatformSpecificService
        {
            LocalSoftware = (_, folder) =>
            {
                foreach (var entry in TwoVolumes)
                {
                    var path = Path.Combine(folder, entry);
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    File.WriteAllBytes(path, [0xFF, 0xD8, 0xFF]);
                }
                return Task.FromResult(true);
            }
        };
        var tab = CreateTab(_testDirectory, platform: platform);
        var page = ViewHistoryEntry.ForArchivePage(archive, Path.Combine("Volume 02", "002.jpg"));
        var left = ViewHistoryEntry.ForFile(await WriteImageAsync("left.jpg"));
        tab.ViewHistory.Record(page);
        tab.ViewHistory.Record(left);

        try
        {
            var back = tab.ViewHistory.BeginStep(forwards: false)!.Value;
            await tab.Load.LoadAsync(archive, new LoadOptions(ArchiveEntry: back.Target.ArchiveEntry));
            tab.ViewHistory.EndStep(back);

            Assert.Equal(Path.Combine(tab.Archives.OpenFolder!, "Volume 02", "002.jpg"),
                tab.Model.FileInfo?.FullName);
            Assert.Equal([page, left], tab.ViewHistory.Entries);
            Assert.Equal(0, tab.ViewHistory.Cursor);
        }
        finally
        {
            tab.Archives.Cleanup();
        }
    }

    [Fact]
    public async Task LoadAsync_ArchiveWithStartEntryAtTheEndOfAFolderThenNext_ShowsTheNextFoldersFirstPage()
    {
        // With the app's watcher, which adds each page to the tab's files as it is extracted, flat in the extraction
        // folder with its folder in its name
        var zip = await CreateZipWithEntriesAsync("next_folder.zip", TwoVolumes);
        var folderWatcher = new FileWatcherService(string.CompareOrdinal, _cache);
        var tab = CreateTab(_testDirectory, watcher: folderWatcher);

        try
        {
            await tab.Load.LoadAsync(zip, new LoadOptions(ArchiveEntry: "Volume 01/003.jpg"));
            await tab.ImageIterator.NavigateAsync(NavigateTo.Next, SkipAmount.One, new CancellationTokenSource())
                .AsTask().WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

            Assert.Equal("Volume 02 - 001.jpg", tab.Model.FileInfo!.Name);
            Assert.Equal(ViewHistoryEntry.ForArchivePage(zip, "Volume 02/001.jpg"), tab.ViewHistory.Current);
        }
        finally
        {
            folderWatcher.Dispose();
            tab.Archives.Cleanup();
        }
    }

    [Fact]
    public async Task LoadAsync_ArchiveWithStartEntryThenNext_ShowsThePageAfterIt()
    {
        var zip = await CreateZipAsync("next.zip", 5);
        // The app's watcher, which adds each page to the tab's files as it is extracted
        var folderWatcher = new FileWatcherService(string.CompareOrdinal, _cache);
        var tab = CreateTab(_testDirectory, watcher: folderWatcher);

        try
        {
            await tab.Load.LoadAsync(zip, new LoadOptions(ArchiveEntry: "img_03.jpg"));
            await tab.ImageIterator.NavigateAsync(NavigateTo.Next, SkipAmount.One, new CancellationTokenSource())
                .AsTask().WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

            Assert.Equal("img_04.jpg", tab.Model.FileInfo!.Name);
        }
        finally
        {
            folderWatcher.Dispose();
            tab.Archives.Cleanup();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LoadAsync_ArchiveNextWaitingForAPageStillBeingWritten_ShowsItOnceItIsWritten(bool looping)
    {
        // The start page comes first, and img_02 is extracted first of the rest, held by the test while it is written. The
        // opening lists it the moment it is written, with no folder watcher in between
        Settings.UIProperties.Looping = looping;
        var zip = await CreateZipAsync($"page_written_{looping}.zip", 15);
        var writer = new HeldEntryWriter("img_02.jpg");
        var tab = CreateTab(_testDirectory, writer: writer);

        try
        {
            await tab.Load.LoadAsync(zip, new LoadOptions(ArchiveEntry: "img_01.jpg"));
            // The extraction writes img_02 until the test lets it
            await writer.Holding("img_02.jpg").WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

            // Next at the start page, the only one listed, waits for img_02
            var next = tab.ImageIterator.NavigateAsync(NavigateTo.Next, SkipAmount.One, new CancellationTokenSource())
                .AsTask();
            var wasWaiting = !next.IsCompleted;
            writer.Release("img_02.jpg");
            await next.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

            Assert.True(wasWaiting);
            // Not the shown page again, nor round to the first one with looping
            Assert.Equal("img_02.jpg", tab.Model.FileInfo?.Name);
            Assert.Equal(
                [ViewHistoryEntry.ForArchivePage(zip, "img_01.jpg"), ViewHistoryEntry.ForArchivePage(zip, "img_02.jpg")],
                tab.ViewHistory.Entries);

            await WaitUntil(() => tab.Archives.State.Value is not ExtractionState.Extracting,
                () => string.Join(", ", tab.ImageIterator.Files.Select(f => f.Name)));
            Assert.Equal(Enumerable.Range(1, 15).Select(i => $"img_{i:D2}.jpg"),
                tab.ImageIterator.Files.Select(f => f.Name));
        }
        finally
        {
            writer.Release("img_02.jpg");
            tab.Archives.Cleanup();
            Settings.UIProperties.Looping = false;
        }
    }

    [Fact]
    public async Task LoadAsync_ArchiveBackgroundExtractionEnds_ListsItsPagesOffTheLowPriorityWorker()
    {
        // The end waits for the last pages to be listed, then clears the progress and wakes a Next waiting for a page.
        // The extraction's own thread has a low priority, so on a busy machine the end would keep a key press
        // waiting if it ran there
        var zip = await CreateZipAsync("end_priority.zip", 12);
        var writer = new HeldEntryWriter("img_12.jpg");
        var tab = CreateTab(_testDirectory, writer: writer);
        var endedOn = new TaskCompletionSource<ThreadPriority>(TaskCreationOptions.RunContinuationsAsynchronously);
        // The end clears the progress text right after it listed the pages, on the same thread
        using var ending = tab.Archives.ProgressText.AsObservable().Subscribe(progress =>
        {
            if (progress is null && writer.Holding("img_12.jpg").IsCompleted)
            {
                endedOn.TrySetResult(Thread.CurrentThread.Priority);
            }
        });

        try
        {
            await tab.Load.LoadAsync(zip, new LoadOptions(ArchiveEntry: "img_01.jpg"));
            // Holding the last page's write keeps the extraction from ending before the load awaits it
            await writer.Holding("img_12.jpg").WaitAsync(WaitLimit, TestContext.Current.CancellationToken);
            writer.Release("img_12.jpg");
            var priority = await endedOn.Task.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

            Assert.Equal(ThreadPriority.Normal, priority);
            Assert.Equal(Enumerable.Range(1, 12).Select(i => $"img_{i:D2}.jpg"),
                tab.ImageIterator.Files.Select(f => f.Name));
        }
        finally
        {
            writer.Release("img_12.jpg");
            tab.Archives.Cleanup();
        }
    }

    [Fact]
    public async Task LoadAsync_Relist_ListsTheShownFilesFolderAndKeepsTheShownFile()
    {
        var a = await WriteImageAsync("a.jpg");
        var b = await WriteImageAsync("b.jpg");
        var tab = CreateTab(_testDirectory);
        await tab.Load.LoadAsync(a);
        var c = await WriteImageAsync("c.jpg");
        var shown = 0;
        using var showing = tab.Shown.Subscribe(_ => Interlocked.Increment(ref shown));

        var result = await tab.Load.LoadAsync(a, new LoadOptions(Relist: true));

        Assert.Equal(LoadOutcome.Loaded, result.Outcome);
        Assert.Equal([a, b, c], tab.ImageIterator!.Files.Select(f => f.FullName));
        Assert.Equal(a, tab.Model.FileInfo?.FullName);
        Assert.Equal(0, tab.ImageIterator.CurrentIndex);
        // A relist lists again and shows nothing
        Assert.Equal(0, shown);
    }

    [Fact]
    public async Task LoadAsync_RelistOfADeletedFile_FailsAndChangesNothing()
    {
        var a = await WriteImageAsync("a.jpg");
        var b = await WriteImageAsync("b.jpg");
        var tab = CreateTab(_testDirectory);
        await tab.Load.LoadAsync(a);
        File.Delete(a);
        var unwatches = _mockFileWatcherService.UnwatchCount;
        var phase = tab.Titles.Phase;
        // The tab's latest load, which a relist that changes nothing leaves the latest
        var latest = tab.BeginLoad();

        var result = await tab.Load.LoadAsync(a, new LoadOptions(Relist: true));

        Assert.Equal(LoadOutcome.Failed, result.Outcome);
        Assert.Equal([a, b], tab.ImageIterator!.Files.Select(f => f.FullName));
        Assert.Equal(0, tab.ImageIterator.CurrentIndex);
        Assert.Equal(a, tab.Model.FileInfo?.FullName);
        Assert.Equal(phase, tab.Titles.Phase);
        Assert.Equal(unwatches, _mockFileWatcherService.UnwatchCount);
        Assert.True(tab.IsLatestLoad(latest), "The relist began a load, which supersedes the one running");
        Assert.False(tab.Load.IsLoading.Value);
    }

    [Fact]
    public async Task LoadAsync_WithModel_ShowsItWithoutDecodingAndListsTheFiles()
    {
        var a = await WriteImageAsync("a.jpg");
        var b = await WriteImageAsync("b.jpg");
        // A tab that never opened a source, as at startup, so the Load makes its iterator
        var tab = CreateTab(_testDirectory, withIterator: false);
        var model = new ImageModel { FileInfo = new FileInfo(a) };

        var result = await tab.Load.LoadAsync(a, new LoadOptions(Model: model));

        Assert.Equal(LoadOutcome.Loaded, result.Outcome);
        Assert.Equal(0, _mockImageModelLoader.Calls);
        Assert.DoesNotContain(a, _decoded);
        Assert.Same(model, tab.Model);
        Assert.NotNull(tab.ImageIterator);
        Assert.Equal([a, b], tab.ImageIterator.Files.Select(f => f.FullName));
        Assert.Equal(0, tab.ImageIterator.CurrentIndex);
    }

    [Fact]
    public async Task LoadAsync_OvertakenByANewerLoad_ReturnsSuperseded()
    {
        var a = await WriteImageAsync("a.jpg");
        var b = await WriteImageAsync("b.jpg");
        var tab = CreateTab(_testDirectory);
        _heldPath = a;

        var first = tab.Load.LoadAsync(a).AsTask();
        await _heldRequested.Task.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);
        var second = await tab.Load.LoadAsync(b);
        _held.SetResult(new ImageModel { FileInfo = new FileInfo(a) });
        var overtaken = await first.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

        Assert.Equal(LoadOutcome.Loaded, second.Outcome);
        Assert.Equal(LoadOutcome.Superseded, overtaken.Outcome);
        Assert.False(overtaken.IsLoaded);
        Assert.Null(overtaken.Error);
        Assert.Equal(b, tab.Model.FileInfo?.FullName);
    }

    [Fact]
    public async Task LoadAsync_IntoADisposedTab_ReturnsSupersededAndThrowsNothing()
    {
        var a = await WriteImageAsync("a.jpg");
        var tab = CreateTab(_testDirectory);
        tab.Dispose();

        var result = await tab.Load.LoadAsync(a);

        Assert.Equal(LoadOutcome.Superseded, result.Outcome);
        Assert.Null(result.Error);
        Assert.False(tab.Load.IsLoading.Value);
        Assert.NotEqual(a, tab.Model.FileInfo?.FullName);
    }

    [Fact]
    public async Task LoadAsync_SideBySideOnTheLastFile_ShowsItAloneAndLoads()
    {
        Settings.ImageScaling.ShowImageSideBySide = true;
        try
        {
            var a = await WriteImageAsync("a.jpg");
            var b = await WriteImageAsync("b.jpg");
            var tab = CreateTab(_testDirectory);

            var result = await tab.Load.LoadAsync(b);

            Assert.Equal(LoadOutcome.Loaded, result.Outcome);
            Assert.Equal(b, tab.Model.FileInfo?.FullName);
            Assert.Null(tab.SecondaryModel);
            Assert.Equal([a, b], tab.ImageIterator!.Files.Select(f => f.FullName));
            Assert.Equal(1, tab.ImageIterator.CurrentIndex);
            Assert.False(tab.CanNavigateForwards.Value);
        }
        finally
        {
            Settings.ImageScaling.ShowImageSideBySide = false;
        }
    }

    [Fact]
    public async Task LoadAsync_RootDirectoryWithoutTheFile_ListsTheFilesOwnFolder()
    {
        var a = await WriteImageAsync("a.jpg");
        var b = await WriteImageAsync("b.jpg");
        var other = Directory.CreateDirectory(Path.Combine(_testDirectory, "other")).FullName;
        await File.WriteAllBytesAsync(Path.Combine(other, "x.jpg"), [0xFF, 0xD8, 0xFF], TestContext.Current.CancellationToken);
        var tab = CreateTab(_testDirectory);

        var result = await tab.Load.LoadAsync(b, new LoadOptions(RootDirectory: other));

        Assert.Equal(LoadOutcome.Loaded, result.Outcome);
        Assert.Equal([a, b], tab.ImageIterator!.Files.Select(f => f.FullName));
        Assert.Equal(1, tab.ImageIterator.CurrentIndex);
        Assert.Equal(b, tab.Model.FileInfo?.FullName);
    }

    [Fact]
    public async Task LoadAsync_GivingUpAfterANewerLoadBegan_LeavesTheNewerLoadingPhase()
    {
        var a = await WriteImageAsync("a.jpg");
        var b = await WriteImageAsync("b.jpg");
        var tab = CreateTab(_testDirectory);
        _heldPath = a;
        var heldB = Hold(b);

        var older = tab.Load.LoadAsync(a).AsTask();
        await _heldRequested.Task.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);
        var newer = tab.Load.LoadAsync(b).AsTask();
        await heldB.Requested.Task.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);
        // The older load gets its image after the newer one began, gives up, and ends its own Loading
        _held.SetResult(new ImageModel { FileInfo = new FileInfo(a) });
        var olderResult = await older.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

        Assert.Equal(LoadOutcome.Superseded, olderResult.Outcome);
        Assert.Equal(TitlePhase.Loading, tab.Titles.Phase);

        heldB.Model.SetResult(new ImageModel { FileInfo = new FileInfo(b) });
        var newerResult = await newer.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

        Assert.Equal(LoadOutcome.Loaded, newerResult.Outcome);
        Assert.Equal(TitlePhase.None, tab.Titles.Phase);
    }

    [Fact]
    public async Task IsLoading_IsTrueFromStartToEnd_ForLoadedSupersededAndFailed()
    {
        var a = await WriteImageAsync("a.jpg");
        var b = await WriteImageAsync("b.jpg");
        var c = await WriteImageAsync("c.jpg");
        // Held before any load, so a tab preloading a neighbour cannot cache it unheld for the next one
        var heldA = Hold(a);
        var heldB = Hold(b);

        // Loaded: true while the image loads, false once it shows
        var loadedTab = CreateTab(_testDirectory);
        var loading = loadedTab.Load.LoadAsync(a).AsTask();
        await heldA.Requested.Task.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);
        Assert.True(loadedTab.Load.IsLoading.Value);
        heldA.Model.SetResult(new ImageModel { FileInfo = new FileInfo(a) });
        Assert.Equal(LoadOutcome.Loaded, (await loading.WaitAsync(WaitLimit, TestContext.Current.CancellationToken)).Outcome);
        Assert.False(loadedTab.Load.IsLoading.Value);

        // Superseded: a newer load ending leaves the older one loading, which ends it once it gives up
        var supersededTab = CreateTab(_testDirectory);
        var overtaken = supersededTab.Load.LoadAsync(b).AsTask();
        await heldB.Requested.Task.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);
        Assert.Equal(LoadOutcome.Loaded, (await supersededTab.Load.LoadAsync(c)).Outcome);
        Assert.True(supersededTab.Load.IsLoading.Value);
        heldB.Model.SetResult(new ImageModel { FileInfo = new FileInfo(b) });
        Assert.Equal(LoadOutcome.Superseded,
            (await overtaken.WaitAsync(WaitLimit, TestContext.Current.CancellationToken)).Outcome);
        Assert.False(supersededTab.Load.IsLoading.Value);

        // Failed: it said it was loading, and stopped saying so
        var failedTab = CreateTab(_testDirectory);
        var seen = new List<bool>();
        using var watching = failedTab.Load.IsLoading.AsObservable().Subscribe(seen.Add);
        var empty = Directory.CreateDirectory(Path.Combine(_testDirectory, "empty")).FullName;
        Assert.Equal(LoadOutcome.Failed, (await failedTab.Load.LoadAsync(empty)).Outcome);
        Assert.Equal([false, true, false], seen);
    }

    [Fact]
    public void Load_OnATabWithoutServices_Throws()
    {
        var tab = new TabViewModel(new MainWindowViewModel(null!, null!, null!, null!, null!));

        var thrown = Assert.Throws<InvalidOperationException>(() => tab.Load);

        Assert.Equal("The tab has no services", thrown.Message);
    }

    private static async Task WaitUntil(Func<bool> condition, Func<string>? describe = null)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!condition())
        {
            Assert.True(DateTime.UtcNow < deadline, "Timed out waiting for the gallery: " + describe?.Invoke());
            await Task.Delay(20, TestContext.Current.CancellationToken);
        }
    }

    // A tab with the fakes as its services, so its Load works. Without an iterator it is a tab that never opened a source
    private TabViewModel CreateTab(string directory, MainWindowViewModel? parentVm = null, SharedImageCache? cache = null,
        IArchiveEntryWriter? writer = null, IPlatformSpecificService? platform = null, IFileWatcherService? watcher = null,
        bool withIterator = true)
    {
        var dummyParent = parentVm ?? new MainWindowViewModel(null!, null!, null!, null!, null!);
        var thumbCache = new MockThumbnailCache();
        var services = new TabServices(cache ?? _cache, thumbCache, _mockThumbnailLoader,
            watcher ?? _mockFileWatcherService, _mockImageModelLoader, platform ?? new MockPlatformSpecificService());
        var tab = new TabViewModel(dummyParent, services, writer);
        if (withIterator)
        {
            // Initialize with mocks to avoid null refs
            tab.Initialize(services.Cache, thumbCache, _mockThumbnailLoader, null, thumbCache);
            tab.ImageIterator.Initialize([]);
        }
        return tab;
    }

    // Holds the load of the file at path until the test sets its model
    private HeldLoad Hold(string path) => _holds.GetOrAdd(path, static _ => new HeldLoad());

    private sealed class HeldLoad
    {
        public TaskCompletionSource<ImageModel> Model { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Requested { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    // A zip of images at the paths given, with their folders, as a comic archive with a folder per volume is built
    private async Task<string> CreateZipWithEntriesAsync(string name, params string[] entries)
    {
        var source = Directory.CreateDirectory(
            Path.Combine(_testDirectory, Path.GetFileNameWithoutExtension(name) + "_source")).FullName;
        foreach (var entry in entries)
        {
            var path = Path.Combine(source, entry);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllBytesAsync(path, [0xFF, 0xD8, 0xFF], TestContext.Current.CancellationToken);
        }
        var zip = Path.Combine(_testDirectory, name);
        System.IO.Compression.ZipFile.CreateFromDirectory(source, zip);
        return zip;
    }

    private async Task<string> WriteImageAsync(string name)
    {
        var path = Path.Combine(_testDirectory, name);
        await File.WriteAllBytesAsync(path, [0xFF, 0xD8, 0xFF], TestContext.Current.CancellationToken);
        return path;
    }

    // A tab, and a file in a folder it is not browsing. With tabHasFiles the tab browses a file in its own folder,
    // else it has no files
    private async Task<(TabViewModel Tab, FileInfo Elsewhere)> CreateTabAndFileElsewhereAsync(bool tabHasFiles)
    {
        var tab = CreateTab(_testDirectory);
        var browsed = new FileInfo(Path.Combine(_testDirectory, "browsed.jpg"));
        var elsewhere = new FileInfo(Path.Combine(Directory.CreateDirectory(Path.Combine(_testDirectory, "other")).FullName,
            "elsewhere.jpg"));
        await File.WriteAllTextAsync(browsed.FullName, "x", TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(elsewhere.FullName, "x", TestContext.Current.CancellationToken);
        if (tabHasFiles)
        {
            tab.Model = new ImageModel { FileInfo = browsed };
            tab.ImageIterator.Initialize([browsed]);
        }

        return (tab, elsewhere);
    }

    // Starts the load that a site is: a file, a directory, or a file listed from a root directory
    private static Task StartLoad(LoadSite site, TabViewModel tab, FileInfo file) => site switch
    {
        LoadSite.File => tab.Load.LoadAsync(file.FullName).AsTask(),
        LoadSite.Directory => tab.Load.LoadAsync(file.DirectoryName!).AsTask(),
        _ => tab.Load.LoadAsync(file.FullName, new LoadOptions(RootDirectory: file.DirectoryName)).AsTask()
    };

    // Does what opening an archive does first: the tab's extraction service makes the archive's temp folder
    private async Task<string> PrepareExtractionAsync(TabViewModel tab, string name)
    {
        var zip = await CreateZipAsync(name, 1);
        await tab.Archives.OpenAsync(zip, null, (_, _) => Task.FromResult(false), string.CompareOrdinal,
            TestContext.Current.CancellationToken);
        return tab.Archives.OpenFolder!;
    }

    private async Task<string> CreateZipAsync(string name, int images)
    {
        var source = Directory.CreateDirectory(
            Path.Combine(_testDirectory, Path.GetFileNameWithoutExtension(name) + "_source")).FullName;
        for (var i = 1; i <= images; i++)
        {
            await File.WriteAllBytesAsync(Path.Combine(source, $"img_{i:D2}.jpg"), [0xFF, 0xD8, 0xFF],
                TestContext.Current.CancellationToken);
        }
        var zip = Path.Combine(_testDirectory, name);
        System.IO.Compression.ZipFile.CreateFromDirectory(source, zip);
        return zip;
    }

    // Reports only the file events the test raises, so the test decides when the watcher hears of each page
    private class MockFileWatcherService : IFileWatcherService
    {
        private int _unwatchCount;

        public bool WatchCalled { get; private set; }
        public bool UnwatchCalled => UnwatchCount > 0;
        public int UnwatchCount => Volatile.Read(ref _unwatchCount);
        public TabViewModel? WatchedTab { get; private set; }
        public string? WatchedDirectory { get; private set; }

        // Hears each folder a tab starts watching
        public Action<string?>? OnWatch { get; set; }

        public void Watch(TabViewModel tab, string? directory = null)
        {
            WatchCalled = true;
            WatchedTab = tab;
            WatchedDirectory = directory;
            OnWatch?.Invoke(directory);
        }

        public void Unwatch(TabViewModel tab)
        {
            Interlocked.Increment(ref _unwatchCount);
        }

        public void RestartWatches()
        {
        }
    }

    // Counts what it is asked to decode: the URL path decodes its download with it
    private class MockImageModelLoader : IImageModelLoader
    {
        private int _calls;

        public int Calls => Volatile.Read(ref _calls);

        public ValueTask<ImageModel> GetImageModelAsync(FileInfo file, CancellationToken ct)
        {
            Interlocked.Increment(ref _calls);
            return ValueTask.FromResult(new ImageModel { FileInfo = file });
        }

        public ValueTask<ImageModel?> GetBase64ImageAsync(FileInfo file, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public ValueTask<ImageModel?> GetBase64ImageAsync(string base64String, CancellationToken ct)
        {
            throw new NotImplementedException();
        }
    }

    private class MockThumbnailLoader : IThumbnailLoader
    {
        public int GetThumbnailAsyncCalledCount { get; private set; }

        public ValueTask<object?> GetThumbnailAsync(FileInfo file, uint size, CoreViewModel? core = null, MagickImage? magick = null)
        {
            GetThumbnailAsyncCalledCount++;
            return ValueTask.FromResult<object?>(null);
        }

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

    private class MockPlatformSpecificService : IPlatformSpecificService
    {
        // Opens a .7z as local software does: with the archive and the folder to extract it to, and whether it did
        public Func<string, string, Task<bool>>? LocalSoftware { get; init; }

        public void SetTaskbarProgress(ulong progress, ulong maximum) { }
        public void StopTaskbarProgress() { }
        public void SetCursorPos(int x, int y) { }
        public void DisableScreensaver() { }
        public void EnableScreensaver() { }
        public List<FileInfo> GetFiles(FileInfo fileInfo) => new();
        public int CompareStrings(string str1, string str2) => string.CompareOrdinal(str1, str2);
        public void OpenWith(string path) { }
        public void LocateOnDisk(string path) { }
        public void ShowFileProperties(string path) { }
        public Task SetAsWallpaper(string path, int wallpaperStyle) => Task.CompletedTask;
        public bool SetAsLockScreen(string path) => false;
        public bool CopyFile(string path) => false;
        public Task<bool> CutFile(string path) => throw new NotImplementedException();
        public Task CopyImageToClipboard(object bitmap) => Task.CompletedTask;
        public Task<object?> GetImageFromClipboard() => Task.FromResult<object?>(null);
        public Task<bool> ExtractWithLocalSoftwareAsync(string path, string tempDirectory) =>
            LocalSoftware?.Invoke(path, tempDirectory) ?? Task.FromResult(false);
        public string DefaultJsonKeyMap() => "{}";
        public void InitiateFileAssociationService() { }
        public Task<bool> DeleteFile(string path, bool recycle) => Task.FromResult(false);
        public bool CanMoveToRecycleBin(string path) => true;
        public byte[]? GetShellThumbnail(string path, int width, int height, out int pixelWidth, out int pixelHeight)
        {
            pixelWidth = 0;
            pixelHeight = 0;
            return null;
        }
        public byte[]? GetPixelsFromNativeImagingComponent(string path, out int pixelWidth, out int pixelHeight)
        {
            pixelWidth = 0;
            pixelHeight = 0;
            return null;
        }
    }

    private class MockFrameProvider : FrameProvider
    {
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) => callback.MoveNext(0);
    }
}
