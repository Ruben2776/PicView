using PicView.Core.FileHistory;
using PicView.Core.ViewModels;

namespace PicView.Core.Navigation;

/// <summary>
/// The one entry point that applies a rename everywhere: in File History, in the Last File, and in the View History of
/// every tab in every Main Window. Call it before a tab shows the renamed file, or the tab would record the file under
/// its new name as a new view, and drop its entries after the cursor.
/// </summary>
public static class FileRenames
{
    /// <summary>
    /// Renames the file <paramref name="oldPath"/> to <paramref name="newPath"/> in every history.
    /// </summary>
    /// <param name="core">Where the Main Windows are. Null reaches only File History and the Last File.</param>
    public static void Follow(CoreViewModel? core, string oldPath, string newPath)
    {
        FileHistoryManager.Rename(oldPath, newPath);
        LastFile.Rename(oldPath, newPath);
        foreach (var tab in TabsOf(core))
        {
            tab.ViewHistory.Rename(oldPath, newPath);
        }
    }

    /// <summary>
    /// Renames the folder <paramref name="oldFolder"/> to <paramref name="newFolder"/> in every history, which moves
    /// every entry under it.
    /// </summary>
    /// <param name="core">Where the Main Windows are. Null reaches only File History and the Last File.</param>
    public static void FollowFolder(CoreViewModel? core, string oldFolder, string newFolder)
    {
        FileHistoryManager.RenameFolder(oldFolder, newFolder);
        LastFile.RenameFolder(oldFolder, newFolder);
        foreach (var tab in TabsOf(core))
        {
            tab.ViewHistory.RenameFolder(oldFolder, newFolder);
        }
    }

    // The folder watcher calls this from its own thread, while windows and tabs open and close, so the lists are copied
    // instead of walked live
    private static IEnumerable<TabViewModel> TabsOf(CoreViewModel? core)
    {
        if (core is null)
        {
            yield break;
        }

        foreach (var window in core.MainWindows.MainWindows.ToArray())
        {
            foreach (var tab in window.WindowTabs.Tabs.Value.ToArray())
            {
                yield return tab;
            }
        }
    }
}
