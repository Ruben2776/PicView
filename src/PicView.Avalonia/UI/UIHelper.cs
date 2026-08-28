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
    
    public static void CloseMenus(CoreViewModel core)
    {
        core.MainWindows.ActiveWindow.CurrentValue.TopTitlebarViewModel.CloseMenu();
        core.MainWindows.ActiveWindow.CurrentValue.TopTitlebarViewModel.CloseDropDownMenu();
        core.MainWindows.ActiveWindow.CurrentValue.TopTitlebarViewModel.DropDownMenu.CloseMenus();
    }
    
    public static ClickArrowRight? GetClickArrowRight(MainWindowViewModel vm)
    {
        if (vm.WindowTabs.ActiveTab.CurrentValue.CurrentView.CurrentValue is ImageViewer imageViewer)
        {
            return imageViewer.ClickArrowRight;
        }
        return null;
    }
    
    public static ClickArrowLeft? GetClickArrowLeft(MainWindowViewModel vm)
    {
        if (vm.WindowTabs.ActiveTab.CurrentValue.CurrentView.CurrentValue is ImageViewer imageViewer)
        {
            return imageViewer.ClickArrowLeft;
        }
        return null;
    }
    
    public static HoverBar? GetHoverBar()
    {
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return null;
        }

        if (core.MainWindows.ActiveWindow.CurrentValue.WindowTabs.ActiveTab.CurrentValue.CurrentView.CurrentValue is ImageViewer imageViewer)
        {
            return imageViewer.HoverBar;
        }

        return null;
    }
    
    private const string BoldFontLocation = "avares://PicView.Avalonia/Assets/Fonts/Roboto-Medium.ttf#Roboto";
    public static FontFamily BoldFontFamily => new(BoldFontLocation);
    private const string MediumFontLocation = "avares://PicView.Avalonia/Assets/Fonts/Roboto-Medium.ttf#Roboto";
    public static FontFamily MediumFontFamily => new(MediumFontLocation);

    public static void SetCtrlToZoomImage(MainWindowViewModel vm)
    {
        // Set source for ChangeCtrlZoomImage
        if (!Application.Current.TryGetResource("ScanEyeImage", Application.Current.RequestedThemeVariant, out var scanEyeImage ))
        {
            return;
        }
        if (!Application.Current.TryGetResource("LeftRightArrowsImage", Application.Current.RequestedThemeVariant, out var leftRightArrowsImage ))
        {
            return;
        }
        var isNavigatingWithCtrl = Settings.Zoom.CtrlZoom;
        vm.ChangeCtrlZoomImage.Value = isNavigatingWithCtrl ? leftRightArrowsImage as DrawingImage : scanEyeImage as DrawingImage;
    }

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
    
    public static T? GetResource<T>(object key)
    {
        if (!Application.Current.TryGetResource(key, Application.Current.RequestedThemeVariant, out var resource))
        {
            return default;
        }

        if (resource is T value)
        {
            return value;
        }
#if DEBUG
        DebugHelper.LogDebug(nameof(UIHelper), nameof(GetResource), "Missing resource: " + key);
#endif
        return default;
    }

    public static DrawingImage? GetIcon(string resourceName)
    {
        if (!Application.Current.TryGetResource(resourceName,
                Application.Current.RequestedThemeVariant, out var icon))
        {
            return null;
        }

        return icon as DrawingImage;
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

    public static void SetButtonHover(Control button, SolidColorBrush brush)
    {
        button.PointerEntered += (_, _) =>
        {
            brush.Color = GetColor("SecondaryTextColor");
        };
        button.PointerExited += (s, e) =>
        {
            brush.Color = GetColor("MainTextColor");
        };
    }

    public static void SwitchHoverClass(Control control)
    {
        control.Classes.Remove("altHover");
        control.Classes.Add("hover");
    }
    
    public static void SwitchAccentHoverClass(Control control)
    {
        control.Classes.Remove("altHover");
        control.Classes.Add("accentHover");
    }

    public static void SwitchHoverBorderClass(Control control)
    {
        control.Classes.Remove("noBorderHover");
        control.Classes.Add("hover");
    }

    public static SolidColorBrush? GetMenuBackgroundColor() =>
        GetBrush("MenuBackgroundColor");
}