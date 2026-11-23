using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.Views.UC;
using PicView.Avalonia.Views.UC.Buttons;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.DebugTools;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.UI;

/// <summary>
/// Provides UI-related helper methods and properties
/// </summary>
public static class UIHelper
{
    
    public static void CloseMenus(MainWindowViewModel vm)
    {
        vm.TopTitlebarViewModel.CloseMenu();
        vm.TopTitlebarViewModel.CloseDropDownMenu();
        vm.TopTitlebarViewModel.DropDownMenu.CloseMenus();
    }
    
    public static ClickArrowRight? GetClickArrowRight(MainWindowViewModel vm)
    {
        if (vm.WindowTabs.ActiveTab.CurrentValue.CurrentView.Value is NavigableView nav)
        {
            return nav.ClickArrowRight;
        }
        return null;
    }
    
    public static ClickArrowLeft? GetClickArrowLeft(MainWindowViewModel vm)
    {
        if (vm.WindowTabs.ActiveTab.CurrentValue.CurrentView.Value is NavigableView nav)
        {
            return nav.ClickArrowLeft;
        }
        return null;
    }
    
    public static HoverBar? GetHoverBar(MainWindowViewModel vm)
    {
        if (vm.WindowTabs.ActiveTab.CurrentValue.CurrentView.Value is NavigableView nav)
        {
            return nav.HoverBar;
        }

        return null;
    }
    
    private const string BoldFontLocation = "avares://PicView.Avalonia/Assets/Fonts/Roboto-Medium.ttf#Roboto";
    public static FontFamily BoldFontFamily => new(BoldFontLocation);
    private const string MediumFontLocation = "avares://PicView.Avalonia/Assets/Fonts/Roboto-Medium.ttf#Roboto";
    public static FontFamily MediumFontFamily => new(MediumFontLocation);

    /// <summary>
    /// Centers the window or gallery based on current state
    /// </summary>
    public static void Center(MainWindowViewModel vm, MainWindow mainWindow)
    {
        if (vm.WindowTabs.ActiveTab.CurrentValue.Gallery.IsGalleryExpanded.CurrentValue)
        {
            GalleryHelper.CenterGallery(vm);
        }
        else
        {
            WindowFunctions.CenterWindowOnScreen(mainWindow);
        }
    }

    public static void SetButtonInterval(RepeatButton? button)
    {
        button?.Interval = (int)TimeSpan.FromSeconds(Settings.UIProperties.NavSpeed).TotalMilliseconds;
    }

    public static void SetButtonInterval(IconButton? button)
    {
        button?.Interval = (int)TimeSpan.FromSeconds(Settings.UIProperties.NavSpeed).TotalMilliseconds;
    }

    public static SolidColorBrush GetBrush(string resourceName) =>
        new(GetColor(resourceName));

    public static Color GetColor(string resourceName)
    {
        if (!Application.Current.TryGetResource(resourceName,
                Application.Current.RequestedThemeVariant, out var textColor))
        {
            return default;
        }

        return textColor is not Color color ? default : color;
    }

    public static SolidColorBrush? GetSolidColorBrush(string resourceName)
    {
        if (!Application.Current.TryGetResource(resourceName,
        Application.Current.RequestedThemeVariant, out var textColor))
        {
            return null;
        }

        return textColor as SolidColorBrush ?? null;
    }

    public static void SwitchHoverBorderClass(Control control)
    {
        control.Classes.Remove("noBorderHover");
        control.Classes.Add("hover");
    }

    public static SolidColorBrush GetMenuBackgroundColor() =>
        GetBrush("MenuBackgroundColor");
    
    public static SolidColorBrush GetWindowBackgroundColor() =>
        GetBrush("WindowMainBackgroundColor");
}