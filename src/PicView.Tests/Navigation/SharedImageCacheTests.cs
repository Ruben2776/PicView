using PicView.Core.Config;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Preloading;

namespace PicView.Tests.Navigation;

public class SharedImageCacheTests
{
    private readonly SharedImageCache _cache;
    private readonly Func<FileInfo, ValueTask<ImageModel>> _mockLoader;

    public SharedImageCacheTests()
    {
        SetDefaults(); // Initialize settings
        _mockLoader = f => new ValueTask<ImageModel>(new ImageModel { FileInfo = f });
        _cache = new SharedImageCache(_mockLoader);
    }

    [Fact]
    public async Task LoadAsync_ShouldLoadImage()
    {
        uint ownerId = 1;
        _cache.RegisterOwner(ownerId);
        
        var files = new[] { new FileInfo("test1.jpg") };
        var result = await _cache.LoadAsync(ownerId, 0, files);
        
        Assert.NotNull(result);
        Assert.Equal("test1.jpg", result.FileInfo.Name);
    }

    [Fact]
    public void Resynchronize_ShouldUpdateIndices()
    {
        uint ownerId = 1;
        _cache.RegisterOwner(ownerId);
        
        var files = new[] { new FileInfo("test1.jpg"), new FileInfo("test2.jpg") };
        var preLoadValue = new PreLoadValue(new ImageModel { FileInfo = files[0] });
        
        _cache.Add(ownerId, 0, preLoadValue, files.Length, false);
        
        // Now resynchronize with a new list where test1.jpg is at index 1
        var newFiles = new[] { new FileInfo("test2.jpg"), new FileInfo("test1.jpg") };
        _cache.Resynchronize(ownerId, newFiles);
        
        // Should no longer be at index 0
        Assert.False(_cache.TryGet(ownerId, 0, out _));
        
        // Should be at index 1
        Assert.True(_cache.TryGet(ownerId, 1, out var value));
        Assert.Equal(preLoadValue, value);
    }

    [Fact]
    public void Resynchronize_ShouldEvictRemovedItems()
    {
        uint ownerId = 1;
        _cache.RegisterOwner(ownerId);
        
        var files = new[] { new FileInfo("test1.jpg"), new FileInfo("test2.jpg") };
        var preLoadValue = new PreLoadValue(new ImageModel { FileInfo = files[0] });
        
        _cache.Add(ownerId, 0, preLoadValue, files.Length, false);
        
        // Resynchronize with a list that doesn't have test1.jpg
        var newFiles = new[] { new FileInfo("test2.jpg"), new FileInfo("test3.jpg") };
        _cache.Resynchronize(ownerId, newFiles);
        _cache.ForceDisposalQueue();
        
        // Should be evicted from the cache completely
        Assert.False(_cache.TryGet(ownerId, 0, out _));
        Assert.False(_cache.Contains(preLoadValue));
    }

    [Fact]
    public void MultiOwner_ShouldShareData()
    {
        uint ownerId1 = 1;
        uint ownerId2 = 2;
        _cache.RegisterOwner(ownerId1);
        _cache.RegisterOwner(ownerId2);
        
        var file = new FileInfo("test1.jpg");
        var preLoadValue = new PreLoadValue(new ImageModel { FileInfo = file });
        
        _cache.Add(ownerId1, 0, preLoadValue, 1, false);
        
        // Even if owner 2 doesn't have it added in its dictionary at index,
        // TryGet by FileInfo should return it because of _pathLookup.
        Assert.True(_cache.TryGet(file, out var sharedValue));
        Assert.Equal(preLoadValue, sharedValue);
    }
}