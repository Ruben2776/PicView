using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using PicView.Avalonia.Keybindings;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;
using ReactiveUI;

namespace PicView.Avalonia.Win32.Views;

public partial class WinMainWindow : Window
{
    public WinMainWindow()
    {
        InitializeComponent();
        Loaded += delegate
        {
            if (DataContext == null)
            {
                return;
            }

            // Keep window position when resizing
            ClientSizeProperty.Changed.Subscribe(size =>
            {
                WindowHelper.HandleWindowResize(this, size);
            });
            
            this.WhenAnyValue(x => x.WindowState).Subscribe(state =>
            {
                switch (state)
                {
                    case WindowState.Normal:
                        SettingsHelper.Settings.WindowProperties.Maximized = false;
                        SettingsHelper.Settings.WindowProperties.Fullscreen = false;
                        break;
                    case WindowState.Minimized:
                        break;
                    case WindowState.Maximized:
                        WindowHelper.Maximize();
                        break;
                    case WindowState.FullScreen:
                        //WindowHelper.Fullscreen(DataContext as MainViewModel, Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime);
                        break;
                }
            });
        };
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        desktop.ShutdownRequested += async (_, e) =>
        {
            await WindowHelper.WindowClosingBehavior(this);
        };
    }

    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        e.Cancel = true;
        await WindowHelper.WindowClosingBehavior(this);
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
        var wm = (MainViewModel)DataContext;
        WindowHelper.SetSize(wm);
    }
}