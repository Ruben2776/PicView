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
    #region Window creation behavior

    public static void NewWindow(MainWindow mainWindow)
    {
        if (Application.Current.DataContext is not CoreViewModel core || mainWindow.MainWindowInitializer is null)
        {
            return;
        }
        var window = mainWindow.MainWindowInitializer.CreateMainWindow();
        var vm = window.DataContext as MainWindowViewModel;
        HandleWindowScalingMode(window, isAppStart: false);
        var startUpMenu = new StartUpMenu
        {
            Buttons =
            {
                DataContext = vm.WindowTabs.ActiveTab.CurrentValue
            }
        };
        vm.WindowTabs.ActiveTab.Value.CurrentView.Value = startUpMenu;
        window.Show();
        StartUpHelper.HandlePostWindowUpdates(core, window, isAppStart: false);
    }

    public static void DetachedWindowStartup(CoreViewModel core, MainWindow window)
    {
        HandleWindowScalingMode(window, isAppStart: false, adjustPos: false);
        window.Show();
        
        StartUpHelper.HandlePostWindowUpdates(core, window, isAppStart: false);
    }
    
    public static void RegularWindowStartUp(CoreViewModel core, bool settingsExists, MainWindow window)
    {
        StartUpHelper.HandleWindowStartUpSettings(core, settingsExists, window);

        StartUpHelper.StartUpMenuOrLastFile(window, core);

        StartUpHelper.HandlePostWindowUpdates(core, window, isAppStart: true);

    }
    
    public static void ImageStartUp(string filePath, CoreViewModel core, bool settingsExists, MainWindow window)
    {
        StartUpHelper.HandleWindowStartUpSettings(core, settingsExists, window);

        StartUpHelper.HandleStartImage(window, core, filePath);

        StartUpHelper.HandlePostWindowUpdates(core, window, isAppStart: true);
    }

    #endregion
    
    #region Window Closing Behavior

    public static async Task WindowClosingBehavior()
    {
        var desktop = await Dispatcher.UIThread.InvokeAsync(() => Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime);
        if (desktop is null)
        {
            return;
        }

        await WindowClosingBehavior(desktop.MainWindow).ConfigureAwait(true);
    }

    public static async Task WindowClosingBehavior(Window window)
    {
        SaveSize(window);

        var core = await Dispatcher.UIThread.InvokeAsync(() => Application.Current.DataContext as CoreViewModel);
        if (core is null)
        {
            return;
        }

        var vm = Dispatcher.UIThread.Invoke(() => window.DataContext as MainWindowViewModel);
        if (vm is null)
        {
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(window.Hide);
        
        string? lastFile;
        var tab = vm.WindowTabs.ActiveTab.CurrentValue;

        if (!string.IsNullOrEmpty(tab.ArchiveExtractionService?.LastOpenedArchive))
        {
            lastFile = tab.ArchiveExtractionService.LastOpenedArchive;
        }
        else if (tab.SingleImageType is SingleImageType.Url && tab.SourceURL is not null)
        {
            lastFile = vm.WindowTabs.ActiveTab.CurrentValue.SourceURL;
        }
        else
        {
            lastFile = vm.WindowTabs.ActiveTab.CurrentValue?.Model?.FileInfo?.FullName ?? FileHistoryManager.GetLastEntry() ?? null;
        }


        if (lastFile is not null)
        {
            Settings.StartUp.LastFile = lastFile;
        }

        try
        {
            await SaveSettingsAsync().ConfigureAwait(true);
            await Keymaps.SaveAsync().ConfigureAwait(true);
            TempFileManager.Cleanup();
            await FileHistoryManager.SaveToFileAsync().ConfigureAwait(true);
            foreach (var tabViewModel in vm.WindowTabs.Tabs.CurrentValue)
            {
                tabViewModel.ArchiveExtractionService?.Cleanup();
            }
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(WindowFunctions), nameof(WindowClosingBehavior), e);
        }

        if (vm.WindowTabs.ActiveTab.CurrentValue.CurrentView.CurrentValue is IDisposable disposable)
        {
            disposable.Dispose();
        }
        core.MainWindows.MainWindows.Remove(vm);

        if (core.SettingsViewModel?.SettingsWindowConfig is not null)
        {
            await core.SettingsViewModel.SettingsWindowConfig.SaveAsync().ConfigureAwait(false);
        }

        if (core.MainWindows.MainWindows.Count <= 0)
        {
            // No mainWindow, close it manually to not have it running in the background
            Environment.Exit(0);
        }
    }
    
    #endregion

    #region Window State
    
    /// <summary>
    /// Sizes and places a new window for the mode it is about to start in, before it is shown.
    /// </summary>
    /// <param name="isAppStart">Whether this is the first window at app start, the only one that may start fullscreen.</param>
    public static void HandleWindowScalingMode(MainWindow window, bool isAppStart, bool adjustPos = true)
    {
        ScreenHelper.UpdateScreenSize(window);

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
    /// Keeps an AutoFit window on where its center was while it changes size.
    /// </summary>
    /// <returns>Whether the window was kept centered.</returns>
    public static bool KeepCenteredWhileResizing(Window window, AvaloniaPropertyChangedEventArgs<Size> size)
    {
        var oldSize = size.OldValue.Value;
        var newSize = size.NewValue.Value;
        if (!size.OldValue.HasValue || !size.NewValue.HasValue ||
            !Equals(size.Sender, window) || oldSize.Width is 0 || oldSize.Height is 0 ||
            newSize.Width is 0 || newSize.Height is 0)
        {
            return false;
        }

        var x = (oldSize.Width - newSize.Width) / 2;
        var y = (oldSize.Height - newSize.Height) / 2;
        window.Position = new PixelPoint(window.Position.X + (int)x, window.Position.Y + (int)y);
        return true;
    }

    /// <summary>
    /// Centers the window in the work area of the screen, from its current size.
    /// </summary>
    public static void FastCenterWindow(Window window)
    {
        var screen = ScreenHelper.ScreenSize;
        var windowSize = window.ClientSize;
        var centeredX = screen.X + (screen.WorkingAreaWidth - windowSize.Width) / 2;
        var centeredY = screen.Y + (screen.WorkingAreaHeight - windowSize.Height) / 2;
        window.Position = new PixelPoint((int)centeredX, (int)centeredY);
    }

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

    public static void CenterWindowOnScreen(Window window)
    {
        CenterWindowOnScreen(horizontal: true, top: false, window: window);
    }

    public static void CenterWindowOnScreen(bool horizontal = true, bool top = false, Window? window = null)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            {
                return;
            }
            
            window ??= desktop.MainWindow;

            if (window is null)
            {
                return;
            }

            ScreenHelper.UpdateScreenSize(window);
            var screen = ScreenHelper.ScreenSize;

            // Get the size of the window
            var windowSize = window.ClientSize;

            var x = screen.X;
            var y = screen.Y;

            // Calculate the position to center the window on the screen
            var centeredX = x + (screen.WorkingAreaWidth - windowSize.Width) / 2;
            var centeredY = y + (top ? 0 : (screen.WorkingAreaHeight - windowSize.Height) / 2);

            // Set the window's new position
            window.Position = horizontal
                ? new PixelPoint((int)centeredX, (int)centeredY)
                : new PixelPoint(window.Position.X, (int)centeredY);
        }, DispatcherPriority.Render);
    }

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
        var currentScreen = ScreenHelper.ScreenSize;

        var screen = window.Screens.ScreenFromVisual(window);
        if (screen is null)
        {
            return;
        }

        if (e.ClickCount == 2 && e.GetCurrentPoint(window).Properties.IsLeftButtonPressed)
        {
            _ = window.WindowModeSwitcher.ToggleMaximized();
            return;
        }

        window.BeginMoveDrag(e);

        if (screen.WorkingArea.Width == currentScreen.WorkingAreaWidth &&
            screen.WorkingArea.Height == currentScreen.WorkingAreaHeight && screen.Scaling == currentScreen.Scaling)
        {
            return;
        }

        ScreenHelper.UpdateScreenSize(window);
        window.WindowFitter.Refit();
    }

    public static void WindowDragBehavior(MainWindow window, PointerPressedEventArgs e)
    {
        var currentScreen = ScreenHelper.ScreenSize;
        window.BeginMoveDrag(e);
        var screen = window.Screens.ScreenFromVisual(window);
        if (screen == null)
        {
            return;
        }

        if (screen.WorkingArea.Width == currentScreen.WorkingAreaWidth &&
            screen.WorkingArea.Height == currentScreen.WorkingAreaHeight && screen.Scaling == currentScreen.Scaling)
        {
            return;
        }

        ScreenHelper.UpdateScreenSize(window);
        window.WindowFitter.Refit();
    }

    #endregion
}