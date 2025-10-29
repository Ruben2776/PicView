using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace PicView.Avalonia.ColorManagement;

public static class GlassThemeHelper
{
    public static void GlassThemeUpdates()
    {
        if (!Application.Current.TryGetResource("MainTextColor",
                ThemeVariant.Dark, out var textColor))
        {
            return;
        }

        if (textColor is not Color mainColor)
        {
            return;
        }

        Application.Current.Resources["MainTextColor"] = mainColor;
        
        Application.Current.Resources["MainButtonBackgroundColor"] = Color.Parse("#4D000000");
        Application.Current.Resources["MainBackgroundColor"] = Color.Parse("#4D000000");
        
        Application.Current.Resources["SecondaryButtonBackgroundColor"] = Color.Parse("#D1464646");
        Application.Current.Resources["SecondaryBackgroundColor"] = Color.Parse("#7E5B5B5B");
        
        Application.Current.Resources["AltBackgroundColor"] = Color.Parse("#7E5B5B5B");
        Application.Current.Resources["AltBackgroundHoverColor"] = Color.Parse("#59E6E6E6");
        
        Application.Current.Resources["DisabledBackgroundColor"] = Color.Parse("#5D5B5B5B");

        Application.Current.Resources["MainBorderColor"] = Colors.Transparent;
        Application.Current.Resources["SecondaryBorderColor"] = Color.Parse("#48000000");
        Application.Current.Resources["TertiaryBorderColor"] = Colors.Transparent;
        
        Application.Current.Resources["OuterBorderColor"] = Color.Parse("#48000000");
        
        Application.Current.Resources["ContextMenuTextColor"] = mainColor;
        Application.Current.Resources["ContextMenuBackgroundColor"] = Color.Parse("#A1464646");
            
        Application.Current.Resources["MenuBackgroundColor"] = Color.Parse("#6D5B5B5B");
        Application.Current.Resources["MenuButtonColor"] = Color.Parse("#50797979");
        
        Application.Current.Resources["WindowBorderColor"] = Color.Parse("#15FFFFFF");

        Application.Current.Resources["DropDownBgColor"] = Color.Parse("#CC464646");

        Application.Current.Resources["WindowButtonBackgroundColor"] = Color.Parse("#CC464646");

        Application.Current.Resources["SoftenColor"] = Color.Parse("#49878787");
        
    }

    public static void ApplyTransparentStyle(Menu borderLike)
    {
        borderLike.Background = Brushes.Transparent;
        borderLike.BorderThickness = new Thickness(0);
    }

    public static void ApplyTransparentStyle(UserControl control)
    {
        control.Background = Brushes.Transparent;
        control.BorderThickness = new Thickness(0);
    }

    public static void ApplyTransparentStyle(Button button)
    {
        button.Background = Brushes.Transparent;
        button.BorderThickness = new Thickness(0);
    }

    public static void ApplyTransparentStyle(Border borderLike)
    {
        borderLike.Background = Brushes.Transparent;
        borderLike.BorderThickness = new Thickness(0);
    }
}
