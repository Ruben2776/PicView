using Avalonia.Media.Imaging;
using ImageMagick;
using PicView.Core.DebugTools;
using PicView.Core.ImageDecoding;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.FileSystem;

/// <summary>
/// Saves what a tab shows, with its rotation and flip applied. It decides what the save source is: the image with
/// effects applied, the file on disk, or the in-memory bitmap of a single image (clipboard, base64).
/// </summary>
/// <param name="pathPicker">Asks the user where to save, for Save As.</param>
/// <param name="getEffects">The effects view model, if the effects window has been opened.</param>
public class FileSavingService(ISavePathPicker pathPicker, Func<EffectsViewModel?> getEffects)
{
    private const string PngExtension = ".png";
    private static readonly char[] InvalidFileNameChars = ['<', '>', ':', '"', '/', '\\', '|', '?', '*'];

    /// <summary>
    /// Saves the tab's image over its file, or asks where to save it when it is not a file.
    /// </summary>
    public async ValueTask<bool> SaveAsync(TabViewModel tab)
    {
        if (tab.FileInfo.CurrentValue is not { } file)
        {
            return await SaveAsAsync(tab).ConfigureAwait(false);
        }

        return await SaveToAsync(tab, file.FullName).ConfigureAwait(false);
    }

    /// <summary>
    /// Asks the user where to save the tab's image, then saves it there.
    /// </summary>
    public async ValueTask<bool> SaveAsAsync(TabViewModel tab)
    {
        var destination = await pathPicker.PickSavePathAsync(GetSuggestedFileName(tab, DateTime.Now))
            .ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(destination))
        {
            return false;
        }

        return await SaveToAsync(tab, destination).ConfigureAwait(false);
    }

    /// <summary>
    /// Saves the tab's image to <paramref name="destination"/>, converted to the format of its extension.
    /// </summary>
    public async ValueTask<bool> SaveToAsync(TabViewModel tab, string destination)
    {
        var angle = tab.RotationAngle.CurrentValue;
        var isFlipped = tab.ScaleX.CurrentValue is -1;
        var file = tab.FileInfo.CurrentValue;

        bool isSaved;
        try
        {
            if (getEffects()?.ProcessedImage is { } processed && tab.ImageType.CurrentValue is not ImageType.Svg)
            {
                isSaved = await SaveProcessedImageAsync(processed, destination, angle, isFlipped).ConfigureAwait(false);
            }
            else if (file is not null)
            {
                isSaved = await SaveImageFileHelper.SaveImageAsync(null, file.FullName, destination,
                    ext: Path.GetExtension(destination), rotationAngle: angle, flipImage: isFlipped).ConfigureAwait(false);
            }
            else if (tab.Image.CurrentValue is Bitmap bitmap)
            {
                isSaved = await SaveBitmapAsync(bitmap, destination, angle, isFlipped).ConfigureAwait(false);
            }
            else
            {
                return false;
            }
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(FileSavingService), nameof(SaveToAsync), e);
            return false;
        }

        if (isSaved && file is not null && file.FullName.Equals(destination, StringComparison.Ordinal))
        {
            tab.ImageIterator?.Cache.Invalidate(file.FullName);
        }

        return isSaved;
    }

    /// <summary>
    /// The file name to suggest when saving: the file's own name, or the tab title with the date for an image that is
    /// not a file.
    /// </summary>
    internal static string GetSuggestedFileName(TabViewModel tab, DateTime now)
    {
        if (tab.FileInfo.CurrentValue is { } file)
        {
            return file.Name;
        }

        var title = string.Concat((tab.TabTitle.CurrentValue ?? string.Empty)
            .Where(c => !char.IsControl(c) && Array.IndexOf(InvalidFileNameChars, c) < 0)).Trim();
        if (string.IsNullOrEmpty(title))
        {
            title = "PicView";
        }

        return $"{title} {now:yyyy-MM-dd HHmmss}{PngExtension}";
    }

    private static async ValueTask<bool> SaveProcessedImageAsync(MagickImage processed, string destination, int angle,
        bool isFlipped)
    {
        // Transform a copy, so saving twice does not rotate or flip the processed image twice
        using var image = (MagickImage)processed.Clone();
        if (angle is not 0)
        {
            image.Rotate(angle);
        }

        if (isFlipped)
        {
            image.Flop();
        }

        await image.WriteAsync(destination).ConfigureAwait(false);
        return true;
    }

    private static async ValueTask<bool> SaveBitmapAsync(Bitmap bitmap, string destination, int angle, bool isFlipped)
    {
        var ext = Path.GetExtension(destination);
        if (ext.Equals(PngExtension, StringComparison.OrdinalIgnoreCase) && angle is 0 && !isFlipped)
        {
            var fileStream = File.Create(destination);
            await using (fileStream.ConfigureAwait(false))
            {
                bitmap.Save(fileStream, PngBitmapEncoderOptions.Default);
            }

            return true;
        }

        // Convert with Magick to the destination's format, and to apply the rotation and flip
        using var pngStream = new MemoryStream();
        bitmap.Save(pngStream, PngBitmapEncoderOptions.Default);
        pngStream.Position = 0;
        return await SaveImageFileHelper.SaveImageAsync(pngStream, null, destination, ext: ext,
            rotationAngle: angle, flipImage: isFlipped).ConfigureAwait(false);
    }
}
