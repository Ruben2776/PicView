using System.IO.Compression;
using System.Text;
using PicView.Core.Svg;

namespace PicView.Tests.Svg;

public class SvgFileReaderTests : IDisposable
{
    private const string Text = "<svg xmlns=\"http://www.w3.org/2000/svg\"/>";
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "svgreader-" + Guid.NewGuid().ToString("N"));

    public SvgFileReaderTests()
    {
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        Directory.Delete(_dir, true);
    }

    private string Write(string name, byte[] bytes)
    {
        var path = Path.Combine(_dir, name);
        File.WriteAllBytes(path, bytes);
        return path;
    }

    private static byte[] Gzip(byte[] bytes)
    {
        using var ms = new MemoryStream();
        using (var gz = new GZipStream(ms, CompressionLevel.Fastest))
        {
            gz.Write(bytes);
        }

        return ms.ToArray();
    }

    private static byte[] WithBom(byte[] bom, Encoding encoding) => [.. bom, .. encoding.GetBytes(Text)];

    [Fact]
    public async Task ReadAsync_PlainUtf8()
    {
        var path = Write("a.svg", Encoding.UTF8.GetBytes(Text));
        Assert.Equal(Text, await SvgFileReader.ReadAsync(path, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ReadAsync_Utf8Bom()
    {
        var path = Write("a.svg", WithBom([0xEF, 0xBB, 0xBF], new UTF8Encoding(false)));
        Assert.Equal(Text, await SvgFileReader.ReadAsync(path, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ReadAsync_Utf16LeBom()
    {
        var path = Write("a.svg", WithBom([0xFF, 0xFE], new UnicodeEncoding(false, false)));
        Assert.Equal(Text, await SvgFileReader.ReadAsync(path, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ReadAsync_Utf16BeBom()
    {
        var path = Write("a.svg", WithBom([0xFE, 0xFF], new UnicodeEncoding(true, false)));
        Assert.Equal(Text, await SvgFileReader.ReadAsync(path, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ReadAsync_GzippedNamedSvgz()
    {
        var path = Write("a.svgz", Gzip(Encoding.UTF8.GetBytes(Text)));
        Assert.Equal(Text, await SvgFileReader.ReadAsync(path, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ReadAsync_GzippedNamedSvg()
    {
        var path = Write("a.svg", Gzip(Encoding.UTF8.GetBytes(Text)));
        Assert.Equal(Text, await SvgFileReader.ReadAsync(path, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ReadAsync_PlainNamedSvgz()
    {
        var path = Write("a.svgz", Encoding.UTF8.GetBytes(Text));
        Assert.Equal(Text, await SvgFileReader.ReadAsync(path, TestContext.Current.CancellationToken));
    }
}

