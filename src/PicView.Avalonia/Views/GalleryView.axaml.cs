using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using PicView.Avalonia.Helpers;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;
using System.Runtime.InteropServices;

namespace PicView.Avalonia.Views;

public partial class GalleryView : UserControl
{
    public GalleryView()
    {
        InitializeComponent();
        AddHandler(PointerPressedEvent, PreviewPointerPressedEvent, RoutingStrategies.Tunnel);
        GalleryListBox.SelectionChanged += async (_, _) => await GalleryListBox_OnSelectionChanged();
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

    private async Task GalleryListBox_OnSelectionChanged()
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        var selectedItem = vm.SelectedGalleryItem;
        if (selectedItem is null) { return; }
        var selectedItemIndex = vm.ImageIterator.Pics.IndexOf(selectedItem.Value.FileLocation);

        _ = FunctionsHelper.ToggleGallery();
        await vm.LoadPicAtIndex(selectedItemIndex).ConfigureAwait(false);
    }
}