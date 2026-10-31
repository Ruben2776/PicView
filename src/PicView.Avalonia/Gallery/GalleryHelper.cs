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
        if (!tab.Gallery.IsDockedGalleryVisible.CurrentValue)
        {
            return (0, 0);
        }

        // From the setting, not the tab's dock flags: they follow it on a frame of their own, which can come after
        // the gallery has moved and asked for a Fit
        return Settings.Gallery.DockPosition switch
        {
            GalleryDockPosition.Left or GalleryDockPosition.Right => (GalleryDefaults.GetDockedGalleryWidth, 0),
            GalleryDockPosition.Top or GalleryDockPosition.Bottom => (0, GalleryDefaults.GetDockedGalleryHeight),
            _ => (0, 0)
        };
    }

    public static void CenterGallery(MainWindowViewModel main)
    {
        if (main.WindowTabs.ActiveTab.CurrentValue.CurrentView.CurrentValue is not ImageViewer imageViewer)
        {
            return;
        }

        imageViewer.GalleryView.GalleryItemsControl.ScrollToCenterOfCurrentItem();
    }

    public static async Task GalleryClick(MainWindowViewModel vm)
    {
        var tab = vm.WindowTabs.ActiveTab.CurrentValue;
        var index = tab.Gallery.SelectedGalleryItemIndex.Value;
        if (index == -1)
        {
            index = tab.NavigationIndex.Value;
        }

        await tab.GalleryCoordinator.ToggleGalleryAndLoadItem(index).ConfigureAwait(false);
    }
}