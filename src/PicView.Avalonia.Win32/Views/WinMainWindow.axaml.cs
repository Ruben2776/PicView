using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Config;

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
                WindowResizing.HandleWindowResize(this, size);
            });
            ScalingChanged += (_, _) =>
            {
                ScreenHelper.UpdateScreenSize(this);
                WindowResizing.SetSize(DataContext as MainViewModel);
            };
            PointerExited += (_, _) =>
            {
                DragAndDropHelper.RemoveDragDropView();
            };
        };
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        desktop.ShutdownRequested += async (_, e) =>
        {
            await WindowFunctions.WindowClosingBehavior(this);
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

        if (SettingsHelper.Settings.WindowProperties.AutoFit)
        {
            return;
        }
        var wm = (MainViewModel)DataContext;
        WindowResizing.SetSize(wm);
    }
}