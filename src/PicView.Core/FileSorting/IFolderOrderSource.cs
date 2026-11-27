namespace PicView.Core.FileSorting;

/// <summary>
/// The order the platform's file manager shows a folder in now, as Windows Explorer's open windows do.
/// </summary>
public interface IFolderOrderSource
{
    /// <summary>
    /// The order the file manager shows <paramref name="directory"/> in, when it shows it and the order is one a
    /// <see cref="FileOrder"/> gives; null otherwise. May wait on another process, so not for the UI thread.
    /// </summary>
    FileOrder? OrderOf(string directory);
}
