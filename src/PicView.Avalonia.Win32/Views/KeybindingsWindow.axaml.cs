using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using PicView.Core.Localization;

namespace PicView.Avalonia.Win32.Views;

public partial class KeybindingsWindow : Window
{
    public KeybindingsWindow()
    {
        InitializeComponent();
        Loaded += (sender, e) =>
        {
            MinWidth = MaxWidth = Width;
            Title = $"{TranslationHelper.GetTranslation("ApplicationShortcuts")}  - PicView";
        };
    }

    private void MoveWindow(object? sender, PointerPressedEventArgs e)
    {
        if (VisualRoot is null) { return; }

        var hostWindow = (Window)VisualRoot;
        hostWindow?.BeginMoveDrag(e);
    }

    private void Close(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Minimize(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }
}