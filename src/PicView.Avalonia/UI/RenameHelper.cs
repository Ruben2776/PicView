using ImageMagick;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Input;
using PicView.Core.FileHandling;
using PicView.Core.FileHistory;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.UI;

public static class RenameHelper
{
    public static void Rename(MainWindowViewModel vm, MainWindow mainWindow)
    {
        if (!vm.IsTopToolbarShown.CurrentValue || vm.IsFullscreen.CurrentValue)
        {
            mainWindow.AddRenameDialog();
            return;
        }
        mainWindow.UIHelper.GetEditableTitlebar.SelectFileName();   
    }

    public static async Task<bool> RenameAction(MainWindowViewModel vm, string newName, MainWindow? mainWindow = null)
    {
        vm.IsLoadingIndicatorShown.Value = true;
        var tab = vm.WindowTabs.ActiveTab.CurrentValue;
        var oldPath = tab.FileInfo.CurrentValue.FullName;
        var newPath = Path.Combine(tab.FileInfo.CurrentValue.DirectoryName, newName);
        
        var isRenamed = await Task.Run(async () =>
        {
            if (newPath == oldPath)
            {
                // TODO
                //ShowFileExistsError(vm);
                return false;
            }
    
            var currentExtension = Path.GetExtension(oldPath);
            var newExtension = Path.GetExtension(newPath);
            if (currentExtension.Equals(newExtension, StringComparison.OrdinalIgnoreCase))
            {
                FileHelper.RenameFile(oldPath, newPath);
            }
            else
            {
                using var magick = new MagickImage(oldPath);
                await magick.WriteAsync(newPath);
                vm.WindowTabs.ActiveTab.CurrentValue.IsFileWatcherNavigationEnabled = false;
                File.Delete(oldPath);
                await vm.WindowTabs.LoadFromFileAsync(newPath).ConfigureAwait(false);
                vm.WindowTabs.ActiveTab.CurrentValue.IsFileWatcherNavigationEnabled = true;
            }

            var newFileInfo = new FileInfo(newPath);
            tab.FileInfo.Value = newFileInfo;
            tab.Model.FileInfo = newFileInfo;
            tab.UpdateTabTitle();
            vm.IsLoadingIndicatorShown.Value = false;
            return true;
        });

        if (isRenamed)
        {
            FileHistoryManager.Rename(oldPath, newPath);
        }
        
        mainWindow?.UIHelper.GetMainView.Focus();
        MainKeyboardShortcuts.IsKeysEnabled = true;
        return isRenamed;
    }
}