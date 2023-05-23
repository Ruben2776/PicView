using ImageMagick;
using PicView.UILogic;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
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
    private static async Task<string> ConvertToBase64()
    {
        var frame = await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(ImageDecoder.GetRenderedBitmapFrame);
        if (frame == null) return string.Empty;

        byte[]? bytes = null;
        string b64 = "";
        await Task.Run(() =>
        {
            using var ms = new MemoryStream();
            var pngBitmapEncoder = new PngBitmapEncoder();
            pngBitmapEncoder.Frames.Add(frame);
            pngBitmapEncoder.Save(ms);
            bytes = ms.ToArray();
            b64 = Convert.ToBase64String(bytes);
        }).ConfigureAwait(false);

        return b64;
    }

    /// <summary>
    /// Converts the current rendered bitmap frame to a Base64 string and sets it to the clipboard.
    /// </summary>
    /// <returns>A Task representing the asynchronous operation.</returns>
    internal static async Task SendToClipboard()
    {
        var base64String = await ConvertToBase64().ConfigureAwait(true); // Need to be true to avoid thread errors
        if (string.IsNullOrWhiteSpace(base64String))
        {
            Tooltip.ShowTooltipMessage(Application.Current.Resources["UnexpectedError"]);
            return;
        }

        Clipboard.SetText(base64String);
        Tooltip.ShowTooltipMessage(Application.Current.Resources["ConvertedToBase64"]);
    }
}
