using Avalonia.Threading;
using ImageMagick;
using PicView.Avalonia.CustomControls;
using PicView.Core.DebugTools;
using PicView.Core.FileHandling;
using PicView.Core.FileHistory;
using PicView.Core.Localization;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.UI;

public static class RenameHelper
{
    public static void Rename(MainWindowViewModel vm, MainWindow mainWindow)
    {
        if (!vm.IsTopToolbarShown.CurrentValue || vm.IsFullscreen.Value)
        {
            mainWindow.AddRenameDialog();
            return;
        }
        mainWindow.UIHelper.GetEditableTitlebar.SelectFileName();
    }

    public static async Task<bool> RenameAction(MainWindowViewModel vm, string newName, MainWindow? mainWindow = null)
    {
        var tab = vm.WindowTabs.ActiveTab.CurrentValue;
        var oldPath = tab.FileInfo.CurrentValue.FullName;
        var newPath = Path.Combine(tab.FileInfo.CurrentValue.DirectoryName, newName);
        if (string.Equals(newPath, oldPath, StringComparison.Ordinal) || IsNameTaken(vm, newName))
        {
            return false;
        }

        vm.IsLoadingIndicatorShown.Value = true;
        var isRenamed = false;
        try
        {
            isRenamed = await Task.Run(() => RenameFileAsync(vm, tab, oldPath, newPath)).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(RenameHelper), nameof(RenameAction), e);
        }
        finally
        {
            vm.IsLoadingIndicatorShown.Value = false;
        }

        if (isRenamed)
        {
            FileHistoryManager.Rename(oldPath, newPath);
        }

        if (mainWindow is not null)
        {
            await Dispatcher.UIThread.InvokeAsync(() => mainWindow.UIHelper.GetMainView.Focus());
        }
        return isRenamed;
    }

    /// <summary>
    /// Whether another file in the active tab's folder already has <paramref name="newName"/>, in which case it tells the
    /// user, since renaming would replace that file.
    /// </summary>
    public static bool IsNameTaken(MainWindowViewModel vm, string? newName)
    {
        var fileInfo = vm.WindowTabs.ActiveTab.CurrentValue.FileInfo.CurrentValue;
        if (fileInfo?.DirectoryName is null || string.IsNullOrWhiteSpace(newName))
        {
            return false;
        }

        var newPath = Path.Combine(fileInfo.DirectoryName, newName);
        if (!FileHelper.IsNameTaken(fileInfo.FullName, newPath))
        {
            return false;
        }

        TooltipHelper.ShowTooltipMessage(vm,
            TranslationManager.Translation.FileAlreadyExists + Environment.NewLine + Path.GetFileName(newPath), true);
        return true;
    }

    /// <summary>
    /// Renames the file, or converts it when the extension changes. The folder watcher moves it to its sorted place in
    /// the tab's files and gallery.
    /// </summary>
    private static async Task<bool> RenameFileAsync(MainWindowViewModel vm, TabViewModel tab, string oldPath,
        string newPath)
    {
        var currentExtension = Path.GetExtension(oldPath);
        var newExtension = Path.GetExtension(newPath);
        if (currentExtension.Equals(newExtension, StringComparison.OrdinalIgnoreCase))
        {
            if (!FileHelper.RenameFile(oldPath, newPath))
            {
                return false;
            }

            var newFileInfo = new FileInfo(newPath);
            tab.FileInfo.Value = newFileInfo;
            tab.Model.FileInfo = newFileInfo;
            tab.UpdateTabTitle();
            return true;
        }

        using (var magick = new MagickImage(oldPath))
        {
            await magick.WriteAsync(newPath).ConfigureAwait(false);
        }

        // Show the converted file before deleting the original, so the watcher removes a file that is no longer shown
        // instead of navigating away from it
        await vm.WindowTabs.LoadFromFileAsync(newPath, tab).ConfigureAwait(false);
        File.Delete(oldPath);
        return true;
    }
}
