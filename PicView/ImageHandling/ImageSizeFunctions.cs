using ImageMagick;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace PicView.ImageHandling
{
    internal static class ImageSizeFunctions
    {
        internal static Size? GetImageSize(string file)
        {
            if (!File.Exists(file))
            {
                return null;
            }

            using var magick = new MagickImage();
            try
            {
                magick.Ping(file);
                if (magick.Width <= 0)
                {
                    magick.Read(file);
                }
            }
#if DEBUG
            catch (MagickException e)
            {
                Trace.WriteLine("ImageSize returned " + file + " null\n" + e.Message);
                return null;
            }
#else
        catch (MagickException)
        {
            return null;
        }
#endif
            return new Size(magick.Width, magick.Height);
        }

        internal static async Task<bool> ResizeImageAsync(string file, int width, int height, int quality = 100, Percentage? percentage = null, string? destination = null, bool? compress = null, string? ext = null)
        {
            if (string.IsNullOrWhiteSpace(file)) { return false; }
            if (File.Exists(file) == false) { return false; }
            if (width < 0 && percentage is not null || height < 0 && percentage is not null) { return false; }

            var magick = new MagickImage
            {
                ColorSpace = ColorSpace.Transparent
            };

            if (quality > 0) // not inputting quality results in lower file size
            {
                magick.Quality = quality;
            }

            try
            {
                await magick.ReadAsync(file).ConfigureAwait(false);
            }
            catch (MagickException e)
            {
#if DEBUG
                Trace.WriteLine($"{nameof(ResizeImageAsync)} magic read exception caught \n {e.Message}");
#endif
                return false;
            }

            try
            {
                if (percentage is not null)
                {
                    magick.Resize(percentage.Value);
                }
                else
                {
                    magick.Resize(width, height);
                }

                if (destination is null)
                {
                    if (ext is not null)
                    {
                        Path.ChangeExtension(file, ext);
                    }
                    await magick.WriteAsync(file).ConfigureAwait(false);
                }
                else
                {
                    var dir = Path.GetDirectoryName(destination);
                    if (dir is null) { return false; }
                    if (Directory.Exists(dir) == false)
                    {
                        Directory.CreateDirectory(dir);
                    }
                    if (ext is not null)
                    {
                        Path.ChangeExtension(destination, ext);
                    }
                    await magick.WriteAsync(destination).ConfigureAwait(false);
                }
            }
            catch (MagickException e)
            {
#if DEBUG
                Trace.WriteLine($"{nameof(ResizeImageAsync)} exception caught \n {e.Message}");
#endif
                return false;
            }

            magick.Dispose();

            if (!compress.HasValue) return true;
            ImageOptimizer imageOptimizer = new()
            {
                OptimalCompression = compress.Value,
            };

            var x = destination ?? file;

            if (imageOptimizer.IsSupported(x) == false)
            {
                return true;
            }
            try
            {
                imageOptimizer.Compress(x);
            }
            catch (Exception)
            {
                return true;
            }

            return true;
        }
    }
}