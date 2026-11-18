using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Core.Config;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.WindowBehavior;

public static class WindowFunctions
{
    #region Window State
    
    /// <summary>
    /// Sizes and places a new window for the mode it is about to start in, before it is shown. An AutoFit window at App
    /// Start opens centered on its screen; with <paramref name="adjustPos"/>, a window takes the size and position in its
    /// Window Settings, which a window opened while PicView runs copied from its opener.
    /// </summary>
    /// <param name="isAppStart">Whether this is the first window at app start, the only one that may start fullscreen.</param>
    /// <param name="adjustPos">Whether to give the window the size and position in its Window Settings; false when it is
    /// placed already.</param>
    public static void HandleWindowScalingMode(MainWindow window, bool isAppStart, bool adjustPos = true)
    {
        if (Settings.WindowProperties.Margin < 0)
        {
            Settings.WindowProperties.Margin = GlobalSettingsViewModel.NegativeMarginReplacement;
        }

        if (window.DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        var starting = WindowModeSwitcher.Starting(vm, isAppStart);
        if (starting.Mode is not WindowMode.Normal)
        {
            InitializeWindowPosition(window);
        }
        else if (starting.AutoFit)
        {
            window.SizeToContent = SizeToContent.WidthAndHeight;
            if (adjustPos)
            {
                window.WindowStartupLocation = WindowStartupLocation.Manual;
                InitializeWindowSizeAndPosition(window);
            }
            else
            {
                window.WindowStartupLocation = isAppStart ? WindowStartupLocation.CenterScreen : WindowStartupLocation.Manual;
            }
        }
        else
        {
            window.WindowFitter.KeepSize();
            if (adjustPos)
            {
                InitializeWindowSizeAndPosition(window);
            }
        }
    }

    public static void ShowMinimizedWindow(Window window)
    {
        window.BringIntoView();
        window.WindowState = WindowState.Normal;
        window.Activate();
        window.Focus();
    }

    #endregion

    #region Window Size and Position

    /// <summary>
    /// Remembers the window's size and position in its Window Settings when it is in Normal mode. A window in another
    /// mode, or one the operating system shows in another state, keeps the last Normal ones.
    /// </summary>
    public static void RememberBounds(Window window)
    {
        if (Dispatcher.CurrentDispatcher.CheckAccess())
        {
            Set();
        }
        else
        {
            Dispatcher.CurrentDispatcher.Invoke(Set);
        }

        return;

        void Set()
        {
            if (window.DataContext is not MainWindowViewModel vm
                || vm.WindowModeState.CurrentValue.Mode is not WindowMode.Normal
                || window.WindowState is not WindowState.Normal)
            {
                return;
            }

            vm.WindowSettings.Bounds = new NormalBounds(window.Position.X, window.Position.Y, window.Bounds.Width,
                window.Bounds.Height);
        }
    }

    /// <summary>
    /// Centers the window in the work area of its own screen, after the next render, from the size it has then. It
    /// serves the user's Center action, where the window's size is not changing.
    /// </summary>
    /// <param name="top">Keeps it at the top of the work area instead of centering it vertically.</param>
    public static void CenterWindowOnScreen(MainWindow window, bool top = false) =>
        Dispatcher.UIThread.Post(() => window.WindowFitter.Center(top), DispatcherPriority.Render);

    public static void InitializeWindowSizeAndPosition(MainWindow window)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            Set();
        }
        else
        {
            Dispatcher.UIThread.Post(Set);
        }

        return;

        void Set()
        {
            if (window.DataContext is not MainWindowViewModel vm)
            {
                return;
            }

            var bounds = vm.WindowSettings.Bounds;
            window.Width = bounds.Width;
            window.Height = bounds.Height;
            PlaceFromSettings(window);
        }
    }

    public static void InitializeWindowPosition(MainWindow window)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            Set();
        }
        else
        {
            Dispatcher.UIThread.Post(Set);
        }

        return;

        void Set() => PlaceFromSettings(window);
    }

    /// <summary>
    /// Puts a window that is not shown yet at the position in its Window Settings, or in the center of the screen when
    /// that position is on none of the screens connected now, such as Windows' minimized position (-32000, -32000) or a
    /// monitor that has since been unplugged. Positions left of or above the primary screen are negative and valid.
    /// </summary>
    private static void PlaceFromSettings(Window window)
    {
        if (window.DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        var bounds = vm.WindowSettings.Bounds;
        var left = (int)bounds.Left;
        var top = (int)bounds.Top;
        if (!RememberedPlacement.IsOnAScreen(left, top, bounds.Width, bounds.Height, ScreenAreas(window)))
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            return;
        }

        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.Position = new PixelPoint(left, top);
    }

    /// <summary>
    /// The screens a window can open on, in the physical pixels its position is in. Empty when the platform reports
    /// none.
    /// </summary>
    public static ScreenArea[] ScreenAreas(Window window)
    {
        var screens = window.Screens.All;
        var areas = new ScreenArea[screens.Count];
        for (var i = 0; i < areas.Length; i++)
        {
            var bounds = screens[i].Bounds;
            areas[i] = new ScreenArea(bounds.X, bounds.Y, bounds.Width, bounds.Height);
        }

        return areas;
    }

    #endregion

    #region Window Drag and Behavior

    public static void WindowDragAndDoubleClickBehavior(MainWindow window, PointerPressedEventArgs e)
    {
        if (e.ClickCount == 2 && e.GetCurrentPoint(window).Properties.IsLeftButtonPressed)
        {
            _ = window.WindowModeSwitcher.ToggleMaximized();
            return;
        }

        WindowDragBehavior(window, e);
    }

    /// <summary>
    /// Drags the window. The fitter fits it to the monitor it was dragged to.
    /// </summary>
    public static void WindowDragBehavior(MainWindow window, PointerPressedEventArgs e)
    {
        window.BeginMoveDrag(e);
    }

    #endregion
}