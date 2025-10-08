using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using PicView.Avalonia.ViewModels;
using PicView.Core.ViewModels;
using R3;
using System.Drawing.Printing;
using System.Runtime.InteropServices;
using Avalonia.Platform;
using PicView.Avalonia.Printing;

namespace PicView.Avalonia.Win32.Printing
{
    public static class PrintEngine
    {
        private const float PreviewDpi = 96f;
        private const double MmPerInch = 25.4;

        // Entry point used by UI layer
        public static void RunPrintJob(PrintSettings settings, Bitmap avaloniaBmp)
        {
            if (settings.ColorMode.Value == (int)ColorModes.BlackAndWhite)
                avaloniaBmp = ToGrayScale(avaloniaBmp, PreviewDpi);

            using var gdiBitmap = BuildPrintableBitmap(settings, avaloniaBmp);
            Print(settings, gdiBitmap);
        }


        public static System.Drawing.Bitmap BuildPrintableBitmap(PrintSettings settings, Bitmap avaloniaBmp)
        {
            var paperInfo = ResolvePaper(settings.PrinterName.Value, settings.PaperSize.Value, (settings.Orientation.Value == (int)Orientations.Landscape));
            if (paperInfo is null)
                throw new InvalidOperationException("Invalid paper configuration.");

            int paperWidthPx = MmToPx(paperInfo.WidthMm, PreviewDpi);
            int paperHeightPx = MmToPx(paperInfo.HeightMm, PreviewDpi);

            var gdiBmp = new System.Drawing.Bitmap(paperWidthPx, paperHeightPx, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            using (var g = System.Drawing.Graphics.FromImage(gdiBmp))
            {
                g.Clear(System.Drawing.Color.White);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

                var contentRect = GetContentRect(settings, paperWidthPx, paperHeightPx);
                using var src = AvaloniaToGdi(avaloniaBmp);
                var destRect = ComputeDestRect(settings, src, contentRect);

                g.DrawImage(src, destRect);
            }

            return gdiBmp;
        }

        public static void Print(PrintSettings settings, System.Drawing.Bitmap image)
        {
            using var pd = new System.Drawing.Printing.PrintDocument
            {
                PrinterSettings = new System.Drawing.Printing.PrinterSettings
                {
                    PrinterName = settings.PrinterName.Value ?? string.Empty,
                    Copies = (short)settings.Copies.Value
                },
                PrintController = new StandardPrintController() // no dialog
            };

            var paperInfo = ResolvePaper(settings.PrinterName.Value, settings.PaperSize.Value, false);
            if (paperInfo?.DriverPaper is PaperSize paper)
                pd.DefaultPageSettings.PaperSize = paper;

            pd.DefaultPageSettings.Landscape = settings.Orientation.Value == (int)Orientations.Landscape;
            pd.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
            pd.DefaultPageSettings.Color = settings.ColorMode.Value != (int)ColorModes.BlackAndWhite;

            pd.PrintPage += (_, e) =>
            {
                e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                float dpiX = e.Graphics.DpiX;
                float dpiY = e.Graphics.DpiY;

                var page = e.PageBounds;                // 1/100 inch units
                const double mmToHi = 100.0 / 25.4;     // 1 mm = 3.937 hundredths inch

                // margins in hundredths of inch
                float ml = (float)(settings.MarginLeft.Value * mmToHi);
                float mr = (float)(settings.MarginRight.Value * mmToHi);
                float mt = (float)(settings.MarginTop.Value * mmToHi);
                float mb = (float)(settings.MarginBottom.Value * mmToHi);

                // drawable content area (full page coordinates)
                var contentRect = new System.Drawing.RectangleF(0, 0, page.Width, page.Height);

                var img = image;
                float imgW = img.Width * 100f / dpiX;
                float imgH = img.Height * 100f / dpiY;

                double sx = contentRect.Width / imgW;
                double sy = contentRect.Height / imgH;
                double s = settings.ScaleMode.Value switch
                {
                    (int)ScaleModes.Fill => Math.Max(sx, sy),
                    (int)ScaleModes.Fit => Math.Min(sx, sy),
                    (int)ScaleModes.Stretch => 0,
                    _ => 1.0
                };

                System.Drawing.RectangleF destRect =
                    settings.ScaleMode.Value == (int)ScaleModes.Stretch
                    ? contentRect
                    : new System.Drawing.RectangleF(
                        (float)((contentRect.Width - imgW * s) / 2),
                        (float)((contentRect.Height - imgH * s) / 2),
                        (float)(imgW * s),
                        (float)(imgH * s));

                e.Graphics.DrawImage(img, destRect);
                e.HasMorePages = false;
            };



            pd.Print();
        }


        // -----------------------------------------------------------
        //   Utility Methods
        // -----------------------------------------------------------

        private static System.Drawing.Rectangle GetContentRect(PrintSettings settings, int pageW, int pageH)
        {
            int ml = MmToPx(settings.MarginLeft.Value, PreviewDpi);
            int mr = MmToPx(settings.MarginRight.Value, PreviewDpi);
            int mt = MmToPx(settings.MarginTop.Value, PreviewDpi);
            int mb = MmToPx(settings.MarginBottom.Value, PreviewDpi);

            return new System.Drawing.Rectangle(
                ml, mt,
                pageW - (ml + mr),
                pageH - (mt + mb));
        }

        private static System.Drawing.Rectangle ComputeDestRect(PrintSettings settings, System.Drawing.Bitmap src, System.Drawing.Rectangle content)
        {
            double sx = (double)content.Width / src.Width;
            double sy = (double)content.Height / src.Height;

            double scale = settings.ScaleMode.Value switch
            {
                (int)ScaleModes.Fill => Math.Max(sx, sy),
                (int)ScaleModes.Fit => Math.Min(sx, sy),
                (int)ScaleModes.Stretch => 0,
                _ => 1.0
            };

            if (settings.ScaleMode.Value == (int)ScaleModes.Stretch)
                return content;

            if (settings.ScaleMode.Value == (int)ScaleModes.Center)
            {
                int dx = content.X + (content.Width - src.Width) / 2;
                int dy = content.Y + (content.Height - src.Height) / 2;
                return new System.Drawing.Rectangle(dx, dy, src.Width, src.Height);
            }

            int dw = (int)Math.Round(src.Width * scale);
            int dh = (int)Math.Round(src.Height * scale);
            int x = content.X + (content.Width - dw) / 2;
            int y = content.Y + (content.Height - dh) / 2;
            return new System.Drawing.Rectangle(x, y, dw, dh);
        }

        private static int MmToPx(double mm, float dpi) => (int)Math.Round(mm / MmPerInch * dpi);
        private static int MmToHundredths(double mm) => (int)Math.Round(mm * 100.0 / MmPerInch);
        private static int HundredthsToMm(double hdth) => (int)Math.Round(hdth / 100.0 * MmPerInch);

        public static PaperInfo? ResolvePaper(string printerName, string requestedName, bool landscape)
        {
            try
            {
                var ps = new PrinterSettings { PrinterName = printerName };
                foreach (PaperSize p in ps.PaperSizes)
                {
                    if (requestedName.StartsWith(p.PaperName, StringComparison.OrdinalIgnoreCase) ||
                        p.PaperName.StartsWith(requestedName, StringComparison.OrdinalIgnoreCase))
                    {
                        return new PaperInfo(p.PaperName,
                                HundredthsToMm((landscape ? p.Height : p.Width)),
                                HundredthsToMm((landscape ? p.Width : p.Height)),
                                p);
                    }
                }
            }
            catch { }
            return null;
        }

        public static IEnumerable<string> GetPaperSizes(string printerName)
        {
            var ps = new PrinterSettings { PrinterName = printerName };
            return ps.PaperSizes.Cast<PaperSize>().Select(p => p.PaperName);
        }

        private static System.Drawing.Bitmap AvaloniaToGdi(Bitmap avaloniaBmp)
        {
            int width = avaloniaBmp.PixelSize.Width;
            int height = avaloniaBmp.PixelSize.Height;
            int stride = width * 4;
            int bufferSize = stride * height;

            IntPtr bufferPtr = Marshal.AllocHGlobal(bufferSize);
            try
            {
                avaloniaBmp.CopyPixels(new PixelRect(0, 0, width, height), bufferPtr, bufferSize, stride);

                var data = new byte[bufferSize];
                Marshal.Copy(bufferPtr, data, 0, bufferSize);

                var bmp = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                var bmpData = bmp.LockBits(
                    new System.Drawing.Rectangle(0, 0, width, height),
                    System.Drawing.Imaging.ImageLockMode.WriteOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                Marshal.Copy(data, 0, bmpData.Scan0, data.Length);
                bmp.UnlockBits(bmpData);
                return bmp;
            }
            finally
            {
                if (bufferPtr != IntPtr.Zero)
                    Marshal.FreeHGlobal(bufferPtr);
            }
        }

        public static Bitmap ToGrayScale(Bitmap src, float dpi)
        {
            int width = src.PixelSize.Width;
            int height = src.PixelSize.Height;
            int stride = width * 4;
            int bufferSize = height * stride;

            IntPtr pixelBuffer = IntPtr.Zero;

            try
            {
                pixelBuffer = Marshal.AllocHGlobal(bufferSize);
                var rect = new PixelRect(0, 0, width, height);
                src.CopyPixels(rect, pixelBuffer, bufferSize, stride);

                var managed = new byte[bufferSize];
                Marshal.Copy(pixelBuffer, managed, 0, bufferSize);

                for (int i = 0; i < managed.Length; i += 4)
                {
                    byte gray = (byte)(0.2126 * managed[i + 2] +
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

                using (var fb = dst.Lock())
                    Marshal.Copy(managed, 0, fb.Address, managed.Length);

                return dst;
            }
            finally
            {
                if (pixelBuffer != IntPtr.Zero)
                    Marshal.FreeHGlobal(pixelBuffer);
            }
        }




        public static PrintLayout ComputeLayout(double pageWidthMm, double pageHeightMm, PrintSettings settings, double imageWidthPx, double imageHeightPx, double dpi)
        {
            double mmToPx = dpi / 25.4;
            double pageWpx = pageWidthMm * mmToPx;
            double pageHpx = pageHeightMm * mmToPx;

            // Margins
            double ml = settings.MarginLeft.Value * mmToPx;
            double mr = settings.MarginRight.Value * mmToPx;
            double mt = settings.MarginTop.Value * mmToPx;
            double mb = settings.MarginBottom.Value * mmToPx;

            double contentW = pageWpx - (ml + mr);
            double contentH = pageHpx - (mt + mb);

            // Scale
            double sX = contentW / imageWidthPx;
            double sY = contentH / imageHeightPx;
            double s = settings.ScaleMode.Value switch
            {
                (int)ScaleModes.Fill => Math.Max(sX, sY),
                (int)ScaleModes.Stretch => 1.0,
                _ => Math.Min(sX, sY)
            };

            double drawW = (settings.ScaleMode.Value == (int)ScaleModes.Stretch)
                ? contentW : imageWidthPx * s;
            double drawH = (settings.ScaleMode.Value == (int)ScaleModes.Stretch)
                ? contentH : imageHeightPx * s;
            double dx = ml + (contentW - drawW) / 2;
            double dy = mt + (contentH - drawH) / 2;

            return new PrintLayout(dx, dy, drawW, drawH, pageWpx, pageHpx, ml, mt, contentW, contentH);
        }



        public readonly struct PrintLayout
        {
            public readonly double DrawX;
            public readonly double DrawY;
            public readonly double DrawWidth;
            public readonly double DrawHeight;
            public readonly double PageWidthPx;
            public readonly double PageHeightPx;
            public readonly double ContentX;
            public readonly double ContentY;
            public readonly double ContentWidth;
            public readonly double ContentHeight;

            public PrintLayout(double dx, double dy, double dw, double dh, double pw, double ph, double cx, double cy, double cw, double ch)
            {
                DrawX = dx; DrawY = dy; DrawWidth = dw; DrawHeight = dh;
                PageWidthPx = pw; PageHeightPx = ph;
                ContentX = cx; ContentY = cy; ContentWidth = cw; ContentHeight = ch;
            }
        }


        public class PaperInfo
        {
            public string Name { get; }
            public double WidthMm { get; }
            public double HeightMm { get; }
            public PaperSize? DriverPaper { get; }
            public PaperInfo(string name, double widthMm, double heightMm, PaperSize? driverPaper)
            {
                Name = name;
                WidthMm = widthMm;
                HeightMm = heightMm;
                DriverPaper = driverPaper;
            }
        }
    }
}
