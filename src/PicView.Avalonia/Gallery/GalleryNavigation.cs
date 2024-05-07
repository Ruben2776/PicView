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
            // var mainView = desktop.MainWindow.GetControl<MainView>("MainView");
            // var mainGrid = mainView.GetControl<Panel>("MainGrid");
            // var galleryView = mainGrid.GetControl<GalleryView>("GalleryView");
            // var galleryListbox = galleryView.GetControl<ListBox>("GalleryListBox");
            // var galleryItems = galleryListbox.Items.Source;
            // var scrollViewer = galleryListbox.Scroll;
            // var size = galleryListbox.Bounds;
            // var center = galleryListbox.TransformToVisual(scrollViewer).Value.Transform(new Point(size.Width / 2, size.Height / 2));
            // scrollPresenter.Offset = new Vector(center.X + scrollPresenter.Offset.X, center.Y + scrollPresenter.Offset.Y);   
            
        }
    }
}

public static class ItemsControlExtensions
{
        public static void ScrollToCenterOfView(this ItemsControl itemsControl, object item)
        {
            // Scroll immediately if possible
            if (itemsControl.TryScrollToCenterOfView(item))
            {
                return;
            }

            // Otherwise wait until everything is loaded, then scroll
            if (itemsControl is ListBox box) box.ScrollIntoView(item);

            itemsControl.TryScrollToCenterOfView(item);
        }
         


        private static bool TryScrollToCenterOfView(this ItemsControl itemsControl, object item)
        {
            // Find the container
            if (itemsControl.ContainerFromItem(item) is not { } container) return false;
      
            if (container.GetLogicalParent() is not ScrollViewer scrollPresenter) return false;

            var size = container.Bounds;

            var center = container.TransformToVisual(scrollPresenter).Value.Transform(new Point(size.Width / 2, size.Height / 2));

            scrollPresenter.Offset = new Vector(center.X + scrollPresenter.Offset.X, center.Y + scrollPresenter.Offset.Y);   
           

            // Scroll the center of the container to the center of the viewport
            //if (scrollPresenter.CanVerticallyScroll) scrollPresenter.SetVerticalOffset(CenteringOffset(center.Y, scrollPresenter.ViewportHeight, scrollPresenter.ExtentHeight));
            //if (scrollPresenter.CanHorizontallyScroll) scrollPresenter.SetHorizontalOffset(CenteringOffset(center.X, scrollPresenter.ViewportWidth, scrollPresenter.ExtentWidth));
            return true;
        }
}