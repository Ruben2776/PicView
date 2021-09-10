using ImageMagick;
using PicView.FileHandling;
using PicView.UILogic.Sizing;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PicView.ImageHandling
{
    internal static class ImageDecoder
    {
        /// <summary>
        /// Decodes image from file to BitMapSource
        /// </summary>
        /// <param name="file">Absolute path of the file</param>
        /// <returns></returns>
        internal static async Task<BitmapSource> RenderToBitmapSource(string file)
        {
            try
            {
                var ext = Path.GetExtension(file).ToUpperInvariant();
                FileStream filestream;
                switch (ext)
                {
                    case ".JPG":
                    case ".JPEG":
                    case ".JPE":
                    case ".PNG":
                    case ".BMP":
                    case ".ICO":
                    case ".JFIF":
                    case ".WEBP":
                    case ".WBMP":
                        filestream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.SequentialScan);
                        var sKBitmap = SKBitmap.Decode(filestream);
                        await filestream.DisposeAsync().ConfigureAwait(false);

                        if (sKBitmap == null)
                        {
                            return null; 
                        }

                        var skPic = sKBitmap.ToWriteableBitmap();
                        skPic.Freeze();
                        sKBitmap.Dispose();
                        return skPic;
                    // ".GIF": empty since XamlAnimatedGif dynamically loads it

                    case ".TIF":
                    case ".TIFF":
                    case ".DDS":
                    case "TGA": // TODO some tga files are created upside down https://github.com/Ruben2776/PicView/issues/22
                    case ".PSD":
                    case ".PSB":
                    case ".SVG":
                    case ".XCF":
                        filestream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.SequentialScan);
                        MagickImage magickImage = new();
                        magickImage.Read(filestream);
                        await filestream.DisposeAsync().ConfigureAwait(false);

                        magickImage.Quality = 100;
                        magickImage.ColorSpace = ColorSpace.Transparent;

                        var bitmap = magickImage.ToBitmapSource();
                        magickImage.Dispose();
                        bitmap.Freeze();

                        return bitmap;

                    default: // some formats cause exceptions when using filestream, so defaulting to reading from file
                        var magick = new MagickImage();
                        magick.Read(file);
                        magick.Quality = 100;

                        var pic = magick.ToBitmapSource();
                        magick.Dispose();
                        pic.Freeze();

                        return pic;
                }
            }
            catch (Exception e)
            {
#if DEBUG
                Trace.WriteLine("RenderToBitmapSource returned " + file + " null, \n" + e.Message);
#endif
                return null;
            }
        }

        /// <summary>Gets the magick image.</summary>
        /// <param name="s">The stream</param>
        /// <returns></returns>
        internal static BitmapSource GetMagickImage(byte[] bytes)
        {
            BitmapSource pic;

            using MagickImage magick = new()
            {
                Quality = 100
            };

            magick.Read(bytes);
            magick.ColorSpace = ColorSpace.Transparent;
            pic = magick.ToBitmapSource();

            pic.Freeze();
            return pic;
        }

        internal static MagickImage GetRenderedMagickImage()
        {
            try
            {
                var frame = BitmapFrame.Create(GetRenderedBitmapFrame());
                var encoder = new PngBitmapEncoder();

                encoder.Frames.Add(frame);

                var SaveImage = new MagickImage();
                using (var stream = new MemoryStream())
                {
                    encoder.Save(stream);
                    SaveImage.Read(stream.ToArray());
                }

                SaveImage.Quality = 100;

                // Apply transformation values
                if (UILogic.TransformImage.Rotation.Flipped)
                {
                    SaveImage.Flop();
                }

                SaveImage.Rotate(UILogic.TransformImage.Rotation.Rotateint);

                return SaveImage;
            }
            catch (Exception) { return null; }
        }

        internal static BitmapFrame GetRenderedBitmapFrame()
        {
            try
            {
                var sauce = UILogic.ConfigureWindows.GetMainWindow.MainImage.Source as BitmapSource;

                if (sauce == null)
                {
                    return null;
                }

                var effect = UILogic.ConfigureWindows.GetMainWindow.MainImage.Effect;

                var rectangle = new System.Windows.Shapes.Rectangle
                {
                    Fill = new ImageBrush(sauce),
                    Effect = effect
                };

                var sz = new Size(sauce.PixelWidth, sauce.PixelHeight);
                rectangle.Measure(sz);
                rectangle.Arrange(new Rect(sz));

                var rtb = new RenderTargetBitmap(sauce.PixelWidth, sauce.PixelHeight, sauce.DpiX, sauce.DpiY, PixelFormats.Default);
                rtb.Render(rectangle);

                BitmapFrame bitmapFrame = BitmapFrame.Create(rtb);
                bitmapFrame.Freeze();

                return bitmapFrame;
            }
            catch (Exception) { return null; }
        }

        internal static Size? ImageSize(string file)
        {
            var check = SupportedFiles.IsSupportedFile(file);
            if (!check.HasValue) { return null; }
            if (!check.Value) { return null; }

            using var magick = new MagickImage();

            try
            {
                magick.Read(file);
            }
#if DEBUG
            catch (MagickException e)
            {
                Trace.WriteLine("ImageSize returned " + file + " null, \n" + e.Message);
                return null;
            }
#else
                catch (MagickException) { return null; }
#endif

            return new Size(magick.Width, magick.Height);
        }

        internal static RenderTargetBitmap ImageErrorMessage()
        {
            var w = ScaleImage.XWidth != 0 ? ScaleImage.XWidth : 300 * WindowSizing.MonitorInfo.DpiScaling;
            var h = ScaleImage.XHeight != 0 ? ScaleImage.XHeight : 300 * WindowSizing.MonitorInfo.DpiScaling;
            var rect = new Rect(new Size(w, h));
            var visual = new DrawingVisual();
            using (var ctx = visual.RenderOpen())
            {
                var typeface = new Typeface("/PicView;component/Themes/Resources/fonts/#Tex Gyre Heros");
                //text
                var text = new FormattedText("Unable to render image", CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, 16, (Brush)Application.Current.Resources["MainColorBrush"], WindowSizing.MonitorInfo.DpiScaling)
                {
                    TextAlignment = System.Windows.TextAlignment.Center
                };

                ctx.DrawText(text, new Point(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2));
            }
            RenderTargetBitmap rtv = new((int)w, (int)h, 96.0, 96.0, PixelFormats.Default);
            rtv.Render(visual);
            rtv.Freeze();
            return rtv;
        }
    }
}