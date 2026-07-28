using PicView.Core.Models;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.Preloading;
using PicView.Core.ViewModels;

namespace PicView.Tests.Preload;

public class PreloaderTests
{
    private class FakeImageCache : IImageCache
    {
        public Dictionary<int, PreLoadValue> Items { get; } = new();
        public Dictionary<string, PreLoadValue> ItemsByFile { get; } = new();
        
        public bool TryGet(FileInfo f, out PreLoadValue? value)
        {
            return ItemsByFile.TryGetValue(f.FullName, out value);
        }

        public bool TryGet(uint ownerId, int index, out PreLoadValue? value)
        {
            return Items.TryGetValue(index, out value);
        }
        
        public bool TryAdd(uint ownerId, int index, PreLoadValue preLoadValue, int listCount, bool isReverse, out PreLoadValue? value)
        {
            Items[index] = preLoadValue;
            ItemsByFile[preLoadValue.ImageModel.FileInfo!.FullName] = preLoadValue;
            value = null;
            return false;
        }

        public void Add(uint ownerId, int index, PreLoadValue preLoadValue, int listCount, bool isReverse)
        {
            Items[index] = preLoadValue;
            ItemsByFile[preLoadValue.ImageModel.FileInfo!.FullName] = preLoadValue;
        }

        public Task<ImageModel?> LoadAsync(uint ownerId, int index, IReadOnlyList<FileInfo> list, CancellationToken ct = default) => Task.FromResult<ImageModel?>(null);
        public void Clear() { }
        public void Clear(uint ownerId) { }
        public bool Contains(PreLoadValue value) => false;
        public void Preload(uint ownerId, int currentIndex, bool reversed, IReadOnlyList<FileInfo> files, CancellationToken token) { }
        public void RemoveOwner(uint ownerId) { }
        public void RegisterOwner(uint ownerId) { }
        public void Clear(TabViewModel tab, string directory) { }
        public void TryRemove(uint ownerId, int index) { }
        public void Resynchronize(uint ownerId, IReadOnlyList<FileInfo> files) { }
        public ValueTask<bool> WaitForLoadingCompleteAsync(uint ownerId, int index, IReadOnlyList<FileInfo> list, CancellationToken ct = default) => ValueTask.FromResult(true);
    }

    public PreloaderTests()
    {
        SetDefaults();
    }

    [Fact]
    public async Task AddAsync_WhenNotInCache_ShouldLoadAndAddToCache()
    {
        // Arrange
        var cache = new FakeImageCache();
        var loadCount = 0;
        var preloader = new Preloader(f =>
        {
            loadCount++;
            return ValueTask.FromResult(new ImageModel { FileInfo = f });
        }, cache);

        var list = new List<FileInfo> { new FileInfo("test1.jpg"), new FileInfo("test2.jpg") };

        // Act
        var result = await preloader.AddAsync(1, 0, list);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test1.jpg", result!.FileInfo!.Name);
        Assert.Equal(1, loadCount);
        Assert.True(cache.Items.ContainsKey(0));
    }

    [Fact]
    public async Task AddAsync_WhenAlreadyInCache_ShouldNotLoadAgain()
    {
        // Arrange
        var cache = new FakeImageCache();
        var loadCount = 0;
        var preloader = new Preloader(f =>
        {
            loadCount++;
            return ValueTask.FromResult(new ImageModel { FileInfo = f });
        }, cache);

        var list = new List<FileInfo> { new FileInfo("test1.jpg") };
        var fileInfo = list[0];
        
        var preloadValue = new PreLoadValue(new ImageModel { FileInfo = fileInfo });
        cache.Add(1, 0, preloadValue, 1, false);

        // Act
        var result = await preloader.AddAsync(1, 0, list);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test1.jpg", result!.FileInfo!.Name);
        Assert.Equal(0, loadCount); // Should not load from disk
    }

    [Fact]
    public async Task Preload_ShouldLoadAdjacentItems()
    {
        // Arrange
        var cache = new FakeImageCache();
        var loadCount = 0;
        var preloader = new Preloader(f =>
        {
            Interlocked.Increment(ref loadCount);
            return ValueTask.FromResult(new ImageModel { FileInfo = f });
        }, cache);

        const int capacity = 15;
        var list = new List<FileInfo>(capacity);
        for (var i = 1; i <= capacity; i++)
        {
            list.Add(new FileInfo($"test{i}.jpg"));
        }

        SetDefaults();
        // Act
        // Current index is 2 ("test3.jpg"). Next starting is 3. Prev starting is 1.
        // It should load next items (index 3 and 4) and prev items (index 1) based on limits.
        preloader.Preload(1, 2, false, list, TestContext.Current.CancellationToken);

        // Wait to allow background tasks to complete
        await Task.Delay(1000, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(Settings.Navigation.NegativeIterations + Settings.Navigation.PositiveIterations, loadCount);
        Assert.True(cache.TryGet(list[0], out _)); // negative iteration
        Assert.True(cache.TryGet(list[3], out _)); // positive iteration
        Assert.True(cache.TryGet(list[4], out _)); // positive iteration
    }
}