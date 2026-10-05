using Avalonia;
using Avalonia.Media.Imaging;
using PicView.Core.FileHandling;
using PicView.Core.Http;
using PicView.Core.ImageDecoding;
using PicView.Core.Preloading;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.ImageHandling;

/// <summary>
///     Handles conversion of image files to common supported formats.
/// </summary>
public static class ImageFormatConverter
{
    /// <summary>
    ///     Converts an image to a commonly supported format
    /// </summary>
    /// <param name="path">Path to the image file</param>
    /// <param name="vm">MainViewModel instance</param>
    /// <returns>Path to the converted image file, or empty string if conversion failed</returns>
    /// <remarks>Saves the image to a temporary file and returns its path, if an effect is applied, or if the path is empty</remarks>
    public static async Task<string> ConvertToCommonSupportedFormatAsync(string path, MainWindowViewModel vm)
    {
        ArgumentNullException.ThrowIfNull(vm);

        Bitmap? source = null;

        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return string.Empty;
        }
        var tab = vm.WindowTabs.ActiveTab.CurrentValue;

        // Primary case: Handle effect applied or empty path by saving current ImageSource
        if (core.Effects?.ProcessedImage is not null || string.IsNullOrWhiteSpace(path))
        {
            if (tab.Image.CurrentValue is Bitmap bmp)
            {
                source = bmp;
            }
        }
        else if (tab.ImageIterator.Files.Count <= 0 && !string.IsNullOrEmpty(path))
        {
            // Handle effects for the current file
            if (core.Effects?.EffectConfig?.Value is not null && string.Equals(tab.FileInfo?.CurrentValue.FullName, path, StringComparison.Ordinal))
            {
                if (tab.Image.CurrentValue is Bitmap bmp)
                {
                    source = bmp;
                }
            }
            // Current path that's already in common format
            else if (string.Equals(path, tab.FileInfo?.CurrentValue.FullName, StringComparison.Ordinal))
            {
                if (path.IsCommon())
                {
                    // No need to convert
                    return path;
                }

                if (tab.Image.CurrentValue is Bitmap bmp)
                {
                    source = bmp;
                }
            }
            // Different path - try to get from preload
            else
            {
                var file = new FileInfo(path);
                if (core.SharedCache.TryGet(file, out var value))
                {
                    source = value.ImageModel.Image as Bitmap;
                }
                else
                {
                    await core.SharedCache.LoadAsync(tab.Id, tab.ImageIterator.CurrentIndex, tab.ImageIterator.Files, tab.GetTabCancellation().Token).ConfigureAwait(false);
                    core.SharedCache.TryGet(file, out var preloadValue);
                    source = preloadValue?.ImageModel.Image as Bitmap;
                }
            }
        }

        // If we have a valid source bitmap, save it to a temp file
        var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".png");
        if (source is not null)
        {
            await Task.Run(() => source.Save(tempPath, PngBitmapEncoderOptions.Default)).ConfigureAwait(false);
            return tempPath;
        }

        // Cleanup file:/// prefixes
        if (path?.StartsWith("file:///", StringComparison.Ordinal) == true)
        {
            path = path.Replace("file:///", "");
            path = path.Replace("%20", " ");
        }

        if (!File.Exists(path))
        {
            return string.Empty;
        }

        // Convert using SaveImageFileHelper as a fallback
        var success = await SaveImageFileHelper.SaveImageAsync(null, path, tempPath, null, null, null, ".png").ConfigureAwait(false);
        return success ? tempPath : string.Empty;
    }
}