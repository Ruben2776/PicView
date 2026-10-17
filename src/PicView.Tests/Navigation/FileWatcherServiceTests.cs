using ImageMagick;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.Navigation;

[Collection("Sequential")]
public class FileWatcherServiceTests : IDisposable
{
    private readonly FileWatcherService _service;
    private readonly SharedImageCache _cache;
    private readonly MockThumbnailCache _mockThumbnailCache;
    private readonly string _testDirectory;

    public FileWatcherServiceTests()
    {
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();

        // Ensure Settings are initialized for FileWatcherService usage
        SetDefaults();

        _testDirectory = Path.Combine(Path.GetTempPath(), "PicViewTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_testDirectory);

        _cache = new SharedImageCache(static f => ValueTask.FromResult(new ImageModel { FileInfo = f }));
        _mockThumbnailCache = new MockThumbnailCache();
        // Use default ordinal comparison for tests
        _service = new FileWatcherService((s1, s2) => string.CompareOrdinal(s1, s2), _cache, _mockThumbnailCache, new MockThumbnailLoader());
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
        // Arrange
        var existingFilePath = Path.Combine(_testDirectory, "existing.jpg");
        await File.WriteAllTextAsync(existingFilePath, "dummy content", TestContext.Current.CancellationToken);

        var tab = CreateTab(_testDirectory);
        var files = new List<FileInfo> { new(existingFilePath) };
        tab.InitializeImageIterator(files, _cache, _mockThumbnailCache, new MockThumbnailLoader(), null, _mockThumbnailCache);
        tab.Model = new ImageModel { FileInfo = files[0] };

        _service.Watch(tab);

        // Initial state: 1 file
        Assert.Single(tab.ImageIterator.Files);

        // Act: Create a file
        var filePath = Path.Combine(_testDirectory, "test.jpg");
        await File.WriteAllTextAsync(filePath, "dummy content", TestContext.Current.CancellationToken);

        // Wait for watcher event (async nature of FileSystemWatcher)
        await Task.Delay(500, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, tab.ImageIterator.Files.Count);
        Assert.Contains(tab.ImageIterator.Files, f => f.FullName == filePath);
    }

    [Fact]
    public async Task OnFileCreated_WhenFileAlreadyInList_DoesNotDuplicate()
    {
        // Arrange: Tab already has this file in files list, but file does not exist on disk yet
        var existingFilePath = Path.Combine(_testDirectory, "overwrite.jpg");

        var tab = CreateTab(_testDirectory);
        var files = new List<FileInfo> { new(existingFilePath) };
        tab.InitializeImageIterator(files, _cache, _mockThumbnailCache, new MockThumbnailLoader(), null, _mockThumbnailCache);
        tab.Model = new ImageModel { FileInfo = files[0] };

        _service.Watch(tab);

        // Initial state: 1 file
        Assert.Single(tab.ImageIterator.Files);

        // Act: Create the file on disk (triggers Created event)
        await File.WriteAllTextAsync(existingFilePath, "created content", TestContext.Current.CancellationToken);

        // Wait for watcher event
        await Task.Delay(500, TestContext.Current.CancellationToken);

        // Assert: No duplicates should be inserted
        Assert.Single(tab.ImageIterator.Files);
        Assert.Equal(existingFilePath, tab.ImageIterator.Files[0].FullName);
    }

    [Fact]
    public async Task OnFileChanged_UpdatesTabFiles_AndResyncs()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "change.jpg");
        await File.WriteAllTextAsync(filePath, "dummy content", TestContext.Current.CancellationToken);

        var tab = CreateTab(_testDirectory);
        var files = new List<FileInfo> { new(filePath) };
        tab.InitializeImageIterator(files, _cache, _mockThumbnailCache, new MockThumbnailLoader(), null, _mockThumbnailCache);
        tab.Model = new ImageModel { FileInfo = files[0] };

        _service.Watch(tab);
        _cache.Put(tab.Id, files[0], new ImageModel { FileInfo = files[0] });

        // Act: Modify the file (triggers Changed event)
        await File.WriteAllTextAsync(filePath, "dummy content modified", TestContext.Current.CancellationToken);
        await Task.Delay(500, TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(tab.ImageIterator.Files);
        Assert.False(_cache.TryGetReady(new FileInfo(filePath), out _), "The stale image should no longer be served");
        Assert.Contains(filePath, _mockThumbnailCache.RemovedPaths);
    }

    [Fact]
    public async Task OnFileDeleted_UpdatesTabFiles_AndResyncs()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test1.jpg");
        var filePath2 = Path.Combine(_testDirectory, "test2.jpg");
        var filePath3 = Path.Combine(_testDirectory, "test3.jpg");
        await File.WriteAllTextAsync(filePath, "dummy content", TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(filePath2, "dummy content", TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(filePath3, "dummy content", TestContext.Current.CancellationToken);
        
        var tab = CreateTab(_testDirectory);
        
        // Manually initialize tab with files
        var files = new List<FileInfo> { new(filePath), new(filePath2), new(filePath3) };
        tab.InitializeImageIterator(files, _cache, _mockThumbnailCache, new MockThumbnailLoader(), null, _mockThumbnailCache);
        tab.Model = new ImageModel { FileInfo = files[0] };
        
        _service.Watch(tab);
        _cache.Put(tab.Id, files[0], new ImageModel { FileInfo = files[0] });

        // Act: Delete file
        File.Delete(filePath);
        await Task.Delay(500, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, tab.ImageIterator.Files.Count);
        Assert.DoesNotContain(tab.ImageIterator.Files, f => f.FullName == filePath);
        Assert.False(_cache.TryGetReady(new FileInfo(filePath), out _), "The stale image should no longer be served");
        Assert.Contains(filePath, _mockThumbnailCache.RemovedPaths);
        
        File.Delete(filePath2);
        File.Delete(filePath3);
    }
    
    [Fact]
    public async Task OnFileRenamed_UpdatesTabFiles_AndModel()
    {
        // Arrange
        var oldPath = Path.Combine(_testDirectory, "old.jpg");
        var newPath = Path.Combine(_testDirectory, "new.jpg");
        await File.WriteAllTextAsync(oldPath, "dummy content", TestContext.Current.CancellationToken);
        
        var tab = CreateTab(_testDirectory);
        var files = new List<FileInfo> { new(oldPath) };
        tab.InitializeImageIterator(files, _cache, _mockThumbnailCache, new MockThumbnailLoader(), null, _mockThumbnailCache);
        tab.Model = new ImageModel { FileInfo = files[0] };
        
        _service.Watch(tab);
        _cache.Put(tab.Id, files[0], new ImageModel { FileInfo = files[0] });

        // Act
        File.Move(oldPath, newPath);
        await Task.Delay(500, TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(tab.ImageIterator.Files);
        Assert.Equal(newPath, tab.ImageIterator.Files[0].FullName);
        // Verify model update if it was current file
        Assert.Equal(newPath, tab.Model.FileInfo.FullName);
        Assert.False(_cache.TryGetReady(new FileInfo(oldPath), out _), "The stale image should no longer be served");
        Assert.Contains(oldPath, _mockThumbnailCache.RemovedPaths);
    }

    private TabViewModel CreateTab(string directory)
    {
        var tab = new TabViewModel(_ => { }, null!, _service);
        // We need to set a model so Watcher can find directory
        // But usually Watch() is called after InitializeImageIterator which likely has files.
        // If Model is null, Watch does nothing.
        // So let's fake a model with a file in that directory
        var dummyFile = new FileInfo(Path.Combine(directory, "placeholder.png"));
        tab.Model = new ImageModel { FileInfo = dummyFile };
        
        tab.Initialize(_cache, _mockThumbnailCache, new MockThumbnailLoader(), _service, _mockThumbnailCache);
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
    private class MockThumbnailLoader : IThumbnailLoader
    {
        public ValueTask<object?> GetThumbnailAsync(FileInfo file, CoreViewModel core) => ValueTask.FromResult<object?>(null);
        public ValueTask<object?> GetThumbnailAsync(FileInfo file, uint size, CoreViewModel? core = null, MagickImage? magick = null) => ValueTask.FromResult<object?>(null);
        public object? GetExifThumbnail(FileInfo file) => null;
        public object? GetThumbQuick(FileInfo file, CoreViewModel? core = null) => null;
    }
    
    private class MockThumbnailCache : IThumbnailCache
    {
        public List<string> RemovedPaths { get; } = new();

        public void Add(uint ownerId, string path, object thumbnail) { }
        public bool TryGet(string path, out object? thumbnail) { thumbnail = null; return false; }
        public void Remove(string path) => RemovedPaths.Add(path);
        public void RemoveOwner(uint ownerId) { }
        public bool IsEmpty => true;
    }

    private class MockFrameProvider : FrameProvider
    {
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) => callback.MoveNext(0);
    }
}