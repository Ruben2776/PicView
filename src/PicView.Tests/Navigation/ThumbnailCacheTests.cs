using PicView.Core.Navigation;
using Xunit;

namespace PicView.Tests.Navigation;

public class ThumbnailCacheTests
{
    private readonly ThumbnailCache _cache;

    public ThumbnailCacheTests()
    {
        _cache = new ThumbnailCache();
    }

    [Fact]
    public void Add_And_Get_Works()
    {
        // TODO: Create thumb tests
    }

    [Fact]
    public void RemoveOwner_RemovesFile_WhenNoOwnersLeft()
    {
        // TODO: Create thumb tests
    }

    [Fact]
    public void RemoveOwner_DoesNotRemoveFile_WhenOtherOwnerExists()
    {
        // TODO: Create thumb tests
    }

    [Fact]
    public void Remove_RemovesFile_RegardlessOfOwners()
    {
        // TODO: Create thumb tests
    }
}
