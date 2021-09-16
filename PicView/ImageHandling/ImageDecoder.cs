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
                        using (var imgStream = new SKManagedStream(filestream))
                        {
                            using var skData = SKData.Create(filestream);
                            await filestream.DisposeAsync().ConfigureAwait(false);
                            using var codec = SKCodec.Create(skData);
                            var sKBitmap = SKBitmap.Decode(codec);

                            if (sKBitmap == null)
                            {
                                return null;
                            }

                            var skPic = sKBitmap.ToWriteableBitmap();
                            skPic.Freeze();
                            sKBitmap.Dispose();
                            return skPic;
                        }

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
                        MagickImage magickImage = new()
                        {
                            Quality = 100,
                            ColorSpace = ColorSpace.Transparent
                        };
                        magickImage.Read(filestream); // Reading it async causes slowdown
                        await filestream.DisposeAsync().ConfigureAwait(false);

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


    }
}