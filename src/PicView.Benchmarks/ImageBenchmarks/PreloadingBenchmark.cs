using BenchmarkDotNet.Attributes;
using PicView.Avalonia.ImageHandling;
using PicView.Core.FileHandling;
using PicView.Core.Navigation;
using PicView.Core.Preloading;
using PicView.Core.DebugTools;
using ZLinq;

namespace PicView.Benchmarks.ImageBenchmarks;

[MemoryDiagnoser] // track allocations
public class PreloadingBenchmark
{
    private List<FileInfo>? _fileInfos;
    private const int MaxSize = 12;
    
    private Preloader? _preLoader;
    private readonly Lock _lock = new();
    private bool _isRunning;
    
    private SharedImageCache _sharedImageCache;

    [BenchmarkCancellation]
    public CancellationToken CancellationToken { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        LoadSettings();
        
        var picturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        _fileInfos = new DirectoryInfo(picturesPath)
            .DescendantsAndSelf()
            .OfType<FileInfo>()
            .Where(x => x.IsSupported())
            .Take(MaxSize * 6)
            .ToList();
        
        _preLoader = new Preloader(GetImageModel.GetImageModelAsync, _sharedImageCache = new SharedImageCache(GetImageModel.GetImageModelAsync));
    }
    
    [Benchmark]
    public async ValueTask Current()
    {
        uint id = 0;
        for (var i = 0; i < MaxSize; i++)
        {
            await _preLoader.PreLoadInternalAsync(id, i, _fileInfos, false, CancellationToken);
            await Task.Delay(200, CancellationToken); // Simulate human switching time
        }
    }
    
    [Benchmark]
    public async ValueTask ForLoop()
    {
        uint id = 0;
        for (var i = 0; i < MaxSize; i++)
        {
            await PreLoadForLoopAsync(id, i, _fileInfos, false, CancellationToken);
            await Task.Delay(200, CancellationToken); // Simulate human switching time
        }
    }
    
    
    public async Task PreLoadForLoopAsync(uint ownerId, int currentIndex, IReadOnlyList<FileInfo> list,
        bool reversed, CancellationToken token)
    {
        var count = list.Count;
        var nextStartingIndex = (currentIndex + 1) % count;
        var prevStartingIndex = (currentIndex - 1 + count) % count;

        try
        {
            if (reversed)
            {
                await LoopAsync(false).ConfigureAwait(false);
                await LoopAsync(true).ConfigureAwait(false);
            }
            else
            {
                await LoopAsync(true).ConfigureAwait(false);
                await LoopAsync(false).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(Preloader), nameof(PreLoadForLoopAsync), ex);
        }
        finally
        {
            lock (_lock)
            {
                _isRunning = false;
            }
        }

        return;

        async Task LoopAsync(bool positive)
        {
            if (positive)
            {
                for (int i = 0; i < PreLoaderConfig.PositiveIterations; i++)
                {
                    await AddAddition((nextStartingIndex + i) % count).ConfigureAwait(false);
                }
            }
            else
            {
                for (int i = 0; i < PreLoaderConfig.NegativeIterations; i++)
                {
                    await AddAddition((prevStartingIndex - i + count) % count).ConfigureAwait(false);
                }
            }
        }

        async Task AddAddition(int index)
        {
            token.ThrowIfCancellationRequested();
            // Double check cancellation after waiting
            if (token.IsCancellationRequested)
            {
                return;
            }

            if (_sharedImageCache.Contains(list[index]))
            {
                // Return early if cached
                return;
            }

            await _preLoader.AddAsync(ownerId, index, list, reversed, token).ConfigureAwait(false);
        }
    }
}

/*
 
 BenchmarkDotNet v0.15.2, Windows 10 (10.0.19045.6216/22H2/2022Update)
AMD Ryzen 7 9800X3D 4.70GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.100-preview.6.25358.103
  [Host]     : .NET 10.0.0 (10.0.25.35903), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 10.0.0 (10.0.25.35903), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  

| Method        | Mean     | Error   | StdDev  | Allocated |
|-------------- |---------:|--------:|--------:|----------:|
| PreloadImages | 127.1 ms | 2.44 ms | 2.39 ms | 488.67 KB |
with Parallel.ForAsync


| Method        | Mean     | Error   | StdDev  | Allocated |
|-------------- |---------:|--------:|--------:|----------:|
| PreloadImages | 133.8 ms | 2.00 ms | 1.67 ms | 475.34 KB |
with regular for loop


| Method        | Mean     | Error   | StdDev  | Allocated |
|-------------- |---------:|--------:|--------:|----------:|
| PreloadImages | 133.4 ms | 2.23 ms | 2.08 ms | 474.69 KB |
using void non-async for loop

*/