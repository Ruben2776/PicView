using ImageMagick;
using PicView.ChangeImage;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using static PicView.ChangeImage.Navigation;
using static PicView.Library.Fields;

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
            using var filestream = new FileStream(file, FileMode.Open, FileAccess.Read);
            var ext = Path.GetExtension(file).ToLower(CultureInfo.CurrentCulture);
            switch (ext)
            {
                // Skia supports? https://docs.microsoft.com/en-us/dotnet/api/skiasharp.skimageencodeformat?view=skiasharp-1.59.3
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
                        using var memStream = new MemoryStream();
                        // Have to wait for it, or it will produce an unfinished image
                        await filestream.CopyToAsync(memStream).ConfigureAwait(true);
                        memStream.Seek(0, SeekOrigin.Begin);

                        var sKBitmap = SKBitmap.Decode(memStream);
                        if (sKBitmap == null) { return null; }

                        var pic = sKBitmap.ToWriteableBitmap();
                        pic.Freeze();
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
                        return pic;
                    }
            };
        }

        /// <summary>Gets the magick image.</summary>
        /// <param name="s">The stream</param>
        /// <returns></returns>
        internal static BitmapSource GetMagickImage(Stream s)
        {
            BitmapSource pic;

            using MagickImage magick = new MagickImage
            {
                Quality = 100
            };

            magick.Read(s);
            magick.ColorSpace = ColorSpace.Transparent;
            pic = magick.ToBitmapSource();

            pic.Freeze();
            return pic;
        }

        internal static Size? ImageSize(string file, bool usePreloader = false, bool advancedFormats = false)
        {
            if (usePreloader)
            {
                var pic = Preloader.Load(Pics[FolderIndex]);
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
                    if (!advancedFormats) { return null; }

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