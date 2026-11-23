using PicView.Core.Saving;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Views.Main;

/// <summary>
/// Writes the Shown File of a tab for the Convert window, through the save module.
/// </summary>
public static class ConvertWriter
{
    /// <summary>
    /// Saves the tab's Save Source to <paramref name="destination"/>. Save replaces the Shown File with the converted
    /// one, or writes it over itself when the destination is the Shown File; Save As, and a Single Image which has no
    /// file to replace, writes a copy there.
    /// </summary>
    /// <param name="tab">The tab as it was when the user clicked, which is kept for the whole write.</param>
    /// <param name="destination">Where to write it; null when the user picked nowhere.</param>
    /// <param name="saveAs">Whether to keep the Shown File and write a copy.</param>
    public static async ValueTask<WriteResult> ConvertAsync(ShownFileWriter writer, TabViewModel tab,
        string? destination, bool saveAs)
    {
        if (string.IsNullOrWhiteSpace(destination))
        {
            return new WriteResult(WriteOutcome.NothingToWrite, null, false, false);
        }

        var source = tab.FileInfo.CurrentValue?.FullName;
        WriteDestination target;
        if (saveAs || source is null)
        {
            // A copy onto the Shown File is written over itself by the save module
            target = new WriteDestination.Copy(destination);
        }
        else if (string.Equals(destination, source, StringComparison.OrdinalIgnoreCase))
        {
            target = new WriteDestination.OverItself();
        }
        else
        {
            target = new WriteDestination.Replace(destination);
        }

        return await writer.WriteAsync(tab, new ShownFileWrite(WriteSource.SaveSource, default, target))
            .ConfigureAwait(false);
    }
}
