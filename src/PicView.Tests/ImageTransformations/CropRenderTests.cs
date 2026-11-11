using ImageMagick;
using PicView.Core.ImageTransformations;
using Orientation = PicView.Core.ImageTransformations.Orientation;

namespace PicView.Tests.ImageTransformations;

public class CropRenderTests
{
    private static MagickImage CreateFourByTwo()
    {
        var image = new MagickImage(MagickColors.Black, 4, 2);
        Paint(image, 0, 0, 255, 0, 0);
        Paint(image, 3, 0, 0, 255, 0);
        Paint(image, 0, 1, 0, 0, 255);
        Paint(image, 3, 1, 255, 255, 255);
        return image;
    }

    private static void Paint(MagickImage image, int x, int y, byte r, byte g, byte b)
    {
        var values = new byte[image.ChannelCount];
        values[0] = r;
        values[1] = g;
        values[2] = b;
        for (var i = 3; i < values.Length; i++)
        {
            values[i] = byte.MaxValue;
        }

        image.GetPixels().SetPixel(x, y, values);
    }

    private static string ColorAt(MagickImage image, int x, int y)
    {
        var values = image.GetPixels().GetPixel(x, y).ToArray();
        return $"{values[0] * 255 / Quantum.Max},{values[1] * 255 / Quantum.Max},{values[2] * 255 / Quantum.Max}";
    }

    private const string Red = "255,0,0";
    private const string Green = "0,255,0";
    private const string Blue = "0,0,255";
    private const string White = "255,255,255";

    [Fact]
    public void CutAndOrient_Upright_CutsTheArea()
    {
        using var image = CreateFourByTwo();

        CropRender.CutAndOrient(image, new PixelArea(0, 0, 4, 2), Orientation.Upright);

        Assert.Equal(4u, image.Width);
        Assert.Equal(2u, image.Height);
        Assert.Equal(Red, ColorAt(image, 0, 0));
    }

    [Fact]
    public void CutAndOrient_OneTurnRight_TurnsTheCut()
    {
        using var image = CreateFourByTwo();

        CropRender.CutAndOrient(image, new PixelArea(0, 0, 4, 2), new Orientation(1, false));

        Assert.Equal(2u, image.Width);
        Assert.Equal(4u, image.Height);
        Assert.Equal(Blue, ColorAt(image, 0, 0));
        Assert.Equal(Red, ColorAt(image, 1, 0));
    }

    [Fact]
    public void CutAndOrient_Flipped_MirrorsTheCut()
    {
        using var image = CreateFourByTwo();

        CropRender.CutAndOrient(image, new PixelArea(0, 0, 4, 2), new Orientation(0, true));

        Assert.Equal(Green, ColorAt(image, 0, 0));
    }

    [Fact]
    public void CutAndOrient_ACornerTurned_KeepsOnlyThatCorner()
    {
        using var image = CreateFourByTwo();

        CropRender.CutAndOrient(image, new PixelArea(3, 0, 1, 2), new Orientation(1, false));

        Assert.Equal(2u, image.Width);
        Assert.Equal(1u, image.Height);
        Assert.Equal(White, ColorAt(image, 0, 0));
        Assert.Equal(Green, ColorAt(image, 1, 0));
    }

    [Fact]
    public void CutAndOrient_AnExifTurnedImage_CutsTheAreaAsShown()
    {
        var path = Path.Combine(Path.GetTempPath(), $"picview-croprender-{Guid.NewGuid():N}.png");
        try
        {
            using (var source = CreateFourByTwo())
            {
                source.Orientation = ImageMagick.Orientation.RightTop;
                source.Write(path, MagickFormat.Png);
            }

            using var image = new MagickImage(path);
            Assert.Equal(ImageMagick.Orientation.RightTop, image.Orientation);

            CropRender.CutAndOrient(image, new PixelArea(1, 0, 1, 1), Orientation.Upright);

            Assert.Equal(1u, image.Width);
            Assert.Equal(1u, image.Height);
            Assert.Equal(Red, ColorAt(image, 0, 0));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
