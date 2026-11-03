using BenchmarkDotNet.Attributes;
using ImageMagick;
using PicView.Avalonia.ImageHandling;
using PicView.Core.FileHandling;
using PicView.Core.ImageReading;
using ZLinq;

namespace PicView.Benchmarks.Images;

[MemoryDiagnoser] // track allocations
public class ImageBenchmarks
{
    private List<FileInfo>? _fileInfos;
    private const int MaxSize = 200;
    private List<object> _images;
    
    [GlobalSetup]
    public void Setup()
    {
        var picturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        _fileInfos = new DirectoryInfo(picturesPath)
            .DescendantsAndSelf()
            .OfType<FileInfo>()
            .Where(x => x.IsSupported())
            .Take(MaxSize)
            .ToList();
        _images = [];
    }
    
    [Benchmark]
    public async ValueTask ReadWithReadMagickImageWithSpanAsyncMagick()
    {
        for (var i = 0; i < MaxSize; i++)
        {
            _images.Add(await MagickPerformanceReader.ReadMagickImageWithSpanAsync(_fileInfos[i]));
        }
    }
    
    [Benchmark]
    public async ValueTask ReadMagickWithFileStreamAsync()
    {
        for (var i = 0; i < MaxSize; i++)
        {
            _images.Add(await MagickPerformanceReader.ReadMagickWithFileStreamAsync(_fileInfos[i], new MagickImage()));
        }
    }
}