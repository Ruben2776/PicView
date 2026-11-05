using PicView.Avalonia.Views.UC;
using PicView.Core.Gallery;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Gallery;

public static class GalleryHelper
{
    public static void CenterGallery(MainWindowViewModel main)
    {
        if (main.WindowTabs.ActiveTab.CurrentValue.CurrentView.Value is not ImageViewer imageViewer)
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