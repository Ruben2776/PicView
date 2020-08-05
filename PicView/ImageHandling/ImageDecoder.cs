using ImageMagick;
using PicView.ChangeImage;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static PicView.ChangeImage.Navigation;

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
            var ext = Path.GetExtension(file).ToLower(CultureInfo.CurrentCulture);
            switch (ext)
            {
                case ".jpg":
                case ".jpeg":
                case ".jpe":
                case ".png":
                case ".bmp":
                case ".tif":
                case ".tiff":
                case ".gif":
                case ".ico":
                case ".wdp":
                case ".jfif":
                case ".ktx":
                case ".webp":
                case ".wbmp":

                    try
                    {
                        using var filestream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.SequentialScan);

                        var sKBitmap = SKBitmap.Decode(filestream);

                        if (sKBitmap == null) { return null; }

                        var pic = sKBitmap.ToWriteableBitmap();
                        pic.Freeze();
                        sKBitmap.Dispose();
                        

                        return pic;
                    }
                    catch (Exception e)
                    {
#if DEBUG
                        Trace.WriteLine("RenderToBitmapSource exception: " + e.Message);
#endif
                        return null;
                    }

                default:

                    using (MagickImage magick = new MagickImage())
                    {
                        var mrs = new MagickReadSettings()
                        {
                            Density = new Density(300, 300),
                            BackgroundColor = MagickColors.Transparent,
                        };

                        try
                        {
                            using var filestream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan);
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
            };
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


                var frame = BitmapFrame.Create(rtb);
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

        internal static Size? ImageSize(string file, bool usePreloader = false, bool advancedFormats = false)
        {
            if (usePreloader)
            {
                var pic = Preloader.Get(Pics.IndexOf(file));
                if (pic != null)
                {
                    return new Size(pic.PixelWidth, pic.PixelHeight);
                }
            }

            using var magick = new MagickImage();
            var ext = Path.GetExtension(file).ToUpperInvariant();
            switch (ext)
            {
                // Standards
                case ".JPG":
                case ".JPEG":
                case ".JPE":
                case ".JFIF":
                    magick.Format = MagickFormat.Jpg;
                    break;

                case ".PNG":
                    magick.Format = MagickFormat.Png;
                    break;

                case ".BMP":
                    magick.Format = MagickFormat.Bmp;
                    break;

                case ".TIF":
                case ".TIFF":
                    magick.Format = MagickFormat.Tif;
                    break;

                case ".GIF":
                    magick.Format = MagickFormat.Gif;
                    break;

                case ".ICO":
                    magick.Format = MagickFormat.Ico;
                    break;

                default:
                    if (!advancedFormats)
                    {
                        // don't read advanced formats
                        return null;
                    }

                    break;
            }

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

    }      
}