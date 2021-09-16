using ImageMagick;
using PicView.UILogic;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace PicView.ImageHandling
{
    internal static class Base64
    {
        internal static Task<BitmapSource> Base64StringToBitmap(string base64String) => Task.Run(() =>
        {
            byte[] binaryData = Convert.FromBase64String(base64String);

            using MagickImage magick = new MagickImage();
            var mrs = new MagickReadSettings()
            {
                Density = new Density(300, 300),
                BackgroundColor = MagickColors.Transparent,
            };

            try
            {
                magick.Read(new MemoryStream(binaryData), mrs);
            }
#if DEBUG
            catch (MagickException e)
            {
                Trace.WriteLine("Base64StringToBitmap " + base64String + " null, \n" + e.Message);
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
        });

        internal static bool IsBase64String(string base64)
        {
            Span<byte> buffer = new Span<byte>(new byte[base64.Length]);
            return Convert.TryFromBase64String(base64, buffer, out _);
        }

        internal static async Task<string> ConvertToBase64()
        {
            BitmapFrame frame = null;
            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, (Action)(() =>
            {
                frame = ImageDecoder.GetRenderedBitmapFrame();
            }));

            PngBitmapEncoder pngBitmapEncoder = new();
            using var ms = new MemoryStream();
            pngBitmapEncoder.Frames.Add(frame);
            pngBitmapEncoder.Save(ms);
            var bytes = ms.ToArray();
            ms.Close();
            return Convert.ToBase64String(bytes);
        }

        internal static async Task SendToClipboard()
        {
            string path = await ConvertToBase64().ConfigureAwait(true); // Need to be true to avoid thread errors

            if (!string.IsNullOrWhiteSpace(path))
            {
                Clipboard.SetText(path);
                await Tooltip.ShowTooltipMessage(Application.Current.Resources["ConvertedToBase64"]).ConfigureAwait(false);
            }
        }
    }
}