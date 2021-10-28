using ImageMagick;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace PicView.ImageHandling
{
    internal static class ImageSizeFunctions
    {
        internal static async Task<Size?> ImageSizeAsync(string file)
        {
            using var magick = new MagickImage();
            FileInfo? fileInfo = new FileInfo(file);
            if (fileInfo.Length > 2e+9)
            {
#if DEBUG
                Trace.WriteLine("File size bigger than 2gb");
#endif
                return null;
            }
            try
            {
                await magick.ReadAsync(fileInfo).ConfigureAwait(false);
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

        internal static async Task<bool> ResizeImageAsync(string file, int width, int height, Percentage? percentage = null) => await Task.Run(async () =>
        {
            if (string.IsNullOrWhiteSpace(file)) { return false; }
            if (File.Exists(file) == false) { return false; }
            if (width < 0 && percentage is not null || height < 0 && percentage is not null) { return false; }

            var magick = new MagickImage()
            {
                Quality = 100,
                ColorSpace = ColorSpace.Transparent
            };

            FileStream? filestream = null;
            try
            {
                filestream = new FileStream(file, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096, true);
            }
            catch (Exception e)
            {
#if DEBUG
                Trace.WriteLine($"{nameof(ResizeImageAsync)} exception caught \n {e.Message}");
#endif
                return false;
            }

            if (filestream is null) { return false; }

            try
            {
                magick.Read(filestream);
            }
            catch (MagickException e)
            {
#if DEBUG
                Trace.WriteLine($"{nameof(ResizeImageAsync)} exception caught \n {e.Message}");
#endif
                return false;
            }

            await filestream.DisposeAsync().ConfigureAwait(false);

            if (percentage is not null)
            {
                try
                {
                    magick.Resize(percentage.Value);
                }
                catch (MagickException e)
                {
#if DEBUG
                    Trace.WriteLine($"{nameof(ResizeImageAsync)} exception caught \n {e.Message}");
#endif
                    return false;
                }
            }
            else
            {
                try
                {
                    magick.Resize(width, height);
                }
                catch (MagickException e)
                {
#if DEBUG
                    Trace.WriteLine($"{nameof(ResizeImageAsync)} exception caught \n {e.Message}");
#endif
                    return false;
                }
            }

            try
            {
                await magick.WriteAsync(file).ConfigureAwait(false);
            }
            catch (Exception e)
            {
#if DEBUG
                Trace.WriteLine($"{nameof(ResizeImageAsync)} exception caught \n {e.Message}");
#endif
                return false;
            }

            magick.Dispose();
            return true;
        });
    }
}
