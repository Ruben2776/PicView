using Avalonia;
using Avalonia.Threading;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.Calculations;
using PicView.Core.Config;
using PicView.Core.Gallery;
using StartUpMenu = PicView.Avalonia.Views.StartUpMenu;

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
            if (vm.CurrentView is not StartUpMenu)
            {
                var startUpMenu = new StartUpMenu();
                if (SettingsHelper.Settings.WindowProperties.AutoFit)
                {
                    startUpMenu.Width = SizeDefaults.WindowMinSize;
                    startUpMenu.Height = SizeDefaults.WindowMinSize;
                    if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
                    {
                        vm.GalleryWidth = SizeDefaults.WindowMinSize;
                    }
                }
                vm.CurrentView = startUpMenu;
            }
            else
            {
                SetTitleHelper.ResetTitle(vm);
            }

            vm.GalleryMode = GalleryMode.Closed;
            GalleryFunctions.Clear();
            UIHelper.CloseMenus(vm);
            vm.ImageIterator?.Dispose();
            vm.ImageIterator = null;
            vm.GalleryMargin = new Thickness(0, 0, 0, 0);
            vm.GetIndex = 0;
            vm.PlatformService.StopTaskbarProgress();
            vm.IsLoading = false;
        }
    }

    public static async Task ReloadAsync(MainViewModel vm)
    {
        if (vm.ImageSource is null)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ShowStartUpMenu(vm);
            });
            return;
        }
        
        if (!NavigationHelper.CanNavigate(vm))
        {
            await FileHistoryNavigation.OpenLastFileAsync(vm);
            return;
        }
        
        if (vm.ImageIterator is null)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ShowStartUpMenu(vm);
            });
            return;
        }
        
        vm.ImageIterator.Clear();
        await vm.ImageIterator.ReloadFileList().ConfigureAwait(false);
        var index = vm.ImageIterator.ImagePaths.IndexOf(vm.FileInfo.FullName);
        await NavigationHelper.Navigate(index, vm).ConfigureAwait(false);
    }
}
