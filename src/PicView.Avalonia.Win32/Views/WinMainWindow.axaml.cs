using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using PicView.Avalonia.DragAndDrop;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Win32.WindowImpl;
using PicView.Avalonia.WindowBehavior;
using R3;
using R3.Avalonia;

namespace PicView.Avalonia.Win32.Views;

public partial class WinMainWindow : Window
{
    private readonly AvaloniaRenderingFrameProvider _frameProvider;

    public WinMainWindow()
    {
        InitializeComponent();

        // initialize RenderingFrameProvider
        _frameProvider = new AvaloniaRenderingFrameProvider(GetTopLevel(this)!);
        UIHelper.SetFrameProvider(_frameProvider);

        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        Loaded += delegate
        {
            if (DataContext is not MainViewModel vm)
            {
                return;
            }

            // Keep window position when resizing
            ClientSizeProperty.Changed.ToObservable()
                .ObserveOn(_frameProvider)
                .Subscribe(size =>
                {
                    if (Win32Window.IsChangingWindowState || WindowState != WindowState.Normal)
                    {
                        return;
                    }

                    WindowResizing.HandleWindowResize(this, size);
                });
            ScalingChanged += (_, _) =>
            {
                ScreenHelper.UpdateScreenSize(this);
                WindowResizing.SetSize(DataContext as MainViewModel);
            };
            PointerExited += (_, _) => { DragAndDropHelper.RemoveDragDropView(); };

            Observable.EveryValueChanged(this, x => x.WindowState, _frameProvider).Subscribe(state =>
            {
                switch (state)
                {
                    case WindowState.FullScreen:
                        if (!Settings.WindowProperties.Fullscreen)
                        {
                            vm.PlatformWindowService.Fullscreen();
                        }

                        break;
                    case WindowState.Maximized:
                        if (!Settings.WindowProperties.Maximized)
                        {
                            vm.PlatformWindowService.Maximize();
                        }

                        break;
                    case WindowState.Normal:
                        if (Settings.WindowProperties.Fullscreen || Settings.WindowProperties.Maximized)
                        {
                            vm.PlatformWindowService.Restore();
                        }

                        break;
                }
            });
        };
    }

    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        e.Cancel = true;
        await WindowFunctions.WindowClosingBehavior(this);
        base.OnClosing(e);
    }

    private void Control_OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (DataContext == null)
        {
            return;
        }

        if (e is { HeightChanged: false, WidthChanged: false })
        {
            return;
        }

        if (Settings.WindowProperties.AutoFit)
        {
            return;
        }

        var wm = (MainViewModel)DataContext;
        WindowResizing.SetSize(wm);
    }

    protected override void OnClosed(EventArgs e)
    {
        _frameProvider.Dispose();
    }
}