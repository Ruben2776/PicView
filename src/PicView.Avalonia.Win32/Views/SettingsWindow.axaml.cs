using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using PicView.Core.Config;
using PicView.Core.Localization;

namespace PicView.Avalonia.Win32.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        if (SettingsHelper.Settings.Theme.GlassTheme)
        {
            TopWindowBorder.Background = Brushes.Transparent;
            TopWindowBorder.BorderThickness = new Thickness(0);
            
            CloseButton.Background = Brushes.Transparent;
            CloseButton.BorderThickness = new Thickness(0);
            MinimizeButton.Background = Brushes.Transparent;
            MinimizeButton.BorderThickness = new Thickness(0);
            
            TitleText.Background = Brushes.Transparent;
            
            if (!Application.Current.TryGetResource("SecondaryTextColor",
                    Application.Current.RequestedThemeVariant, out var textColor))
            {
                return;
            }

            if (textColor is not Color color)
            {
                return;
            }
            
            TitleText.Foreground = new SolidColorBrush(color);
            MinimizeButton.Foreground = new SolidColorBrush(color);
            CloseButton.Foreground = new SolidColorBrush(color);
        }
        else if (!SettingsHelper.Settings.Theme.Dark)
        {
            ParentBorder.Background = new SolidColorBrush(Color.FromArgb(114,132, 132, 132));
        }
        Loaded += delegate
        {
            MinWidth = Width;
            Title = TranslationHelper.GetTranslation("Settings") + " - PicView";
        };
        KeyDown += (_, e) =>
        {
            if (e.Key is Key.Escape)
            {
                Close();
            }
        };

        Closing += async delegate
        {
            Hide();
            await SettingsHelper.SaveSettingsAsync();
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