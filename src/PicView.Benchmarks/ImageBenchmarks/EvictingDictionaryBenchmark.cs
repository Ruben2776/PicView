using BenchmarkDotNet.Attributes;
using ImageMagick;
using PicView.Core.FileHandling;
using PicView.Core.ImageReading;
using PicView.Core.Preloading;
using ZLinq;

namespace PicView.Benchmarks.ImageBenchmarks;

[MemoryDiagnoser] // track allocations
public class EvictingDictionaryBenchmark
{
    private List<FileInfo>? _fileInfos;
    private const int MaxSize = 12;
    private EvictingDictionary<MagickImage> _evictingDict = new(MaxSize);
    private List<MagickImage> _images;
    
    [GlobalSetup]
    public async Task Setup()
    {
        var picturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        _fileInfos = new DirectoryInfo(picturesPath)
            .DescendantsAndSelf()
            .OfType<FileInfo>()
            .Where(x => x.IsSupported())
            .Take(MaxSize * 3)
            .ToList();
        _images = [];
        await Parallel.ForEachAsync(_fileInfos, async (file, _) =>
        {
            try
            {
                _images.Add(await MagickPerformanceReader.ReadMagickImageWithSpanAsync(file));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Skipping {file.Name}: {ex.Message}");
            }
        });
    }
    
    [Benchmark]
    public void ReadAllImages()
    {
        for (var i = 0; i < _images.Count; i++)
        {
            _evictingDict.TryAdd(i, _images[i], _images.Count, false, out _, out _);
        }
    }
}