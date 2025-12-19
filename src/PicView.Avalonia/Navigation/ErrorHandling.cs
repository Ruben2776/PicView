using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;
using PicView.Avalonia.Clipboard;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views.UC;
using PicView.Core.DebugTools;
using PicView.Core.FileHandling;
using PicView.Core.FileHistory;
using PicView.Core.Gallery;
using PicView.Core.Sizing;

namespace PicView.Avalonia.Navigation;

public static class ErrorHandling
{
    public static void ShowStartUpMenu(MainViewModel vm)
    {
        if (vm is null)
        {
            return;
        }

        if (Dispatcher.UIThread.CheckAccess())
        {
            Start();
        }
        else
        {
            Dispatcher.UIThread.Post(Start);
        }

        return;
        void Start()
        {
            if (vm.MainWindow.CurrentView.CurrentValue is not StartUpMenu)
            {
                var startUpMenu = new StartUpMenu();
                if (Settings.WindowProperties.AutoFit)
                {
                    startUpMenu.MinWidth = SizeDefaults.WindowMinSize;
                    startUpMenu.MinHeight = SizeDefaults.WindowMinSize;
                    vm.PicViewer.GalleryWidth.Value = SizeDefaults.WindowMinSize;
                }

                vm.MainWindow.CurrentView.Value = startUpMenu;
            }
            
            TitleManager.SetNoImageTitle(vm);
            GalleryFunctions.Clear();
            MenuManager.CloseMenus(vm);

            vm.PicViewer.GetIndex.Value = 0;
            vm.PlatformService.StopTaskbarProgress();
            vm.MainWindow.IsLoadingIndicatorShown.Value = false;

            _ = NavigationManager.DisposeImageIteratorAsync();
            if (UIHelper.GetEditableTitlebar is not null)
            {
                UIHelper.GetEditableTitlebar.TextBlock.TextAlignment = TextAlignment.Center;
            }
            if (vm.Gallery is null)
            {
                return;
            }
            vm.Gallery.GalleryMode.Value = GalleryMode.Closed;
            vm.Gallery.GalleryMargin.Value = new Thickness(0, 0, 0, 0);
        }
    }

    public static async Task ReloadAsync(MainViewModel vm)
    {
        vm.MainWindow.IsLoadingIndicatorShown.Value = true;
        
        if (vm.PicViewer.ImageSource is null)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ShowStartUpMenu(vm);
            });
            return;
        }
        
        if (!NavigationManager.CanNavigate(vm))
        {
            var lastEntry = FileHistoryManager.GetLastEntry();
            if (string.IsNullOrEmpty(lastEntry) || !File.Exists(lastEntry))
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ShowStartUpMenu(vm);
                });
                return;
            }
            await NavigationManager.LoadPicFromStringAsync(lastEntry, vm).ConfigureAwait(false);
            return;
        }
        
        if (vm.PicViewer.ImageSource is null || !NavigationManager.CanNavigate(vm))
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ShowStartUpMenu(vm);
            });
            return;
        }

        try
        {
            if (!NavigationManager.CanNavigate(vm))
            {
                var url = vm.PicViewer.Title.CurrentValue.GetURL();
                if (!string.IsNullOrEmpty(url))
                {
                    await NavigationManager.LoadPicFromUrlAsync(url, vm).ConfigureAwait(false);
                }
                else 
                {
                    await ClipboardImageOperations.PasteClipboardImage(vm);
                }
            }
            else
            {
                await NavigationManager.QuickReload().ConfigureAwait(false);
            }
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(ErrorHandling), nameof(ReloadAsync), e);
            await Dispatcher.UIThread.InvokeAsync(() => { ShowStartUpMenu(vm); });
        }
        finally
        {
            vm.MainWindow.IsLoadingIndicatorShown.Value = false;
        }
    }
}
