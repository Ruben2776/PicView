using Avalonia;
using Avalonia.Media;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.UI;

namespace PicView.Avalonia.Win32.Views;

public partial class PrintPreviewWindow : GenericWindow
{
    public PrintPreviewWindow()
    {
        InitializeComponent();

        ThemeUpdates();
    }

    private void ThemeUpdates()
    {
        if (!Settings.Theme.Dark)
        {
            PrintPreviewView.Background = UIHelper.GetMenuBackgroundColor();
        }

        // Glass/Transparent theme support
        if (!Settings.Theme.GlassTheme)
        {
            return;
        }

        PrintPreviewView.Background = Brushes.Transparent;
        IconBorder.Background = Brushes.Transparent;
        IconBorder.BorderThickness = new Thickness(0);
        MinimizeButton.Background = Brushes.Transparent;
        MinimizeButton.BorderThickness = new Thickness(0);
        CloseButton.Background = Brushes.Transparent;
        CloseButton.BorderThickness = new Thickness(0);
        BorderRectangle.Height = 0;
        TitleText.Background = Brushes.Transparent;
        var brush = UIHelper.GetBrush("SecondaryTextColor");
        TitleText.Foreground = brush;
        MinimizeButton.Foreground = brush;
        CloseButton.Foreground = brush;
    }

}