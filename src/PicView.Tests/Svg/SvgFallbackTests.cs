using Avalonia.Media.Imaging;
using PicView.Avalonia.Svg;

namespace PicView.Tests.Svg;

[Collection("Sequential")]
public class SvgFallbackTests : IDisposable
{
    private const string FilterAndTextSvg =
        "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"100\" height=\"50\" viewBox=\"0 0 100 50\">" +
        "<defs><filter id=\"b\"><feGaussianBlur stdDeviation=\"1\"/></filter></defs>" +
        "<rect x=\"10\" y=\"10\" width=\"30\" height=\"30\" fill=\"red\" filter=\"url(#b)\"/>" +
        "<text x=\"50\" y=\"30\" font-size=\"20\" fill=\"black\">Hi</text></svg>";

    private readonly List<string> _tempFiles = [];

    public void Dispose()
    {
        foreach (var file in _tempFiles)
        {
            File.Delete(file);
        }
    }

    [Theory]
    [InlineData(100u, 50u, 20.48)]
    [InlineData(3000u, 1000u, 1.0)]
    [InlineData(10000u, 100u, 0.8192)]
    public void Scale_ReachesMinLongSideWithinCap(uint w, uint h, double expected) =>
        Assert.Equal(expected, SvgFallback.Scale(w, h), 4);

    [AvaloniaFact]
    public void Render_FilterAndText_ShowsPixelsAtSvgSize()
    {
        using var bitmap = SvgFallback.Render(WriteTemp(FilterAndTextSvg), 100, 50);
        Assert.NotNull(bitmap);
        Assert.Equal(2048, bitmap.PixelSize.Width);
        Assert.Equal(100, bitmap.Size.Width, 1);
        Assert.Equal(50, bitmap.Size.Height, 1);
        Assert.True(HasVisiblePixel(bitmap));
        Assert.Equal(0, AlphaAt(bitmap, 0, 0));
    }

    [AvaloniaFact]
    public void Render_NotAnSvg_ReturnsNull() => Assert.Null(SvgFallback.Render(WriteTemp("not svg"), 10, 10));

    private FileInfo WriteTemp(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".svg");
        File.WriteAllText(path, content);
        _tempFiles.Add(path);
        return new FileInfo(path);
    }

    private static byte[] Pixels(Bitmap bitmap)
    {
        var size = bitmap.PixelSize;
        var stride = size.Width * 4;
        var buffer = new byte[stride * size.Height];
        var handle = System.Runtime.InteropServices.GCHandle.Alloc(buffer, System.Runtime.InteropServices.GCHandleType.Pinned);
        try
        {
            bitmap.CopyPixels(new global::Avalonia.PixelRect(0, 0, size.Width, size.Height),
                handle.AddrOfPinnedObject(), buffer.Length, stride);
        }
        finally
        {
            handle.Free();
        }

        return buffer;
    }

    private static bool HasVisiblePixel(Bitmap bitmap)
    {
        var pixels = Pixels(bitmap);
        for (var i = 3; i < pixels.Length; i += 4)
        {
            if (pixels[i] > 0)
            {
                return true;
            }
        }

        return false;
    }

    private static byte AlphaAt(Bitmap bitmap, int x, int y) => Pixels(bitmap)[(y * bitmap.PixelSize.Width + x) * 4 + 3];
}
