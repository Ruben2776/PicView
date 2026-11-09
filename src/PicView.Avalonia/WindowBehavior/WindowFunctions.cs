using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.WindowBehavior;

public static class WindowFunctions
{
    #region Window State
    
    /// <summary>
    /// Sizes and places a new window for the mode it is about to start in, before it is shown. An AutoFit window at App
    /// Start opens centered on its screen; with <paramref name="adjustPos"/>, a window takes the saved size and position,
    /// which a window opened while PicView runs saved from its opener.
    /// </summary>
    /// <param name="isAppStart">Whether this is the first window at app start, the only one that may start fullscreen.</param>
    /// <param name="adjustPos">Whether to give the window the saved size and position; false when it is placed already.</param>
    public static void HandleWindowScalingMode(MainWindow window, bool isAppStart, bool adjustPos = true)
    {
        if (Settings.WindowProperties.Margin < 0)
        {
            Settings.WindowProperties.Margin = GlobalSettingsViewModel.NegativeMarginReplacement;
        }

        var starting = WindowModeSwitcher.Starting(isAppStart);
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

    /// <summary>
    /// Turns AutoFit on or off in every main window, the opposite of what it is in <paramref name="vm"/>'s window.
    /// </summary>
    public static Task ToggleAutoFit(MainWindowViewModel vm) =>
        SetAutoFitInEveryWindow(!vm.WindowModeState.CurrentValue.AutoFit);

    /// <summary>
    /// Turns AutoFit on or off in every main window. Each window's fitter places those that turn it on.
    /// </summary>
    public static async Task SetAutoFitInEveryWindow(bool autoFit)
    {
        if (Application.Current.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        foreach (var window in desktop.Windows.OfType<MainWindow>().ToArray())
        {
            if (window.DataContext is not MainWindowViewModel)
            {
                continue;
            }

            await window.WindowModeSwitcher.SetAutoFit(autoFit).ConfigureAwait(true);
        }
    }

    #endregion

    #region Window Size and Position

    /// <summary>
    /// Remembers the window's position, and its size unless it is maximized or fullscreen.
    /// </summary>
    public static void SaveSize(Window window)
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
            Settings.WindowProperties.Top = window.Position.Y;
            Settings.WindowProperties.Left = window.Position.X;
            if (window.DataContext is MainWindowViewModel { WindowModeState.CurrentValue.Mode: not WindowMode.Normal })
            {
                return;
            }
            Settings.WindowProperties.Width = window.Bounds.Width;
            Settings.WindowProperties.Height = window.Bounds.Height;
        }
    }

    /// <summary>
    /// Centers the window in the work area of its own screen, after the next render, once its new size is laid out.
    /// </summary>
    /// <param name="top">Keeps it at the top of the work area instead of centering it vertically.</param>
    public static void CenterWindowOnScreen(MainWindow window, bool top = false) =>
        Dispatcher.UIThread.Post(() => window.WindowFitter.Center(top), DispatcherPriority.Render);

    public static void InitializeWindowSizeAndPosition(MainWindow window)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            window.Position = new PixelPoint((int)Settings.WindowProperties.Left,
                (int)Settings.WindowProperties.Top);
            window.Width = Settings.WindowProperties.Width;
            window.Height = Settings.WindowProperties.Height;
        }
        else
        {
            Dispatcher.UIThread.Post(() =>
            {
                window.Position = new PixelPoint((int)Settings.WindowProperties.Left,
                    (int)Settings.WindowProperties.Top);
                window.Width = Settings.WindowProperties.Width;
                window.Height = Settings.WindowProperties.Height;
            });
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

        void Set()
        {
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Position = new PixelPoint((int)Settings.WindowProperties.Left,
                (int)Settings.WindowProperties.Top);
        }
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