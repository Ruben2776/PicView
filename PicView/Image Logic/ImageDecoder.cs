using ImageMagick;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace PicView
{
    internal static class ImageDecoder
    {
        /// <summary>
        /// Decodes image from file to BitMapSource
        /// </summary>
        /// <param name="file">Full path of the file</param>
        /// <returns></returns>
        internal static BitmapSource RenderToBitmapSource(string file)
        {
            if (string.IsNullOrWhiteSpace(file) || file.Length < 2)
                return null;

            using (MagickImage magick = new MagickImage())
            {
                var mrs = new MagickReadSettings()
                {
                    Density = new Density(300, 300),
                    BackgroundColor = MagickColors.Transparent,
                };

                try
                {
                    magick.Read(file, mrs);
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
                // Set values for maximum quality
                magick.Quality = 100;
                magick.ColorSpace = ColorSpace.Transparent;

                var pic = magick.ToBitmapSource();
                pic.Freeze();
                return pic;
            }
        }

        /// <summary>
        /// Decodes image from file to BitMapSource
        /// </summary>
        /// <param name="file">Full path of the file</param>
        /// <returns></returns>
        internal static BitmapSource RenderToBitmapSource(string file, int quality)
        {
            /// TODO find an alternmative faster way of decoding images on the fly

            if (string.IsNullOrWhiteSpace(file) || file.Length < 2)
                return null;

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
                var mrs = new MagickReadSettings()
                {
                    Density = new Density(300)
                };

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
                            SaveImage.Flop();
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

        internal static Size? ImageSize (string file)
        {
            var magick = new MagickImage();
            var ext = Path.GetExtension(file).ToLower();
            switch (ext)
            {
                // Standards
                case ".jpg":
                case ".jpeg":
                case ".jpe":
                    magick.Format = MagickFormat.Jpg;
                    break;
                case ".png":
                    magick.Format = MagickFormat.Png;
                    break;
                case ".bmp":
                    magick.Format = MagickFormat.Bmp;
                    break;
                case ".tif":
                case ".tiff":
                    magick.Format = MagickFormat.Tif;
                    break;
                case ".gif":
                    magick.Format = MagickFormat.Gif;
                    break;
                case ".ico":
                    magick.Format = MagickFormat.Ico;
                    break;
                default: return null;
            }

            using (magick)
            {
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
