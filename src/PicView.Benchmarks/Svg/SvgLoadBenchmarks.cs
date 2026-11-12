using Avalonia.Svg.Skia;
using BenchmarkDotNet.Attributes;
using PicView.Avalonia.Svg;

namespace PicView.Benchmarks.Svg;

/// <summary>
/// The SVG load path as it is before the native parser: preprocess the text, then let Svg.Skia parse it.
/// </summary>
[MemoryDiagnoser]
public class SvgLoadBenchmarks
{
    private string _directory = null!;
    private string _icon = null!;
    private string _cssExport = null!;
    private string _largeMap = null!;

    [Params("Icon", "CssExport", "LargeMap")]
    public string File { get; set; } = "";

    private string Path => File switch
    {
        "Icon" => _icon,
        "CssExport" => _cssExport,
        _ => _largeMap
    };

    [GlobalSetup]
    public void Setup()
    {
        _directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "picview-svg-bench-" + Guid.NewGuid().ToString("N"));
        (_icon, _cssExport, _largeMap) = SvgBenchmarkFiles.WriteAll(_directory);
    }

    [GlobalCleanup]
    public void Cleanup() => Directory.Delete(_directory, true);

    [Benchmark]
    public async Task<SvgSource?> Legacy()
    {
        var text = await SvgLoader.GetContentFromSvgFileAsync(Path);
        return SvgSource.LoadFromSvg(text);
    }
}
