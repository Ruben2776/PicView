using System.Runtime.InteropServices;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using ImageMagick;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.ViewModels;
using PicView.Core.DebugTools;
using PicView.Core.ImageDecoding;
using PicView.Core.Localization;

namespace PicView.Avalonia.Clipboard;

/// <summary>
///     Handles clipboard operations related to images
/// </summary>
public static class ClipboardImageOperations
{
    /// <summary>
    ///     Copies the current image to the clipboard
    /// </summary>
    /// <param name="vm">The main view model</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public static async Task<bool> CopyImageToClipboard(MainViewModel vm)
    {
        var clipboard = ClipboardService.GetClipboard();
        if (clipboard == null || vm.PicViewer.ImageSource.CurrentValue is not Bitmap bitmap)
        {
            return false;
        }

        return await ClipboardService.ExecuteClipboardOperation(async () =>
        {
            await clipboard.ClearAsync();
            
            // Handle for Windows
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                await vm.PlatformService.CopyImageToClipboard(bitmap);
                return true;
            }

            // Handle for other platforms
            const string pngIdentifier = "image/png";
            const string jpegIdentifier = "image/jpeg";
            const string gifIdentifier = "image/gif";
            const string tiffIdentifier = "image/tiff";
            string identifier;
            switch (vm.PicViewer.Format.Value)
            {
                case MagickFormat.Tif:
                case MagickFormat.Tiff:
                case MagickFormat.Tiff64:
                    identifier = tiffIdentifier;
                    break;
                case MagickFormat.Gif:
                case MagickFormat.Gif87:
                    identifier = gifIdentifier;
                    break;
                case MagickFormat.Hdr:
                case MagickFormat.Heic:
                case MagickFormat.Heif:
                case MagickFormat.J2c:
                case MagickFormat.J2k:
                case MagickFormat.Jng:
                case MagickFormat.Jnx:
                case MagickFormat.Jp2:
                case MagickFormat.Jpc:
                case MagickFormat.Jpe:
                case MagickFormat.Jpeg:
                case MagickFormat.Jpg:
                case MagickFormat.Jpm:
                case MagickFormat.Jps:
                    identifier = jpegIdentifier;
                    break;
                case MagickFormat.Svg:
                case MagickFormat.Svgz:
                case MagickFormat.Text:
                case null:
                    return false;
                default:
                    identifier = pngIdentifier;
                    break;
            }

            using var ms = new MemoryStream();
            bitmap.Save(ms);
            var dataItem = DataTransferItem.Create(DataFormat.CreateBytesPlatformFormat(identifier), ms.ToArray());
            var dataTransfer = new DataTransfer();
            dataTransfer.Add(dataItem);
            await clipboard.SetDataAsync(dataTransfer);

            return true;
        });
    }

    /// <summary>
    ///     Copies an image as base64 string to the clipboard
    /// </summary>
    /// <param name="path">Optional path to the image file</param>
    /// <param name="vm">The main view model</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public static async Task<bool> CopyBase64ToClipboard(string path, MainViewModel vm)
    {
        var clipboard = ClipboardService.GetClipboard();
        if (clipboard == null)
        {
            return false;
        }

        return await ClipboardService.ExecuteClipboardOperation(async () =>
        {
            try
            {
                var base64 = await GetBase64String(path, vm);

                if (string.IsNullOrEmpty(base64))
                {
                    return false;
                }

                await clipboard.SetTextAsync(base64);
                return true;
            }
            catch (Exception ex)
            {
                DebugHelper.LogDebug(nameof(ClipboardImageOperations), nameof(CopyBase64ToClipboard), ex);
                return false;
            }
        });
    }

    private static async Task<string> GetBase64String(string path, MainViewModel vm)
    {
        if (!string.IsNullOrWhiteSpace(path))
        {
            return Convert.ToBase64String(await File.ReadAllBytesAsync(path));
        }

        switch (vm.PicViewer.ImageType.CurrentValue)
        {
            case ImageType.AnimatedGif:
            case ImageType.AnimatedWebp:
            case ImageType.Bitmap:
                if (vm.PicViewer.ImageSource.CurrentValue is not Bitmap bitmap)
                {
                    return string.Empty;
                }

                using (var stream = new MemoryStream())
                {
                    bitmap.Save(stream, 100);
                    return Convert.ToBase64String(stream.ToArray());
                }

            case ImageType.Svg:
                return string.Empty;

            default:
                throw new ArgumentOutOfRangeException(nameof(vm.PicViewer.ImageType),
                    $"Unsupported image type: {vm.PicViewer.ImageType}");
        }
    }

    /// <summary>
    ///     Pastes an image from the clipboard
    /// </summary>
    /// <param name="vm">The main view model</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public static async Task PasteClipboardImage(MainViewModel vm)
    {
        var clipboard = ClipboardService.GetClipboard();
        if (clipboard == null)
        {
            return;
        }

        try
        {
            var name = TranslationManager.Translation.ClipboardImage;

            // Try standard image formats
            var bitmap = await TryGetBitmapFromClipboard(clipboard);

            // Try Windows-specific clipboard handling if needed
            if (bitmap == null && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                bitmap = await vm.PlatformService.GetImageFromClipboard();
            }

            // Set the image if we got one
            if (bitmap != null)
            {
                await UpdateImage.SetSingleImageAsync(bitmap, ImageType.Bitmap, name, vm);
            }
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(ClipboardImageOperations), nameof(PasteClipboardImage), ex);
        }
    }

    private static async Task<Bitmap?> TryGetBitmapFromClipboard(IClipboard clipboard)
    {
        // List of formats to try
        var formats = new[]
        {
            "PNG", "image/jpeg", "image/png", "image/bmp", "BMP",
            "JPG", "JPEG", "image/tiff", "GIF", "image/gif"
        };

        foreach (var format in formats)
        {
            try
            {
                var data = await clipboard.GetDataAsync(format);
                if (data is not byte[] dataBytes)
                {
                    continue;
                }

                using var memoryStream = new MemoryStream(dataBytes);
                return new Bitmap(memoryStream);
            }
            catch (Exception ex)
            {
                // Ignore format errors and try next format
                DebugHelper.LogDebug(nameof(ClipboardImageOperations), nameof(TryGetBitmapFromClipboard), ex);
            }
        }

        return null;
    }
}