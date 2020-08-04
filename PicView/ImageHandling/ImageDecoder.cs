using ImageMagick;
using PicView.ChangeImage;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
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

        internal static Size? ImageSize(string file, bool usePreloader = false, bool advancedFormats = false)
        {
            if (usePreloader)
            {
                var pic = Preloader.Get(FolderIndex);
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

        internal static string TransformImage(string path,
                                            bool resize,
                                            int width,
                                            int height,
                                            bool aspectRatio,
                                            int rotation,
                                            int quality,
                                            bool optimize,
                                            bool flip,
                                            string name,
                                            string destination)
        {
            using MagickImage magick = new MagickImage
            {
                Quality = quality,
            };

            try
            {
                magick.Read(path);
            }
            catch (Exception e)
            {
                var errorMessage = "TransformImage caught exception " + Environment.NewLine + e.Message;
#if DEBUG
                Trace.WriteLine(errorMessage);
#endif
                return errorMessage;
            }

            if (rotation != 0)
            {
                try
                {
                    magick.Rotate(rotation);
                }
                catch (Exception e)
                {
                    var errorMessage = "TransformImage caught exception " + Environment.NewLine + e.Message;
#if DEBUG
                    Trace.WriteLine(errorMessage);
#endif
                    return errorMessage;
                }
            }

            if (resize)
            {
                var size = new MagickGeometry(width, height)
                {
                    IgnoreAspectRatio = !aspectRatio
                };
                magick.Resize(size);
            }

            if (optimize)
            {
                try
                {
                    magick.Strip();
                }
                catch (Exception e)
                {
                    var errorMessage = "TransformImage caught exception " + Environment.NewLine + e.Message;
#if DEBUG
                    Trace.WriteLine(errorMessage);
#endif
                    return errorMessage;
                }
            }

            if (flip)
            {
                try
                {
                    magick.Flop();
                }
                catch (Exception e)
                {
                    var errorMessage = "TransformImage caught exception " + Environment.NewLine + e.Message;
#if DEBUG
                    Trace.WriteLine(errorMessage);
#endif
                    return errorMessage;
                }
            }

            try
            {
                var result = destination + "\\" + Path.GetFileName(path);
                magick.Write(result);
                return "Written " + result;
            }
            catch (Exception e)
            {
                var errorMessage = "TransformImage caught exception " + Environment.NewLine + e.Message;
#if DEBUG
                Trace.WriteLine(errorMessage);
#endif
                return errorMessage;
            }
        }
    }
}