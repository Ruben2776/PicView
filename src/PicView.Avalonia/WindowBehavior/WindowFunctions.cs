using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Input;
using PicView.Avalonia.StartUp;
using PicView.Avalonia.UI;
using PicView.Avalonia.Views.UC;
using PicView.Core.Config;
using PicView.Core.DebugTools;
using PicView.Core.FileHandling;
using PicView.Core.FileHistory;
using PicView.Core.Models;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;

// ReSharper disable CompareOfFloatsByEqualityOperator

namespace PicView.Avalonia.WindowBehavior;

public static class WindowFunctions
{
    #region Window State
    
    /// <summary>
    /// Sizes and places a new window for the mode it is about to start in, before it is shown.
    /// </summary>
    /// <param name="isAppStart">Whether this is the first window at app start, the only one that may start fullscreen.</param>
    public static void HandleWindowScalingMode(MainWindow window, bool isAppStart, bool adjustPos = true)
    {
        if (Settings.WindowProperties.Margin < 0)
        {
            Settings.WindowProperties.Margin = 45;
        }

        var starting = WindowModeSwitcher.Starting(isAppStart);
        if (starting.Mode is not WindowMode.Normal)
        {
            InitializeWindowPosition(window);
        }
        else if (starting.AutoFit)
        {
            window.WindowStartupLocation = adjustPos ? WindowStartupLocation.CenterScreen : WindowStartupLocation.Manual;
            window.WindowFitter.FollowContent();
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

    public static async Task ToggleTopMost(MainWindowViewModel vm)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        var shouldBeTopMost = !Settings.WindowProperties.TopMost;
        desktop.MainWindow.Topmost = shouldBeTopMost;
        Settings.WindowProperties.TopMost = shouldBeTopMost;
        vm.IsTopMost.Value = shouldBeTopMost;

        await SaveSettingsAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Turns AutoFit on or off in every main window, the opposite of what it is in <paramref name="vm"/>'s window.
    /// </summary>
    public static Task ToggleAutoFit(MainWindowViewModel vm) =>
        SetAutoFitInEveryWindow(!vm.WindowModeState.CurrentValue.AutoFit);

    /// <summary>
    /// Turns AutoFit on or off in every main window, and centers the windows that turn it on.
    /// </summary>
    public static async Task SetAutoFitInEveryWindow(bool autoFit)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        foreach (var window in desktop.Windows.OfType<MainWindow>().ToArray())
        {
            if (window.DataContext is not MainWindowViewModel windowVm)
            {
                continue;
            }

            var wasAutoFit = windowVm.WindowModeState.CurrentValue.AutoFit;
            await window.WindowModeSwitcher.SetAutoFit(autoFit);
            if (autoFit && !wasAutoFit)
            {
                CenterWindowOnScreen(window);
            }
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

    public static void InitializeWindowPosition(Window window, IWindowProperties properties)
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
            if (properties is { Left: not null, Top: not null })
            {
                window.WindowStartupLocation = WindowStartupLocation.Manual;
                window.Position = new PixelPoint(properties.Left.GetValueOrDefault(),
                    properties.Top.GetValueOrDefault());
            }
            else
            {
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
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

    public static void InitializeWindowSizeAndPosition(Window window, IWindowProperties properties)
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
            if (properties.Maximized)
            {
                window.WindowState = WindowState.Maximized;
            }
            else if (properties is { Left: not null, Top: not null })
            {
                window.WindowStartupLocation = WindowStartupLocation.Manual;
                window.Position = new PixelPoint(properties.Left.GetValueOrDefault(),
                    properties.Top.GetValueOrDefault());
            }
            else
            {
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            if (properties is not { Width: not null, Height: not null })
            {
                return;
            }

            if (properties.Width > window.MinWidth && properties.Width < window.MaxWidth &&
                properties.Height > window.MinHeight && properties.Height < window.MaxHeight)
            {
                window.Width = properties.Width.Value;
                window.Height = properties.Height.Value;
            }
            else
            {
                DebugHelper.LogDebug(nameof(WindowFunctions), nameof(InitializeWindowSizeAndPosition),
                    "Invalid width and height values");
            }
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
    /// Drags the window, and fits it to the monitor it was dragged to.
    /// </summary>
    public static void WindowDragBehavior(MainWindow window, PointerPressedEventArgs e)
    {
        window.BeginMoveDrag(e);
        if (window.WindowFitter.IsOnAnotherScreen())
        {
            window.WindowFitter.Refit();
        }
    }

    #endregion
}