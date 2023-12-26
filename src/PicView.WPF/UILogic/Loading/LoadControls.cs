using PicView.WPF.Views.UserControls.Buttons;
using PicView.WPF.Views.UserControls.Menus;
using PicView.WPF.Views.UserControls.Misc;
using System.Diagnostics;
using System.Windows;
using PicView.Core.Config;
using static PicView.WPF.UILogic.UC;

namespace PicView.WPF.UILogic.Loading;

internal static class LoadControls
{
    // Load controls

    /// <summary>
    /// Loads ClickArrow and adds it to the window
    /// </summary>
    internal static void LoadClickArrow(bool right)
    {
        if (right)
        {
            GetClickArrowRight = new ClickArrow(true)
            {
                Focusable = false,
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Collapsed,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            ConfigureWindows.GetMainWindow.ParentContainer.Children.Add(GetClickArrowRight);
        }
        else
        {
            GetClickArrowLeft = new ClickArrow(false)
            {
                Focusable = false,
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Collapsed,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            ConfigureWindows.GetMainWindow.ParentContainer.Children.Add(GetClickArrowLeft);

#if DEBUG
            var x = right ? nameof(GetClickArrowRight) : nameof(GetClickArrowLeft);
            Trace.WriteLine(x + " loaded ");
#endif
        }
    }

    /// <summary>
    /// Loads x2 and adds it to the window
    /// </summary>
    internal static void Loadx2()
    {
        GetX2 = new X2
        {
            Focusable = false,
            VerticalAlignment = VerticalAlignment.Top,
            Visibility = Visibility.Collapsed,
            HorizontalAlignment = HorizontalAlignment.Right,
            ToolTip = Application.Current.Resources["Close"]
        };

        ConfigureWindows.GetMainWindow.ParentContainer.Children.Add(GetX2);

#if DEBUG
        Trace.WriteLine(nameof(GetX2) + " loaded ");
#endif
    }

    /// <summary>
    /// Loads Minus and adds it to the window
    /// </summary>
    internal static void LoadMinus()
    {
        GetMinus = new Minus
        {
            Focusable = false,
            VerticalAlignment = VerticalAlignment.Top,
            Visibility = Visibility.Collapsed,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 0, 90, 0)
        };

        ConfigureWindows.GetMainWindow.ParentContainer.Children.Add(GetMinus);

#if DEBUG
        Trace.WriteLine(nameof(GetMinus) + " loaded ");
#endif
    }

    /// <summary>
    /// Loads RestoreButton and adds it to the window
    /// </summary>
    internal static void LoadRestoreButton()
    {
        GetRestoreButton = new RestoreButton
        {
            Focusable = false,
            VerticalAlignment = VerticalAlignment.Top,
            Visibility = Visibility.Collapsed,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 0, 50, 0)
        };

        ConfigureWindows.GetMainWindow.ParentContainer.Children.Add(GetRestoreButton);

#if DEBUG
        Trace.WriteLine(nameof(GetRestoreButton) + " loaded ");
#endif
    }

    /// <summary>
    /// Loads GalleryShortcut and adds it to the window
    /// </summary>
    internal static void LoadGalleryShortcut()
    {
        GetGalleryShortcut = new GalleryShortcut
        {
            Focusable = false,
            VerticalAlignment = VerticalAlignment.Bottom,
            Visibility = Visibility.Collapsed,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        ConfigureWindows.GetMainWindow.ParentContainer.Children.Add(GetGalleryShortcut);

#if DEBUG
        Trace.WriteLine(nameof(GetGalleryShortcut) + " loaded ");
#endif
    }

    /// <summary>
    /// Loads FileMenu and adds it to the window
    /// </summary>
    internal static void LoadFileMenu()
    {
        GetFileMenu = new FileMenu
        {
            Focusable = false,
            Opacity = 0,
            Visibility = Visibility.Hidden,
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 168, 0)
        };

        ConfigureWindows.GetMainWindow.ParentContainer.Children.Add(GetFileMenu);

#if DEBUG
        Trace.WriteLine(nameof(GetFileMenu) + " loaded ");
#endif
    }

    /// <summary>
    /// Loads ImageSettingsMenu and adds it to the window
    /// </summary>
    internal static void LoadImageSettingsMenu()
    {
        GetImageSettingsMenu = new ImageSettings
        {
            Focusable = false,
            Opacity = 0,
            Visibility = Visibility.Hidden,
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 58, 0)
        };

        ConfigureWindows.GetMainWindow.ParentContainer.Children.Add(GetImageSettingsMenu);

#if DEBUG
        Trace.WriteLine(nameof(GetImageSettingsMenu) + " loaded ");
#endif
    }

    /// <summary>
    /// Loads QuickSettingsMenu and adds it to the window
    /// </summary>
    internal static void LoadQuickSettingsMenu()
    {
        GetQuickSettingsMenu = new QuickSettingsMenu
        {
            Focusable = false,
            Opacity = 0,
            Visibility = Visibility.Hidden,
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(81, 0, 0, 0)
        };

        GetQuickSettingsMenu.SetFit.IsChecked = SettingsHelper.Settings.WindowProperties.AutoFit;

        ConfigureWindows.GetMainWindow.ParentContainer.Children.Add(GetQuickSettingsMenu);

#if DEBUG
        Trace.WriteLine(nameof(GetQuickSettingsMenu) + " loaded ");
#endif
    }

    /// <summary>
    /// Loads FunctionsMenu and adds it to the window
    /// </summary>
    internal static void LoadToolsAndEffectsMenu()
    {
        GetToolsAndEffectsMenu = new ToolsAndEffectsMenu
        {
            Focusable = false,
            Opacity = 0,
            Visibility = Visibility.Hidden,
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(67, 0, 0, 0)
        };

        ConfigureWindows.GetMainWindow.ParentContainer.Children.Add(GetToolsAndEffectsMenu);

#if DEBUG
        Trace.WriteLine(nameof(GetToolsAndEffectsMenu) + " loaded ");
#endif
    }

    // Tooltip

    /// <summary>
    /// Loads TooltipStyle and adds it to the window
    /// </summary>
    internal static void LoadTooltipStyle()
    {
        GetToolTipMessage = new ToolTipMessage
        {
            Focusable = false,
            Opacity = 0,
            Visibility = Visibility.Hidden
        };

        ConfigureWindows.GetMainWindow.ParentContainer.Children.Add(GetToolTipMessage);

#if DEBUG
        Trace.WriteLine(nameof(GetToolTipMessage) + " loaded ");
#endif
    }

    /// <summary>
    /// Loads AutoScrollSign and adds it to the window
    /// </summary>
    internal static void LoadAutoScrollSign()
    {
        GetAutoScrollSign = new AutoScrollSign
        {
            Focusable = false,
            Opacity = 0,
            Visibility = Visibility.Hidden,
            Width = 20,
            Height = 35
        };

        ConfigureWindows.GetMainWindow.TopLayer.Children.Add(GetAutoScrollSign);

#if DEBUG
        Trace.WriteLine(nameof(GetAutoScrollSign) + " loaded ");
#endif
    }

    /// <summary>
    /// Loads Croppingtool and adds it to the window
    /// </summary>
    internal static void LoadCroppingTool()
    {
        GetCroppingTool = new CroppingTool();

        ConfigureWindows.GetMainWindow.ParentContainer.Children.Add(GetCroppingTool);

#if DEBUG
        Trace.WriteLine(nameof(GetCroppingTool) + " loaded ");
#endif
    }

    /// <summary>
    /// Loads ColorPicker and adds it to the window
    /// </summary>
    internal static void LoadColorPicker()
    {
        GetColorPicker = new ColorPicker();

        ConfigureWindows.GetMainWindow.TopLayer.Children.Add(GetColorPicker);

#if DEBUG
        Trace.WriteLine(nameof(GetColorPicker) + " loaded ");
#endif
    }

    internal static void LoadQuickResize()
    {
        GetQuickResize = new QuickResize
        {
            Opacity = 0,
            Visibility = Visibility.Collapsed,
        };

        ConfigureWindows.GetMainWindow.ParentContainer.Children.Add(GetQuickResize);

#if DEBUG
        Trace.WriteLine(nameof(GetQuickResize) + " loaded ");
#endif
    }
}