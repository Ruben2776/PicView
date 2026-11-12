using BenchmarkDotNet.Attributes;
using PicView.Core.Svg;

namespace PicView.Benchmarks.Svg;

/// <summary>
/// The SVG load path: read the text, then parse it into a drawing.
/// </summary>
[MemoryDiagnoser]
public class SvgLoadBenchmarks
{
    private string _directory = null!;
    private string _icon = null!;
    private string _cssExport = null!;
    private string _largeMap = null!;

    [Params("Icon", "CssExport", "LargeMap")]
    public string Svg { get; set; } = "";

    private string FilePath => Svg switch
    {
        "Icon" => _icon,
        "CssExport" => _cssExport,
        _ => _largeMap
    };

    [GlobalSetup]
    public void Setup()
    {
        _directory = Path.Combine(Path.GetTempPath(), "picview-svg-bench-" + Guid.NewGuid().ToString("N"));
        (_icon, _cssExport, _largeMap) = SvgBenchmarkFiles.WriteAll(_directory);
    }

    [GlobalCleanup]
    public void Cleanup() => Directory.Delete(_directory, true);

    [Benchmark]
    public async Task<SvgParseResult> Parse()
    {
        var text = await SvgFileReader.ReadAsync(FilePath).ConfigureAwait(false);
        return SvgParser.Parse(text);
    }
}
