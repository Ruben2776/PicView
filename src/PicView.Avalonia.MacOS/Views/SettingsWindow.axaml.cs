using Avalonia.Controls;
using Avalonia.Input;
using PicView.Core.Localization;

namespace PicView.Avalonia.MacOS.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        Loaded += (sender, e) =>
        {
            MinWidth = MaxWidth = Width;
            Height = 500;
            Title = TranslationHelper.Translation.Settings + " - PicView";
        };
    }

    private void MoveWindow(object? sender, PointerPressedEventArgs e)
    {
        if (VisualRoot is null) { return; }

        var hostWindow = (Window)VisualRoot;
        hostWindow?.BeginMoveDrag(e);
    }
}