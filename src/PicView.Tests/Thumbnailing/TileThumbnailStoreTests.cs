using PicView.Core.Thumbnailing;

namespace PicView.Tests.Thumbnailing;

public class TileThumbnailStoreTests
{
    private readonly TileThumbnailStore _store = new(budgetInBytes: 300);

    [Fact]
    public void Add_PastTheBudget_DropsTheLeastRecentlyUsed()
    {
        _store.Add("a", "A", 100, 100);
        _store.Add("b", "B", 100, 100);
        _store.Add("c", "C", 100, 100);
        _store.Add("d", "D", 100, 100);

        Assert.False(_store.TryGet("a", out _, out _));
        Assert.True(_store.TryGet("b", out _, out _));
        Assert.Equal(300, _store.Bytes);
    }

    [Fact]
    public void TryGet_MarksUsed_SoAnotherIsDroppedInstead()
    {
        _store.Add("a", "A", 100, 100);
        _store.Add("b", "B", 100, 100);
        _store.Add("c", "C", 100, 100);
        _store.TryGet("a", out _, out _);

        _store.Add("d", "D", 100, 100);

        Assert.True(_store.TryGet("a", out _, out _));
        Assert.False(_store.TryGet("b", out _, out _));
    }

    [Fact]
    public void Add_OneLargerThanTheBudget_IsKeptAlone()
    {
        _store.Add("a", "A", 100, 100);
        _store.Add("big", "BIG", 900, 1000);

        Assert.True(_store.TryGet("big", out var kept, out var height));
        Assert.Equal("BIG", kept);
        Assert.Equal(900u, height);
        Assert.Equal(1, _store.Count);
    }

    [Fact]
    public void Add_SamePath_ReplacesAndCountsOnlyTheNewBytes()
    {
        _store.Add("a", "A", 100, 100);
        _store.Add("A", "A2", 200, 150);

        Assert.True(_store.TryGet("a", out var kept, out var height));
        Assert.Equal("A2", kept);
        Assert.Equal(200u, height);
        Assert.Equal(150, _store.Bytes);
    }

    [Fact]
    public void Move_KeepsTheThumbnailUnderTheNewPath()
    {
        _store.Add("old", "A", 100, 100);
        _store.Move("old", "new");

        Assert.False(_store.TryGet("old", out _, out _));
        Assert.True(_store.TryGet("new", out var kept, out _));
        Assert.Equal("A", kept);
        Assert.Equal(100, _store.Bytes);
    }

    [Fact]
    public void Move_OntoAKeptPath_LetsGoOfTheOneKeptThereAndCountsOnlyTheMovedBytes()
    {
        _store.Add("old", "A", 100, 100);
        _store.Add("new", "N", 50, 50);

        _store.Move("old", "new");

        Assert.Equal(1, _store.Count);
        Assert.Equal(100, _store.Bytes);
        Assert.True(_store.TryGet("new", out var kept, out var height));
        Assert.Equal("A", kept);
        Assert.Equal(100u, height);
    }

    [Fact]
    public void Move_KeepsTheEntrysPlaceInTheEvictionOrder()
    {
        _store.Add("a", "A", 100, 100);
        _store.Add("b", "B", 100, 100);
        _store.Add("c", "C", 100, 100);

        _store.Move("a", "moved");
        _store.Add("d", "D", 100, 100);

        // still the least recently used, so it is the one let go of
        Assert.False(_store.TryGet("moved", out _, out _));
        Assert.True(_store.TryGet("b", out _, out _));
        Assert.Equal(3, _store.Count);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("A")]
    public void Move_OntoItself_KeepsTheOneEntry(string newPath)
    {
        _store.Add("a", "A", 100, 100);

        _store.Move("a", newPath);

        Assert.Equal(1, _store.Count);
        Assert.Equal(100, _store.Bytes);
        Assert.True(_store.TryGet("a", out var kept, out _));
        Assert.Equal("A", kept);
        _store.Remove(newPath);
        Assert.Equal(0, _store.Count);
        Assert.Equal(0, _store.Bytes);
    }

    [Fact]
    public void Remove_FreesItsBytes()
    {
        _store.Add("a", "A", 100, 100);
        _store.Remove("a");
        Assert.Equal(0, _store.Bytes);
        _store.Remove("missing"); // nothing, no throw
        _store.Move("missing", "x"); // nothing, no throw
    }

    [Fact]
    public void AddIfTaller_KeptIsAsTallOrTaller_KeepsTheKeptAndCountsNothingMore()
    {
        _store.Add("a", "TALL", 600, 100);

        Assert.False(_store.AddIfTaller("a", "SHORT", 400, 50));
        Assert.False(_store.AddIfTaller("a", "SAME", 600, 100));

        Assert.True(_store.TryGet("a", out var kept, out var height));
        Assert.Equal("TALL", kept);
        Assert.Equal(600u, height);
        Assert.Equal(100, _store.Bytes);
    }

    [Fact]
    public void AddIfTaller_NothingKeptOrKeptIsShorter_Keeps()
    {
        Assert.True(_store.AddIfTaller("a", "SHORT", 400, 50));
        Assert.True(_store.AddIfTaller("a", "TALL", 600, 100));

        Assert.True(_store.TryGet("a", out var kept, out var height));
        Assert.Equal("TALL", kept);
        Assert.Equal(600u, height);
        Assert.Equal(100, _store.Bytes);
    }
}
