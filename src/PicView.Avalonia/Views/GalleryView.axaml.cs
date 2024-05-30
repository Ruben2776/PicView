using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Controls.Primitives;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Keybindings;

namespace PicView.Avalonia.Views;

public partial class GalleryAnimationControlView : GalleryAnimationControl
{
    public GalleryAnimationControlView()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            AddHandler(PointerPressedEvent, PreviewPointerPressedEvent, RoutingStrategies.Tunnel);
            AddHandler(KeyDownEvent, PreviewKeyDownEvent, RoutingStrategies.Tunnel);
            AddHandler(KeyUpEvent, PreviewKeyUpEvent, RoutingStrategies.Tunnel);
        };
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

    private void Flyout_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control ctl)
        {
            FlyoutBase.ShowAttachedFlyout(ctl);
        }
    }
}