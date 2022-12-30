using ImageMagick;
using PicView.UILogic;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace PicView.ImageHandling
{
    internal static class Base64
    {
        /// <summary>
        /// Converts string from base64 value to BitmapSource
        /// </summary>
        /// <param name="base64String"></param>
        /// <returns></returns>
        internal static Task<BitmapSource?> Base64StringToBitmap(string base64String) => Task.Run(async () =>
        {
            byte[] binaryData = Convert.FromBase64String(base64String);
            return await Task.FromResult(Base64FromBytes(binaryData)).ConfigureAwait(false);
        });

        internal static Task<BitmapSource?> Base64StringToBitmap(FileInfo fileInfo) => Task.Run(async () =>
        {
            var text = await File.ReadAllTextAsync(fileInfo.FullName).ConfigureAwait(false);
            byte[] binaryData = Convert.FromBase64String(text);
            return await Task.FromResult(Base64FromBytes(binaryData)).ConfigureAwait(false);
        });

        static BitmapSource? Base64FromBytes(byte[] binaryData)
        {
            using MagickImage magick = new MagickImage();
            var mrs = new MagickReadSettings
            {
                Density = new Density(300, 300),
                BackgroundColor = MagickColors.Transparent,
            };

            try
            {
                magick.Read(new MemoryStream(binaryData), mrs);
            }
            catch (MagickException)
            {
                return null;
            }
            // Set values for maximum quality
            magick.Quality = 100;
            magick.ColorSpace = ColorSpace.Transparent;

            var pic = magick.ToBitmapSource();
            pic.Freeze();
            return pic;
        }

        internal static bool IsBase64String(string base64)
        {
            Span<byte> buffer = new Span<byte>(new byte[base64.Length]);
            return Convert.TryFromBase64String(base64, buffer, out _);
        }

        internal static async Task<string> ConvertToBase64()
        {
            BitmapFrame? frame = null;
            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
            {
                frame = ImageDecoder.GetRenderedBitmapFrame();
            });

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
                Tooltip.ShowTooltipMessage(Application.Current.Resources["ConvertedToBase64"]);
            }
        }
    }
}