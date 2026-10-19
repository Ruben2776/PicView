using Avalonia.Media.Imaging;
using ImageMagick;
using PicView.Core.DebugTools;
using PicView.Core.FileHandling;
using PicView.Core.ImageDecoding;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.FileSystem;

/// <summary>
/// Saves what a tab shows, with its rotation and flip applied. It decides what the Save Source is: the Effects Render
/// of the tab's file, the file on disk, or the in-memory bitmap of a single image (clipboard, base64).
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
        using var effectsRender = CloneEffectsRender(tab);
        return await SaveToAsync(tab, destination, effectsRender).ConfigureAwait(false);
    }

    /// <summary>
    /// A file in a common format that shows the tab's image as a save would write it, such as for a wallpaper: the
    /// file itself when it is already common and untouched, else a temporary PNG. <see langword="null"/> when it
    /// could not be written.
    /// </summary>
    public async ValueTask<string?> ExportCommonFormatAsync(TabViewModel tab)
    {
        var file = tab.FileInfo.CurrentValue;
        using var effectsRender = CloneEffectsRender(tab);
        if (effectsRender is null && file is not null && file.IsCommon() && tab.RotationAngle.CurrentValue is 0 &&
            tab.ScaleX.CurrentValue is not -1)
        {
            return file.FullName;
        }

        var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + PngExtension);
        return await SaveToAsync(tab, tempPath, effectsRender).ConfigureAwait(false) ? tempPath : null;
    }

    private MagickImage? CloneEffectsRender(TabViewModel tab) =>
        tab.FileInfo.CurrentValue is { } file && tab.ImageType.CurrentValue is not ImageType.Svg
            ? getEffects()?.CloneRender(file)
            : null;

    /// <param name="effectsRender">A copy of the Effects Render of the tab's file, if it has one.</param>
    private async ValueTask<bool> SaveToAsync(TabViewModel tab, string destination, MagickImage? effectsRender)
    {
        var angle = tab.RotationAngle.CurrentValue;
        var isFlipped = tab.ScaleX.CurrentValue is -1;
        var file = tab.FileInfo.CurrentValue;

        bool isSaved;
        try
        {
            if (effectsRender is not null)
            {
                isSaved = await SaveEffectsRenderAsync(effectsRender, destination, angle, isFlipped).ConfigureAwait(false);
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

    /// <param name="effectsRender">A copy of the Effects Render, rotated and flipped in place.</param>
    private static async ValueTask<bool> SaveEffectsRenderAsync(MagickImage effectsRender, string destination,
        int angle, bool isFlipped)
    {
        if (angle is not 0)
        {
            effectsRender.Rotate(angle);
        }

        if (isFlipped)
        {
            effectsRender.Flop();
        }

        await effectsRender.WriteAsync(destination).ConfigureAwait(false);
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
