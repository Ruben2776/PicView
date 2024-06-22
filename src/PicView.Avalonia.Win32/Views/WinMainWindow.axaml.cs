using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using PicView.Avalonia.Helpers;
using PicView.Avalonia.Keybindings;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;

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
        };
        PointerPressed += (_, e) => MoveWindow(e);
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        desktop.ShutdownRequested += async (_, e) =>
        {
            await WindowHelper.WindowClosingBehavior(this);
        };
    }

    private void MoveWindow(PointerPressedEventArgs e)
    {
        if (VisualRoot is null) { return; }
        if (!MainKeyboardShortcuts.ShiftDown) { return; }

        var hostWindow = (Window)VisualRoot;
        hostWindow?.BeginMoveDrag(e);
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