using BenchmarkDotNet.Attributes;
using PicView.Core.Models;
using PicView.Core.Navigation;

namespace PicView.Benchmarks.ImageBenchmarks;

[MemoryDiagnoser]
public class ImageCacheBenchmarks
{
    /// <summary>
    /// Number of items to operate on per benchmark iteration.
    /// </summary>
    private const int ItemCount = 64;

    private const uint Owner = 1;

    private SharedImageCache _cache = null!;
    private ImageModel[] _models = null!;
    private FileInfo[] _fileInfos = null!;

    [GlobalSetup]
    public void Setup()
    {
        SetDefaults();

        // Use a no-op image loader since benchmarks only test the cache structure,
        // not actual image decoding.
        _cache = new SharedImageCache(static f => new ValueTask<ImageModel>(new ImageModel { FileInfo = f }));

        _fileInfos = new FileInfo[ItemCount];
        _models = new ImageModel[ItemCount];
        for (var i = 0; i < ItemCount; i++)
        {
            _fileInfos[i] = new FileInfo($@"C:\bench\img_{i:D4}.jpg");
            _models[i] = new ImageModel { FileInfo = _fileInfos[i] };
        }
        _cache.SetFiles(Owner, _fileInfos);
    }

    [IterationSetup]
    public void IterationSetup()
    {
        // Each iteration starts with a clean cache so prior state does not skew results.
        _cache.ReleaseImages(Owner);
        _cache.ForceDisposalQueue();
    }

    /// <summary>
    /// Measures storing already loaded models in order, evicting as the window moves forward.
    /// </summary>
    [Benchmark(Description = "Put_Forward")]
    public void Put_Forward()
    {
        for (var i = 0; i < ItemCount; i++)
        {
            _cache.Put(Owner, _fileInfos[i], _models[i]);
        }
    }

    /// <summary>
    /// Measures requests for models that are already in memory.
    /// </summary>
    [Benchmark(Description = "GetAsync_Hit")]
    public async Task GetAsync_Hit()
    {
        SeedCache();
        for (var i = 0; i < ItemCount; i++)
        {
            await _cache.GetAsync(Owner, _fileInfos[i]);
        }
    }

    /// <summary>
    /// Measures the non-blocking lookup on a populated cache.
    /// </summary>
    [Benchmark(Description = "TryGetReady_Hit")]
    public void TryGetReady_Hit()
    {
        SeedCache();
        for (var i = 0; i < ItemCount; i++)
        {
            _cache.TryGetReady(_fileInfos[i], out _);
        }
    }

    /// <summary>
    /// Measures the non-blocking lookup for files not in memory.
    /// </summary>
    [Benchmark(Description = "TryGetReady_Miss")]
    public void TryGetReady_Miss()
    {
        for (var i = 0; i < ItemCount; i++)
        {
            _cache.TryGetReady(_fileInfos[i], out _);
        }
    }

    /// <summary>
    /// Measures a second owner taking references to images the first owner holds.
    /// </summary>
    [Benchmark(Description = "Put_MultiOwner")]
    public void Put_MultiOwner()
    {
        const uint owner2 = 2;
        _cache.SetFiles(owner2, _fileInfos);
        SeedCache();
        for (var i = 0; i < ItemCount; i++)
        {
            _cache.Put(owner2, _fileInfos[i], _models[i]);
        }
        _cache.ReleaseOwner(owner2);
    }

    /// <summary>
    /// Measures re-mapping held images when the file list is reordered.
    /// </summary>
    [Benchmark(Description = "SetFiles_Reordered")]
    public void SetFiles_Reordered()
    {
        SeedCache();
        _cache.SetFiles(Owner, _fileInfos.Reverse().ToArray());
        _cache.SetFiles(Owner, _fileInfos);
    }

    /// <summary>
    /// Measures releasing a fully populated owner.
    /// </summary>
    [Benchmark(Description = "ReleaseImages_Owner")]
    public void ReleaseImages_Owner()
    {
        SeedCache();
        _cache.ReleaseImages(Owner);
    }

    private void SeedCache()
    {
        for (var i = 0; i < ItemCount; i++)
        {
            _cache.Put(Owner, _fileInfos[i], _models[i]);
        }
    }
}
