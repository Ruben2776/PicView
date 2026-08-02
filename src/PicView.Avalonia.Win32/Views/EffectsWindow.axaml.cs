using Avalonia;
using Avalonia.Media;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.UI;
using PicView.Core.Config;
using PicView.Core.Localization;

namespace PicView.Avalonia.Win32.Views;

public partial class EffectsWindow : GenericWindow
{
    public EffectsWindow(EffectsWindowConfig config)
    {
        InitializeComponent();

        if (Settings.Theme.GlassTheme)
        {
            IconBorder.Background = Brushes.Transparent;
            IconBorder.BorderThickness = new Thickness(0);
            MinimizeButton.Background = Brushes.Transparent;
            MinimizeButton.BorderThickness = new Thickness(0);
            CloseButton.Background = Brushes.Transparent;
            CloseButton.BorderThickness = new Thickness(0);
            BorderRectangle.Height = 0;
            TitleText.Background = Brushes.Transparent;
            TitleBarPanel.Background = Brushes.Transparent;
            
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
        
        GenericWindowHelper.GenericWindowInitialize(this, TranslationManager.Translation.Effects, true, config.WindowProperties);
    }
}