using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using PicView.Avalonia.Views;
using PicView.Core.Config;
using PicView.Core.Localization;

namespace PicView.Avalonia.MacOS.Views;

public partial class ExifWindow : Window
{
    public ExifWindow()
    {
        InitializeComponent();
        if (!SettingsHelper.Settings.Theme.Dark)
        {
            WindowBorder.Background = Brushes.Transparent;
            XExifView.Background = Brushes.Transparent;
        }
        Title = TranslationHelper.Translation.ImageInfo + " - PicView";
        KeyDown += (_, e) =>
        {
            if (e.Key is Key.Escape)
            {
                Close();
            }
        };
    }

    private void MoveWindow(object? sender, PointerPressedEventArgs e)
    {
        if (VisualRoot is null) { return; }

        var hostWindow = (Window)VisualRoot;
        hostWindow?.BeginMoveDrag(e);
    }
}