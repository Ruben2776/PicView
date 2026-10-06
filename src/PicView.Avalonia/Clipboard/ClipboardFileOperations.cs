using Avalonia;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.StartUp;
using PicView.Avalonia.Views.UC;
using PicView.Core.FileHandling;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Clipboard;

/// <summary>
/// Handles clipboard operations related to files (MVVM refactor)
/// </summary>
public static class ClipboardFileOperations
{
    public static async ValueTask ProcessStorageItems(IStorageItem[] storageItems, MainWindowViewModel vm, MainWindow mainWindow)
    {
        if (storageItems.Length is 0 || Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }

        // Load the first file
        var firstItem = storageItems[0].Path.LocalPath;
        
        if (!vm.WindowTabs.ActiveTab.Value.IsInitialized)
        {
            await QuickLoad.QuickLoadAsync(mainWindow, core, firstItem, continueFromLeftOff: false).ConfigureAwait(false);
        }
        else
        {
            if (firstItem.IsArchive())
            {
                await vm.WindowTabs.LoadFromArchiveAsync(firstItem).ConfigureAwait(false);
            }
            else
            {
                await vm.WindowTabs.LoadFromFileAsync(firstItem).ConfigureAwait(false);
            }
        }

        if (vm.WindowTabs.ActiveTab.CurrentValue.Gallery.IsGalleryDocked.CurrentValue)
        {
            // TODO: Consecutive tabs or windows currently not supported when gallery is enabled
            return;
        }
        
        // Open consecutive files in a new tab
        foreach (var file in storageItems.Skip(1))
        {
            var path = file.Path.LocalPath;
            var fileInfo = new FileInfo(path);
            var tab = vm.WindowTabs.CreateTab(fileInfo);
            TabNavigationInitializer.InitializeConsecutiveTab(core, mainWindow, tab); 
            await Dispatcher.UIThread.InvokeAsync(() => 
            {
                tab.CurrentView.Value = new ImageViewer();
            });
            if (fileInfo.IsArchive())
            {
                _ = Task.Run(() => vm.WindowTabs.LoadFromArchiveAsync(path, tab).ConfigureAwait(false));
            }
            else
            {
                _ = Task.Run(() => vm.WindowTabs.LoadFromFileAsync(fileInfo, tab).ConfigureAwait(false));
            }
            
            file.Dispose();
        }
    }
}
