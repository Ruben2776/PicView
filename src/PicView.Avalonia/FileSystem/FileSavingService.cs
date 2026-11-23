using PicView.Core.FileHandling;
using PicView.Core.Saving;
using PicView.Core.ViewModels;
using Orientation = PicView.Core.ImageTransformations.Orientation;

namespace PicView.Avalonia.FileSystem;

/// <summary>
/// Saves what a tab shows, with its rotation and flip applied, through the save module, which decides what the Save
/// Source is: the Effects Render of the tab's file, the file on disk, or the in-memory bitmap of a single image.
/// </summary>
/// <param name="pathPicker">Asks the user where to save, for Save As.</param>
/// <param name="writer">Writes the shown image.</param>
public class FileSavingService(ISavePathPicker pathPicker, ShownFileWriter writer)
{
    private const string PngExtension = ".png";
    private static readonly char[] InvalidFileNameChars = ['<', '>', ':', '"', '/', '\\', '|', '?', '*'];

    /// <summary>
    /// Saves the tab's image over its file, or asks where to save it when it is not a file.
    /// </summary>
    public async ValueTask<bool> SaveAsync(TabViewModel tab)
    {
        var result = await writer.WriteAsync(tab, new ShownFileWrite(WriteSource.SaveSource, default,
            new WriteDestination.OverItself())).ConfigureAwait(false);
        if (result.Outcome is WriteOutcome.NothingToWrite && tab.FileInfo.CurrentValue is null)
        {
            return await SaveAsAsync(tab).ConfigureAwait(false);
        }

        return result.Outcome is WriteOutcome.Written;
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
        var result = await writer.WriteAsync(tab, new ShownFileWrite(WriteSource.SaveSource, default,
            new WriteDestination.Copy(destination))).ConfigureAwait(false);
        return result.Outcome is WriteOutcome.Written;
    }

    /// <summary>
    /// A file in a common format that shows the tab's image as a save would write it, such as for a wallpaper: the
    /// file itself when it is already common and untouched, else a temporary PNG. <see langword="null"/> when it
    /// could not be written.
    /// </summary>
    public async ValueTask<string?> ExportCommonFormatAsync(TabViewModel tab)
    {
        var file = tab.FileInfo.CurrentValue;
        if (file is not null && file.IsCommon() && tab.Orientation.Current.CurrentValue == Orientation.Upright &&
            !writer.WouldWriteEffects(tab))
        {
            return file.FullName;
        }

        var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + PngExtension);
        return await SaveToAsync(tab, tempPath).ConfigureAwait(false) ? tempPath : null;
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

        var title = string.Concat(tab.Titles.TabTitle
            .Where(c => !char.IsControl(c) && Array.IndexOf(InvalidFileNameChars, c) < 0)).Trim();
        if (string.IsNullOrEmpty(title))
        {
            title = "PicView";
        }

        return $"{title} {now:yyyy-MM-dd HHmmss}{PngExtension}";
    }
}
