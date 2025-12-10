using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.DebugTools;
using PicView.Core.Printing;

namespace PicView.Avalonia.Printing;

public static class PrintCore
{
    private const double MmPerInch = 25.4;

    public static PrintLayout ComputeLayout(
        double pageWidthMm,
        double pageHeightMm,
        PrintSettings settings,
        double imageWidthPx,
        double imageHeightPx,
        double dpi)
    {
        var mmToPx = dpi / MmPerInch;
        var pageWpx = pageWidthMm * mmToPx;
        var pageHpx = pageHeightMm * mmToPx;

        // Margins (mm -> px)
        var ml = settings.MarginLeft.Value * mmToPx;
        var mr = settings.MarginRight.Value * mmToPx;
        var mt = settings.MarginTop.Value * mmToPx;
        var mb = settings.MarginBottom.Value * mmToPx;

        var contentW = pageWpx - (ml + mr);
        var contentH = pageHpx - (mt + mb);

        // Scale
        var sX = contentW / imageWidthPx;
        var sY = contentH / imageHeightPx;
        var s = settings.ScaleMode.Value switch
        {
            (int)ScaleModes.Fill => Math.Max(sX, sY),
            (int)ScaleModes.Stretch => 1.0,
            _ => Math.Min(sX, sY) // Fit / default
        };

        var drawW = settings.ScaleMode.Value == (int)ScaleModes.Stretch
            ? contentW
            : imageWidthPx * s;
        var drawH = settings.ScaleMode.Value == (int)ScaleModes.Stretch
            ? contentH
            : imageHeightPx * s;

        var dx = ml + (contentW - drawW) / 2;
        var dy = mt + (contentH - drawH) / 2;

        return new PrintLayout(
            dx, dy, drawW, drawH,
            pageWpx, pageHpx,
            ml, mt, contentW, contentH);
    }

    public static Bitmap ToGrayScale(Bitmap src, float dpi)
    {
        ArgumentNullException.ThrowIfNull(src);

        int width, height;
        try
        {
            width = src.PixelSize.Width;
            height = src.PixelSize.Height;
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(PrintCore), nameof(ToGrayScale), e);
            var mainVm = Dispatcher.UIThread.Invoke(() => UIHelper.GetMainView.DataContext as MainViewModel);
            var cached = NavigationManager.GetPreLoadValueAsync(mainVm.PicViewer.FileInfo.Value).Result;
            src = cached.ImageModel.Image as Bitmap ?? throw new NullReferenceException();
            width = src.PixelSize.Width;
            height = src.PixelSize.Height;
        }
        var stride = width * 4;
        var bufferSize = height * stride;

        var pixelBuffer = IntPtr.Zero;

        try
        {
            pixelBuffer = Marshal.AllocHGlobal(bufferSize);
            var rect = new PixelRect(0, 0, width, height);
            src.CopyPixels(rect, pixelBuffer, bufferSize, stride);

            var managed = new byte[bufferSize];
            Marshal.Copy(pixelBuffer, managed, 0, bufferSize);

            for (var i = 0; i < managed.Length; i += 4)
            {
                var gray = (byte)(0.2126 * managed[i + 2] +
                                  0.7152 * managed[i + 1] +
                                  0.0722 * managed[i + 0]);
                managed[i + 0] = gray;
                managed[i + 1] = gray;
                managed[i + 2] = gray;
            }

            var dst = new WriteableBitmap(
                src.PixelSize,
                new Vector(dpi, dpi),
                PixelFormat.Bgra8888,
                AlphaFormat.Premul);

            using var fb = dst.Lock();
            Marshal.Copy(managed, 0, fb.Address, managed.Length);

            return dst;
        }
        finally
        {
            if (pixelBuffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(pixelBuffer);
            }
        }
    }
}

public readonly struct PrintLayout(
    double dx,
    double dy,
    double dw,
    double dh,
    double pw,
    double ph,
    double cx,
    double cy,
    double cw,
    double ch)
{
    public readonly double DrawX = dx;
    public readonly double DrawY = dy;
    public readonly double DrawWidth = dw;
    public readonly double DrawHeight = dh;
    public readonly double PageWidthPx = pw;
    public readonly double PageHeightPx = ph;
    public readonly double ContentX = cx;
    public readonly double ContentY = cy;
    public readonly double ContentWidth = cw;
    public readonly double ContentHeight = ch;
}
