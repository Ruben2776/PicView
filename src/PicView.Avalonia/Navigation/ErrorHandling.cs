using Avalonia.Threading;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views.UC;
using PicView.Core.Calculations;
using PicView.Core.Config;
using PicView.Core.Gallery;

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
            vm.CurrentView = vm.CurrentView = startUpMenu;
            vm.GalleryMode = GalleryMode.BottomToClosed;
            GalleryFunctions.Clear(vm);
            UIHelper.CloseMenus(vm);
            vm.ImageIterator?.Dispose();
            vm.ImageIterator = null;
        }
    }
}
