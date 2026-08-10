using BenchmarkDotNet.Attributes;
using ImageMagick;
using PicView.Core.FileHandling;
using PicView.Core.Localization;
using PicView.Core.Titles;
using ZLinq;

namespace PicView.Benchmarks.StringBenchmarks;

[MemoryDiagnoser] // track allocations
public class AspectRatioBenchmark
{
    private const int MaxSize = 12;
    private List<FileInfo>? _fileInfos;
    private List<DummySize>? _dummySizes;
    
    [GlobalSetup]
    public async Task Setup()
    {
        SetDefaults();
        await TranslationManager.LoadLanguage(Settings.UIProperties.UserLanguage);
        var picturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        _fileInfos = new DirectoryInfo(picturesPath)
            .DescendantsAndSelf()
            .OfType<FileInfo>()
            .Where(x => x.IsSupported())
            .Take(MaxSize * 6)
            .ToList();
        _dummySizes = [with(MaxSize)];
        for (var i = 0; i < MaxSize; i++)
        {
            using var magickImage = new MagickImage();
            await magickImage.PingAsync(_fileInfos[i]);
            _dummySizes.Add(new DummySize(magickImage.Width, magickImage.Height));
        }
    }
    
    [Benchmark]
    public void Initial()
    {
        for (var i = 0; i < MaxSize; i++)
        {
            var width = _dummySizes[i].Width;
            var height = _dummySizes[i].Height;
            var gcd = AspectRatioFormatter.GCD(width, height);
            GetFormattedAspectRatio(gcd, width, height);
        }
    }
    
    [Benchmark]
    public void DeferTranslationLookups()
    {
        for (var i = 0; i < MaxSize; i++)
        {
            var width = _dummySizes[i].Width;
            var height = _dummySizes[i].Height;
            var gcd = AspectRatioFormatter.GCD(width, height);
            DeferTranslationLookups(gcd, width, height);
        }
    }
    
    [Benchmark]
    public void GetFormattedAspectRatio_StackAlloc()
    {
        for (var i = 0; i < MaxSize; i++)
        {
            var width = _dummySizes[i].Width;
            var height = _dummySizes[i].Height;
            var gcd = AspectRatioFormatter.GCD(width, height);
            GetFormattedAspectRatio_StackAlloc(gcd, width, height);
        }
    }
    
    public static string GetFormattedAspectRatio(uint gcd, uint width, uint height)
    {
        var square = TranslationManager.Translation.Square;
        var landscape = TranslationManager.Translation.Landscape;
        var portrait = TranslationManager.Translation.Portrait;

        var firstRatio = width / gcd;
        var secondRatio = height / gcd;

        if (firstRatio == secondRatio)
        {
            return $"{firstRatio}:{secondRatio} ({square})";
        }

        return firstRatio > secondRatio
            ? $"{firstRatio}:{secondRatio} ({landscape})"
            : $"{firstRatio}:{secondRatio} ({portrait})";
    }
    
    public static string DeferTranslationLookups(uint gcd, uint width, uint height)
    {
        var firstRatio = width / gcd;
        var secondRatio = height / gcd;

        // 1. Defer translation lookups to avoid unnecessary property accesses
        var orientation = firstRatio == secondRatio 
            ? TranslationManager.Translation.Square 
            : firstRatio > secondRatio 
                ? TranslationManager.Translation.Landscape 
                : TranslationManager.Translation.Portrait;

        // 2. In .NET 11, the DefaultInterpolatedStringHandler makes this highly efficient.
        return $"{firstRatio}:{secondRatio} ({orientation})";
    }
    
    public static string GetFormattedAspectRatio_StackAlloc(uint gcd, uint width, uint height)
    {
        var firstRatio = width / gcd;
        var secondRatio = height / gcd;

        // Defer the translation lookup just like in DeferTranslationLookups
        var orientation = firstRatio == secondRatio 
            ? TranslationManager.Translation.Square 
            : firstRatio > secondRatio 
                ? TranslationManager.Translation.Landscape 
                : TranslationManager.Translation.Portrait;

        // 64 chars is more than enough for "9999:9999 (Landscape)"
        Span<char> buffer = stackalloc char[64];
        var charsWritten = 0;

        // Format the first ratio
        firstRatio.TryFormat(buffer, out var written);
        charsWritten += written;

        buffer[charsWritten++] = ':';

        // Format the second ratio
        secondRatio.TryFormat(buffer[charsWritten..], out written);
        charsWritten += written;

        buffer[charsWritten++] = ' ';
        buffer[charsWritten++] = '(';

        // Copy the orientation string directly into the span
        var orientationSpan = orientation.AsSpan();
        orientationSpan.CopyTo(buffer[charsWritten..]);
        charsWritten += orientationSpan.Length;

        buffer[charsWritten++] = ')';

        // This is the only allocation that will occur
        return new string(buffer[..charsWritten]);
    }

    private readonly record struct DummySize(uint Width, uint Height);
    
    /*
     
     * Summary *
                                                                                                                                                                                                                                                         
    BenchmarkDotNet v0.16.0-preview.1, Windows 10 (10.0.19045.6466/22H2/2022Update)
    AMD Ryzen 7 9800X3D 4.70GHz, 1 CPU, 16 logical and 8 physical cores                                                                                                                                                                                      
    Memory: 61.65 GB Total, 34.15 GB Available                                                                                                                                                                                                               
    .NET SDK 11.0.100-preview.6.26359.118                                                                                                                                                                                                                    
      [Host]     : .NET 11.0.0 (11.0.0-preview.6.26359.118, 11.0.26.36018), X64 RyuJIT x86-64-v4                                                                                                                                                             
      DefaultJob : .NET 11.0.0 (11.0.0-preview.6.26359.118, 11.0.26.36018), X64 RyuJIT x86-64-v4                                                                                                                                                             
                                                                                                                                                                                                                                                             

    | Method                             | Mean     | Error   | StdDev  | Gen0   | Allocated |
    |----------------------------------- |---------:|--------:|--------:|-------:|----------:|
    | Initial                            | 335.2 ns | 3.41 ns | 3.19 ns | 0.0257 |    1312 B |                                                                                                                                                               
    | DeferTranslationLookups            | 324.5 ns | 1.30 ns | 1.09 ns | 0.0257 |    1312 B |
    | GetFormattedAspectRatio_StackAlloc | 271.1 ns | 1.08 ns | 0.96 ns | 0.0143 |     736 B |
    
    
     */
}

