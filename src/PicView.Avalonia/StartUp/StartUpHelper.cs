using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ImageMagick;
using PicView.Avalonia.ColorManagement;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Functions;
using PicView.Avalonia.Input;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.SettingsManagement;
using PicView.Avalonia.UI;
using PicView.Avalonia.Views.UC;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.FileAssociations;
using PicView.Core.FileHistory;
using PicView.Core.FileSorting;
using PicView.Core.Localization;
using PicView.Core.ProcessHandling;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.StartUp;

public static class StartUpHelper
{
    public static void StartWithArguments(CoreViewModel vm, bool settingsExists,
        IClassicDesktopStyleApplicationLifetime desktop, MainWindow window)
    {
        var args = Environment.GetCommandLineArgs();
        if (args.Length > 1)
        {
            var arg = args[1];
            if (arg.StartsWith("associate:", StringComparison.OrdinalIgnoreCase))
            {
                // Set file associations and exit
                Task.Run(async () =>
                {
                    try
                    {
                        vm.PlatformService.InitiateFileAssociationService();
                        Debug.WriteLine($"Processing file association argument: {arg}");
                        await FileAssociationProcessor.ProcessFileAssociationArguments(arg);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error in file association processing: {ex.Message}");
                    }
                    finally
                    {
                        // Always exit the elevated process after processing associations
                        Environment.Exit(0);
                    }
                });
            }
            else if (arg.Equals("blank:", StringComparison.OrdinalIgnoreCase))
            {
                BlankStartUp();
            }
            else if (Settings.UIProperties.OpenInSameWindow)
            {
                if (!ProcessHelper.CheckIfAnotherInstanceIsRunning())
                {
                    ImageStartUp(arg);
                }
                else
                {
                    IPC.SendWithArgs(args);
                }
            }
            else
            {
                ImageStartUp(arg);
            }
        }
        else
        {
            RegularStartUp(vm, settingsExists, desktop, window);
        }
            
        return;
        
        void ImageStartUp(string filePath)
        {
            desktop.MainWindow = window;
            
            SettingsUpdater.InitializeSettings(vm.MainWindows.ActiveWindow.CurrentValue, settingsExists);

            HandleWindowScalingMode(vm, window);

            HandleStartImage(window, vm, filePath);
            window.Show();

            HandlePostWindowUpdates(vm, desktop, window);
        }

        void BlankStartUp()
        {
            desktop.MainWindow = window;
            
            SettingsUpdater.InitializeSettings(vm.MainWindows.ActiveWindow.CurrentValue, settingsExists);

            HandleWindowScalingMode(vm, window);

            HandlePostWindowUpdates(vm, desktop, window);
        }
    }
    
    public static void DetachedWindowStartup(CoreViewModel core, IClassicDesktopStyleApplicationLifetime desktop, MainWindow window)
    {
        SettingsUpdater.InitializeSettings(window.DataContext as MainWindowViewModel, true);
        HandleWindowScalingMode(core, window, false);
        window.Show();
        
        HandlePostWindowUpdates(core, desktop, window);
    }
    
    public static void RegularStartUp(CoreViewModel vm, bool settingsExists,
        IClassicDesktopStyleApplicationLifetime desktop, MainWindow window)
    {
        desktop.MainWindow = window;
        TranslationManager.Init();
        SettingsUpdater.InitializeSettings(vm.MainWindows.ActiveWindow.CurrentValue, settingsExists);

        HandleWindowScalingMode(vm, window);

        StartUpMenuOrLastFile(window, vm);
        window.Show();

        HandlePostWindowUpdates(vm, desktop, window);
    }
    
    public static void HandleWindowScalingMode(CoreViewModel vm, MainWindow window, bool adjustPos = true)
    {
        ScreenHelper.UpdateScreenSize(window);

        if (Settings.WindowProperties.Margin < 0)
        {
            Settings.WindowProperties.Margin = 45;
        }
        
        else if (Settings.WindowProperties.AutoFit && !Settings.WindowProperties.Maximized && !Settings.WindowProperties.Fullscreen)
        {
            window.WindowStartupLocation = adjustPos ? WindowStartupLocation.CenterScreen : WindowStartupLocation.Manual;
            WindowFunctions.SetAutoFit(vm.MainWindows.ActiveWindow.CurrentValue, window, false);
        }
        else 
        {
            WindowFunctions.SetSingleManualWindow(vm.MainWindows.ActiveWindow.CurrentValue, window);
            if (adjustPos)
            {
                WindowFunctions.InitializeWindowSizeAndPosition(window);
            }
        }
    }

    public static void HandlePostWindowUpdates(CoreViewModel core, IClassicDesktopStyleApplicationLifetime desktop, MainWindow mainWindow)
    {
        var vm = core.MainWindows.ActiveWindow.CurrentValue;
        // Need to delay setting fullscreen or maximized until after the window is shown to select the correct monitor
        if (Settings.WindowProperties.Maximized && !Settings.WindowProperties.Fullscreen)
        {
            vm.PlatformWindowService.Maximize(false);
        }
        else if (Settings.WindowProperties.Fullscreen)
        {
            vm.PlatformWindowService.Fullscreen(false);
        }
        
        SetMemorySettings();
        
        BackGroundLoadings();

        SetWindowEventHandlers(mainWindow);
        HandleThemeUpdates(vm);
        mainWindow.UIHelper.AddDropDownMenu(mainWindow);

        vm.ToolTip ??= new ToolTipViewModel();
        TooltipHelper.StartTooltipSubscription(vm.ToolTip, mainWindow);
        
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // Windows needs a named pipe server to open files in the same window
            if (Settings.UIProperties.OpenInSameWindow && !ProcessHelper.CheckIfAnotherInstanceIsRunning())
            {
                _ = IPC.StartListeningForArguments();
            }
        }
        
        Application.Current.Name = "PicView";
        
        return;
        
        void BackGroundLoadings()
        {
            Task.Run(async() =>
            {
                await KeybindingManager.LoadKeybindings(core.PlatformService);
                core.MainWindows.ActiveWindow.Value.Mapper = new FunctionsMapper(vm, mainWindow);
                FileHistoryManager.Initialize();
                HandleWindowControlSettings(core, desktop);
                vm.WindowTabs.SetSortOrder((SortFilesBy)Settings.Sorting.SortPreference);
            });
        }
    }

    private static void SetMemorySettings()
    {
        ResourceLimits.LimitMemory(new Percentage(80));
        GCSettings.LatencyMode = GCLatencyMode.LowLatency;
    }

    private static void HandleThemeUpdates(MainWindowViewModel vm)
    {
        if (Settings.Theme.GlassTheme)
        {
            GlassThemeHelper.GlassThemeUpdates();
        }

        BackgroundManager.SetBackground(Settings.UIProperties.BgColorChoice);
        ColorManager.UpdateAccentColors(Settings.Theme.ColorTheme);
        UIHelper.SetCtrlToZoomImage(vm);
    }

    private static void HandleWindowControlSettings(CoreViewModel vm, IClassicDesktopStyleApplicationLifetime desktop)
    {
        vm.MainWindows.ActiveWindow.CurrentValue.IsScrollingEnabled.Value = Settings.Zoom.ScrollEnabled;

        if (Settings.WindowProperties.TopMost)
        {
            Dispatcher.UIThread.Invoke(() => { desktop.MainWindow.Topmost = true; });
        }
    }

    private static void HandleStartImage(MainWindow mainWindow, CoreViewModel vm, string arg)
    {
        Task.Run(() => QuickLoad.QuickLoadAsync(mainWindow, vm, arg, continueFromLeftOff: false, isStartup: true));
    }

    public static void StartUpMenuOrLastFile(MainWindow mainWindow, CoreViewModel vm)
    {
        if (Settings.StartUp.OpenLastFile)
        {
            if (string.IsNullOrWhiteSpace(Settings.StartUp.LastFile))
            {
                ShowStartUpMenu();
            }
            else
            {
                Task.Run(() => QuickLoad.QuickLoadAsync(mainWindow, vm, Settings.StartUp.LastFile, continueFromLeftOff: true, isStartup: true));
            }
        }
        else
        {
            ShowStartUpMenu();
        }

        return;

        void ShowStartUpMenu()
        {
            var startUpMenu = new StartUpMenu
            {
                Buttons =
                {
                    DataContext = vm
                }
            };
            vm.MainWindows.ActiveWindow.CurrentValue.WindowTabs.ActiveTab.Value.CurrentView.Value = startUpMenu;
        }
    }

    private static void SetWindowEventHandlers(Window w)
    {
        // Using AddHandler fixes the first keydown event not firing properly
        w.AddHandler(InputElement.KeyDownEvent, MainWindow_KeysDownAsync, RoutingStrategies.Tunnel);
        w.AddHandler(InputElement.KeyUpEvent, MainWindow_KeyUpAsync, RoutingStrategies.Tunnel);
        w.PointerPressed += async (_, e) => await MouseShortcuts.MainWindow_PointerPressed(e, w).ConfigureAwait(false);

        w.Deactivated += delegate
        {
            MainKeyboardShortcuts.Reset();
            MainKeyboardShortcuts.ClearKeyDownModifiers();
        };
    }

    private static async ValueTask MainWindow_KeysDownAsync(object? sender, KeyEventArgs e)
    {
        // Extract the ViewModel from the window that received the key press
        var mainWindow = sender as MainWindow;
        var vm = mainWindow.DataContext as MainWindowViewModel;
        await MainKeyboardShortcuts.MainWindow_KeysDownAsync(e, vm, mainWindow).ConfigureAwait(false);
    }

    private static async ValueTask MainWindow_KeyUpAsync(object? sender, KeyEventArgs e)
    {
        // Extract the ViewModel from the window that received the key press
        var vm = (sender as Control)?.DataContext as MainWindowViewModel;
        await MainKeyboardShortcuts.MainWindow_KeysUpAsync(e, vm);
    }
}