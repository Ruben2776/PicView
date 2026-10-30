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

    public NavigationServiceTests()
    {
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
        SetDefaults();
        TranslationManager.LoadLanguage("en");
        FileHistoryManager.Initialize();

        _testDirectory = Path.Combine(Path.GetTempPath(), "PicViewTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_testDirectory);

        _mockImageModelLoader = new MockImageModelLoader();
        _cache = new SharedImageCache(static f => ValueTask.FromResult(new ImageModel { FileInfo = f }));
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



    private static async Task WaitUntil(Func<bool> condition, Func<string>? describe = null)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!condition())
        {
            Assert.True(DateTime.UtcNow < deadline, "Timed out waiting for the gallery: " + describe?.Invoke());
            await Task.Delay(20, TestContext.Current.CancellationToken);
        }
    }

    private TabViewModel CreateTab(string directory, MainWindowViewModel? parentVm = null)
    {
        var dummyParent = parentVm ?? new MainWindowViewModel(null!, null!, null!, null!, null!);
        var tab = new TabViewModel(dummyParent);
        // Initialize with mocks to avoid null refs
        var thumbCache = new MockThumbnailCache();
        tab.Initialize(_cache, thumbCache, _mockThumbnailLoader, null, thumbCache);
        tab.ImageIterator.Initialize([]);
        return tab;
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

        public void Watch(TabViewModel tab, string? directory = null)
        {
            WatchCalled = true;
            WatchedTab = tab;
            WatchedDirectory = directory;
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
        public Task<bool> ExtractWithLocalSoftwareAsync(string path, string tempDirectory) => Task.FromResult(false);
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
