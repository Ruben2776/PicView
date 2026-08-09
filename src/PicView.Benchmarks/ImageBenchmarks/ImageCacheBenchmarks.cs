using BenchmarkDotNet.Attributes;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Preloading;

namespace PicView.Benchmarks.ImageBenchmarks;

[MemoryDiagnoser]
public class ImageCacheBenchmarks
{
    /// <summary>
    /// Number of items to operate on per benchmark iteration.
    /// </summary>
    private const int ItemCount = 64;

    /// <summary>
    /// Simulated total file list count passed to TryAdd for eviction calculations.
    /// </summary>
    private const int ListCount = 200;

    private SharedImageCache _cache = null!;
    private PreLoadValue[] _preLoadValues = null!;
    private FileInfo[] _fileInfos = null!;
    private string[] _filePaths = null!;

    [GlobalSetup]
    public void Setup()
    {
        SetDefaults();

        // Use a no-op image loader since benchmarks only test the cache structure,
        // not actual image decoding.
        _cache = new SharedImageCache(static f => new ValueTask<ImageModel>(new ImageModel { FileInfo = f }));
        _cache.RegisterOwner(1);

        _fileInfos = new FileInfo[ItemCount];
        _filePaths = new string[ItemCount];
        _preLoadValues = new PreLoadValue[ItemCount];

        for (var i = 0; i < ItemCount; i++)
        {
            _fileInfos[i] = new FileInfo($@"C:\bench\img_{i:D4}.jpg");
            _filePaths[i] = _fileInfos[i].FullName;
            _preLoadValues[i] = new PreLoadValue(new ImageModel { FileInfo = _fileInfos[i] });
        }
    }

    [IterationSetup]
    public void IterationSetup()
    {
        // Each iteration starts with a clean cache so prior state does not skew results.
        _cache.Clear(1);
        _cache.ForceDisposalQueue();

        // Reset reference counts by recreating PreLoadValues.
        for (var i = 0; i < ItemCount; i++)
        {
            _preLoadValues[i] = new PreLoadValue(new ImageModel { FileInfo = _fileInfos[i] });
        }
    }

    // ---------------------------------------------------------------
    //  TryAdd benchmarks
    // ---------------------------------------------------------------

    /// <summary>
    /// Measures the cost of adding items sequentially (forward iteration).
    /// </summary>
    [Benchmark(Description = "TryAdd_Forward")]
    public void TryAdd_Forward()
    {
        for (var i = 0; i < ItemCount; i++)
        {
            _cache.TryAdd(1, i, _preLoadValues[i], ListCount, false, out _);
        }
    }

    /// <summary>
    /// Measures the cost of adding items in reverse (backward iteration triggers different eviction logic).
    /// </summary>
    [Benchmark(Description = "TryAdd_Reverse")]
    public void TryAdd_Reverse()
    {
        for (var i = ItemCount - 1; i >= 0; i--)
        {
            _cache.TryAdd(1, i, _preLoadValues[i], ListCount, true, out _);
        }
    }

    /// <summary>
    /// Measures TryAdd when the item already exists (duplicate add, should be fast-path).
    /// </summary>
    [Benchmark(Description = "TryAdd_Duplicate")]
    public void TryAdd_Duplicate()
    {
        // Seed the cache first.
        for (var i = 0; i < ItemCount; i++)
        {
            _cache.TryAdd(1, i, _preLoadValues[i], ListCount, false, out _);
        }

        // Re-add the exact same objects.
        for (var i = 0; i < ItemCount; i++)
        {
            _cache.TryAdd(1, i, _preLoadValues[i], ListCount, false, out _);
        }
    }

    // ---------------------------------------------------------------
    //  Contains benchmarks
    // ---------------------------------------------------------------

    /// <summary>
    /// Measures lookup by file path string on a populated cache.
    /// </summary>
    [Benchmark(Description = "Contains_String")]
    public void Contains_String()
    {
        SeedCache();
        for (var i = 0; i < ItemCount; i++)
        {
            _cache.Contains(_filePaths[i]);
        }
    }

    /// <summary>
    /// Measures lookup by FileInfo on a populated cache.
    /// </summary>
    [Benchmark(Description = "Contains_FileInfo")]
    public void Contains_FileInfo()
    {
        SeedCache();
        for (var i = 0; i < ItemCount; i++)
        {
            _cache.Contains(_fileInfos[i]);
        }
    }

    /// <summary>
    /// Measures lookup by PreLoadValue on a populated cache.
    /// </summary>
    [Benchmark(Description = "Contains_PreLoadValue")]
    public void Contains_PreLoadValue()
    {
        SeedCache();
        for (var i = 0; i < ItemCount; i++)
        {
            _cache.Contains(_preLoadValues[i]);
        }
    }

    /// <summary>
    /// Measures Contains for items that are NOT in the cache (miss path).
    /// </summary>
    [Benchmark(Description = "Contains_Miss")]
    public void Contains_Miss()
    {
        // Don't seed — cache is empty after IterationSetup.
        for (var i = 0; i < ItemCount; i++)
        {
            _cache.Contains(_filePaths[i]);
        }
    }

    // ---------------------------------------------------------------
    //  TryGet benchmarks
    // ---------------------------------------------------------------

    /// <summary>
    /// Measures TryGet by FileInfo (hit path).
    /// </summary>
    [Benchmark(Description = "TryGet_FileInfo")]
    public void TryGet_FileInfo()
    {
        SeedCache();
        for (var i = 0; i < ItemCount; i++)
        {
            _cache.TryGet(_fileInfos[i], out _);
        }
    }

    /// <summary>
    /// Measures TryGet using the ReadOnlySpan&lt;char&gt; alternate lookup (allocation-free path).
    /// </summary>
    [Benchmark(Description = "TryGet_Span")]
    public void TryGet_Span()
    {
        SeedCache();
        for (var i = 0; i < ItemCount; i++)
        {
            _cache.TryGet(_filePaths[i].AsSpan(), out _);
        }
    }

    /// <summary>
    /// Measures TryGet for items that are NOT in the cache (miss path).
    /// </summary>
    [Benchmark(Description = "TryGet_Miss")]
    public void TryGet_Miss()
    {
        // Don't seed — cache is empty.
        for (var i = 0; i < ItemCount; i++)
        {
            _cache.TryGet(_fileInfos[i], out _);
        }
    }

    // ---------------------------------------------------------------
    //  Multi-owner benchmarks
    // ---------------------------------------------------------------

    /// <summary>
    /// Measures TryAdd across multiple owners sharing the same images
    /// (exercises reference counting and path lookup contention).
    /// </summary>
    [Benchmark(Description = "TryAdd_MultiOwner")]
    public void TryAdd_MultiOwner()
    {
        const uint owner2 = 2;
        _cache.RegisterOwner(owner2);

        // Owner 1 adds all items.
        for (var i = 0; i < ItemCount; i++)
        {
            _cache.TryAdd(1, i, _preLoadValues[i], ListCount, false, out _);
        }

        // Owner 2 adds the same items (shared references).
        for (var i = 0; i < ItemCount; i++)
        {
            _cache.TryAdd(owner2, i, _preLoadValues[i], ListCount, false, out _);
        }

        // Cleanup owner 2 so the next iteration starts clean.
        _cache.Clear(owner2);
        _cache.RemoveOwner(owner2);
    }

    // ---------------------------------------------------------------
    //  Eviction-heavy benchmark
    // ---------------------------------------------------------------

    /// <summary>
    /// Measures cache behavior under pressure: adds many more items than the cache capacity
    /// to force continuous eviction through the EvictingDictionary.
    /// </summary>
    [Benchmark(Description = "TryAdd_EvictionPressure")]
    public void TryAdd_EvictionPressure()
    {
        // Use a small list count so the eviction window is tight,
        // forcing evictions on almost every add.
        const int tightListCount = 16;
        for (var i = 0; i < ItemCount; i++)
        {
            _cache.TryAdd(1, i, _preLoadValues[i], tightListCount, false, out _);
        }
    }

    // ---------------------------------------------------------------
    //  Clear benchmark
    // ---------------------------------------------------------------

    /// <summary>
    /// Measures the cost of clearing a fully populated owner cache.
    /// </summary>
    [Benchmark(Description = "Clear_Owner")]
    public void Clear_Owner()
    {
        SeedCache();
        _cache.Clear(1);
    }

    // ---------------------------------------------------------------
    //  Helper
    // ---------------------------------------------------------------

    private void SeedCache()
    {
        for (var i = 0; i < ItemCount; i++)
        {
            _cache.TryAdd(1, i, _preLoadValues[i], ListCount, false, out _);
        }
    }
}