using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Preloading;

namespace PicView.Tests.Navigation;

public class SharedImageCacheTests
{
    private readonly SharedImageCache _cache;

    public SharedImageCacheTests()
    {
        SetDefaults(); // Initialize settings
        ValueTask<ImageModel> MockLoader(FileInfo f) => new(new ImageModel { FileInfo = f });
        _cache = new SharedImageCache(MockLoader);
    }

    [Fact]
    public async Task LoadAsync_ShouldLoadImage()
    {
        uint ownerId = 1;
        _cache.RegisterOwner(ownerId);
        
        var files = new[] { new FileInfo("test1.jpg") };
        var result = await _cache.LoadAsync(ownerId, 0, files, TestContext.Current.CancellationToken);
        
        Assert.NotNull(result);
        Assert.Equal("test1.jpg", result.FileInfo.Name);
    }

    [Fact]
    public void TryAdd_SameImage_ShouldNotIncrementReferenceCountTwice()
    {
        uint ownerId = 1;
        _cache.RegisterOwner(ownerId);
        
        var file = new FileInfo("test1.jpg");
        var preLoadValue = new PreLoadValue(new ImageModel { FileInfo = file });
        
        _cache.TryAdd(ownerId, 0, preLoadValue, 10, false, out _);
        Assert.Equal(1, preLoadValue.ReferenceCount);
        
        // Add the EXACT SAME object to the same index
        _cache.TryAdd(ownerId, 0, preLoadValue, 10, false, out _);
        
        // Reference count should still be 1 because the underlying EvictingDictionary caught it!
        Assert.Equal(1, preLoadValue.ReferenceCount); 
    }

    [Fact]
    public void Resynchronize_ShouldEvictRemovedItems_And_DecrementReferences()
    {
        uint ownerId = 1;
        _cache.RegisterOwner(ownerId);
        
        var files = new[] { new FileInfo("test1.jpg"), new FileInfo("test2.jpg") };
        var preLoadValue = new PreLoadValue(new ImageModel { FileInfo = files[0] });
        
        _cache.Add(ownerId, 0, preLoadValue, files.Length, false);
        Assert.Equal(1, preLoadValue.ReferenceCount);

        // Resynchronize with a list that doesn't have test1.jpg
        var newFiles = new[] { new FileInfo("test2.jpg"), new FileInfo("test3.jpg") };
        _cache.Resynchronize(ownerId, newFiles);
        
        // Should be decremented to 0
        Assert.Equal(0, preLoadValue.ReferenceCount);
        
        // It is still lazily sitting in the path lookup...
        Assert.True(_cache.Contains(preLoadValue));
        
        // Force garbage collection
        _cache.ForceDisposalQueue();
        
        // Should be evicted from the cache completely
        Assert.False(_cache.Contains(preLoadValue));
    }

    [Fact]
    public void TryRemove_ShouldSafelyDecrementReference_AndKeepInLookupUntilForced()
    {
        uint ownerId = 1;
        _cache.RegisterOwner(ownerId);
        
        var file = new FileInfo("test1.jpg");
        var preLoadValue = new PreLoadValue(new ImageModel { FileInfo = file });
        
        _cache.TryAdd(ownerId, 0, preLoadValue, 10, false, out _);
        Assert.Equal(1, preLoadValue.ReferenceCount);
        
        _cache.TryRemove(ownerId, 0);
        
        // Ref count hits 0, but it is not destroyed yet
        Assert.Equal(0, preLoadValue.ReferenceCount);
        Assert.True(_cache.Contains(file));
        
        // Sweeping clears the 0-reference orphans
        _cache.ForceDisposalQueue();
        Assert.False(_cache.Contains(file));
    }

    [Fact]
    public void MultiOwner_ShouldShareData_AndManageReferences()
    {
        uint ownerId1 = 1;
        uint ownerId2 = 2;
        _cache.RegisterOwner(ownerId1);
        _cache.RegisterOwner(ownerId2);
        
        var file = new FileInfo("test1.jpg");
        var preLoadValue = new PreLoadValue(new ImageModel { FileInfo = file });
        
        _cache.Add(ownerId1, 0, preLoadValue, 1, false);
        _cache.Add(ownerId2, 0, preLoadValue, 1, false);
        
        // Both tabs added it, so reference count should be 2
        Assert.Equal(2, preLoadValue.ReferenceCount);

        // Tab 1 closes/removes it
        _cache.Clear(ownerId1);
        
        // Reference count drops to 1, meaning it stays alive and is NOT added to the disposal queue
        Assert.Equal(1, preLoadValue.ReferenceCount);
        
        // Ensure Tab 2 can still access it via the shared lookup
        Assert.True(_cache.TryGet(file, out var sharedValue));
        Assert.Equal(preLoadValue, sharedValue);
    }
}