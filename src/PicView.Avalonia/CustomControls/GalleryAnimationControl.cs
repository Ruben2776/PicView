using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using PicView.Avalonia.Animations;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;
using PicView.Core.Gallery;
using ReactiveUI;

namespace PicView.Avalonia.CustomControls;

public class GalleryAnimationControl : UserControl
{
    #region Constructors
    protected GalleryAnimationControl()
    {
        Loaded += (_, _) =>
        {
            AddHandler(PointerPressedEvent, PreviewPointerPressedEvent, RoutingStrategies.Tunnel);
            
            this.WhenAnyValue(x => x.GalleryMode)
                .Select(galleryMode =>
                {
                    return galleryMode switch
                    {
                        GalleryMode.FullToBottom => FullToBottomAnimation(),
                        GalleryMode.FullToClosed => FullToClosedAnimation(),
                        GalleryMode.BottomToFull => BottomToFullAnimation(),
                        GalleryMode.BottomToClosed => BottomToClosedAnimation(),
                        GalleryMode.ClosedToFull => ClosedToFullAnimation(),
                        GalleryMode.ClosedToBottom => ClosedToBottomAnimation(),
                        GalleryMode.Closed => CloseWithNoAnimation(),
                        _ => throw new ArgumentOutOfRangeException(nameof(galleryMode), galleryMode, null)
                    };
                }).Subscribe();
            
            if (Parent is not Control parent)
            {
                return;
            }
            parent.SizeChanged += (_, e) => ParentSizeChanged(parent, e);
        };
    }

    #endregion
    
    #region Properties

    public static readonly AvaloniaProperty<GalleryMode?> GalleryModeProperty =
        AvaloniaProperty.Register<GalleryAnimationControl, GalleryMode?>(nameof(GalleryMode));

    public GalleryMode GalleryMode
    {
        get => (GalleryMode)(GetValue(GalleryModeProperty) ?? false);
        set => SetValue(GalleryModeProperty, value);
    }

    private bool _isAnimating;
    
    #endregion

    #region Animation Methods
    
    private async Task CloseWithNoAnimation()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            IsVisible = false;
            UIHelper.GetGalleryView.BlurMask.BlurEnabled = false;
            Height = 0;
        });
    }

    private async Task ClosedToFullAnimation()
    {
        if (DataContext is not MainViewModel vm || _isAnimating)
        {
            return;
        }
        if (Parent is not Control parent)
        {
            return;
        }
        await vm.GalleryItemStretchTask(SettingsHelper.Settings.Gallery.FullGalleryStretchMode);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            IsVisible = true;
            Opacity = 0;
            Height = parent.Bounds.Height;
            UIHelper.GetGalleryView.BlurMask.BlurEnabled = true;
            vm.GalleryItemMargin = new Thickness(25);
        });

        vm.GalleryOrientation = Orientation.Vertical;
        GalleryStretchMode.DetermineStretchMode(vm);
        vm.IsGalleryCloseIconVisible = true;
        
        const double from = 0d;
        const double to = 1d;
        const double speed = 0.5;
        var opacityAnimation = AnimationsHelper.OpacityAnimation(from, to, speed);
        _isAnimating = true;
        await opacityAnimation.RunAsync(this);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Opacity = to;
            vm.GalleryVerticalAlignment = VerticalAlignment.Stretch;
        });
        _isAnimating = false;
        await Task.Delay(50); // Need to wait for the animation
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            GalleryNavigation.CenterScrollToSelectedItem(vm);
        });
    }

    private async Task FullToClosedAnimation()
    {
        if (Parent is not Control parent || _isAnimating || DataContext is not MainViewModel vm)
        {
            return;
        }
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Height = parent.Bounds.Height;
            UIHelper.GetGalleryView.BlurMask.BlurEnabled = false;
        });
        const double from = 1d;
        const double to = 0d;
        const double speed = 0.3;
        var opacityAnimation = AnimationsHelper.OpacityAnimation(from, to, speed);
        _isAnimating = true;
        vm.GalleryMargin = new Thickness(0);
        await opacityAnimation.RunAsync(this);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Opacity = to;
            IsVisible = false;
            Height = 0;
        });
        _isAnimating = false;
    }

    private async Task ClosedToBottomAnimation()
    {
        if (DataContext is not MainViewModel vm || _isAnimating)
        {
            return;
        }
        await vm.GalleryItemStretchTask(SettingsHelper.Settings.Gallery.BottomGalleryStretchMode);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            IsVisible = true;
            Opacity = 1;
            WindowHelper.SetSize(vm);
            UIHelper.GetGalleryView.BlurMask.BlurEnabled = false;
            vm.GalleryItemMargin = new Thickness(2,0);
        });
        
        vm.GalleryOrientation = Orientation.Horizontal;
        GalleryStretchMode.DetermineStretchMode(vm);
        vm.IsGalleryCloseIconVisible = false;
        vm.GalleryVerticalAlignment = VerticalAlignment.Bottom;
        
        const int from = 0;
        var to = vm.GalleryHeight;
        const double speed = 0.3;
        var heightAnimation = AnimationsHelper.HeightAnimation(from, to, speed);
        _isAnimating = true;
        await heightAnimation.RunAsync(this);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Height = to;
            IsVisible = true;
            GalleryNavigation.CenterScrollToSelectedItem(vm);
        });
        
        _isAnimating = false;
    }

    private async Task BottomToClosedAnimation()
    {
        if (DataContext is not MainViewModel vm || _isAnimating)
        {
            return;
        }
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            WindowHelper.SetSize(vm);
            UIHelper.GetGalleryView.BlurMask.BlurEnabled = false;
        });
        
        vm.GalleryOrientation = Orientation.Horizontal;
        vm.IsGalleryCloseIconVisible = false;
        vm.GalleryMargin = new Thickness(2,0);
        var from = vm.GalleryHeight;
        const int to = 0;
        const double speed = 0.7;
        _isAnimating = true;
        var heightAnimation = AnimationsHelper.HeightAnimation(from, to, speed);
        await heightAnimation.RunAsync(this);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Height = to;
            IsVisible = false;
            WindowHelper.SetSize(vm);
        });
        _isAnimating = false;
    }

    private async Task BottomToFullAnimation()
    {
        if (DataContext is not MainViewModel vm || _isAnimating)
        {
            return;
        }
        if (Parent is not Control parent)
        {
            return;
        }
        vm.GalleryOrientation = Orientation.Vertical;
        vm.IsGalleryCloseIconVisible = true;
        GalleryStretchMode.DetermineStretchMode(vm);
        vm.GalleryItemMargin = new Thickness(25);
        var from = vm.GalleryHeight;
        var to = parent.Bounds.Height;
        const double speed = 0.5;
        var heightAnimation = AnimationsHelper.HeightAnimation(from, to, speed);
        _isAnimating = true;
        await heightAnimation.RunAsync(this);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Height = to;
            UIHelper.GetGalleryView.BlurMask.BlurEnabled = true;
        });
        vm.GalleryVerticalAlignment = VerticalAlignment.Stretch;
        
        _isAnimating = false;
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            GalleryNavigation.CenterScrollToSelectedItem(vm);
        });
    }

    private async Task FullToBottomAnimation()
    {
        if (DataContext is not MainViewModel vm || _isAnimating)
        {
            return;
        }
        if (Parent is not Control parent)
        {
            return;
        }
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Height = parent.Bounds.Height;
            UIHelper.GetGalleryView.BlurMask.BlurEnabled = false;
            vm.GalleryItemMargin = new Thickness(2,0);
        });
        vm.GalleryVerticalAlignment = VerticalAlignment.Bottom;
        vm.IsGalleryCloseIconVisible = false;
        
        var from = Bounds.Height;
        var to = vm.GalleryHeight;
        const double speed = 0.6;
        var heightAnimation = AnimationsHelper.HeightAnimation(from, to, speed);
        _isAnimating = true;
        await heightAnimation.RunAsync(this);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Height = double.NaN;
            vm.GalleryOrientation = Orientation.Horizontal;
        });
        if (!GalleryLoad.IsLoading)
        {
            GalleryStretchMode.DetermineStretchMode(vm);
        }
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            GalleryNavigation.CenterScrollToSelectedItem(vm);
        });
        
        _isAnimating = false;
    }
    
    #endregion

    #region Events

    private void ParentSizeChanged(Control parent, SizeChangedEventArgs e)
    {
        if (_isAnimating)
        {
            return;
        }

        if (!GalleryFunctions.IsFullGalleryOpen)
        {
            return;
        }

        Width = parent.Bounds.Width;
        Height = parent.Bounds.Height;
    }

    private void PreviewPointerPressedEvent(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        // Disable right click selection, to not interfere with context menu
        e.Handled = true;
    }
    
    #endregion
}