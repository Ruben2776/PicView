using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Styling;
using PicView.Avalonia.CustomControls;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.ColorManagement;
public static class ThemeManager
{
    public enum Theme
    {
        Dark = 0,
        Light = 1,
        Glass = 2
    }
    
    public static void SetTheme(Theme theme)
    {
        var application = Application.Current;
        if (application is null)
            return;
        
        // StyleInclude breaks trimming and AOT

        switch (theme)
        {
            default:
                Settings.Theme.Dark = true;
                Settings.Theme.GlassTheme = false;
                application.RequestedThemeVariant = ThemeVariant.Dark;
                break;
            case Theme.Light:
                Settings.Theme.Dark = false;
                Settings.Theme.GlassTheme = false;
                application.RequestedThemeVariant = ThemeVariant.Light;
                break;
            case Theme.Glass:
                Settings.Theme.GlassTheme = true;
                application.RequestedThemeVariant = ThemeVariant.Light;
                break;
        }
        
        ColorManager.UpdateAccentColors(Settings.Theme.ColorTheme);
    }

    public static void DetermineTheme(Application? application, bool settingsExists)
    {
        if (!settingsExists)
        {
            // Use dark theme by default, until I can make a proper good light theme
            application.RequestedThemeVariant = ThemeVariant.Dark;
            
            // application.RequestedThemeVariant = application.ActualThemeVariant;
            // Settings.Theme.Dark = application.RequestedThemeVariant == ThemeVariant.Dark;
        }
        else
        {
            if (Settings.Theme.GlassTheme)
            {
                application.RequestedThemeVariant = ThemeVariant.Light;
            }
            else if (Settings.Theme.UseSystemTheme)
            {
                application.RequestedThemeVariant = ThemeVariant.Dark; // TODO : Figure out how to get the system theme
            }
            else
            {
                application.RequestedThemeVariant = Settings.Theme.Dark ? ThemeVariant.Dark : ThemeVariant.Light;
            }
        }
    }
    
    public static void SetColorTheme(int colorIndex)
    {
        ColorManager.UpdateAccentColors(colorIndex);
    }

    /// <summary>
    /// The style class, set in the Window theme, that turns a window's blur off.
    /// </summary>
    public const string NoAcrylicClass = "no-acrylic";

    /// <summary>
    /// Whether windows blur what is behind them: the setting, or always for the glass theme.
    /// </summary>
    public static bool IsAcrylicEnabled => Settings.Theme.UseAcrylic || Settings.Theme.GlassTheme;

    /// <summary>
    /// The view each tool window paints its background with, and the tool window background laid over it while the
    /// blur is off. Weak, so a closed window is not kept.
    /// </summary>
    private static readonly ConditionalWeakTable<Window, ToolWindowBackground> ToolWindowBackgrounds = new();

    private sealed class ToolWindowBackground(Control view)
    {
        public Control View { get; } = view;

        /// <summary>
        /// Takes the tool window background off again, back to whatever the view was given in XAML or code.
        /// </summary>
        public IDisposable? Override { get; set; }
    }

    /// <summary>
    /// Turns the window's blur on or off, as <see cref="IsAcrylicEnabled"/> says, and keeps doing so when
    /// <see cref="SetAcrylic"/> changes it. Without the blur, <paramref name="view"/> is painted with
    /// <see cref="BackgroundManager.GetToolWindowBg"/>; with it, the view keeps its own background.
    /// </summary>
    /// <param name="window">The tool window.</param>
    /// <param name="view">The control that paints the window's background.</param>
    /// <remarks>Call after <c>InitializeComponent</c>, on the UI thread.</remarks>
    public static void ApplyAcrylic(Window window, Control view)
    {
        if (ToolWindowBackgrounds.TryGetValue(window, out var previous))
        {
            previous.Override?.Dispose();
        }

        ToolWindowBackgrounds.AddOrUpdate(window, new ToolWindowBackground(view));
        ApplyAcrylic(window);
    }

    /// <summary>
    /// Turns the window's blur on or off, as <see cref="IsAcrylicEnabled"/> says, and paints the background view
    /// given to <see cref="ApplyAcrylic(Window, Control)"/>, if any. A Main Window paints its own background from
    /// its background choice.
    /// </summary>
    public static void ApplyAcrylic(Window window)
    {
        var isEnabled = IsAcrylicEnabled;
        window.Classes.Set(NoAcrylicClass, !isEnabled);

        if (!ToolWindowBackgrounds.TryGetValue(window, out var background))
        {
            return;
        }

        background.Override?.Dispose();
        background.Override = null;
        if (isEnabled || BackgroundPropertyOf(background.View) is not { } property)
        {
            return;
        }

        // Above the local value, so what the view sets for itself (a theme resource, or the glass or light
        // theme's brush from code) stays underneath and comes back when the blur is turned on again
        background.Override = background.View.SetValue(property, BackgroundManager.GetToolWindowBg(),
            BindingPriority.Animation);
    }

    private static StyledProperty<IBrush?>? BackgroundPropertyOf(Control control) => control switch
    {
        TemplatedControl => TemplatedControl.BackgroundProperty,
        Panel => Panel.BackgroundProperty,
        Border => Border.BackgroundProperty,
        _ => null
    };

    /// <summary>
    /// Turns the blur on or off for every open window. Windows opened later read the setting themselves.
    /// </summary>
    public static void SetAcrylic(bool isEnabled)
    {
        Settings.Theme.UseAcrylic = isEnabled;
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        foreach (var window in desktop.Windows)
        {
            ApplyAcrylic(window);
            if (window is MainWindow { DataContext: MainWindowViewModel vm })
            {
                // Its Transparent background choice depends on the blur
                BackgroundManager.Repaint(vm.WindowSettings);
            }
        }
    }
}
