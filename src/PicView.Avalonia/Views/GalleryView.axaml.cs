using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using PicView.Core.Config;
using System.Runtime.InteropServices;
using Avalonia;
using PicView.Avalonia.Keybindings;

namespace PicView.Avalonia.Views;

public partial class GalleryView : UserControl
{
    public GalleryView()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            AddHandler(PointerPressedEvent, PreviewPointerPressedEvent, RoutingStrategies.Tunnel);
            AddHandler(KeyDownEvent, PreviewKeyDownEvent, RoutingStrategies.Tunnel);
        };
    }

    private async Task PreviewKeyDownEvent(object? sender, KeyEventArgs e)
    {
        // Prevent control from hijacking keys
        await MainKeyboardShortcuts.MainWindow_KeysDownAsync(e).ConfigureAwait(false); 
        e.Handled = true;
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

        const int speed = 34;

        if (e.Delta.Y > 0)
        {
            if (SettingsHelper.Settings.Zoom.HorizontalReverseScroll)
            {
                scrollViewer.Offset -= new Vector(speed, 0);
            }
            else
            {
                scrollViewer.Offset -= new Vector(-speed, 0);
            }
        }
        else
        {
            if (SettingsHelper.Settings.Zoom.HorizontalReverseScroll)
            {
                scrollViewer.Offset -= new Vector(-speed, 0);
            }
            else
            {
                scrollViewer.Offset -= new Vector(speed, 0);
            }
        }
    }
}