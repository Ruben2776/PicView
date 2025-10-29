using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using PicView.Avalonia.Views.UC;
//using PicView.Avalonia.Win32.Views;

namespace PicView.Avalonia.FileSystem;

public class ColorPickerToolManager
{
    private ColorPickerTool? _colorTool;
    private Control? _imageHost;
    private RenderTargetBitmap? _mainBitmap;

    public ColorPickerToolManager(Control imageHost, RenderTargetBitmap mainBitmap)
    {
        _imageHost = imageHost;
        _mainBitmap = mainBitmap;
    }

    public void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!isColorPicking || _colorTool is null || _imageHost is null)
            return;

        var pos = e.GetPosition(_imageHost);
        var bmp = CaptureRegionAsBitmap(_imageHost, pos, 10, 10);
        var color = GetColorAtPoint(_mainBitmap!, pos);

        _colorTool.UpdateImage(bmp);
        _colorTool.UpdateColor(color);

        // ✅ Replace Canvas positioning with a RenderTransform
        Dispatcher.UIThread.Post(() =>
        {
            _colorTool.RenderTransform = new TranslateTransform(pos.X + 20, pos.Y + 20);
        });
    }


    public void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!isColorPicking || _colorTool is null || _imageHost is null)
            return;

        var pos = e.GetPosition(_imageHost);
        var color = GetColorAtPoint(_mainBitmap, pos);

        isColorPicking = false;
        _colorTool.IsVisible = false;

        //new ColorPickerToolWindow(color).Show();
    }

    private static RenderTargetBitmap CaptureRegionAsBitmap(Control host, Point pos, int radiusX, int radiusY)
    {
        var bmp = new RenderTargetBitmap(new PixelSize(radiusX * 2, radiusY * 2));
        using (var ctx = bmp.CreateDrawingContext())
        {
            host.Render(ctx);
        }
        return bmp;
    }

    private static Color GetColorAtPoint(RenderTargetBitmap bmp, Point pos)
    {
        return Color.FromArgb(255, 255, 255, 255);

        var pixelRect = new PixelRect((int)pos.X, (int)pos.Y, 1, 1);
        var buffer = new byte[4];

        unsafe
        {
            fixed (byte* p = buffer)
            {
                bmp.CopyPixels(pixelRect, (IntPtr)p, 4, 0);
            }
        }

        // Avalonia uses BGRA byte order
        return Color.FromArgb(buffer[3], buffer[2], buffer[1], buffer[0]);
    }

    public bool isColorPicking { get; set; }

    public void AttachTool(ColorPickerTool tool)
    {
        _colorTool = tool;
    }

    public static (double H, double S, double B) ToHsb(Color color)
    {
        var r = color.R / 255.0;
        var g = color.G / 255.0;
        var b = color.B / 255.0;
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var diff = max - min;
        double h = 0;

        if (diff != 0)
        {
            if (max == r) h = (g - b) / diff % 6;
            else if (max == g) h = (b - r) / diff + 2;
            else h = (r - g) / diff + 4;
            h *= 60;
        }

        var s = max == 0 ? 0 : diff / max;
        return (h < 0 ? h + 360 : h, s, max);
    }

    public static Color FromHsb(double h, double s, double b)
    {
        var c = b * s;
        var x = c * (1 - Math.Abs((h / 60) % 2 - 1));
        var m = b - c;
        double r = 0, g = 0, bl = 0;

        if (h < 60) (r, g, bl) = (c, x, 0);
        else if (h < 120) (r, g, bl) = (x, c, 0);
        else if (h < 180) (r, g, bl) = (0, c, x);
        else if (h < 240) (r, g, bl) = (0, x, c);
        else if (h < 300) (r, g, bl) = (x, 0, c);
        else (r, g, bl) = (c, 0, x);

        return Color.FromRgb(
            (byte)((r + m) * 255),
            (byte)((g + m) * 255),
            (byte)((bl + m) * 255)
        );
    }
}
