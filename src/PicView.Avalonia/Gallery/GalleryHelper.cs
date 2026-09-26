using Avalonia.Threading;
using PicView.Avalonia.Navigation.Services;
using PicView.Avalonia.Views.UC;
using PicView.Core.Gallery;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Gallery;

public static class GalleryHelper
{
    public static (double width, double height) GetGallerySize(MainWindowViewModel vm)
    {
        var tabs = vm.WindowTabs;
        var tab = tabs.ActiveTab.CurrentValue;

        if (tab.Gallery.IsLeftDocked.CurrentValue || tab.Gallery.IsRightDocked.CurrentValue)
        {
            return (GalleryDefaults.GetDockedGalleryWidth, 0);
        }
        if (tab.Gallery.IsBottomDocked.CurrentValue|| tab.Gallery.IsTopDocked.CurrentValue)
        {
            return (0, GalleryDefaults.GetDockedGalleryHeight);
        }

        return (0, 0);
    }

    public static void CenterGallery(MainWindowViewModel main)
    {
        if (main.WindowTabs.ActiveTab.CurrentValue.CurrentView.CurrentValue is not ImageViewer imageViewer)
        {
            return;
        }

        imageViewer.GalleryView.GalleryItemsControl.ScrollToCenterOfCurrentItem();
    }

    public static async ValueTask GalleryClick(MainWindowViewModel vm)
    {
        var tab = vm.WindowTabs.ActiveTab.CurrentValue;
        var index = tab.Gallery.SelectedGalleryItemIndex.Value;
        if (index == -1)
        {
            index = tab.NavigationIndex.Value;
        }

        await GalleryLoader.ToggleGalleryAndLoadItem(tab, index).ConfigureAwait(false);
    }
    
    public static async ValueTask LoadGallery(CoreViewModel core)
    {
        await GalleryLoader.LoadGalleryAsync(core.MainWindows.ActiveWindow.Value.WindowTabs.ActiveTab.Value,
                core.MainWindows.ActiveWindow.Value.WindowTabs.ActiveTab.Value.ImageIterator.Files,
                ServiceHelper.ThumbLoader,
                core.SharedThumbnailCache,
                core.MainWindows.ActiveWindow.Value.WindowTabs.ActiveTab.Value.GetTabCancellation().Token)
            .ConfigureAwait(false);
        Dispatcher.UIThread.Post(() =>
        {
            var tab = core.MainWindows.ActiveWindow.Value.WindowTabs.ActiveTab.Value;
            if (tab.CurrentView.CurrentValue is not ImageViewer imageViewer)
            {
                return;
            }

            imageViewer.GalleryView.GalleryItemsControl.CurrentItemIndex = tab.NavigationIndex.Value;
            imageViewer.GalleryView.GalleryItemsControl.ScrollToCenterOfCurrentItem();
        }, DispatcherPriority.Loaded);
    }
}