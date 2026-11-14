using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PicView.Avalonia.CustomControls;

namespace PicView.Tests.UI;

/// <summary>
/// Comparison mode stacks the partner image on the current one and blends it in Difference (issue #367). The blend
/// must mix with the image drawn beneath it, not with an empty layer of its own.
/// </summary>
[Collection("Sequential")]
public class CompareBlendTests
{
    private const int Size = 20;
    private const uint Red = 0xFFFF0000;
    private const uint Black = 0xFF000000;

    public CompareBlendTests()
    {
        SetDefaults();
    }

    [AvaloniaFact]
    public void StackedPicBox_DifferenceBlend_MixesWithTheImageBeneath()
    {
        var (r, g, b) = RenderStacked(bottom: Red, top: Red);

        Assert.True(r <= 5 && g <= 5 && b <= 5, $"Expected black, got ({r}, {g}, {b})");
    }

    [AvaloniaFact]
    public void StackedPicBox_DifferenceOverBlack_ShowsTheTopImage()
    {
        var (r, g, b) = RenderStacked(bottom: Black, top: Red);

        Assert.True(r >= 250 && g <= 5 && b <= 5, $"Expected red, got ({r}, {g}, {b})");
    }

    private static (byte R, byte G, byte B) RenderStacked(uint bottom, uint top)
    {
        var grid = new Grid();
        grid.Children.Add(new PicBox { Source = Solid(bottom), Stretch = Stretch.Fill });
        grid.Children.Add(new PicBox
        {
            Source = Solid(top), Stretch = Stretch.Fill, BlendMode = BitmapBlendingMode.Difference
        });
        var window = new Window { Width = Size, Height = Size, Content = grid, Background = Brushes.Black };
        window.Show();

        using var frame = window.CaptureRenderedFrame();
        Assert.NotNull(frame);
        using var buffer = frame.Lock();
        var offset = Size / 2 * buffer.RowBytes + Size / 2 * 4;
        var pixel = (uint)Marshal.ReadInt32(buffer.Address, offset);
        var isRgba = buffer.Format == PixelFormat.Rgba8888;
        window.Close();

        // The low byte is red in Rgba8888 and blue in Bgra8888
        return isRgba
            ? ((byte)pixel, (byte)(pixel >> 8), (byte)(pixel >> 16))
            : ((byte)(pixel >> 16), (byte)(pixel >> 8), (byte)pixel);
    }

    private static WriteableBitmap Solid(uint argb)
    {
        var bitmap = new WriteableBitmap(new PixelSize(Size, Size), new Vector(96, 96), PixelFormat.Bgra8888,
            AlphaFormat.Premul);
        using var buffer = bitmap.Lock();
        for (var y = 0; y < Size; y++)
        {
            for (var x = 0; x < Size; x++)
            {
                Marshal.WriteInt32(buffer.Address, y * buffer.RowBytes + x * 4, unchecked((int)argb));
            }
        }

        return bitmap;
    }
}
