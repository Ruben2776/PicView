using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Input;
using PicView.Avalonia.SettingsManagement;
using PicView.Avalonia.StartUp;
using PicView.Avalonia.UI;
using PicView.Avalonia.Views.UC;
using PicView.Core.ArchiveHandling;
using PicView.Core.Config;
using PicView.Core.DebugTools;
using PicView.Core.FileHandling;
using PicView.Core.FileHistory;
using PicView.Core.IPlatform;
using PicView.Core.Localization;
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
        if (Application.Current.DataContext is not CoreViewModel core ||
            mainWindow.MainWindowInitializer is null ||
            Application.Current.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }
        var window = mainWindow.MainWindowInitializer.CreateMainWindow();
        var vm = window.DataContext as MainWindowViewModel;
        SettingsUpdater.InitializeSettings(vm, true);
        HandleWindowScalingMode(core, window);
        var startUpMenu = new StartUpMenu
        {
            Buttons =
            {
                DataContext = vm
            }
        };
        vm.WindowTabs.ActiveTab.Value.CurrentView.Value = startUpMenu;
        window.Show();
        StartUpHelper.HandlePostWindowUpdates(core, desktop, window);
    }

    public static void DetachedWindowStartup(CoreViewModel core, IClassicDesktopStyleApplicationLifetime desktop, MainWindow window)
    {
        SettingsUpdater.InitializeSettings(window.DataContext as MainWindowViewModel, true);
        HandleWindowScalingMode(core, window, false);
        window.Show();
        
        StartUpHelper.HandlePostWindowUpdates(core, desktop, window);
    }
    
    public static void RegularWindowStartUp(CoreViewModel vm, bool settingsExists,
        IClassicDesktopStyleApplicationLifetime desktop, MainWindow window)
    {
        desktop.MainWindow = window;
        TranslationManager.Init();
        SettingsUpdater.InitializeSettings(vm.MainWindows.ActiveWindow.CurrentValue, settingsExists);
        
        HandleWindowScalingMode(vm, window);

        StartUpHelper.StartUpMenuOrLastFile(window, vm);

        StartUpHelper.HandlePostWindowUpdates(vm, desktop, window);
    }
    
    public static void ImageStartUp(string filePath, CoreViewModel vm, bool settingsExists,
        IClassicDesktopStyleApplicationLifetime desktop, MainWindow window)
    {
        SettingsUpdater.InitializeSettings(vm.MainWindows.ActiveWindow.CurrentValue, settingsExists);

        HandleWindowScalingMode(vm, window);

        StartUpHelper.HandleStartImage(window, vm, filePath);

        StartUpHelper.HandlePostWindowUpdates(vm, desktop, window);
    }

    #endregion
    
    #region Window Closing Behavior

    public static async Task WindowClosingBehavior()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        await WindowClosingBehavior(desktop.MainWindow);
    }

    public static async Task WindowClosingBehavior(Window window)
    {
        WindowResizing.SaveSize(window);

        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }

        if (window.DataContext is not MainWindowViewModel vm)
        {
            return;
        }
        
        window.Hide();
        
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
            await SaveSettingsAsync();
            await KeybindingManager.UpdateKeyBindingsFile();
            TempFileManager.Cleanup();
            await FileHistoryManager.SaveToFileAsync();
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
            await core.SettingsViewModel.SettingsWindowConfig.SaveAsync();
        }

        if (core.MainWindows.MainWindows.Count <= 0)
        {
            // No mainWindow, close it manually to not have it running in the background
            Environment.Exit(0);
        }
    }
    
    #endregion

    #region Window State
    
    public static void HandleWindowScalingMode(CoreViewModel vm, MainWindow window, bool adjustPos = true)
    {
        ScreenHelper.UpdateScreenSize(window);

        if (Settings.WindowProperties.Margin < 0)
        {
            Settings.WindowProperties.Margin = 45;
        }

        if (Settings.WindowProperties.Maximized || Settings.WindowProperties.Fullscreen)
        {
            InitializeWindowPosition(window);
        }
        else if (Settings.WindowProperties.AutoFit)
        {
            window.WindowStartupLocation = adjustPos ? WindowStartupLocation.CenterScreen : WindowStartupLocation.Manual;
            SetAutoFit(vm.MainWindows.ActiveWindow.CurrentValue, window, false);
        }
        else 
        {
            SetSingleManualWindow(vm.MainWindows.ActiveWindow.CurrentValue, window);
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
    /// Toggles the AutoFit setting across all main application windows.
    /// If AutoFit is enabled, adjusts the windows to automatically size to their content.
    /// If AutoFit is disabled, allows manual resizing of windows.
    /// Updates the application settings and persists the changes.
    /// </summary>
    public static async Task ToggleAutoFit()
    {
        if (Application.Current.DataContext is not CoreViewModel core || Application.Current.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        var isAutoFit = Settings.WindowProperties.AutoFit;
        Settings.WindowProperties.AutoFit = !isAutoFit;
        foreach (var vm in core.MainWindows.MainWindows)
        {
            var window = desktop.Windows.FirstOrDefault(w => w.DataContext == vm);
            if (window is not MainWindow consecutiveMainWindow)
            {
                continue;
            }
            Toggle(vm, consecutiveMainWindow);
        }
        
        await SaveSettingsAsync().ConfigureAwait(false);
        return;

        void Toggle(MainWindowViewModel targetVm, MainWindow targetMainWindow)
        {
            if (isAutoFit)
            {
                targetVm.WindowMaxWidth.Value = targetVm.WindowMaxHeight.Value = double.NaN;
                targetMainWindow.SizeToContent = SizeToContent.Manual;
                targetVm.IsAutoFit.Value = false;
            }
            else
            {
                targetMainWindow.SizeToContent = SizeToContent.WidthAndHeight;
                targetVm.IsAutoFit.Value = true;
            }
            WindowResizing.SetSize(targetMainWindow, WindowResizeReason.Application);
        }
    }

    public static void SetAutoFit(MainWindowViewModel vm, Window window, bool center = true)
    {
        window.SizeToContent = SizeToContent.WidthAndHeight;
        Settings.WindowProperties.AutoFit = true;
        vm.IsAutoFit.Value = true;

        if (center)
        {
            // Fix unpleasant window placement
            CenterWindowOnScreen();
        }
    }

    /// <summary>
    /// Configures all windows to use manual sizing and positioning.
    /// This method updates the application state and modifies the behavior
    /// of individual windows to no longer rely on automatic adjustments.
    /// </summary>
    public static void SetManualWindows()
    {
        if (Application.Current.DataContext is not CoreViewModel core || Application.Current.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }
        
        foreach (var vm in core.MainWindows.MainWindows)
        {
            var window = desktop.Windows.FirstOrDefault(w => w.DataContext == vm);
            if (window is not MainWindow mainWindow)
            {
                continue;
            }
            SetSingleManualWindow(vm, mainWindow);
        }
        
        Settings.WindowProperties.AutoFit = false;
    }

    /// <summary>
    /// Configures a single window to operate in manual size mode and updates the associated view model properties.
    /// </summary>
    /// <param name="vm">The view model associated with the window, containing state and settings for the application.</param>
    /// <param name="mainWindow">The main window to be configured for manual size adjustments.</param>
    public static void SetSingleManualWindow(MainWindowViewModel vm, MainWindow mainWindow)
    {
        vm.WindowMaxWidth.Value = vm.WindowMaxHeight.Value = double.NaN;
        mainWindow.SizeToContent = SizeToContent.Manual;
        vm.IsAutoFit.Value = false;
    }

    #endregion

    #region Window Size and Position

    public static void CenterWindowOnScreen(Window window)
    {
        CenterWindowOnScreen(horizontal: true, top: false, window: window);
    }

    public static void CenterWindowOnScreen(bool horizontal = true, bool top = false, Window? window = null)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            window ??= desktop.MainWindow;

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
            window?.Position = horizontal
                ? new PixelPoint((int)centeredX, (int)centeredY)
                : new PixelPoint(window.Position.X, (int)centeredY);
        });
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
            Dispatcher.UIThread.InvokeAsync(() =>
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
            Dispatcher.UIThread.InvokeAsync(Set);
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
            Dispatcher.UIThread.InvokeAsync(Set);
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
            Dispatcher.UIThread.InvokeAsync(Set);
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

    public static void SetWindowSize(Window window, AvaloniaPropertyChangedEventArgs<Size> size,
        IWindowProperties properties)
    {
        if (!size.NewValue.HasValue)
        {
            return;
        }

        if (size.NewValue.Value == size.OldValue.Value)
        {
            return;
        }

        if (size.NewValue.Value.Width < window.MinWidth)
        {
            return;
        }

        if (size.NewValue.Value.Height < window.MinHeight)
        {
            return;
        }

        properties.Width = window.Bounds.Width;
        properties.Height = window.Bounds.Height;
    }

    #endregion

    #region Window Drag and Behavior

    public static void WindowDragAndDoubleClickBehavior(MainWindow window, PointerPressedEventArgs e,
        IPlatformWindowService platformWindowService)
    {
        var currentScreen = ScreenHelper.ScreenSize;

        var screen = window.Screens.ScreenFromVisual(window);
        if (screen == null)
        {
            return;
        }

        if (e.ClickCount == 2 && e.GetCurrentPoint(window).Properties.IsLeftButtonPressed)
        {
            platformWindowService.MaximizeRestore();
            return;
        }

        window.BeginMoveDrag(e);

        if (screen.WorkingArea.Width == currentScreen.WorkingAreaWidth &&
            screen.WorkingArea.Height == currentScreen.WorkingAreaHeight && screen.Scaling == currentScreen.Scaling)
        {
            return;
        }

        ScreenHelper.UpdateScreenSize(window);
        WindowResizing.SetSize(window, WindowResizeReason.DpiChange);
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
        WindowResizing.SetSize(window, WindowResizeReason.DpiChange);
    }

    #endregion
}