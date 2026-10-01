using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using PicView.Avalonia.Controllers;

namespace PicView.Avalonia.Views.Main;

public partial class KeybindingsView : UserControl
{
    public KeybindingsViewController? Controller;

    public KeybindingsView()
    {
        InitializeComponent();

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Controller = new KeybindingsViewController(this);
        Controller.Initialize();
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        Controller?.Dispose();
        Controller = null;
    }

    private void MoveWindow(object? sender, PointerPressedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is Window window)
        {
            window.BeginMoveDrag(e);
        }
    }
}
