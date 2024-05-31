using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Controls.Primitives;
using PicView.Avalonia.CustomControls;

namespace PicView.Avalonia.Views;

public partial class GalleryAnimationControlView : GalleryAnimationControl
{
    public GalleryAnimationControlView()
    {
        InitializeComponent();
    }

    private void Flyout_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control ctl)
        {
            FlyoutBase.ShowAttachedFlyout(ctl);
        }
    }
}