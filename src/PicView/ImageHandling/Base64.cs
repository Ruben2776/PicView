using ImageMagick;
using PicView.UILogic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace PicView.ImageHandling;

internal static class Base64
{
    /// <summary>
    /// Converts a Base64 string to a BitmapSource asynchronously.
    /// </summary>
    /// <param name="base64String">The Base64 string representing the image.</param>
    /// <returns>The BitmapSource created from the Base64 string, or null if conversion fails.</returns>
    internal static async Task<BitmapSource?> Base64StringToBitmapAsync(string base64String)
    {
        return await GetBitmapSourceFromBase64Async(base64String).ConfigureAwait(false);
    }

    /// <summary>
    /// Converts the contents of a file to a BitmapSource asynchronously.
    /// </summary>
    /// <param name="fileInfo">The file to read the Base64 string from.</param>
    /// <returns>The BitmapSource created from the Base64 string in the file, or null if conversion fails.</returns>
    internal static async Task<BitmapSource?> Base64StringToBitmapAsync(FileInfo fileInfo)
    {
        var base64String = await File.ReadAllTextAsync(fileInfo.FullName).ConfigureAwait(false);
        return await GetBitmapSourceFromBase64Async(base64String).ConfigureAwait(false);
    }

    /// <summary>
    /// Converts a Base64 string to a BitmapSource asynchronously.
    /// </summary>
    /// <param name="base64String">The Base64 string representing the image.</param>
    /// <returns>The BitmapSource created from the Base64 string, or null if conversion fails.</returns>
    private static async Task<BitmapSource?> GetBitmapSourceFromBase64Async(string base64String)
    {
        try
        {
            var base64Data = Convert.FromBase64String(base64String);
            using var magickImage = new MagickImage
            {
                Quality = 100,
                ColorSpace = ColorSpace.Transparent
            };

            var readSettings = new MagickReadSettings
            {
                Density = new Density(300, 300),
                BackgroundColor = MagickColors.Transparent
            };

            await magickImage.ReadAsync(new MemoryStream(base64Data), readSettings).ConfigureAwait(false);
            var bitmapSource = magickImage.ToBitmapSource();
            bitmapSource.Freeze();
            return bitmapSource;
        }
        catch (MagickException)
        {
            return null;
        }
    }

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
            frame = await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(ImageDecoder.GetRenderedBitmapFrame);
        }
        else
        {
            var bitmapSource = await ImageDecoder.ReturnBitmapSourceAsync(new FileInfo(path)).ConfigureAwait(false);
            var sourceSize = new Size(bitmapSource.PixelWidth, bitmapSource.PixelHeight);
            var rectangle = new Rectangle
            {
                Fill = new ImageBrush(bitmapSource),
            };
            rectangle.Measure(sourceSize);
            rectangle.Arrange(new Rect(sourceSize));

            var renderedBitmap = new RenderTargetBitmap(bitmapSource.PixelWidth, bitmapSource.PixelHeight, bitmapSource.DpiX, bitmapSource.DpiY, PixelFormats.Default);
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
                Tooltip.ShowTooltipMessage(e.Message);
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
        var base64String = await ConvertToBase64(path).ConfigureAwait(false); // Need to be true to avoid thread errors
        if (string.IsNullOrWhiteSpace(base64String))
        {
            Tooltip.ShowTooltipMessage(Application.Current.Resources["UnexpectedError"]);
            return;
        }

        await UC.GetPicGallery.Dispatcher.InvokeAsync(() =>
        {
            Clipboard.SetText(base64String);
        }, DispatcherPriority.Background);

        Tooltip.ShowTooltipMessage(Application.Current.Resources["ConvertedToBase64"]);
    }
}