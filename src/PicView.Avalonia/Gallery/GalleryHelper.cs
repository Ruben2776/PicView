using PicView.Avalonia.Views.UC;
using PicView.Core.Gallery;
using MainWindowViewModel = PicView.Core.ViewModels.MainWindowViewModel;

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
}