using PicView.Core.ViewModels;

namespace PicView.Core.Navigation;

/// <summary>
/// The Last File: the file, archive or URL that the active tab of the Active Main Window showed most recently. "Open last
/// file" in the start-up menu and App Start's "open last file" open it. This is the one writer of
/// <c>Settings.StartUp.LastFile</c>; everything else reads the setting.
/// </summary>
public static class LastFile
{
    /// <summary>
    /// Makes <paramref name="path"/> the Last File, when <paramref name="tab"/> is the active tab of the Active Main Window.
    /// A tab shows images on whichever thread its load finishes on, so this reads the window's active tab and the core's
    /// active window as they are now. It never waits, and costs two reference comparisons and a string assignment. It
    /// does not check the file history setting, since the Last File is written whether or not file history is on.
    /// </summary>
    public static void Record(TabViewModel tab, string path)
    {
        // A tab without a Main Window, or a window without a core, as in tests, never writes
        if (tab.ParentWindowContext is not { Core: { } core } window)
        {
            return;
        }

        if (ReferenceEquals(window.WindowTabs.ActiveTab.CurrentValue, tab) &&
            ReferenceEquals(core.MainWindows.ActiveWindow.CurrentValue, window))
        {
            Settings.StartUp.LastFile = path;
        }
    }

    /// <summary>
    /// Makes what <paramref name="tab"/> shows the Last File, when that has a path: closing a Main Window does this for
    /// its active tab, whichever window is active by then. A Single Image without a path leaves the Last File as it is.
    /// </summary>
    public static void Remember(TabViewModel tab)
    {
        if (tab.ViewHistory.Current?.HistoryPath is { } path)
        {
            Settings.StartUp.LastFile = path;
        }
    }

    /// <summary>
    /// Follows a rename: the Last File becomes <paramref name="newPath"/> when it is <paramref name="oldPath"/>. Paths
    /// compare ignoring case, and a path that only contains the old one is not the old one.
    /// </summary>
    public static void Rename(string oldPath, string newPath)
    {
        if (string.Equals(Settings.StartUp.LastFile, oldPath, StringComparison.OrdinalIgnoreCase))
        {
            Settings.StartUp.LastFile = newPath;
        }
    }

    /// <summary>
    /// Follows a folder rename: the Last File moves to <paramref name="newFolder"/> when it is in
    /// <paramref name="oldFolder"/>, subfolders included.
    /// </summary>
    public static void RenameFolder(string oldFolder, string newFolder)
    {
        if (Settings.StartUp.LastFile is { } path &&
            FolderChange.PathInRenamedFolder(path, oldFolder, newFolder) is { } movedPath)
        {
            Settings.StartUp.LastFile = movedPath;
        }
    }
}
