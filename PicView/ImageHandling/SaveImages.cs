using ImageMagick;
using PicView.UILogic;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace PicView.ImageHandling
{
    internal static class SaveImages
    {
        internal static async Task<bool> SaveImageAsync(double rotate, bool flipped, BitmapSource? bitmapSource, string? path, string destination, Int32Rect? rect, bool hlsl)
        {
            MagickImage? magickImage = new();
            try
            {
                if (hlsl)
                {
                    await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
                    {
                        magickImage = ImageDecoder.GetRenderedMagickImage();
                    });
                }
                else if (bitmapSource is not null)
                {
                    var encoder = new PngBitmapEncoder();

                    await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
                    {
                        encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                    });

                    using var stream = new MemoryStream();
                    encoder.Save(stream);
                    magickImage.Read(stream.ToArray());
                }
                else if (string.IsNullOrEmpty(path) == false)
                {
                    await magickImage.ReadAsync(path).ConfigureAwait(false);
                }
                else
                {
                    return false;
                }

                // Apply transformation values
                if (flipped && bitmapSource is not null && hlsl is false)
                {
                    magickImage.Flop();
                }

                if (rect is not null && bitmapSource is not null && hlsl is false)
                {
                    magickImage.Crop(new MagickGeometry(rect.Value.X, rect.Value.Y, rect.Value.Width, rect.Value.Height));
                }

                if (rotate != 0)
                {
                    magickImage.Rotate(rotate);
                }

                await magickImage.WriteAsync(destination).ConfigureAwait(false);
                magickImage.Dispose();
                await ImageFunctions.OptimizeImageAsync(destination).ConfigureAwait(false);
            }
            catch (Exception) { return false; }
            return true;
        }

        internal static async Task<bool> SaveImageAsync(Stream? stream, string? path, string? destination, int? width, int? height, int? quality, string? ext)
        {
            try
            {
                using MagickImage magickImage = new();

                if (stream is not null)
                {
                    await magickImage.ReadAsync(stream).ConfigureAwait(false);
                }
                else if (path is not null)
                {
                    await magickImage.ReadAsync(path).ConfigureAwait(false);
                }
                else { return false; }

                if (quality is not null)
                {
                    magickImage.Quality = quality.Value;
                }

                if (width is not null)
                {
                    magickImage.Resize(width.Value, 0);
                }
                else if (height is not null)
                {
                    magickImage.Resize(0, height.Value);
                }

                if (destination is not null)
                {
                    await magickImage.WriteAsync(ext is not null ? Path.ChangeExtension(destination, ext) : destination).ConfigureAwait(false);
                }
                else
                {
                    await magickImage.WriteAsync(ext is not null ? Path.ChangeExtension(path, ext) : path).ConfigureAwait(false);
                }
            }
            catch (Exception) 
            { 
                return false;
            }
            return true;
        }
    }
}