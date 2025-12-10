using Avalonia.Media.Imaging;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.ViewModels;
using PicView.Core.FileHandling;
using PicView.Core.Http;
using PicView.Core.ImageDecoding;

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
    public static async Task<string> ConvertToCommonSupportedFormatAsync(string path, MainViewModel vm)
    {
        ArgumentNullException.ThrowIfNull(vm);

        Bitmap? source = null;

        // Primary case: Handle effect applied or empty path by saving current ImageSource
        if (vm.PicViewer.EffectConfig?.Value is not null || string.IsNullOrWhiteSpace(path))
        {
            if (vm.PicViewer.ImageSource.CurrentValue is Bitmap bmp)
            {
                source = bmp;
            }
        }
        else if (NavigationManager.CanNavigate(vm) && !string.IsNullOrEmpty(path))
        {
            // Handle effects for the current file
            if (vm.PicViewer.EffectConfig?.Value is not null && vm.PicViewer.FileInfo?.CurrentValue.FullName == path)
            {
                if (vm.PicViewer.ImageSource.CurrentValue is Bitmap bmp)
                {
                    source = bmp;
                }
            }
            // Current path that's already in common format
            else if (path == vm.PicViewer.FileInfo?.CurrentValue.FullName)
            {
                if (path.IsCommon())
                {
                    // No need to convert
                    return path;
                }

                if (vm.PicViewer.ImageSource.CurrentValue is Bitmap bmp)
                {
                    source = bmp;
                }
            }
            // Different path - try to get from preload
            else
            {
                var preloadValue = await NavigationManager.GetPreLoadValueAsync(new FileInfo(path)).ConfigureAwait(false);
                if (preloadValue?.ImageModel.Image is Bitmap bitmap)
                {
                    source = bitmap;
                }
            }
        }

        // If we have a valid source bitmap, save it to a temp file
        var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".png");
        if (source is not null)
        {
            await Task.Run(() => source.Save(tempPath));
            return tempPath;
        }

        // Handle URL paths if no source bitmap was found yet
        var url = path.GetURL();
        if (!string.IsNullOrWhiteSpace(url))
        {
            path = await HttpManager.DownloadFileAsync(url).ConfigureAwait(false);
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }
        }

        // Cleanup file:/// prefixes
        if (path?.StartsWith("file:///") == true)
        {
            path = path.Replace("file:///", "");
            path = path.Replace("%20", " ");
        }

        if (!File.Exists(path))
        {
            return string.Empty;
        }

        // Convert using SaveImageFileHelper as a fallback
        var success = await SaveImageFileHelper.SaveImageAsync(null, path, tempPath, null, null, null, ".png");
        return success ? tempPath : string.Empty;
    }
}