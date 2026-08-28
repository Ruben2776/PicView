using PicView.Core.FileHandling.Interfaces;
using PicView.Core.FileHistory;
using PicView.Core.FileSorting;
using PicView.Core.Gallery;
using PicView.Core.Localization;
using PicView.Core.IPlatform;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.Preloading;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.Navigation;

[Collection("Sequential")]
public class NavigationServiceTests : IDisposable
{
    private readonly MockImageModelLoader _mockImageModelLoader;
    private readonly MockImageCache _mockCache;
    private readonly MockFileWatcherService _mockFileWatcherService;
    private readonly MockThumbnailLoader _mockThumbnailLoader;
    private readonly NavigationService _navigationService;
    private readonly string _testDirectory;

    public NavigationServiceTests()
    {
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
        SetDefaults();
        TranslationManager.LoadLanguage("en").AsTask().GetAwaiter().GetResult();
        FileHistoryManager.Initialize();

        _testDirectory = Path.Combine(Path.GetTempPath(), "PicViewTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_testDirectory);

        _mockImageModelLoader = new MockImageModelLoader();
        _mockCache = new MockImageCache();
        _mockFileWatcherService = new MockFileWatcherService();
        _mockThumbnailLoader = new MockThumbnailLoader();

        _navigationService = new NavigationService(
            _mockImageModelLoader,
            _mockCache,
            _mockFileWatcherService,
            new MockPlatformSpecificService(),
            _mockThumbnailLoader,
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

    [Fact]
    public async Task RepopulateIterator_ReloadsGallery_WhenVisible()
    {
        // Arrange
        var tab = CreateTab(_testDirectory);
        // Ensure gallery is considered visible
        tab.Gallery.IsDockedGalleryVisible.Value = true;
        
        var fileInfo = new FileInfo(Path.Combine(_testDirectory, "test.jpg"));
        // Create a dummy file so there is something to load
        await File.Create(fileInfo.FullName).DisposeAsync();
        
        var cts = new CancellationTokenSource();
        // Provide files list to avoid RetrieveFiles attempting to read directory
        var files = new List<FileInfo> { fileInfo };

        // Act
        await _navigationService.RepopulateIterator(fileInfo, tab, cts, files);

        // Assert
        // GalleryLoader.LoadGalleryAsync calls GetThumbnailAsync
        Assert.True(_mockThumbnailLoader.GetThumbnailAsyncCalledCount > 0, "Gallery should be reloaded (GetThumbnailAsync called)");
    }

    private TabViewModel CreateTab(string directory)
    {
        var tab = new TabViewModel(null!, null!);
        // Initialize with mocks to avoid null refs
        var thumbCache = new MockThumbnailCache();
        tab.Initialize(_mockCache, thumbCache, _mockThumbnailLoader, null, thumbCache);
        tab.ImageIterator.Files = new List<FileInfo>();
        return tab;
    }

    [Fact]
    public async Task ApplySortAsync_SortsLoadedGalleryItems()
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
        tab.ImageIterator.Files = new List<FileInfo> { file1, file2, file3 };
        tab.ImageIterator.SetCurrentIndex(0);

        var item1 = new GalleryItemViewModel { FileInfo = file1 };
        var item2 = new GalleryItemViewModel { FileInfo = file2 };
        var item3 = new GalleryItemViewModel { FileInfo = file3 };

        tab.Gallery.GalleryItems.Add(item1);
        tab.Gallery.GalleryItems.Add(item2);
        tab.Gallery.GalleryItems.Add(item3);

        var cts = new CancellationTokenSource();

        // Act - sort descending
        Settings.Sorting.SortPreference = (int)SortFilesBy.Name;
        await _navigationService.SortAsync(tab, ascending: false, cts);

        // Assert
        Assert.Equal(3, tab.ImageIterator.Files.Count);
        Assert.Equal(file3.FullName, tab.ImageIterator.Files[0].FullName);
        Assert.Equal(file2.FullName, tab.ImageIterator.Files[1].FullName);
        Assert.Equal(file1.FullName, tab.ImageIterator.Files[2].FullName);

        Assert.Equal(3, tab.Gallery.GalleryItems.Count);
        Assert.Same(item3, tab.Gallery.GalleryItems[0]);
        Assert.Same(item2, tab.Gallery.GalleryItems[1]);
        Assert.Same(item1, tab.Gallery.GalleryItems[2]);

        Assert.Equal(2, tab.ImageIterator.CurrentIndex);
    }

    [Fact]
    public void SortLoadedGallery_ReordersGalleryItems_ToMatchFileList()
    {
        // Arrange
        var tab = CreateTab(_testDirectory);
        var fileA = new FileInfo(Path.Combine(_testDirectory, "a.jpg"));
        var fileB = new FileInfo(Path.Combine(_testDirectory, "b.jpg"));
        var fileC = new FileInfo(Path.Combine(_testDirectory, "c.jpg"));
        var fileD = new FileInfo(Path.Combine(_testDirectory, "d.jpg"));

        var itemA = new GalleryItemViewModel { FileInfo = fileA };
        var itemB = new GalleryItemViewModel { FileInfo = fileB };
        var itemC = new GalleryItemViewModel { FileInfo = fileC };
        var itemD = new GalleryItemViewModel { FileInfo = fileD };

        tab.Gallery.GalleryItems.Add(itemA);
        tab.Gallery.GalleryItems.Add(itemB);
        tab.Gallery.GalleryItems.Add(itemC);
        tab.Gallery.GalleryItems.Add(itemD);

        var targetOrder = new List<FileInfo> { fileC, fileA, fileD, fileB };

        // Act
        GalleryLoader.SortLoadedGallery(tab, targetOrder);

        // Assert
        Assert.Equal(4, tab.Gallery.GalleryItems.Count);
        Assert.Same(itemC, tab.Gallery.GalleryItems[0]);
        Assert.Same(itemA, tab.Gallery.GalleryItems[1]);
        Assert.Same(itemD, tab.Gallery.GalleryItems[2]);
        Assert.Same(itemB, tab.Gallery.GalleryItems[3]);
    }

    [Fact]
    public void SortLoadedGallery_HandlesEmptyOrSingleItem()
    {
        // Arrange
        var tab = CreateTab(_testDirectory);
        var file = new FileInfo(Path.Combine(_testDirectory, "single.jpg"));
        var item = new GalleryItemViewModel { FileInfo = file };

        // Act & Assert - Empty
        GalleryLoader.SortLoadedGallery(tab, new List<FileInfo> { file });
        Assert.Empty(tab.Gallery.GalleryItems);

        // Act & Assert - Single item
        tab.Gallery.GalleryItems.Add(item);
        GalleryLoader.SortLoadedGallery(tab, new List<FileInfo> { file });
        Assert.Single(tab.Gallery.GalleryItems);
        Assert.Same(item, tab.Gallery.GalleryItems[0]);
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

    private class MockImageCache : IImageCache
    {
        public Task<ImageModel?> LoadAsync(uint ownerId, int index, IReadOnlyList<FileInfo> list, CancellationToken ct = default) => Task.FromResult<ImageModel?>(null);
        public bool TryGet(FileInfo f, out PreLoadValue? value) { value = null; return false; }
        public bool TryGet(ReadOnlySpan<char> f, out PreLoadValue? value) { value = null; return false; }
        public bool Contains(PreLoadValue value) => false;
        public bool Contains(string fileName) => false;
        public bool Contains(FileInfo fileInfo) => false;
        public void Clear(uint ownerId) { }
        public void Add(uint ownerId, int index, PreLoadValue preLoadValue, int listCount, bool isReverse) { }
        public bool TryAdd(uint ownerId, int index, PreLoadValue preLoadValue, int listCount, bool isReverse, out PreLoadValue? value) { value = null; return false; }
        public void Preload(uint ownerId, int currentIndex, bool reversed, IReadOnlyList<FileInfo> files, CancellationToken token) { }
        public void RemoveOwner(uint ownerId) { }
        public void RegisterOwner(uint ownerId) { }
        public void Clear(TabViewModel tab, string directory) { }
        public void Resynchronize(uint ownerId, IReadOnlyList<FileInfo> files) { }
        public ValueTask<bool> WaitForLoadingCompleteAsync(uint ownerId, int index, IReadOnlyList<FileInfo> list, CancellationToken ct = default) => ValueTask.FromResult(false);
        public void DeleteFromCache(string fileName) { }
    }
    
    private class MockThumbnailLoader : IThumbnailLoader
    {
        public int GetThumbnailAsyncCalledCount { get; private set; }

        public ValueTask<object?> GetThumbnailAsync(FileInfo file) 
        {
            GetThumbnailAsyncCalledCount++;
            return ValueTask.FromResult<object?>(null);
        }

        public ValueTask<object?> GetThumbnailAsync(FileInfo file, uint size) 
        {
            GetThumbnailAsyncCalledCount++;
            return ValueTask.FromResult<object?>(null);
        }

        public object? GetExifThumbnail(FileInfo file) => null;
        public object? GetThumbQuick(FileInfo file) => null;
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
        public ValueTask Print(string path) { return ValueTask.CompletedTask; }
        public Task SetAsWallpaper(string path, int wallpaperStyle) => Task.CompletedTask;
        public bool SetAsLockScreen(string path) => false;
        public bool CopyFile(string path) => false;
        public bool CutFile(string path) => false;
        public Task CopyImageToClipboard(object bitmap) => Task.CompletedTask;
        public Task<object?> GetImageFromClipboard() => Task.FromResult<object?>(null);
        public Task<bool> ExtractWithLocalSoftwareAsync(string path, string tempDirectory) => Task.FromResult(false);
        public string DefaultJsonKeyMap() => "{}";
        public void InitiateFileAssociationService() { }
        public Task<bool> DeleteFile(string path, bool recycle) => Task.FromResult(false);
        public byte[]? GetShellThumbnail(string path, int width, int height, out int pixelWidth, out int pixelHeight)
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
