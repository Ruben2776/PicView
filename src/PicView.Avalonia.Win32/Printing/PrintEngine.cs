using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PicView.Core.Printing;
using ZLinq;
using Bitmap = Avalonia.Media.Imaging.Bitmap;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace PicView.Avalonia.Win32.Printing;

public static class PrintEngine
{
    private const float PreviewDpi = 96f;
    private const double MmPerInch = 25.4;

    // Entry point used by UI layer
    public static void RunPrintJob(PrintSettings settings, Bitmap avaloniaBmp)
    {
        if (settings.ColorMode.Value == (int)ColorModes.BlackAndWhite)
        {
            avaloniaBmp = ToGrayScale(avaloniaBmp, PreviewDpi);
        }

        using var gdiBitmap = BuildPrintableBitmap(settings, avaloniaBmp);
        Print(settings, gdiBitmap);
    }


    private static System.Drawing.Bitmap BuildPrintableBitmap(PrintSettings settings, Bitmap avaloniaBmp)
    {
        var paperInfo = ResolvePaper(settings.PrinterName.Value, settings.PaperSize.Value,
            settings.Orientation.Value == (int)Orientations.Landscape);
        if (paperInfo is null)
        {
            throw new InvalidOperationException("Invalid paper configuration.");
        }

        var paperWidthPx = MmToPx(paperInfo.WidthMm, PreviewDpi);
        var paperHeightPx = MmToPx(paperInfo.HeightMm, PreviewDpi);

        var gdiBmp = new System.Drawing.Bitmap(paperWidthPx, paperHeightPx, PixelFormat.Format24bppRgb);

        using var g = Graphics.FromImage(gdiBmp);
        g.Clear(Color.White);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;

        var contentRect = GetContentRect(settings, paperWidthPx, paperHeightPx);
        using var src = AvaloniaToGdi(avaloniaBmp);
        var destRect = ComputeDestRect(settings, src, contentRect);

        g.DrawImage(src, destRect);

        return gdiBmp;
    }

    private static void Print(PrintSettings settings, System.Drawing.Bitmap image)
    {
        using var pd = new PrintDocument();
        pd.PrinterSettings = new PrinterSettings
        {
            PrinterName = settings.PrinterName.Value ?? string.Empty,
            Copies = (short)settings.Copies.Value
        };
        pd.PrintController = new StandardPrintController(); // no dialog

        var paperInfo = ResolvePaper(settings.PrinterName.Value, settings.PaperSize.Value, false);
        if (paperInfo?.DriverPaper is { } paper)
        {
            pd.DefaultPageSettings.PaperSize = paper;
        }

        pd.DefaultPageSettings.Landscape = settings.Orientation.Value == (int)Orientations.Landscape;
        pd.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
        pd.DefaultPageSettings.Color = settings.ColorMode.Value != (int)ColorModes.BlackAndWhite;

        pd.PrintPage += (_, e) =>
        {
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            var dpiX = e.Graphics.DpiX;
            var dpiY = e.Graphics.DpiY;

            var page = e.PageBounds; // 1/100 inch units
            // const double mmToHi = 100.0 / 25.4; // 1 mm = 3.937 hundredths inch

            // margins in hundredths of inch
            // var ml = (float)(settings.MarginLeft.Value * mmToHi);
            // var mr = (float)(settings.MarginRight.Value * mmToHi);
            // var mt = (float)(settings.MarginTop.Value * mmToHi);
            // var mb = (float)(settings.MarginBottom.Value * mmToHi);

            // drawable content area (full page coordinates)
            var contentRect = new RectangleF(0, 0, page.Width, page.Height);

            var imgW = image.Width * 100f / dpiX;
            var imgH = image.Height * 100f / dpiY;

            double scaleFactorX = contentRect.Width / imgW;
            double scaleFactorY = contentRect.Height / imgH;
            var s = settings.ScaleMode.Value switch
            {
                (int)ScaleModes.Fill => Math.Max(scaleFactorX, scaleFactorY),
                (int)ScaleModes.Fit => Math.Min(scaleFactorX, scaleFactorY),
                (int)ScaleModes.Stretch => 0,
                _ => 1.0
            };

            var destRect =
                settings.ScaleMode.Value == (int)ScaleModes.Stretch
                    ? contentRect
                    : new RectangleF(
                        (float)((contentRect.Width - imgW * s) / 2),
                        (float)((contentRect.Height - imgH * s) / 2),
                        (float)(imgW * s),
                        (float)(imgH * s));

            e.Graphics.DrawImage(image, destRect);
            e.HasMorePages = false;
        };


        pd.Print();
    }


    // -----------------------------------------------------------
    //   Utility Methods
    // -----------------------------------------------------------

    private static Rectangle GetContentRect(PrintSettings settings, int pageW, int pageH)
    {
        var ml = MmToPx(settings.MarginLeft.Value, PreviewDpi);
        var mr = MmToPx(settings.MarginRight.Value, PreviewDpi);
        var mt = MmToPx(settings.MarginTop.Value, PreviewDpi);
        var mb = MmToPx(settings.MarginBottom.Value, PreviewDpi);

        return new Rectangle(
            ml, mt,
            pageW - (ml + mr),
            pageH - (mt + mb));
    }

    private static Rectangle ComputeDestRect(PrintSettings settings, System.Drawing.Bitmap src, Rectangle content)
    {
        var sx = (double)content.Width / src.Width;
        var sy = (double)content.Height / src.Height;

        var scale = settings.ScaleMode.Value switch
        {
            (int)ScaleModes.Fill => Math.Max(sx, sy),
            (int)ScaleModes.Fit => Math.Min(sx, sy),
            (int)ScaleModes.Stretch => 0,
            _ => 1.0
        };

        switch (settings.ScaleMode.Value)
        {
            case (int)ScaleModes.Stretch:
                return content;
            case (int)ScaleModes.Center:
            {
                var dx = content.X + (content.Width - src.Width) / 2;
                var dy = content.Y + (content.Height - src.Height) / 2;
                return new Rectangle(dx, dy, src.Width, src.Height);
            }
        }

        var dw = (int)Math.Round(src.Width * scale);
        var dh = (int)Math.Round(src.Height * scale);
        var x = content.X + (content.Width - dw) / 2;
        var y = content.Y + (content.Height - dh) / 2;
        return new Rectangle(x, y, dw, dh);
    }

    private static int MmToPx(double mm, float dpi) => (int)Math.Round(mm / MmPerInch * dpi);

    //private static int MmToHundredths(double mm) => (int)Math.Round(mm * 100.0 / MmPerInch);
    private static int HundredthsToMm(double hdth) => (int)Math.Round(hdth / 100.0 * MmPerInch);

    public static PaperInfo? ResolvePaper(string printerName, string requestedName, bool landscape)
    {
        try
        {
            var ps = new PrinterSettings { PrinterName = printerName };
            foreach (var p in ps.PaperSizes.Cast<PaperSize>().AsValueEnumerable()
                         .Where(p => requestedName
                                         .StartsWith(p.PaperName, StringComparison.OrdinalIgnoreCase) ||
                                     p.PaperName.StartsWith(requestedName, StringComparison.OrdinalIgnoreCase)))
            {
                return new PaperInfo(p.PaperName,
                    HundredthsToMm(landscape ? p.Height : p.Width),
                    HundredthsToMm(landscape ? p.Width : p.Height),
                    p);
            }
        }
        catch
        {
            // ignored
        }

        return null;
    }

    public static IEnumerable<string> GetPaperSizes(string printerName)
    {
        var ps = new PrinterSettings { PrinterName = printerName };
        return ps.PaperSizes.Cast<PaperSize>().Select(p => p.PaperName);
    }

    private static System.Drawing.Bitmap AvaloniaToGdi(Bitmap avaloniaBmp)
    {
        var width = avaloniaBmp.PixelSize.Width;
        var height = avaloniaBmp.PixelSize.Height;
        var stride = width * 4;
        var bufferSize = stride * height;

        var bufferPtr = Marshal.AllocHGlobal(bufferSize);
        try
        {
            avaloniaBmp.CopyPixels(new PixelRect(0, 0, width, height), bufferPtr, bufferSize, stride);

            var data = new byte[bufferSize];
            Marshal.Copy(bufferPtr, data, 0, bufferSize);

            var bmp = new System.Drawing.Bitmap(width, height, PixelFormat.Format32bppArgb);
            var bmpData = bmp.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            Marshal.Copy(data, 0, bmpData.Scan0, data.Length);
            bmp.UnlockBits(bmpData);
            return bmp;
        }
        finally
        {
            if (bufferPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(bufferPtr);
            }
        }
    }

    public static Bitmap ToGrayScale(Bitmap src, float dpi)
    {
        var width = src.PixelSize.Width;
        var height = src.PixelSize.Height;
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
                global::Avalonia.Platform.PixelFormat.Bgra8888,
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


    public static PrintLayout ComputeLayout(double pageWidthMm, double pageHeightMm, PrintSettings settings,
        double imageWidthPx, double imageHeightPx, double dpi)
    {
        var mmToPx = dpi / 25.4;
        var pageWpx = pageWidthMm * mmToPx;
        var pageHpx = pageHeightMm * mmToPx;

        // Margins
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
            _ => Math.Min(sX, sY)
        };

        var drawW = settings.ScaleMode.Value == (int)ScaleModes.Stretch
            ? contentW
            : imageWidthPx * s;
        var drawH = settings.ScaleMode.Value == (int)ScaleModes.Stretch
            ? contentH
            : imageHeightPx * s;
        var dx = ml + (contentW - drawW) / 2;
        var dy = mt + (contentH - drawH) / 2;

        return new PrintLayout(dx, dy, drawW, drawH, pageWpx, pageHpx, ml, mt, contentW, contentH);
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


    public class PaperInfo(string name, double widthMm, double heightMm, PaperSize? driverPaper)
    {
        public string Name { get; } = name;
        public double WidthMm { get; } = widthMm;
        public double HeightMm { get; } = heightMm;
        public PaperSize? DriverPaper { get; } = driverPaper;
    }
}