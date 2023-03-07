using ImageMagick;
using PicView.UILogic;
using System.IO;
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
        internal static async Task<BitmapSource?> Base64StringToBitmapAsync(string base64String)
        {
            return await GetBitmapSourceFromBase64Async(base64String).ConfigureAwait(false);
        }

        internal static async Task<BitmapSource?> Base64StringToBitmapAsync(FileInfo fileInfo)
        {
            var base64String = await File.ReadAllTextAsync(fileInfo.FullName).ConfigureAwait(false);
            return await GetBitmapSourceFromBase64Async(base64String).ConfigureAwait(false);
        }

        private static async Task<BitmapSource?> GetBitmapSourceFromBase64Async(string base64String)
        {
            var base64Data = Convert.FromBase64String(base64String);
            using var magickImage = new MagickImage()
            {
                Quality = 100,
                ColorSpace = ColorSpace.Transparent
            };

            var readSettings = new MagickReadSettings
            {
                Density = new Density(300, 300),
                BackgroundColor = MagickColors.Transparent
            };

            try
            {
                await magickImage.ReadAsync(new MemoryStream(base64Data), readSettings).ConfigureAwait(false);
            }
            catch (MagickException)
            {
                return null;
            }

            var bitmapSource = magickImage.ToBitmapSource();
            bitmapSource.Freeze();
            return bitmapSource;
        }

        internal static bool IsBase64String(string base64)
        {
            if (string.IsNullOrEmpty(base64))
            {
                return false;
            }
            Span<byte> buffer = new Span<byte>(new byte[base64.Length]);
            return Convert.TryFromBase64String(base64, buffer, out _);
        }

        internal static async Task<string> ConvertToBase64()
        {
            var frame = await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(ImageDecoder.GetRenderedBitmapFrame, DispatcherPriority.Background);
            if (frame == null) return string.Empty;

            using var ms = new MemoryStream();
            var pngBitmapEncoder = new PngBitmapEncoder();
            pngBitmapEncoder.Frames.Add(frame);
            pngBitmapEncoder.Save(ms);
            var bytes = ms.ToArray();
            return Convert.ToBase64String(bytes);
        }

        internal static async Task SendToClipboard()
        {
            var base64String = await ConvertToBase64().ConfigureAwait(true); // Need to be true to avoid thread errors
            if (string.IsNullOrWhiteSpace(base64String)) return;

            Clipboard.SetText(base64String);
            Tooltip.ShowTooltipMessage(Application.Current.Resources["ConvertedToBase64"]);
        }
    }
}