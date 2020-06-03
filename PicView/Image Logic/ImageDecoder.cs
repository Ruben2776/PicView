using ImageMagick;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace PicView
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
            using (var filestream = new FileStream(file, FileMode.Open, FileAccess.Read))
            {
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
                            using (var memStream = new MemoryStream())
                            {
                                await filestream.CopyToAsync(memStream).ConfigureAwait(true);
                                memStream.Seek(0, SeekOrigin.Begin);

                                var sKBitmap = SKBitmap.Decode(memStream);
                                if (sKBitmap == null) { return null; }

                                var pic = sKBitmap.ToWriteableBitmap();
                                pic.Freeze();
                                return pic;
                            }

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
        }

        /// <summary>
        /// Decodes image from file to BitMapSource
        /// </summary>
        /// <param name="file">Full path of the file</param>
        /// <returns></returns>
        internal static BitmapSource RenderToBitmapSource(string file, int quality)
        {
            if (string.IsNullOrWhiteSpace(file) || file.Length < 2)
            {
                return null;
            }

            using (MagickImage magick = new MagickImage())
            {
                try
                {
                    magick.Read(file);
                }
#if DEBUG
                catch (MagickException e)
                {
                    Trace.WriteLine("GetMagickImage returned " + file + " null, \n" + e.Message);
                    return null;
                }
#else
                catch (MagickException) { return null; }
#endif
                magick.Quality = quality;

                var pic = magick.ToBitmapSource();
                pic.Freeze();
                return pic;
            }
        }

        internal static BitmapSource GetMagickImage(Stream s)
        {
            BitmapSource pic;

            using (MagickImage magick = new MagickImage())
            {
                magick.Quality = 100;
                //var mrs = new MagickReadSettings()
                //{
                //    Density = new Density(300)
                //};

                magick.Read(s);
                magick.ColorSpace = ColorSpace.Transparent;
                pic = magick.ToBitmapSource();

                pic.Freeze();
                return pic;
            }
        }

        /// <summary>
        /// Tries to save image to the specified destination,
        /// returns false if unsuccesful
        /// </summary>
        /// <param name="rotate">Degrees image is rotated by</param>
        /// <param name="flipped">Whether to flip image or not</param>
        /// <param name="path">The path of the source file</param>
        /// <param name="destination">Where to save image to</param>
        /// <returns></returns>
        internal static bool TrySaveImage(int rotate, bool flipped, string path, string destination)
        {
            if (File.Exists(path))
            {
                try
                {
                    using (var SaveImage = new MagickImage())
                    {
                        // Set maximum quality
                        var mrs = new MagickReadSettings()
                        {
                            Density = new Density(300, 300),
                        };
                        SaveImage.Quality = 100;

                        SaveImage.Read(path, mrs);

                        // Apply transformation values
                        if (flipped)
                        {
                            SaveImage.Flop();
                        }

                        SaveImage.Rotate(rotate);

                        SaveImage.Write(destination);
                    }
                }
                catch (Exception)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            return true;
        }

        internal static Size? ImageSize(string file)
        {
            using (var magick = new MagickImage())
            {
                var ext = Path.GetExtension(file).ToUpperInvariant();
                switch (ext)
                {
                    // Standards
                    case ".JPG":
                    case ".JPEG":
                    case ".JPE":
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

                    default: return null;
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
}