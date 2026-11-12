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
        var watch = System.Diagnostics.Stopwatch.StartNew();
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
}

