using ImageMagick;
using PicView.FileHandling;
using PicView.UILogic.Sizing;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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
        internal static BitmapSource RenderToBitmapSource(string file)
        {
            var check = SupportedFiles.IsSupportedFile(file);
            if (!check.HasValue) { return null; }

            try
            {

                using var filestream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.SequentialScan);

                if (check.Value)
                {
                    var sKBitmap = SKBitmap.Decode(filestream);

                    if (sKBitmap == null) { return null; }

                    var pic = sKBitmap.ToWriteableBitmap();
                    pic.Freeze();
                    sKBitmap.Dispose();


                    return pic;
                }
                else
                {
                    using MagickImage magick = new MagickImage();
                    var mrs = new MagickReadSettings()
                    {
                        Density = new Density(300, 300),
                        BackgroundColor = MagickColors.Transparent,
                    };

                    try
                    {
                        magick.Read(filestream, mrs);
                        filestream.Close();
                    }
                    catch (MagickException e)
                    {
#if DEBUG
                        Trace.WriteLine("GetMagickImage returned " + file + " null, \n" + e.Message);
#endif
                        return null;
                    }

                    // Set values for maximum quality
                    magick.Quality = 100;
                    magick.ColorSpace = ColorSpace.Transparent;

                    var pic = magick.ToBitmapSource();
                    pic.Freeze();
                    magick.Dispose();

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

            using MagickImage magick = new MagickImage
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
                var sauce = UILogic.Loading.LoadWindows.GetMainWindow.MainImage.Source as BitmapSource;
                var effect = UILogic.Loading.LoadWindows.GetMainWindow.MainImage.Effect;

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


                return BitmapFrame.Create(rtb);
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
            var w = ScaleImage.xWidth != 0 ? ScaleImage.xWidth : 300 * WindowLogic.MonitorInfo.DpiScaling;
            var h = ScaleImage.xHeight != 0 ? ScaleImage.xHeight : 300 * WindowLogic.MonitorInfo.DpiScaling;
            var rect = new Rect(new Size(w, h));
            var visual = new DrawingVisual();
            using (var ctx = visual.RenderOpen())
            {
                var typeface = new Typeface("/PicView;component/Themes/Resources/fonts/#Tex Gyre Heros");
                //text
                var text = new FormattedText("Unable to render image", CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, 16, Brushes.White, WindowLogic.MonitorInfo.DpiScaling)
                {
                    TextAlignment = System.Windows.TextAlignment.Center
                };

                ctx.DrawText(text, new Point(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2));
            }
            RenderTargetBitmap rtv = new RenderTargetBitmap((int)w, (int)h, 96.0, 96.0, PixelFormats.Default);
            rtv.Render(visual);
            rtv.Freeze();
            return rtv;
        }

    }      
}