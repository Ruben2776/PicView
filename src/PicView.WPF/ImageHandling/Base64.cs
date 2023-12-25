using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using PicView.Core.Localization;
using PicView.WPF.UILogic;

namespace PicView.WPF.ImageHandling;

internal static class Base64
{
    /// <summary>
    /// Determines whether a string is a valid Base64 string.
    /// </summary>
    /// <param name="base64">The string to check.</param>
    /// <returns>True if the string is a valid Base64 string; otherwise, false.</returns>
    internal static bool IsBase64String(string base64)
    {
        if (string.IsNullOrEmpty(base64))
        {
            return false;
        }

        var buffer = new Span<byte>(new byte[base64.Length]);
        return Convert.TryFromBase64String(base64, buffer, out _);
    }

    /// <summary>
    /// Converts the current rendered bitmap frame to a Base64 string.
    /// </summary>
    /// <returns>A Task representing the asynchronous operation.</returns>
    private static async Task<string> ConvertToBase64(string? path = null)
    {
        BitmapFrame frame;
        if (path is null)
        {
            frame = await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(Image2BitmapSource
                .GetRenderedBitmapFrame);
        }
        else
        {
            var bitmapSource = await Image2BitmapSource.ReturnBitmapSourceAsync(new FileInfo(path)).ConfigureAwait(false);
            var sourceSize = new Size(bitmapSource.PixelWidth, bitmapSource.PixelHeight);
            var rectangle = new Rectangle
            {
                Fill = new ImageBrush(bitmapSource),
            };
            rectangle.Measure(sourceSize);
            rectangle.Arrange(new Rect(sourceSize));

            var renderedBitmap = new RenderTargetBitmap(bitmapSource.PixelWidth, bitmapSource.PixelHeight,
                bitmapSource.DpiX, bitmapSource.DpiY, PixelFormats.Default);
            renderedBitmap.Render(rectangle);

            frame = BitmapFrame.Create(renderedBitmap);
            frame.Freeze();
        }

        byte[]? bytes;
        var b64 = "";
        await Task.Run(() =>
        {
            try
            {
                using var ms = new MemoryStream();
                var pngBitmapEncoder = new PngBitmapEncoder();
                pngBitmapEncoder.Frames.Add(frame);
                pngBitmapEncoder.Save(ms);
                bytes = ms.ToArray();
                b64 = Convert.ToBase64String(bytes);
            }
            catch (Exception e)
            {
                Tooltip.ShowTooltipMessage(e.Message, true, TimeSpan.FromSeconds(5));
            }
        }).ConfigureAwait(false);

        return b64;
    }

    /// <summary>
    /// Converts the current rendered bitmap frame to a Base64 string and sets it to the clipboard.
    /// </summary>
    /// <returns>A Task representing the asynchronous operation.</returns>
    internal static async Task SendToClipboard(string? path = null)
    {
        var base64String = await ConvertToBase64(path).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(base64String))
        {
            Tooltip.ShowTooltipMessage(TranslationHelper.GetTranslation("UnexpectedError"));
            return;
        }

        await UC.GetPicGallery.Dispatcher.InvokeAsync(() => { Clipboard.SetText(base64String); },
            DispatcherPriority.Background);

        Tooltip.ShowTooltipMessage(TranslationHelper.GetTranslation("ConvertedToBase64"));
    }
}