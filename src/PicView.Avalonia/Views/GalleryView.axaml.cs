using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using PicView.Core.Config;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.Helpers;
using PicView.Avalonia.Keybindings;
using PicView.Avalonia.ViewModels;
using PicView.Core.Localization;
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
                .Subscribe(async galleryMode =>
                {
                    switch (galleryMode)
                    {
                        case GalleryMode.FullToBottom:
                            await FullToBottomAnimation();
                            break;
                        case GalleryMode.FullToClosed:
                            await FullToClosedAnimation();
                            break;
                        case GalleryMode.BottomToFull:
                            await BottomToFullAnimation();
                            break;
                        case GalleryMode.BottomToClosed:
                            await BottomToClosedAnimation();
                            break;
                        case GalleryMode.ClosedToFull:
                            await ClosedToFullAnimation();
                            break;
                        case GalleryMode.ClosedToBottom:
                            await ClosedToBottomAnimation();
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(galleryMode), galleryMode, null);
                    }
                });
        };
    }
    
    private static Animation HeightAnimation(double from, double to, double speed)
    {
        return new Animation
        {
            Duration = TimeSpan.FromSeconds(speed),
            Easing = new SplineEasing(),
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame
                {
                    Setters =
                    {
                        new Setter
                        {
                            Property = HeightProperty,
                            Value = from
                        }
                    },
                    Cue = new Cue(0d)
                },
                new KeyFrame
                {
                    Setters =
                    {
                        new Setter
                        {
                            Property = HeightProperty,
                            Value = to
                        }
                    },
                    Cue = new Cue(1d)
                },
            }
        };
    }
    
    private static Animation OpacityAnimation(double from, double to, double speed)
    {
        return new Animation
        {
            Duration = TimeSpan.FromSeconds(speed),
            Easing = new LinearEasing(),
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame
                {
                    Setters =
                    {
                        new Setter
                        {
                            Property = OpacityProperty,
                            Value = from
                        }
                    },
                    Cue = new Cue(0d)
                },
                new KeyFrame
                {
                    Setters =
                    {
                        new Setter
                        {
                            Property = OpacityProperty,
                            Value = to
                        }
                    },
                    Cue = new Cue(1d)
                },
            }
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
        vm.GalleryVerticalAlignment = VerticalAlignment.Bottom;
        vm.IsGalleryCloseIconVisible = true;
        
        var from = 0d;
        var to = 1d;
        var speed = 0.5;
        var opacityAnimation = OpacityAnimation(from, to, speed);
        await opacityAnimation.RunAsync(this);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Opacity = to;
        });
    }

    private async Task FullToClosedAnimation()
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }
        
        var from = 1d;
        var to = 0d;
        var speed = 0.3;
        var opacityAnimation = OpacityAnimation(from, to, speed);
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
        vm.GalleryVerticalAlignment = VerticalAlignment.Bottom;
        vm.IsGalleryCloseIconVisible = false;

        var from = 0;
        var to = vm.GalleryItemSize + 22;
        var speed = 0.3;
        var heightAnimation = HeightAnimation(from, to, speed);

        await heightAnimation.RunAsync(this);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Height = to;
            IsVisible = SettingsHelper.Settings.Gallery.IsBottomGalleryShown;
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
        vm.GalleryVerticalAlignment = VerticalAlignment.Bottom;
        vm.IsGalleryCloseIconVisible = false;
        
        var from = vm.GalleryItemSize + 22;
        var to = 0;
        var speed = 0.5;
        var heightAnimation = HeightAnimation(from, to, speed);

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
        vm.GalleryVerticalAlignment = VerticalAlignment.Bottom;
        vm.GalleryOrientation = Orientation.Vertical;
        vm.IsGalleryCloseIconVisible = true;
        
        
        var from = vm.GalleryItemSize + 22;
        var to = desktop.MainWindow.Bounds.Height - vm.TitlebarHeight - vm.BottombarHeight;
        var speed = 0.5;
        var heightAnimation = HeightAnimation(from, to, speed);
        await heightAnimation.RunAsync(this);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Height = to;
        });
        
        vm.GalleryStretch = Stretch.Uniform;
    }

    private async Task FullToBottomAnimation()
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }
        
        vm.GalleryVerticalAlignment = VerticalAlignment.Bottom;
        vm.IsGalleryCloseIconVisible = false;
        
        var from = Bounds.Height;
        var to = vm.GalleryItemSize + 22;
        var speed = 0.7;
        var heightAnimation = HeightAnimation(from, to, speed);
        await heightAnimation.RunAsync(this);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Height = to;
            vm.GalleryOrientation = Orientation.Horizontal;
            vm.GalleryStretch = Stretch.UniformToFill;
        });

    }

    private async Task PlayToggleBottomOpenAnimation()
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            IsVisible = true;
        });
        vm.GalleryVerticalAlignment = VerticalAlignment.Bottom;
        if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
        {
            vm.GalleryStretch = Stretch.UniformToFill;
        }
        else
        {
            
        }

        var from = Bounds.Height;
        var speed = 0.5;
        vm.IsGalleryCloseIconVisible = false;
        var to = SettingsHelper.Settings.Gallery.IsBottomGalleryShown ? vm.GalleryItemSize + 22 : 0d;
        var heightAnimation = new Animation
        {
            Duration = TimeSpan.FromSeconds(speed),
            Easing = new SplineEasing(),
            FillMode = FillMode.Both,
            Children =
            {
                new KeyFrame
                {
                    Setters =
                    {
                        new Setter
                        {
                            Property = HeightProperty,
                            Value = from
                        }
                    },
                    Cue = new Cue(0d)
                },
                new KeyFrame
                {
                    Setters =
                    {
                        new Setter
                        {
                            Property = HeightProperty,
                            Value = to
                        }
                    },
                    Cue = new Cue(1d)
                },
            }
        };
        await heightAnimation.RunAsync(this);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Height = to;
            IsVisible = SettingsHelper.Settings.Gallery.IsBottomGalleryShown;
            WindowHelper.SetSize(vm);
        });
        if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
        {
            vm.GalleryOrientation = Orientation.Horizontal;
            vm.GalleryStretch = Stretch.UniformToFill;
        }
        else
        {
            
        }
    }

    public async Task PlayClosingAnimation()
    {
        if (DataContext is not MainViewModel vm || !IsVisible)
        {
            return;
        }

        var speed = SettingsHelper.Settings.Gallery.IsBottomGalleryShown ? 0.5 : 0.3;
        var from = Bounds.Height;
        if (!SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
        {
            // Do opacity animation
            var opacityAnimation = new Animation
            {
                Duration = TimeSpan.FromSeconds(speed),
                Children =
                {
                    new KeyFrame
                    {
                        Setters =
                        {
                            new Setter
                            {
                                Property = OpacityProperty,
                                Value = 1d
                            }
                        },
                        Cue = new Cue(1d)
                    },
                    new KeyFrame
                    {
                        Setters =
                        {
                            new Setter
                            {
                                Property = OpacityProperty,
                                Value = 0d
                            }
                        },
                        Cue = new Cue(0d)
                    }
                }
            };
            await opacityAnimation.RunAsync(this);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Height = 0;
                IsVisible = false;
            });
            return;
        }
        vm.IsGalleryCloseIconVisible = false;

        var heightAnimation = new Animation
        {
            Duration = TimeSpan.FromSeconds(speed),
            Easing = new SplineEasing(),
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame
                {
                    Setters =
                    {
                        new Setter
                        {
                            Property = HeightProperty,
                            Value = from
                        }
                    },
                    Cue = new Cue(0d)
                },
                new KeyFrame
                {
                    Setters =
                    {
                        new Setter
                        {
                            Property = HeightProperty,
                            Value = 0d
                        }
                    },
                    Cue = new Cue(1d)
                },
            }
        };
        await heightAnimation.RunAsync(this);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Height = vm.GalleryItemSize + 22;
            IsVisible = true;
            WindowHelper.SetSize(vm);
        });
        vm.GalleryOrientation = Orientation.Horizontal;
        vm.GalleryStretch = Stretch.UniformToFill;
        GalleryNavigation.CenterScrollToSelectedItem(vm);
    }

    public async Task PlayOpenAnimation()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            IsVisible = true;
        });
        
        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        double to;
        if (GalleryFunctions.IsFullGalleryOpen)
        {
            to = double.NaN;
            vm.GalleryVerticalAlignment = VerticalAlignment.Stretch;
            vm.GalleryOrientation = Orientation.Vertical;
            vm.IsGalleryCloseIconVisible = true;
            vm.GalleryStretch = Stretch.Uniform;
        }
        else
        {
            to = vm.GalleryItemSize + 22;
            vm.GalleryVerticalAlignment = VerticalAlignment.Bottom;
            vm.GalleryOrientation = Orientation.Horizontal;
            vm.IsGalleryCloseIconVisible = false;
            vm.GalleryStretch = Stretch.UniformToFill;
        }
        
        var heightAnimation = new Animation
        {
            Duration = TimeSpan.FromSeconds(0.4),
            Easing = new SplineEasing(),
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame
                {
                    Setters =
                    {
                        new Setter
                        {
                            Property = HeightProperty,
                            Value = 0d
                        }
                    },
                    Cue = new Cue(0d)
                },
                new KeyFrame
                {
                    Setters =
                    {
                        new Setter
                        {
                            Property = HeightProperty,
                            Value = to
                        }
                    },
                    Cue = new Cue(1d)
                },
            }
        };
        await heightAnimation.RunAsync(this);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Height = to;
            WindowHelper.SetSize(vm);
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