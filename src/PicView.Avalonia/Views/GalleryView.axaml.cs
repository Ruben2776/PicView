using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static PicView.Core.Gallery.GalleryThumbInfo;

namespace PicView.Avalonia.Views;

public partial class GalleryView : UserControl
{
    public GalleryView()
    {
        InitializeComponent();
        AddHandler(PointerPressedEvent, PreviewPointerPressedEvent, RoutingStrategies.Tunnel);
    }

    private void PreviewPointerPressedEvent(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        // Disable right click selection
        e.Handled = true;
    }

    private void GalleryListBox_OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // macOS already has horizontal scrolling for touchpad
            return;
        }
        var scrollViewer = GalleryListBox.FindDescendantOfType<ScrollViewer>();
        if (scrollViewer is null)
        {
            return;
        }

        if (e.Delta.Y > 0)
        {
            if (SettingsHelper.Settings.Zoom.HorizontalReverseScroll)
            {
                scrollViewer.LineLeft();
                scrollViewer.LineLeft();
            }
            else
            {
                scrollViewer.LineRight();
                scrollViewer.LineRight();
            }
        }
        else
        {
            if (SettingsHelper.Settings.Zoom.HorizontalReverseScroll)
            {
                scrollViewer.LineRight();
                scrollViewer.LineRight();
            }
            else
            {
                scrollViewer.LineLeft();
                scrollViewer.LineLeft();
            }
        }
    }

    private async void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        if (!NavigationHelper.CanNavigate(vm))
        {
            return;
        }

        if (GalleryFunctions.IsFullGalleryOpen)
        {
            await GalleryFunctions.ToggleGallery(vm);
        }

#if DEBUG
        Debug.Assert(sender != null, nameof(sender) + " != null");
#endif
        var border = (Border)sender;
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (border is null) { return; }
#if DEBUG
        Debug.Assert(border != null, nameof(border) + " != null");
        Debug.Assert(border.DataContext != null, "border.DataContext != null");
#endif
        var galleryItem = (GalleryThumbHolder)border.DataContext;

        var selectedItemIndex = vm.ImageIterator.Pics.IndexOf(galleryItem.FileLocation);

        await vm.LoadPicAtIndex(selectedItemIndex).ConfigureAwait(false);
    }

    private void RecycleItem(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }
        GalleryFunctions.RecycleItem(sender, vm);
    }

    private void OpenWithItem(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }
        GalleryFunctions.OpenWithItem(sender, vm);
    }
}