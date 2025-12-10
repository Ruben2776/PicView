using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using PicView.Avalonia.UI;

namespace PicView.Avalonia.Views.Gallery;

public partial class GalleryItem : UserControl
{
    public GalleryItem()
    {
        InitializeComponent();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            CopyFileMenuItem.IsVisible = false;
        }
        GalleryContextMenu.Opened += delegate
        {
            ImageBorder.BorderBrush = UIHelper.GetBrush("SecondaryAccentColor");
        };
        GalleryContextMenu.Closed += delegate
        {
            ImageBorder.BorderBrush = Brushes.Transparent;
        };
    }
        
    private void Flyout_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control ctl)
        {
            return;
        }

        FlyoutBase.ShowAttachedFlyout(ctl);
    }
}