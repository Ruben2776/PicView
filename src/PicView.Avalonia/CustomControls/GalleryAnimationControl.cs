using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.Helpers;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;
using ReactiveUI;

namespace PicView.Avalonia.CustomControls;

public class GalleryAnimationControl : UserControl
{
    public static readonly AvaloniaProperty<GalleryMode?> GalleryModeProperty =
        AvaloniaProperty.Register<GalleryAnimationControl, GalleryMode?>(nameof(GalleryMode));

    public GalleryMode GalleryMode
    {
        get => (GalleryMode)(GetValue(GalleryModeProperty) ?? false);
        set => SetValue(GalleryModeProperty, value);
    }

    private bool _isAnimating;

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
                        _ => throw new ArgumentOutOfRangeException(nameof(galleryMode), galleryMode, null)
                    };
                }).Subscribe();
        };
    }

    private async Task ClosedToFullAnimation()
    {
        if (DataContext is not MainViewModel vm || _isAnimating)
        {
            return;
        }
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }
        await vm.GalleryItemStretchTask(SettingsHelper.Settings.Gallery.FullGalleryStretchMode);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            IsVisible = true;
            Opacity = 0;
            Height = desktop.MainWindow.Bounds.Height - vm.TitlebarHeight - vm.BottombarHeight;
        });

        vm.GalleryOrientation = Orientation.Vertical;
        if (!GalleryLoad.IsLoading)
        {
            GalleryStretchMode.SetStretchMode(vm);
        }
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
            GalleryNavigation.CenterScrollToSelectedItem(vm);
        });
        vm.GalleryVerticalAlignment = VerticalAlignment.Stretch;
        
        _isAnimating = false;
    }

    private async Task FullToClosedAnimation()
    {
        if (DataContext is not MainViewModel vm || _isAnimating)
        {
            return;
        }
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Height = desktop.MainWindow.Bounds.Height - vm.TitlebarHeight - vm.BottombarHeight;
        });
        const double from = 1d;
        const double to = 0d;
        const double speed = 0.3;
        var opacityAnimation = AnimationsHelper.OpacityAnimation(from, to, speed);
        _isAnimating = true;
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
        });
        
        vm.GalleryOrientation = Orientation.Horizontal;
        if (!GalleryLoad.IsLoading)
        {
            GalleryStretchMode.SetStretchMode(vm);
        }
        
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
        });
        
        vm.GalleryOrientation = Orientation.Horizontal;
        vm.IsGalleryCloseIconVisible = false;
        
        var from = vm.GalleryHeight;
        const int to = 0;
        const double speed = 0.5;
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
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }
        vm.GalleryOrientation = Orientation.Vertical;
        vm.IsGalleryCloseIconVisible = true;
        
        
        var from = vm.GalleryHeight;
        var to = desktop.MainWindow.Bounds.Height - vm.TitlebarHeight - vm.BottombarHeight;
        const double speed = 0.5;
        var heightAnimation = AnimationsHelper.HeightAnimation(from, to, speed);
        _isAnimating = true;
        await heightAnimation.RunAsync(this);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Height = to;
            GalleryNavigation.CenterScrollToSelectedItem(vm);
        });
        if (!GalleryLoad.IsLoading)
        {
            GalleryStretchMode.SetStretchMode(vm);
        }
        vm.GalleryVerticalAlignment = VerticalAlignment.Stretch;
        
        _isAnimating = false;
    }

    private async Task FullToBottomAnimation()
    {
        if (DataContext is not MainViewModel vm || _isAnimating)
        {
            return;
        }
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Height = desktop.MainWindow.Bounds.Height - vm.TitlebarHeight - vm.BottombarHeight;
        });
        vm.GalleryVerticalAlignment = VerticalAlignment.Bottom;
        vm.IsGalleryCloseIconVisible = false;
        
        var from = Bounds.Height;
        var to = vm.GalleryHeight;
        const double speed = 0.7;
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
            GalleryStretchMode.SetStretchMode(vm);
        }
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            GalleryNavigation.CenterScrollToSelectedItem(vm);
        });
        
        
        _isAnimating = false;
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
}