using System.Collections.Concurrent;
using System.Text;
using ImageMagick;
using PicView.Core.FileHandling.Interfaces;
using PicView.Core.FileHistory;
using PicView.Core.FileSorting;
using PicView.Core.Localization;
using PicView.Core.IPlatform;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.ViewModels;
using R3;
using PicView.Tests.FileHistory;

namespace PicView.Tests.Navigation;

[Collection("Sequential")]
public class NavigationServiceTests : IDisposable
{
    private readonly FileHistoryFileBackup _fileHistoryBackup = new();
    private readonly MockImageModelLoader _mockImageModelLoader;
    private readonly SharedImageCache _cache;
    private readonly MockFileWatcherService _mockFileWatcherService;
    private readonly MockThumbnailLoader _mockThumbnailLoader;
    private readonly NavigationService _navigationService;
    private readonly string _testDirectory;

    // Bounds each wait on a held load, so a failing test fails instead of hanging the run
    private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(10);

    // How long a call gets to end before the file shows, which is the bug the held-load tests catch
    private static readonly TimeSpan EarlyEndWait = TimeSpan.FromMilliseconds(500);

    // A test holds the load of the file at _heldPath: the cache's loader waits for _held, and says when it is asked
    private string? _heldPath;
    private readonly TaskCompletionSource<ImageModel> _held = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _heldRequested = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public NavigationServiceTests()
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
            if (file.FullName != _heldPath)
            {
                return ValueTask.FromResult(new ImageModel { FileInfo = file });
            }

            _heldRequested.TrySetResult();
            return new ValueTask<ImageModel>(_held.Task);
        });
        _mockFileWatcherService = new MockFileWatcherService();
        _mockThumbnailLoader = new MockThumbnailLoader();

        _navigationService = new NavigationService(
            _mockImageModelLoader,
            _cache,
            _mockFileWatcherService,
            new MockPlatformSpecificService(),
            string.CompareOrdinal);
    }

    public void Dispose()
    {
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
    public async Task RepopulateIterator_UpdatesFileWatcher()
    {
        // Arrange
        var tab = CreateTab(_testDirectory);
        var fileInfo = new FileInfo(Path.Combine(_testDirectory, "test.jpg"));
        var cts = new CancellationTokenSource();

        // Act
        await _navigationService.RepopulateIterator(fileInfo, tab, cts);

        // Assert
        Assert.True(_mockFileWatcherService.UnwatchCalled, "Unwatch should be called");
        Assert.True(_mockFileWatcherService.WatchCalled, "Watch should be called");
        Assert.Equal(_testDirectory, _mockFileWatcherService.WatchedDirectory);
        Assert.Equal(tab, _mockFileWatcherService.WatchedTab);
    }


    [Theory]
    [InlineData("")]
    [InlineData("not a path, url or base64 ¤")]
    public async Task LoadFromStringAsync_UnloadableSource_Fails(string source)
    {
        var tab = CreateTab(_testDirectory);

        var result = await _navigationService.LoadFromStringAsync(source, tab, new CancellationTokenSource());

        Assert.False(result.IsLoaded);
    }

    [Fact]
    public async Task LoadFromStringAsync_DirectoryWithoutImages_Fails()
    {
        var tab = CreateTab(_testDirectory);
        var empty = Directory.CreateDirectory(Path.Combine(_testDirectory, "empty")).FullName;

        var result = await _navigationService.LoadFromStringAsync(empty, tab, new CancellationTokenSource());

        Assert.False(result.IsLoaded);
    }

    [Fact]
    public async Task LoadFromStringAsync_UnreachableUrl_FailsWithMessage()
    {
        var tab = CreateTab(_testDirectory);

        // Port 9 (discard) refuses connections on a machine without that service
        var result = await _navigationService.LoadFromStringAsync("http://127.0.0.1:9/image.jpg", tab, new CancellationTokenSource());

        Assert.False(result.IsLoaded);
        Assert.False(string.IsNullOrWhiteSpace(result.Error));
    }

    [Fact]
    public async Task LoadFromStringAsync_Url_DropsImageIterator()
    {
        var tab = CreateTab(_testDirectory);

        await _navigationService.LoadFromStringAsync("http://127.0.0.1:9/image.jpg", tab, new CancellationTokenSource());

        // Opening a file afterwards recreates it and docks the gallery again, as after a clipboard image
        Assert.Null(tab.ImageIterator);
    }

    [Fact]
    public async Task LoadFromStringAsync_FileWithRootDirectory_ListsFilesFromRoot()
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

            var result = await _navigationService.LoadFromStringAsync(inSub, tab, new CancellationTokenSource(), _testDirectory);

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
    public async Task LoadFromFileAsync_NewFileInShownFolder_WaitsForWatcherInsteadOfRepopulating()
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
        var load = _navigationService.LoadFromFileAsync(copy, tab, new CancellationTokenSource()).AsTask();
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
    public async Task LoadFromFileAsync_FileInAFolderTheTabIsNotBrowsing_FinishesOnlyOnceTheTabShowsIt(bool tabHasFiles)
    {
        var (tab, elsewhere) = await CreateTabAndFileElsewhereAsync(tabHasFiles);
        _heldPath = elsewhere.FullName;

        var load = _navigationService.LoadFromFileAsync(elsewhere, tab, new CancellationTokenSource()).AsTask();
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
    public async Task LoadFromFileOrDirectory_WhileANewerArchiveOpens_KeepsTheNewerExtraction(LoadSite site,
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
            Assert.Equal(newer, tab.ArchiveExtractionService.TempZipDirectory);
            if (left is not null)
            {
                Assert.False(Directory.Exists(left), "The load left the extraction of the archive it left behind");
            }
        }
        finally
        {
            tab.ArchiveExtractionService.Cleanup();
            tab.ArchiveExtractionService.Cleanup(left);
        }
    }

    [Theory]
    [InlineData(LoadSite.File)]
    [InlineData(LoadSite.Directory)]
    [InlineData(LoadSite.FileWithRootDirectory)]
    public async Task LoadFromFileOrDirectory_LeavingAnArchive_CleansUpItsExtraction(LoadSite site)
    {
        var (tab, elsewhere) = await CreateTabAndFileElsewhereAsync(tabHasFiles: true);
        var left = await PrepareExtractionAsync(tab, "left.zip");
        try
        {
            await StartLoad(site, tab, elsewhere).WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

            Assert.False(Directory.Exists(left), "The load left the archive's extraction behind");
            Assert.False(tab.ArchiveExtractionService.IsArchived);
        }
        finally
        {
            tab.ArchiveExtractionService.Cleanup(left);
        }
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

    private TabViewModel CreateTab(string directory, MainWindowViewModel? parentVm = null, SharedImageCache? cache = null)
    {
        var dummyParent = parentVm ?? new MainWindowViewModel(null!, null!, null!, null!, null!);
        var tab = new TabViewModel(dummyParent);
        // Initialize with mocks to avoid null refs
        var thumbCache = new MockThumbnailCache();
        tab.Initialize(cache ?? _cache, thumbCache, _mockThumbnailLoader, null, thumbCache);
        tab.ImageIterator.Initialize([]);
        return tab;
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

    // Gives an entry a compression method no reader knows, as a corrupt entry is: the archive still lists it, but
    // extracting it fails
    private static void CorruptEntry(string zip, string entry)
    {
        var bytes = File.ReadAllBytes(zip);
        var name = Encoding.UTF8.GetBytes(entry);
        for (var i = 0; i + name.Length <= bytes.Length; i++)
        {
            if (!bytes.AsSpan(i, name.Length).SequenceEqual(name))
            {
                continue;
            }

            // A local file header has the name 30 bytes in and the method 8 bytes in; a central directory header, 46 and 10
            if (i >= 30 && BitConverter.ToUInt32(bytes, i - 30) == 0x04034b50)
            {
                BitConverter.TryWriteBytes(bytes.AsSpan(i - 30 + 8), (ushort)77);
            }
            else if (i >= 46 && BitConverter.ToUInt32(bytes, i - 46) == 0x02014b50)
            {
                BitConverter.TryWriteBytes(bytes.AsSpan(i - 46 + 10), (ushort)77);
            }
        }
        File.WriteAllBytes(zip, bytes);
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
    private Task StartLoad(LoadSite site, TabViewModel tab, FileInfo file)
    {
        var ct = new CancellationTokenSource();
        return site switch
        {
            LoadSite.File => _navigationService.LoadFromFileAsync(file, tab, ct).AsTask(),
            LoadSite.Directory => _navigationService.LoadFromDirectoryAsync(new FileInfo(file.DirectoryName!), tab, ct),
            _ => _navigationService.LoadFromStringAsync(file.FullName, tab, ct, file.DirectoryName).AsTask()
        };
    }

    // Does what opening an archive does first: the tab's extraction service makes the archive's temp folder
    private async Task<string> PrepareExtractionAsync(TabViewModel tab, string name)
    {
        var zip = await CreateZipAsync(name, 1);
        await tab.ArchiveExtractionService.PrepareArchiveAsync(zip, (_, _) => Task.FromResult(false), string.CompareOrdinal);
        return tab.ArchiveExtractionService.TempZipDirectory!;
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

    [Fact]
    public async Task LoadFromArchiveAsync_AlwaysUncompressEntireArchiveTrue_ExtractsInitial10SeedFiles()
    {
        Settings.Navigation.AlwaysUncompressEntireArchive = true;
        var tempZipPath = Path.Combine(_testDirectory, "test_archive_true.zip");
        var archiveSourceDir = Path.Combine(_testDirectory, "archive_source_true");
        Directory.CreateDirectory(archiveSourceDir);

        for (var i = 1; i <= 15; i++)
        {
            await File.WriteAllBytesAsync(Path.Combine(archiveSourceDir, $"img_{i:D2}.jpg"), [0xFF, 0xD8, 0xFF], TestContext.Current.CancellationToken);
        }

        System.IO.Compression.ZipFile.CreateFromDirectory(archiveSourceDir, tempZipPath);

        var tab = CreateTab(_testDirectory);
        var cts = new CancellationTokenSource();

        try
        {
            var success = await _navigationService.LoadFromArchiveAsync(tempZipPath, tab, cts);

            Assert.True(success);
            Assert.True(tab.ArchiveExtractionService.IsArchived);
            Assert.NotNull(tab.ImageIterator.Files);
            // Initially populated with 10 seed files
            Assert.True(tab.ImageIterator.Files.Count >= 10);
            Assert.NotNull(tab.Model);
            Assert.NotNull(tab.Model.FileInfo);
            Assert.True(File.Exists(tab.Model.FileInfo.FullName));
        }
        finally
        {
            tab.ArchiveExtractionService.Cleanup();
            Settings.Navigation.AlwaysUncompressEntireArchive = false;
        }
    }

    [Fact]
    public async Task LoadFromArchiveAsync_AlwaysUncompressEntireArchiveFalse_ExtractsSingleSeedFile()
    {
        Settings.Navigation.AlwaysUncompressEntireArchive = false;
        var tempZipPath = Path.Combine(_testDirectory, "test_archive_false.zip");
        var archiveSourceDir = Path.Combine(_testDirectory, "archive_source_false");
        Directory.CreateDirectory(archiveSourceDir);

        for (var i = 1; i <= 15; i++)
        {
            await File.WriteAllBytesAsync(Path.Combine(archiveSourceDir, $"img_{i:D2}.jpg"), [0xFF, 0xD8, 0xFF], TestContext.Current.CancellationToken);
        }

        System.IO.Compression.ZipFile.CreateFromDirectory(archiveSourceDir, tempZipPath);

        var tab = CreateTab(_testDirectory);
        var cts = new CancellationTokenSource();

        try
        {
            var success = await _navigationService.LoadFromArchiveAsync(tempZipPath, tab, cts);

            Assert.True(success);
            Assert.True(tab.ArchiveExtractionService.IsArchived);
            Assert.NotNull(tab.ImageIterator.Files);
            // Initially seeded with 1 file (or 2 if side-by-side)
            Assert.True(tab.ImageIterator.Files.Count >= 1);
            Assert.NotNull(tab.Model);
            Assert.NotNull(tab.Model.FileInfo);
            Assert.True(File.Exists(tab.Model.FileInfo.FullName));
            // Enables forward navigation while extracting in background
            Assert.True(tab.CanNavigateForwards.Value);
            // Should not show the batched progress text indicator
            Assert.Null(tab.ArchiveExtractionProgressText.Value);
        }
        finally
        {
            tab.ArchiveExtractionService.Cleanup();
        }
    }

    [Fact]
    public async Task LoadFromArchiveAsync_ImagesInSeveralFolders_OpensFirstFolderAndKeepsFolderOrder()
    {
        Settings.Navigation.AlwaysUncompressEntireArchive = true;
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
        var cts = new CancellationTokenSource();

        try
        {
            var success = await _navigationService.LoadFromArchiveAsync(tempZipPath, tab, cts);

            Assert.True(success);
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
            tab.ArchiveExtractionService.Cleanup();
            Settings.Navigation.AlwaysUncompressEntireArchive = false;
        }
    }

    [Fact]
    public async Task LoadFromArchiveAsync_RecordsTheArchiveAndNoneOfItsTempFiles()
    {
        Settings.Navigation.IsFileHistoryEnabled = true;
        FileHistoryManager.Initialize(Path.Combine(_testDirectory, "history.json"));
        var zip = await CreateZipAsync("recorded.zip", 3);
        var tab = CreateTab(_testDirectory);

        try
        {
            await _navigationService.LoadFromArchiveAsync(zip, tab, new CancellationTokenSource());

            Assert.Equal([zip], FileHistoryManager.AllEntries.Select(e => e.Path));
        }
        finally
        {
            tab.ArchiveExtractionService.Cleanup();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LoadFromArchiveAsync_StartEntry_ShowsThatPageFirst(bool alwaysUncompressEntireArchive)
    {
        Settings.Navigation.AlwaysUncompressEntireArchive = alwaysUncompressEntireArchive;
        var zip = await CreateZipAsync($"start_{alwaysUncompressEntireArchive}.zip", 5);
        var tab = CreateTab(_testDirectory);

        try
        {
            var loaded = await _navigationService.LoadFromArchiveAsync(zip, tab, new CancellationTokenSource(),
                startEntry: "img_03.jpg");

            Assert.True(loaded);
            Assert.Equal("img_03.jpg", tab.Model.FileInfo!.Name);
            var entry = Assert.Single(tab.ViewHistory.Entries);
            Assert.Equal(ViewHistoryKind.ArchivePage, entry.Kind);
            Assert.Equal("img_03.jpg", entry.ArchiveEntry);
        }
        finally
        {
            tab.ArchiveExtractionService.Cleanup();
            Settings.Navigation.AlwaysUncompressEntireArchive = false;
        }
    }

    [Theory]
    // One by one, the pages either side of the start take turns
    [InlineData(false, 5, new[] { "img_04.jpg", "img_02.jpg", "img_05.jpg", "img_01.jpg" })]
    // In batches, the ten from the start come first, then those either side of them take turns
    [InlineData(true, 5, new[] { "img_04.jpg", "img_05.jpg", "img_02.jpg", "img_01.jpg" })]
    [InlineData(true, 15, new[]
    {
        "img_04.jpg", "img_05.jpg", "img_06.jpg", "img_07.jpg", "img_08.jpg", "img_09.jpg", "img_10.jpg", "img_11.jpg",
        "img_12.jpg", "img_13.jpg", "img_02.jpg", "img_14.jpg", "img_01.jpg", "img_15.jpg"
    })]
    public async Task LoadFromArchiveAsync_StartEntry_ExtractsTheNearestPagesFirst(bool alwaysUncompressEntireArchive,
        int pages, string[] expected)
    {
        Settings.Navigation.AlwaysUncompressEntireArchive = alwaysUncompressEntireArchive;
        var zip = await CreateZipAsync($"order_{alwaysUncompressEntireArchive}_{pages}.zip", pages);
        var tab = CreateTab(_testDirectory);
        var extracted = new ConcurrentQueue<string>();
        FileSystemWatcher? folderWatcher = null;
        // The tab watches the extraction folder before the rest are extracted, so this sees them in the order they come
        _mockFileWatcherService.OnWatch = directory =>
        {
            if (folderWatcher is not null)
            {
                return;
            }

            folderWatcher = new FileSystemWatcher(directory!);
            folderWatcher.Created += (_, e) => extracted.Enqueue(e.Name!);
            folderWatcher.EnableRaisingEvents = true;
        };

        try
        {
            await _navigationService.LoadFromArchiveAsync(zip, tab, new CancellationTokenSource(),
                startEntry: "img_03.jpg");
            // The pages extracted together with the start entry, after it
            var withTheStart = tab.ImageIterator.Files.Select(f => f.Name).Where(name => name != "img_03.jpg").ToArray();
            await WaitUntil(() => !tab.IsArchiveExtracting.Value && withTheStart.Length + extracted.Count >= pages - 1,
                () => string.Join(", ", extracted));

            Assert.Equal(expected, withTheStart.Concat(extracted));
        }
        finally
        {
            folderWatcher?.Dispose();
            tab.ArchiveExtractionService.Cleanup();
            Settings.Navigation.AlwaysUncompressEntireArchive = false;
        }
    }

    [Fact]
    public async Task LoadFromArchiveAsync_TwoQuickBacksIntoAnArchiveTheTabLeft_KeepTheEntriesAfterThem()
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
        var navigation = new NavigationService(_mockImageModelLoader, cache, _mockFileWatcherService, platform,
            string.CompareOrdinal);
        var tab = CreateTab(_testDirectory, cache: cache);
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
            var first = navigation.LoadFromArchiveAsync(archive, tab, new CancellationTokenSource(),
                toSecondPage.Target.ArchiveEntry).AsTask();
            var firstPageFile = await firstPageAsked.Task.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);
            var toFirstPage = tab.ViewHistory.BeginStep(forwards: false)!.Value;
            var second = navigation.LoadFromArchiveAsync(archive, tab, new CancellationTokenSource(),
                toFirstPage.Target.ArchiveEntry).AsTask();
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
            Assert.Equal(folders.Last(), tab.ArchiveExtractionService.TempZipDirectory);
        }
        finally
        {
            secondOpens.TrySetResult();
            firstPage.TrySetResult(new ImageModel());
            tab.ArchiveExtractionService.Cleanup();
            foreach (var folder in folders)
            {
                tab.ArchiveExtractionService.Cleanup(folder);
            }
        }
    }

    [Fact]
    public async Task LoadFromArchiveAsync_SideBySidePartnerThatFailsToExtract_ShowsTheStartPageAlone()
    {
        // Staged, so the start page and its partner are extracted on their own
        Settings.Navigation.AlwaysUncompressEntireArchive = false;
        Settings.ImageScaling.ShowImageSideBySide = true;
        var zip = await CreateZipAsync("corrupt_partner.zip", 3);
        CorruptEntry(zip, "img_02.jpg");
        var tab = CreateTab(_testDirectory);

        try
        {
            var loaded = await _navigationService.LoadFromArchiveAsync(zip, tab, new CancellationTokenSource());

            Assert.True(loaded);
            Assert.Equal("img_01.jpg", tab.Model.FileInfo?.Name);
            Assert.Null(tab.SecondaryModel);
        }
        finally
        {
            tab.ArchiveExtractionService.Cleanup();
            Settings.ImageScaling.ShowImageSideBySide = false;
            Settings.Navigation.AlwaysUncompressEntireArchive = true;
        }
    }

    [Fact]
    public void EndExtraction_OfAnExtractionThatANewerOneReplaced_LeavesTheTabExtracting()
    {
        var tab = CreateTab(_testDirectory);
        // A Back opened the archive again, so a newer extraction replaced the first, and Next waits for its pages
        var replaced = tab.ArchiveExtractionService.ResetExtractionCts();
        tab.ArchiveExtractionService.ResetExtractionCts();
        tab.IsArchiveExtracting.Value = true;

        NavigationService.EndExtraction(tab, replaced);

        Assert.True(tab.IsArchiveExtracting.Value);
    }

    // How an extraction can end, besides running to its end
    public enum ExtractionStop
    {
        None,
        AnotherArchiveOpens,
        TabLeavesTheArchive
    }

    [Theory]
    [InlineData(ExtractionStop.None)]
    [InlineData(ExtractionStop.AnotherArchiveOpens)]
    [InlineData(ExtractionStop.TabLeavesTheArchive)]
    public void EndExtraction_OfTheExtractionStartedLast_EndsTheTabsExtracting(ExtractionStop stop)
    {
        var tab = CreateTab(_testDirectory);
        var latest = tab.ArchiveExtractionService.ResetExtractionCts();
        tab.IsArchiveExtracting.Value = true;
        tab.ArchiveExtractionProgressText.Value = "1/2";
        switch (stop)
        {
            case ExtractionStop.AnotherArchiveOpens:
                tab.ArchiveExtractionService.StopExtraction();
                break;
            case ExtractionStop.TabLeavesTheArchive:
                tab.ArchiveExtractionService.Cleanup();
                break;
        }

        NavigationService.EndExtraction(tab, latest);

        Assert.False(tab.IsArchiveExtracting.Value);
        Assert.Null(tab.ArchiveExtractionProgressText.Value);
    }

    [Theory]
    // One by one, the pages either side of the start on screen take turns
    [InlineData(false, new[] { "Vol 1 - 001.jpg", "Vol 1 (color) - 001.jpg", "Vol 1 - 002.jpg" })]
    // In batches, the ten from the start on screen come first
    [InlineData(true, new[] { "Vol 1 - 001.jpg", "Vol 1 - 002.jpg", "Vol 1 (color) - 001.jpg" })]
    public async Task LoadFromArchiveAsync_StartEntryInAnArchiveWithFolders_ExtractsThePagesNearestOnScreenFirst(
        bool alwaysUncompressEntireArchive, string[] expected)
    {
        // The tab lists the extracted files by name, and "(" sorts before "-", so the color volume's files come first,
        // although its folder's name comes after the other one's
        Settings.Navigation.AlwaysUncompressEntireArchive = alwaysUncompressEntireArchive;
        var zip = await CreateZipWithEntriesAsync($"folders_{alwaysUncompressEntireArchive}.zip",
            "Vol 1/001.jpg", "Vol 1/002.jpg", "Vol 1 (color)/001.jpg", "Vol 1 (color)/002.jpg");
        var tab = CreateTab(_testDirectory);
        var extracted = new ConcurrentQueue<string>();
        FileSystemWatcher? folderWatcher = null;
        // The tab watches the extraction folder before the rest are extracted, so this sees them in the order they come
        _mockFileWatcherService.OnWatch = directory =>
        {
            if (folderWatcher is not null)
            {
                return;
            }

            folderWatcher = new FileSystemWatcher(directory!);
            folderWatcher.Created += (_, e) => extracted.Enqueue(e.Name!);
            folderWatcher.EnableRaisingEvents = true;
        };

        try
        {
            await _navigationService.LoadFromArchiveAsync(zip, tab, new CancellationTokenSource(),
                startEntry: "Vol 1 (color)/002.jpg");
            // The pages extracted together with the start entry, after it
            var withTheStart = tab.ImageIterator.Files.Select(f => f.Name)
                .Where(name => name != "Vol 1 (color) - 002.jpg").ToArray();
            await WaitUntil(() => !tab.IsArchiveExtracting.Value && withTheStart.Length + extracted.Count >= 3,
                () => string.Join(", ", extracted));

            Assert.Equal("Vol 1 (color) - 002.jpg", tab.Model.FileInfo?.Name);
            Assert.Equal(expected, withTheStart.Concat(extracted));
        }
        finally
        {
            folderWatcher?.Dispose();
            tab.ArchiveExtractionService.Cleanup();
            Settings.Navigation.AlwaysUncompressEntireArchive = true;
        }
    }

    private static readonly string[] TwoVolumes =
    [
        "Volume 01/001.jpg", "Volume 01/002.jpg", "Volume 01/003.jpg",
        "Volume 02/001.jpg", "Volume 02/002.jpg", "Volume 02/003.jpg"
    ];

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, true)]
    // With the folders left out of the names, the pages of both volumes share names
    [InlineData(false, false)]
    [InlineData(true, false)]
    public async Task LoadFromArchiveAsync_BackToAPageInAFolderAfterTheTabLeft_OpensTheArchiveAtThatPage(
        bool alwaysUncompressEntireArchive, bool isRespectingArchiveStructure)
    {
        Settings.Navigation.AlwaysUncompressEntireArchive = alwaysUncompressEntireArchive;
        Settings.Navigation.IsRespectingArchiveStructure = isRespectingArchiveStructure;
        var zip = await CreateZipWithEntriesAsync($"back_{alwaysUncompressEntireArchive}_{isRespectingArchiveStructure}.zip",
            TwoVolumes);
        var tab = CreateTab(_testDirectory);
        var page = ViewHistoryEntry.ForArchivePage(zip, "Volume 02/002.jpg");
        var left = ViewHistoryEntry.ForFile(await WriteImageAsync("left.jpg"));
        // The tab showed the page, then left the archive for a file
        tab.ViewHistory.Record(page);
        tab.ViewHistory.Record(left);

        try
        {
            var back = tab.ViewHistory.BeginStep(forwards: false)!.Value;
            await _navigationService.LoadFromArchiveAsync(zip, tab, new CancellationTokenSource(), back.Target.ArchiveEntry);
            tab.ViewHistory.EndStep(back);

            Assert.Equal("Volume 02/002.jpg", tab.ArchiveExtractionService.EntryOf(tab.Model.FileInfo!.FullName));
            Assert.Equal([page, left], tab.ViewHistory.Entries);
            Assert.Equal(0, tab.ViewHistory.Cursor);
        }
        finally
        {
            tab.ArchiveExtractionService.Cleanup();
            Settings.Navigation.AlwaysUncompressEntireArchive = true;
            Settings.Navigation.IsRespectingArchiveStructure = true;
        }
    }

    [Fact]
    public async Task LoadFromArchiveAsync_BackToAPageLocalSoftwareExtractedIntoAFolder_OpensTheArchiveAtThatPage()
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
        var navigation = new NavigationService(_mockImageModelLoader, _cache, _mockFileWatcherService, platform,
            string.CompareOrdinal);
        var tab = CreateTab(_testDirectory);
        var page = ViewHistoryEntry.ForArchivePage(archive, Path.Combine("Volume 02", "002.jpg"));
        var left = ViewHistoryEntry.ForFile(await WriteImageAsync("left.jpg"));
        tab.ViewHistory.Record(page);
        tab.ViewHistory.Record(left);

        try
        {
            var back = tab.ViewHistory.BeginStep(forwards: false)!.Value;
            await navigation.LoadFromArchiveAsync(archive, tab, new CancellationTokenSource(), back.Target.ArchiveEntry);
            tab.ViewHistory.EndStep(back);

            Assert.Equal(Path.Combine(tab.ArchiveExtractionService.TempZipDirectory!, "Volume 02", "002.jpg"),
                tab.Model.FileInfo?.FullName);
            Assert.Equal([page, left], tab.ViewHistory.Entries);
            Assert.Equal(0, tab.ViewHistory.Cursor);
        }
        finally
        {
            tab.ArchiveExtractionService.Cleanup();
        }
    }

    [Fact]
    public async Task LoadFromArchiveAsync_StartEntryAtTheEndOfAFolderThenNext_ShowsTheNextFoldersFirstPage()
    {
        // One by one, with the app's watcher, which adds each page to the tab's files as it is extracted, flat in the
        // extraction folder with its folder in its name
        Settings.Navigation.AlwaysUncompressEntireArchive = false;
        var zip = await CreateZipWithEntriesAsync("next_folder.zip", TwoVolumes);
        var tab = CreateTab(_testDirectory);
        var folderWatcher = new FileWatcherService(string.CompareOrdinal, _cache);
        var navigation = new NavigationService(_mockImageModelLoader, _cache, folderWatcher,
            new MockPlatformSpecificService(), string.CompareOrdinal);

        try
        {
            await navigation.LoadFromArchiveAsync(zip, tab, new CancellationTokenSource(), startEntry: "Volume 01/003.jpg");
            await tab.ImageIterator.NavigateAsync(NavigateTo.Next, SkipAmount.One, new CancellationTokenSource())
                .AsTask().WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

            Assert.Equal("Volume 02 - 001.jpg", tab.Model.FileInfo!.Name);
            Assert.Equal(ViewHistoryEntry.ForArchivePage(zip, "Volume 02/001.jpg"), tab.ViewHistory.Current);
        }
        finally
        {
            folderWatcher.Dispose();
            tab.ArchiveExtractionService.Cleanup();
            Settings.Navigation.AlwaysUncompressEntireArchive = true;
        }
    }

    [Fact]
    public async Task LoadFromArchiveAsync_StartEntryThenNext_ShowsThePageAfterIt()
    {
        var zip = await CreateZipAsync("next.zip", 5);
        var tab = CreateTab(_testDirectory);
        // The app's watcher, which adds each page to the tab's files as it is extracted
        var folderWatcher = new FileWatcherService(string.CompareOrdinal, _cache);
        var navigation = new NavigationService(_mockImageModelLoader, _cache, folderWatcher,
            new MockPlatformSpecificService(), string.CompareOrdinal);

        try
        {
            await navigation.LoadFromArchiveAsync(zip, tab, new CancellationTokenSource(), startEntry: "img_03.jpg");
            await tab.ImageIterator.NavigateAsync(NavigateTo.Next, SkipAmount.One, new CancellationTokenSource())
                .AsTask().WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

            Assert.Equal("img_04.jpg", tab.Model.FileInfo!.Name);
        }
        finally
        {
            folderWatcher.Dispose();
            tab.ArchiveExtractionService.Cleanup();
        }
    }

    [Fact]
    public async Task LoadFromArchiveAsync_StartEntryThenNextTwiceOneByOne_ShowsThePagesAfterIt()
    {
        // One by one, the pages either side of the start take turns, so img_02 comes between img_04 and img_05, before
        // the shown page
        Settings.Navigation.AlwaysUncompressEntireArchive = false;
        var zip = await CreateZipAsync("next_twice.zip", 5);
        var tab = CreateTab(_testDirectory);
        var folderWatcher = new FileWatcherService(string.CompareOrdinal, _cache);
        var navigation = new NavigationService(_mockImageModelLoader, _cache, folderWatcher,
            new MockPlatformSpecificService(), string.CompareOrdinal);

        try
        {
            await navigation.LoadFromArchiveAsync(zip, tab, new CancellationTokenSource(), startEntry: "img_03.jpg");
            await tab.ImageIterator.NavigateAsync(NavigateTo.Next, SkipAmount.One, new CancellationTokenSource())
                .AsTask().WaitAsync(WaitLimit, TestContext.Current.CancellationToken);
            var afterTheFirst = tab.Model.FileInfo!.Name;
            await tab.ImageIterator.NavigateAsync(NavigateTo.Next, SkipAmount.One, new CancellationTokenSource())
                .AsTask().WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

            Assert.Equal("img_04.jpg", afterTheFirst);
            Assert.Equal("img_05.jpg", tab.Model.FileInfo!.Name);
        }
        finally
        {
            folderWatcher.Dispose();
            tab.ArchiveExtractionService.Cleanup();
            Settings.Navigation.AlwaysUncompressEntireArchive = true;
        }
    }

    [Fact]
    public async Task SortAsync_Descending_SortsTheFilesAndKeepsTheShownOne()
    {
        // Arrange
        var tab = CreateTab(_testDirectory);
        var file1 = new FileInfo(Path.Combine(_testDirectory, "1.jpg"));
        var file2 = new FileInfo(Path.Combine(_testDirectory, "2.jpg"));
        var file3 = new FileInfo(Path.Combine(_testDirectory, "3.jpg"));

        await File.Create(file1.FullName).DisposeAsync();
        await File.Create(file2.FullName).DisposeAsync();
        await File.Create(file3.FullName).DisposeAsync();

        tab.Model = new ImageModel { FileInfo = file1 };
        tab.ImageIterator.Initialize([file1, file2, file3]);

        var cts = new CancellationTokenSource();

        // Act - sort descending
        Settings.Sorting.SortPreference = (int)SortFilesBy.Name;
        await _navigationService.SortAsync(tab, ascending: false, cts);

        // Assert
        Assert.Equal(3, tab.ImageIterator.Files.Count);
        Assert.Equal(file3.FullName, tab.ImageIterator.Files[0].FullName);
        Assert.Equal(file2.FullName, tab.ImageIterator.Files[1].FullName);
        Assert.Equal(file1.FullName, tab.ImageIterator.Files[2].FullName);

        Assert.Equal(2, tab.ImageIterator.CurrentIndex);
    }

    private class MockFileWatcherService : IFileWatcherService
    {
        public bool WatchCalled { get; private set; }
        public bool UnwatchCalled { get; private set; }
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
            UnwatchCalled = true;
        }

        public void RestartWatches()
        {
        }
    }

    private class MockImageModelLoader : IImageModelLoader
    {
        public ValueTask<ImageModel> GetImageModelAsync(FileInfo file, CancellationToken ct)
        {
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

    private class MockArchiveService : IArchiveService
    {
        public Task<DirectoryInfo> ExtractToTempAsync(FileInfo archive, CancellationToken ct)
        {
            return Task.FromResult(new DirectoryInfo(Path.GetTempPath()));
        }
    }

    private class MockThumbnailLoader : IThumbnailLoader
    {
        public int GetThumbnailAsyncCalledCount { get; private set; }

        public ValueTask<object?> GetThumbnailAsync(FileInfo file, CoreViewModel core) 
        {
            GetThumbnailAsyncCalledCount++;
            return ValueTask.FromResult<object?>(null);
        }

        public ValueTask<object?> GetThumbnailAsync(FileInfo file, uint size, CoreViewModel? core = null, MagickImage? magick = null) 
        {
            GetThumbnailAsyncCalledCount++;
            return ValueTask.FromResult<object?>(null);
        }

        public object? GetExifThumbnail(FileInfo file) => null;
        public object? GetThumbQuick(FileInfo file, CoreViewModel? core = null) => null;
    }

    private class MockTempFileService : ITempFileService
    {
        public string GetNewTempFilePath(string fileName) => Path.Combine(Path.GetTempPath(), fileName);

        public void Cleanup() { }
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

internal interface IArchiveService
{
}
