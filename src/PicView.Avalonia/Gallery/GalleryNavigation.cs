using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views;

namespace PicView.Avalonia.Gallery;

public static class GalleryNavigation
{
    public static void CenterScrollToSelectedItem(MainViewModel vm)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        if (Dispatcher.UIThread.CheckAccess())
        {
            ScrollToSelected();
        }
        else
        {
            Dispatcher.UIThread.InvokeAsync(ScrollToSelected);
        }
        
        return;
        void ScrollToSelected()
        {
            var mainView = desktop.MainWindow.GetControl<MainView>("MainView");
            var mainGrid = mainView.GetControl<Panel>("MainGrid");
            var galleryView = mainGrid.GetControl<GalleryView>("GalleryView");
            galleryView.GalleryListBox.ScrollIntoView(vm.SelectedGalleryItemIndex);
            
        }
    }
}

