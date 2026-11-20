using System.Diagnostics;
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
    public async Task ReadAsync_GzipLargerThanTheCapWhenDecompressed_ThrowsWithoutDecompressingItAll()
    {
        // 100 MB of zeros compress to about 100 KB.
        using var compressed = new MemoryStream();
        using (var gz = new GZipStream(compressed, CompressionLevel.Fastest, true))
        {
            var zeros = new byte[1024 * 1024];
            for (var i = 0; i < 100; i++)
            {
                gz.Write(zeros);
            }
        }

        var path = Write("bomb.svgz", compressed.ToArray());
        var before = GC.GetTotalAllocatedBytes(true);
        var watch = Stopwatch.StartNew();
        await Assert.ThrowsAsync<InvalidDataException>(() =>
            SvgFileReader.ReadAsync(path, 1024 * 1024, TestContext.Current.CancellationToken));
        watch.Stop();
        var allocated = GC.GetTotalAllocatedBytes(true) - before;

        Assert.True(allocated < 30 * 1024 * 1024, $"Allocated {allocated} bytes");
        Assert.True(watch.ElapsedMilliseconds < 5000, $"Took {watch.ElapsedMilliseconds} ms");
    }

    [Fact]
    public async Task ReadAsync_PlainFileLargerThanTheCap_Throws()
    {
        var path = Write("big.svg", new byte[2048]);
        await Assert.ThrowsAsync<InvalidDataException>(() =>
            SvgFileReader.ReadAsync(path, 1024, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ReadAsync_GzipAtTheCap_IsRead()
    {
        var text = new string('a', 1024);
        var path = Write("a.svgz", Gzip(Encoding.UTF8.GetBytes(text)));
        Assert.Equal(text, await SvgFileReader.ReadAsync(path, 1024, TestContext.Current.CancellationToken));
    }

    [Fact]
    public void MaxBytes_Is256MegaBytes()
    {
        Assert.Equal(256 * 1024 * 1024, SvgFileReader.MaxBytes);
    }

    [Fact]
    public async Task ReadAsync_PlainNamedSvgz()
    {
        var path = Write("a.svgz", Encoding.UTF8.GetBytes(Text));
        Assert.Equal(Text, await SvgFileReader.ReadAsync(path, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("<svg xmlns='http://www.w3.org/2000/svg'></svg>", true)]
    [InlineData("  \r\n<svg></svg>", true)]
    [InlineData("\uFEFF<svg></svg>", true)]
    [InlineData("<?xml version='1.0'?><svg></svg>", true)]
    [InlineData("<!DOCTYPE svg><!-- c --><SVG></SVG>", true)]
    [InlineData("<div><svg></svg></div>", false)]
    // ImageMagick draws the svg element inside this, so only the check of the root element keeps it out
    [InlineData("<div><p>Icon</p><svg xmlns='http://www.w3.org/2000/svg' width='10' height='10'><rect width='5' height='5'/></svg></div>", false)]
    [InlineData("<!DOCTYPE svg [<!ENTITY a \"b\">]><svg></svg>", true)]
    [InlineData("<!-- <svg> --><p/>", false)]
    [InlineData("<?xml version='1.0'?>", false)]
    [InlineData("<!-- unclosed <svg></svg>", false)]
    [InlineData("<svg/>", true)]
    [InlineData("https://example.com/a.svg", false)]
    [InlineData(@"C:\Pictures\<svg>.png", false)]
    [InlineData("<html><body></body></html>", false)]
    [InlineData("<svgfoo></svgfoo>", false)]
    [InlineData("<svg", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void LooksLikeSvg_Text_ReturnsExpected(string? text, bool expected)
    {
        Assert.Equal(expected, SvgFileReader.LooksLikeSvg(text));
    }

    [Fact]
    public void Read_EveryEncodingAndGzip_ReturnsText()
    {
        string[] paths =
        [
            Write("plain.svg", Encoding.UTF8.GetBytes(Text)),
            Write("utf8bom.svg", WithBom([0xEF, 0xBB, 0xBF], Encoding.UTF8)),
            Write("utf16le.svg", WithBom([0xFF, 0xFE], Encoding.Unicode)),
            Write("utf16be.svg", WithBom([0xFE, 0xFF], Encoding.BigEndianUnicode)),
            Write("gzip.svgz", Gzip(Encoding.UTF8.GetBytes(Text))),
            Write("gzipbom.svgz", Gzip(WithBom([0xFF, 0xFE], Encoding.Unicode)))
        ];

        foreach (var path in paths)
        {
            Assert.Equal(Text, SvgFileReader.Read(path));
        }
    }

    [Fact]
    public void Read_FileOverLimit_Throws()
    {
        var path = Write("big.svg", new byte[2048]);

        Assert.Throws<InvalidDataException>(() => SvgFileReader.Read(path, 1024));
    }

    [Fact]
    public void Read_DecompressedOverLimit_Throws()
    {
        var path = Write("bomb.svgz", Gzip(new byte[4096]));

        Assert.Throws<InvalidDataException>(() => SvgFileReader.Read(path, 1024));
    }
}
