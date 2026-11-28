namespace PicView.Core.FileSorting;

/// <summary>
/// The order the platform's file manager shows a folder in now, as the open windows of Windows Explorer and of Finder do.
/// </summary>
public interface IFolderOrderSource
{
    /// <summary>
    /// The order the file manager shows <paramref name="directory"/> in, when it shows it and the order is one a
    /// <see cref="FileOrder"/> gives; null otherwise. May wait on another process, so not for the UI thread.
    /// </summary>
    FileOrder? OrderOf(string directory);

    /// <summary>
    /// Asks the user to let PicView ask the file manager, where the platform wants their consent, as macOS does for
    /// Finder. False when they refuse, or refused before. May wait on their answer, so not for the UI thread.
    /// </summary>
    bool RequestAccess() => true;
}
