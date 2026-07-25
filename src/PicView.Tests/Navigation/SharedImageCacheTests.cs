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
        // TODO: Create cache tests
    }

    [Fact]
    public void Resynchronize_ShouldUpdateIndices()
    {
        // TODO: Create cache tests
    }

    [Fact]
    public void Resynchronize_ShouldEvictRemovedItems()
    {
        // TODO: Create cache tests
    }

    [Fact]
    public async Task MultiOwner_ShouldShareData()
    {
        // TODO: Create cache tests
    }
}