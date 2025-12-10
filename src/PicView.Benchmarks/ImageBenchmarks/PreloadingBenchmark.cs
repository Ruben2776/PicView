using BenchmarkDotNet.Attributes;
using PicView.Avalonia.ImageHandling;
using PicView.Core.FileHandling;
using PicView.Core.Preloading;
using ZLinq;

namespace PicView.Benchmarks.ImageBenchmarks;

[MemoryDiagnoser] // track allocations
public class PreloadingBenchmark
{
    private List<FileInfo>? _fileInfos;
    private const int MaxSize = 12;
    
    private PreLoader? _preLoader;
    
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
        
        _preLoader = new PreLoader(GetImageModel.GetImageModelAsync);
    }
    
    [Benchmark]
    public async ValueTask PreloadImages()
    {
        for (var i = 0; i < MaxSize; i++)
        {
            await _preLoader.PreLoadAsync(i, false, _fileInfos)
                .ConfigureAwait(false);
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