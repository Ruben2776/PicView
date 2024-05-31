using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using PicView.Avalonia.Keybindings;
using PicView.Core.Config;

namespace PicView.Avalonia.CustomControls;
public class GalleryListBox : ListBox
{
    protected override Type StyleKeyOverride => typeof(ListBox);
    
    public GalleryListBox()
    {
        SelectionMode = SelectionMode.Single;
        AddHandler(PointerPressedEvent, PreviewPointerPressedEvent, RoutingStrategies.Tunnel);
        AddHandler(KeyDownEvent, PreviewKeyDownEvent, RoutingStrategies.Tunnel);
        AddHandler(KeyUpEvent, PreviewKeyUpEvent, RoutingStrategies.Tunnel);
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
    
    private async Task PreviewKeyDownEvent(object? sender, KeyEventArgs e)
    {
        // Prevent control from hijacking keys
        await MainKeyboardShortcuts.MainWindow_KeysDownAsync(e).ConfigureAwait(false); 
        e.Handled = true;
    }
    
    private void PreviewKeyUpEvent(object? sender, KeyEventArgs e)
    {
        // Prevent control from hijacking keys
        MainKeyboardShortcuts.MainWindow_KeysUp(e); 
        e.Handled = true;
    }
    
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        e.Handled = true;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // macOS already has horizontal scrolling for touchpad
            return;
        }
        var scrollViewer = this.FindDescendantOfType<ScrollViewer>();
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
