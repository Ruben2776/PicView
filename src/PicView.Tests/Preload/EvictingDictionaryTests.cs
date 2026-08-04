using PicView.Core.Preloading;

namespace PicView.Tests.Preload;

public class EvictingDictionaryTests
{
    [Fact]
    public void Constructor_InvalidSize_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new EvictingDictionary<string>(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EvictingDictionary<string>(-1));
    }

    [Fact]
    public void TryAdd_NotFull_ShouldAddWithoutEvicting()
    {
        var dict = new EvictingDictionary<string>(3);
        
        dict.TryAdd(0, "A", 10, false, out var evictedValue, out var isNew);
        
        Assert.True(isNew);
        Assert.Null(evictedValue);
        Assert.Equal(1, dict.Count);
        Assert.Equal("A", dict[0]);
    }

    [Fact]
    public void TryAdd_UpdateExisting_SameObject_ShouldUpdateWithoutEvicting()
    {
        var dict = new EvictingDictionary<string>(3);
        dict.TryAdd(0, "A", 10, false, out _, out _);
        
        // Add the exact same string reference
        dict.TryAdd(0, "A", 10, false, out var evictedValue, out var isNew);
        
        Assert.False(isNew); // Should flag that it is NOT a new reference
        Assert.Null(evictedValue);
        Assert.Equal(1, dict.Count);
        Assert.Equal("A", dict[0]);
    }

    [Fact]
    public void TryAdd_UpdateExisting_DifferentObject_ShouldEvictOldObject()
    {
        var dict = new EvictingDictionary<string>(3);
        dict.TryAdd(0, "A", 10, false, out _, out _);
        
        // Overwrite index 0 with a DIFFERENT reference
        dict.TryAdd(0, "B", 10, false, out var evictedValue, out var isNew);
        
        Assert.True(isNew); // It's a new reference
        Assert.Equal("A", evictedValue); // The old reference MUST be evicted
        Assert.Equal(1, dict.Count);
        Assert.Equal("B", dict[0]);
    }

    [Fact]
    public void TryAdd_Full_ForwardDirection_ShouldEvictFarthestBehind()
    {
        var dict = new EvictingDictionary<string>(3);
        dict.TryAdd(0, "A", 10, false, out _, out _);
        dict.TryAdd(1, "B", 10, false, out _, out _);
        dict.TryAdd(2, "C", 10, false, out _, out _);
        
        dict.TryAdd(3, "D", 10, false, out var evictedValue, out var isNew);
        
        Assert.True(isNew);
        Assert.Equal("A", evictedValue);
        Assert.Equal(3, dict.Count);
        Assert.False(dict.ContainsKey(0));
        Assert.True(dict.ContainsKey(3));
    }

    [Fact]
    public void TryAdd_Full_ForwardDirection_WrapAround_ShouldEvictFarthestBehind()
    {
        var dict = new EvictingDictionary<string>(3);
        dict.TryAdd(9, "A", 10, false, out _, out _);
        dict.TryAdd(0, "B", 10, false, out _, out _);
        dict.TryAdd(1, "C", 10, false, out _, out _);
        
        dict.TryAdd(2, "D", 10, false, out var evictedValue, out var isNew);
        
        Assert.True(isNew);
        Assert.Equal("A", evictedValue);
        Assert.False(dict.ContainsKey(9));
        Assert.True(dict.ContainsKey(2));
    }

    [Fact]
    public void TryAdd_Full_ReverseDirection_ShouldEvictFarthestAhead()
    {
        var dict = new EvictingDictionary<string>(3);
        dict.TryAdd(2, "C", 10, true, out _, out _);
        dict.TryAdd(1, "B", 10, true, out _, out _);
        dict.TryAdd(0, "A", 10, true, out _, out _);
        
        dict.TryAdd(9, "D", 10, true, out var evictedValue, out var isNew);
        
        Assert.True(isNew);
        Assert.Equal("C", evictedValue);
        Assert.False(dict.ContainsKey(2));
        Assert.True(dict.ContainsKey(9));
    }

    [Fact]
    public void TryAdd_Full_ReverseDirection_WrapAround_ShouldEvictFarthestAhead()
    {
        var dict = new EvictingDictionary<string>(3);
        dict.TryAdd(1, "C", 10, true, out _, out _);
        dict.TryAdd(0, "B", 10, true, out _, out _);
        dict.TryAdd(9, "A", 10, true, out _, out _);
        
        dict.TryAdd(8, "D", 10, true, out var evictedValue, out var isNew);
        
        Assert.True(isNew);
        Assert.Equal("C", evictedValue);
        Assert.False(dict.ContainsKey(1));
        Assert.True(dict.ContainsKey(8));
    }

    [Fact]
    public void Remove_ExistingKey_ShouldReturnTrueAndRemove()
    {
        var dict = new EvictingDictionary<string>(3);
        dict.TryAdd(0, "A", 10, false, out _, out _);
        
        var removed = dict.Remove(0);
        
        Assert.True(removed);
        Assert.Equal(0, dict.Count);
    }

    [Fact]
    public void Remove_NonExistingKey_ShouldReturnFalse()
    {
        var dict = new EvictingDictionary<string>(3);
        
        var removed = dict.Remove(0);
        
        Assert.False(removed);
    }

    [Fact]
    public void RemoveOut_ExistingKey_ShouldReturnTrueAndValue()
    {
        var dict = new EvictingDictionary<string>(3);
        dict.TryAdd(0, "A", 10, false, out _, out _);
        
        var removed = dict.Remove(0, out var value);
        
        Assert.True(removed);
        Assert.Equal("A", value);
        Assert.Equal(0, dict.Count);
    }

    [Fact]
    public void Clear_ShouldRemoveAllItems()
    {
        var dict = new EvictingDictionary<string>(3);
        dict.TryAdd(0, "A", 10, false, out _, out _);
        dict.TryAdd(1, "B", 10, false, out _, out _);
        
        dict.Clear();
        
        Assert.Equal(0, dict.Count);
        Assert.Empty(dict.Keys);
        Assert.Empty(dict.Values);
    }

    [Fact]
    public void Indexer_GetExisting_ShouldReturnValue()
    {
        var dict = new EvictingDictionary<string>(3);
        dict.TryAdd(0, "A", 10, false, out _, out _);
        
        Assert.Equal("A", dict[0]);
    }

    [Fact]
    public void Indexer_GetNonExisting_ShouldThrowKeyNotFoundException()
    {
        var dict = new EvictingDictionary<string>(3);
        
        Assert.Throws<KeyNotFoundException>(() => dict[0]);
    }

    [Fact]
    public void TryGetValue_ExistingKey_ShouldReturnTrueAndValue()
    {
        var dict = new EvictingDictionary<string>(3);
        dict.TryAdd(0, "A", 10, false, out _, out _);
        
        var success = dict.TryGetValue(0, out var value);
        
        Assert.True(success);
        Assert.Equal("A", value);
    }

    [Fact]
    public void TryGetValue_NonExistingKey_ShouldReturnFalse()
    {
        var dict = new EvictingDictionary<string>(3);
        
        var success = dict.TryGetValue(0, out var value);
        
        Assert.False(success);
        Assert.Null(value);
    }

    [Fact]
    public void IEnumerable_ShouldIterateOverAllItems()
    {
        var dict = new EvictingDictionary<string>(3);
        dict.TryAdd(0, "A", 10, false, out _, out _);
        dict.TryAdd(1, "B", 10, false, out _, out _);
        
        var items = dict.ToList();
        
        Assert.Equal(2, items.Count);
        Assert.Contains(new KeyValuePair<int, string>(0, "A"), items);
        Assert.Contains(new KeyValuePair<int, string>(1, "B"), items);
    }
}