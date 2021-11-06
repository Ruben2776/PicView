using ImageMagick;
using PicView.UILogic;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace PicView.ImageHandling
{
    internal static class SaveImages
    {
        internal static async Task<bool> SaveImageAsync(int rotate, bool flipped, BitmapSource? bitmapSource, string? path, string destination, Int32Rect? rect, bool hlsl)
        {
            MagickImage? magickImage = new();
            try
            {
                if (hlsl)
                {
                    await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, () =>
                    {
                        magickImage = ImageDecoder.GetRenderedMagickImage();
                    });

                }
                else if (bitmapSource is not null)
                {
                    var encoder = new PngBitmapEncoder();

                    await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, () =>
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
    }
}