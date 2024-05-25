using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using PicView.Core.Config;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.Helpers;
using PicView.Avalonia.Keybindings;
using PicView.Avalonia.ViewModels;
using ReactiveUI;

namespace PicView.Avalonia.Views;

public partial class GalleryView : UserControl
{
    public static readonly AvaloniaProperty<GalleryMode?> GalleryModeProperty =
        AvaloniaProperty.Register<GalleryView, GalleryMode?>(nameof(GalleryMode));

    public GalleryMode GalleryMode
    {
        get => (GalleryMode)(GetValue(GalleryModeProperty) ?? false);
        set => SetValue(GalleryModeProperty, value);
    }
    
    public GalleryView()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            AddHandler(PointerPressedEvent, PreviewPointerPressedEvent, RoutingStrategies.Tunnel);
            AddHandler(KeyDownEvent, PreviewKeyDownEvent, RoutingStrategies.Tunnel);
            
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
        if (DataContext is not MainViewModel vm)
        {
            return;
        }
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            IsVisible = true;
            Opacity = 0;
            Height = desktop.MainWindow.Bounds.Height - vm.TitlebarHeight - vm.BottombarHeight;
        });

        vm.GalleryOrientation = Orientation.Vertical;
        vm.GalleryStretch = Stretch.UniformToFill;
        vm.IsGalleryCloseIconVisible = true;
        
        const double from = 0d;
        const double to = 1d;
        const double speed = 0.5;
        var opacityAnimation = AnimationsHelper.OpacityAnimation(from, to, speed);
        await opacityAnimation.RunAsync(this);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Opacity = to;
            Height = double.NaN;
            GalleryNavigation.CenterScrollToSelectedItem(vm);
        });
        vm.GalleryVerticalAlignment = VerticalAlignment.Stretch;
    }

    private async Task FullToClosedAnimation()
    {
        if (DataContext is not MainViewModel vm)
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
        await opacityAnimation.RunAsync(this);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Opacity = to;
            IsVisible = false;
            Height = 0;
        });
    }

    private async Task ClosedToBottomAnimation()
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            IsVisible = true;
            Opacity = 1;
            WindowHelper.SetSize(vm);
        });
        
        vm.GalleryOrientation = Orientation.Horizontal;
        vm.GalleryStretch = Stretch.UniformToFill;
        vm.IsGalleryCloseIconVisible = false;
        vm.GalleryVerticalAlignment = VerticalAlignment.Bottom;
        
        const int from = 0;
        var to = vm.GalleryItemSize + 22;
        const double speed = 0.3;
        var heightAnimation = AnimationsHelper.HeightAnimation(from, to, speed);

        await heightAnimation.RunAsync(this);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Height = to;
            IsVisible = true;
            GalleryNavigation.CenterScrollToSelectedItem(vm);
        });
    }

    private async Task BottomToClosedAnimation()
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            WindowHelper.SetSize(vm);
        });
        
        vm.GalleryOrientation = Orientation.Horizontal;
        vm.GalleryStretch = Stretch.UniformToFill;
        vm.IsGalleryCloseIconVisible = false;
        
        var from = vm.GalleryItemSize + 22;
        const int to = 0;
        const double speed = 0.5;
        var heightAnimation = AnimationsHelper.HeightAnimation(from, to, speed);

        await heightAnimation.RunAsync(this);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Height = to;
            IsVisible = false;
            WindowHelper.SetSize(vm);
        });
    }

    private async Task BottomToFullAnimation()
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }
        vm.GalleryOrientation = Orientation.Vertical;
        vm.IsGalleryCloseIconVisible = true;
        
        
        var from = vm.GalleryItemSize + 22;
        var to = desktop.MainWindow.Bounds.Height - vm.TitlebarHeight - vm.BottombarHeight;
        const double speed = 0.5;
        var heightAnimation = AnimationsHelper.HeightAnimation(from, to, speed);
        await heightAnimation.RunAsync(this);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Height = double.NaN;
            GalleryNavigation.CenterScrollToSelectedItem(vm);
        });
        vm.GalleryStretch = Stretch.Uniform;
        vm.GalleryVerticalAlignment = VerticalAlignment.Stretch;
    }

    private async Task FullToBottomAnimation()
    {
        if (DataContext is not MainViewModel vm)
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
        var to = vm.GalleryItemSize + 22;
        const double speed = 0.7;
        var heightAnimation = AnimationsHelper.HeightAnimation(from, to, speed);
        await heightAnimation.RunAsync(this);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Height = double.NaN;
            vm.GalleryOrientation = Orientation.Horizontal;
            vm.GalleryStretch = Stretch.UniformToFill;
            GalleryNavigation.CenterScrollToSelectedItem(vm);
        });
        
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