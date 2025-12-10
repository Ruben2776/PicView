using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views.UC;
using PicView.Avalonia.WindowBehavior;
using R3.Avalonia;
using BottomBar = PicView.Avalonia.Views.UC.BottomBar;
using GalleryAnimationControlView = PicView.Avalonia.Views.Gallery.GalleryAnimationControlView;
using MainView = PicView.Avalonia.Views.Main.MainView;

namespace PicView.Avalonia.UI;

/// <summary>
/// Provides UI-related helper methods and properties
/// </summary>
public static class UIHelper
{
    #region Controls

    public static MainView? GetMainView { get; private set; }
    public static Control? GetTitlebar { get; private set; }
    public static EditableTitlebar? GetEditableTitlebar { get; private set; }
    public static GalleryAnimationControlView? GetGalleryView { get; private set; }
    public static BottomBar? GetBottomBar { get; private set; }
    public static HoverBar? GetHoverBar { get; private set; }
    
    public static ToolTipMessage? GetToolTipMessage { get; private set; }


    public static AvaloniaRenderingFrameProvider? GetFrameProvider { get; private set; }

    public static void SetFrameProvider(AvaloniaRenderingFrameProvider frameProvider) =>
        GetFrameProvider = frameProvider;

    /// <summary>
    /// Sets up control references from the main desktop application
    /// </summary>
    public static void SetControls(IClassicDesktopStyleApplicationLifetime desktop)
    {
        GetMainView = desktop.MainWindow?.FindControl<MainView>("MainView");
        GetTitlebar = desktop.MainWindow?.FindControl<Control>("Titlebar");
        GetEditableTitlebar = GetTitlebar?.FindControl<EditableTitlebar>("EditableTitlebar");
        GetGalleryView = GetMainView?.MainGrid.GetControl<GalleryAnimationControlView>("GalleryView");
        GetBottomBar = desktop.MainWindow?.FindControl<BottomBar>("BottomBar");
        GetToolTipMessage = GetMainView?.MainGrid.FindControl<ToolTipMessage>("ToolTipMessage");
    }

    public static void AddHoverBar(MainViewModel vm)
    {
        if (GetHoverBar is not null)
        {
            return;
        }
        GetHoverBar = new HoverBar();
        GetMainView.MainGrid.Children.Add(GetHoverBar);
        _ = new HoverFadeButtonHandler(GetHoverBar, vm, GetHoverBar.BottomBorder);
        MainWindowViewModel.HoverBarSubscription();
    }

    #endregion

    #region Helper functions
    
    private const string BoldFontLocation = "avares://PicView.Avalonia/Assets/Fonts/Roboto-Medium.ttf#Roboto";
    public static FontFamily BoldFontFamily => new(BoldFontLocation);
    private const string MediumFontLocation = "avares://PicView.Avalonia/Assets/Fonts/Roboto-Medium.ttf#Roboto";
    public static FontFamily MediumFontFamily => new(MediumFontLocation);

    public static void ShowMainContextMenu()
    {
        if (GetMainView.Resources.TryGetResource("MainContextMenu", Application.Current.ActualThemeVariant,
                out var value)
            && value is ContextMenu mainContextMenu)
        {
            mainContextMenu.Open();
        }
    }

    /// <summary>
    /// Centers the window or gallery based on current state
    /// </summary>
    public static void Center(MainViewModel? vm)
    {
        if (vm is null)
        {
            return;
        }

        if (GalleryFunctions.IsFullGalleryOpen)
        {
            GalleryFunctions.CenterGallery(vm);
        }
        else
        {
            WindowFunctions.CenterWindowOnScreen();
        }
    }

    /// <inheritdoc cref="Center"/>
    public static async Task CenterAsync(MainViewModel? vm)
    {
        if (vm is null)
        {
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() => { Center(vm); });
    }

    /// <summary>
    ///     Scrolls to the end of the gallery if the <paramref name="last" /> parameter is true.
    /// </summary>
    /// <param name="last">True to scroll to the end of the gallery.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task ScrollToEndIfNecessary(bool last)
    {
        if (!Settings.Gallery.IsBottomGalleryShown)
        {
            return;
        }

        if (last)
        {
            await Dispatcher.UIThread.InvokeAsync(() => { GetGalleryView.GalleryListBox.ScrollToEnd(); });
        }
        else
        {
            await Dispatcher.UIThread.InvokeAsync(() => { GetGalleryView.GalleryListBox.ScrollToHome(); });
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

    #endregion
}