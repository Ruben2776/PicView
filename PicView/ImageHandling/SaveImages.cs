using ImageMagick;
using PicView.UILogic;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using static PicView.UILogic.TransformImage.Rotation;

namespace PicView.ImageHandling
{
    internal static class SaveImages
    {
        internal static async Task<bool> TrySaveImage(int rotate, bool flipped, BitmapSource? bitmapSource, string? path, string destination, Int32Rect? rect, bool hlsl)
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

                    using (var stream = new MemoryStream())
                    {
                        encoder.Save(stream);
                        magickImage.Read(stream.ToArray());
                    }
                }
                else if (string.IsNullOrEmpty(path) == false)
                {
                    await magickImage.ReadAsync(path).ConfigureAwait(false);
                }
                else
                {
                    return false;
                }

                magickImage.Quality = 100;

                // Apply transformation values
                if (flipped && bitmapSource is not null && hlsl == false)
                {
                    magickImage.Flop();
                }

                if (rect is not null && bitmapSource is not null && hlsl == false)
                {
                    magickImage.Crop(new MagickGeometry(rect.Value.X, rect.Value.Y, rect.Value.Width, rect.Value.Height));
                }

                if (rotate != 0)
                {
                    magickImage.Rotate(rotate);
                }

                var filestream = new FileStream(destination, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.SequentialScan);
                await magickImage.WriteAsync(filestream).ConfigureAwait(false);
                magickImage.Dispose();
                await filestream.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception) { return false; }
            return true;
        }

        internal static void ResizeImageToFile(string file, int NewWidth, int NewHeight)
        {
            try
            {
                using (MagickImage magick = new MagickImage())
                {
                    MagickGeometry size = new MagickGeometry(NewWidth, NewHeight)
                    {
                        IgnoreAspectRatio = true
                    };
                    magick.Resize(size);
                    magick.Quality = 100;
                    magick.Write(file);
                }
            }
#if DEBUG
            catch (MagickException e)
            {
                Trace.WriteLine("ResizeImageToFile " + file + " null, \n" + e.Message);
                return;
            }
#else
                catch (MagickException) { return; }
#endif
        }
    }
}