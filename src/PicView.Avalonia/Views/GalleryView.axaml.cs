using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.ViewModels;

namespace PicView.Avalonia.Views;

public partial class GalleryAnimationControlView : GalleryAnimationControl
{
    public GalleryAnimationControlView()
    {
        InitializeComponent();
    }

    private void Flyout_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control ctl)
        {
            return;
        }

        FlyoutBase.ShowAttachedFlyout(ctl);
        GalleryItemSizeSlider.SetMaxAndMin();
    }

    private void MenuBase_OnOpened(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }
        GalleryStretchMode.DetermineStretchMode(vm);
    }
}