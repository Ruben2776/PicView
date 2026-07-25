using PicView.Core.FileHandling.Interfaces;
using PicView.Core.Localization;
using PicView.Core.IPlatform;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.Preloading;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.Navigation;

public class NavigationServiceTests
{
    private readonly MockImageLoader _mockImageLoader;
    private readonly MockImageCache _mockCache;
    private readonly MockFileWatcherService _mockFileWatcherService;
    private readonly MockThumbnailLoader _mockThumbnailLoader;
    private readonly NavigationService _navigationService;
    private readonly string _testDirectory;

    public NavigationServiceTests()
    {
        SetDefaults();
        TranslationManager.Init();
        // TODO: Create navigation tests
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
        File.Create(fileInfo.FullName).Dispose();
        
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
        var tab = new TabViewModel(null);
        // Initialize with mocks to avoid null refs
        var thumbCache = new MockThumbnailCache();
        tab.Initialize(_mockCache, thumbCache, new MockThumbnailLoader(), null, thumbCache);
        tab.ImageIterator.Files = new List<FileInfo>();
        return tab;
    }

    // Mocks

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

    private class MockImageLoader : IImageLoader
    {
        public ValueTask<ImageModel> GetImageModelAsync(FileInfo file, CancellationToken ct)
        {
            return ValueTask.FromResult(new ImageModel { FileInfo = file });
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
        public bool TryGet(uint ownerId, int index, out PreLoadValue? value) { value = null; return false; }
        public void Clear() { }
        public void Clear(uint ownerId) { }
        public bool Contains(PreLoadValue value) => false;
        public bool Add(uint ownerId, int index, PreLoadValue preLoadValue, int listCount, bool isReverse) => false;
        public bool TryAdd(uint ownerId, int index, PreLoadValue preLoadValue, int listCount, bool isReverse, out PreLoadValue? value) { value = null; return false; }
        public void Preload(uint ownerId, int currentIndex, bool reversed, IReadOnlyList<FileInfo> files, CancellationToken token) { }
        public void RemoveOwner(uint ownerId) { }
        public void RegisterOwner(uint ownerId) { }
        public void Clear(TabViewModel tab, int currentIndex, string directory, IReadOnlyList<FileInfo> files) { }
        public void TryRemove(uint ownerId, int index) { }
        public void Resynchronize(uint ownerId, IReadOnlyList<FileInfo> files) { }
        public ValueTask<bool> WaitForLoadingCompleteAsync(uint ownerId, int index, IReadOnlyList<FileInfo> list, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public ValueTask<bool> WaitForLoadingCompleteAsync(uint ownerId, int index) => ValueTask.FromResult(false);
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
        public bool IsEmpty() => true;
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
