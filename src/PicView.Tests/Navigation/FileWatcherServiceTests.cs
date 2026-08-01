using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.Preloading;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.Navigation;

public class FileWatcherServiceTests : IDisposable
{
    private readonly FileWatcherService _service;
    private readonly MockImageCache _mockCache;
    private readonly MockThumbnailCache _mockThumbnailCache;
    private readonly string _testDirectory;

    public FileWatcherServiceTests()
    {
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();

        // Ensure Settings are initialized for FileWatcherService usage
        SetDefaults();

        _testDirectory = Path.Combine(Path.GetTempPath(), "PicViewTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_testDirectory);

        _mockCache = new MockImageCache();
        _mockThumbnailCache = new MockThumbnailCache();
        // Use default ordinal comparison for tests
        _service = new FileWatcherService((s1, s2) => string.CompareOrdinal(s1, s2), _mockCache, _mockThumbnailCache, new MockThumbnailLoader());
    }

    [Fact]
    public void Watch_AddsSubscriber()
    {
        var tab = CreateTab(_testDirectory);
        _service.Watch(tab);
        
        // No direct way to verify internal state without reflection or observing behavior
        // But we can verify no exception is thrown
    }

    [Fact]
    public async Task OnFileCreated_UpdatesTabFiles()
    {
        var tab = CreateTab(_testDirectory);
        _service.Watch(tab);

        // Initial state: empty
        Assert.Empty(tab.ImageIterator.Files);

        // Act: Create a file
        var filePath = Path.Combine(_testDirectory, "test.jpg");
        await File.WriteAllTextAsync(filePath, "dummy content");

        // Wait for watcher event (async nature of FileSystemWatcher)
        await Task.Delay(500); // Small delay for FS event

        // Assert
        Assert.Single(tab.ImageIterator.Files);
        Assert.Equal(filePath, tab.ImageIterator.Files[0].FullName);
        Assert.True(_mockCache.Resynchronized);
    }

    [Fact]
    public async Task OnFileDeleted_UpdatesTabFiles_AndResyncs()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.jpg");
        await File.WriteAllTextAsync(filePath, "dummy content");
        
        var tab = CreateTab(_testDirectory);
        
        // Manually initialize tab with file
        var files = new List<FileInfo> { new FileInfo(filePath) };
        tab.InitializeImageIterator(files, _mockCache, _mockThumbnailCache, new MockThumbnailLoader(), null, _mockThumbnailCache);
        tab.Model = new ImageModel { FileInfo = files[0] };
        
        _service.Watch(tab);

        // Act: Delete file
        File.Delete(filePath);
        await Task.Delay(500);

        // Assert
        Assert.Empty(tab.ImageIterator.Files);
        Assert.True(_mockCache.Resynchronized);
        Assert.Contains(filePath, _mockThumbnailCache.RemovedPaths);
    }
    
    [Fact]
    public async Task OnFileRenamed_UpdatesTabFiles_AndModel()
    {
        // Arrange
        var oldPath = Path.Combine(_testDirectory, "old.jpg");
        var newPath = Path.Combine(_testDirectory, "new.jpg");
        await File.WriteAllTextAsync(oldPath, "dummy content");
        
        var tab = CreateTab(_testDirectory);
        var files = new List<FileInfo> { new FileInfo(oldPath) };
        tab.InitializeImageIterator(files, _mockCache, _mockThumbnailCache, new MockThumbnailLoader(), null, _mockThumbnailCache);
        tab.Model = new ImageModel { FileInfo = files[0] };
        
        _service.Watch(tab);

        // Act
        File.Move(oldPath, newPath);
        await Task.Delay(500);

        // Assert
        Assert.Single(tab.ImageIterator.Files);
        Assert.Equal(newPath, tab.ImageIterator.Files[0].FullName);
        // Verify model update if it was current file
        Assert.Equal(newPath, tab.Model.FileInfo.FullName);
        Assert.True(_mockCache.Resynchronized);
        Assert.Contains(oldPath, _mockThumbnailCache.RemovedPaths);
    }

    private TabViewModel CreateTab(string directory)
    {
        var tab = new TabViewModel(_ => { }, null!, _service);
        // We need to set a model so Watcher can find directory
        // But usually Watch() is called after InitializeImageIterator which likely has files.
        // If Model is null, Watch does nothing.
        // So let's fake a model with a file in that directory
        var dummyFile = new FileInfo(Path.Combine(directory, "placeholder.txt"));
        tab.Model = new ImageModel { FileInfo = dummyFile };
        
        tab.Initialize(_mockCache, _mockThumbnailCache, new MockThumbnailLoader(), _service, _mockThumbnailCache);
        return tab;
    }

    public void Dispose()
    {
        _service.Dispose();
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, true);
            }
            catch { /* Best effort cleanup */ }
        }
    }
    
    // Mocks
    private class MockImageCache : IImageCache
    {
        public bool Resynchronized { get; private set; }
        public void Resynchronize(uint ownerId, IReadOnlyList<FileInfo> files) => Resynchronized = true;
        public ValueTask<bool> WaitForLoadingCompleteAsync(uint ownerId, int index, IReadOnlyList<FileInfo> list, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        // Stub other methods
        public Task<ImageModel?> LoadAsync(uint ownerId, int index, IReadOnlyList<FileInfo> list, CancellationToken ct = default) => Task.FromResult<ImageModel?>(null);
        public void RegisterOwner(uint ownerId) { }
        public void RemoveOwner(uint ownerId) { }
        public bool TryGet(FileInfo f, out PreLoadValue? value) { value = null; return false; }
        public bool TryGet(uint ownerId, int index, out PreLoadValue? value) { value = null; return false; }
        public bool TryGet(ReadOnlySpan<char> f, out PreLoadValue? value) { value = null; return false; }
        public void Clear() { }
        public void Clear(uint ownerId) { }
        public bool Contains(PreLoadValue value) => false;
        public void Add(uint ownerId, int index, PreLoadValue preLoadValue, int listCount, bool isReverse)
        {
            
        }

        public bool TryAdd(uint ownerId, int index, PreLoadValue preLoadValue, int listCount, bool isReverse, out PreLoadValue? value) { value = null; return false; }
        public void Preload(uint ownerId, int currentIndex, bool reversed, IReadOnlyList<FileInfo> files, CancellationToken token) { }
        public void Clear(TabViewModel tab, string directory) { }
        public void TryRemove(uint ownerId, int index) { }
        public ValueTask<bool> WaitForLoadingCompleteAsync(uint ownerId, int index) => ValueTask.FromResult(false);
    }

    private class MockThumbnailLoader : IThumbnailLoader
    {
        public ValueTask<object?> GetThumbnailAsync(FileInfo file) => ValueTask.FromResult<object?>(null);
        public ValueTask<object?> GetThumbnailAsync(FileInfo file, uint size) => ValueTask.FromResult<object?>(null);
        public object? GetExifThumbnail(FileInfo file) => null;
    }
    
    private class MockThumbnailCache : IThumbnailCache
    {
        public List<string> RemovedPaths { get; } = new();

        public void Add(uint ownerId, string path, object thumbnail) { }
        public bool TryGet(string path, out object? thumbnail) { thumbnail = null; return false; }
        public void Remove(string path) => RemovedPaths.Add(path);
        public void RemoveOwner(uint ownerId) { }
        public void Clear() { }
        public bool IsEmpty() => true;
    }

    private class MockFrameProvider : FrameProvider
    {
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) => callback.MoveNext(0);
    }
}
