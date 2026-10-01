using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using PicView.Avalonia.Controllers;

namespace PicView.Avalonia.Views.Main;

public partial class KeybindingsView : UserControl
{
    private KeybindingsViewController? _controller;

    public KeybindingsView()
    {
        InitializeComponent();

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        _controller = new KeybindingsViewController(this);
        _controller.Initialize();

        KeyDown += OnKeyDown;
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        KeyDown -= OnKeyDown;

        _controller?.Dispose();
        _controller = null;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        _controller?.HandleKeyDown(e);
    }

    private void MoveWindow(object? sender, PointerPressedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is Window window)
        {
            window.BeginMoveDrag(e);
        }
    }
}
