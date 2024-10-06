using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using PicView.Core.Config;
using PicView.Core.Localization;

namespace PicView.Avalonia.Win32.Views;

public partial class ExifWindow : Window
{
    public ExifWindow()
    {
        InitializeComponent();
        if (SettingsHelper.Settings.Theme.GlassTheme)
        {
            TopWindowBorder.Background = Brushes.Transparent;
            
            CloseButton.Background = Brushes.Transparent;
            CloseButton.BorderThickness = new Thickness(0);
            MinimizeButton.Background = Brushes.Transparent;
            MinimizeButton.BorderThickness = new Thickness(0);
            
            if (!Application.Current.TryGetResource("SecondaryTextColor",
                    Application.Current.RequestedThemeVariant, out var textColor))
            {
                return;
            }

            if (textColor is not Color color)
            {
                return;
            }
            
            MinimizeButton.Foreground = new SolidColorBrush(color);
            CloseButton.Foreground = new SolidColorBrush(color);
            RecycleText.Foreground = new SolidColorBrush(color);
            DuplicateText.Foreground = new SolidColorBrush(color);
            OptimizeText.Foreground = new SolidColorBrush(color);
            OpenWithText.Foreground = new SolidColorBrush(color);
            LocateOnDiskText.Foreground = new SolidColorBrush(color);
        }
        else if (!SettingsHelper.Settings.Theme.Dark)
        {
            ParentBorder.Background = new SolidColorBrush(Color.FromArgb(114,132, 132, 132));
        }
        Title = TranslationHelper.GetTranslation("ImageInfo") + " - PicView";
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

    private void Close(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Minimize(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }
}