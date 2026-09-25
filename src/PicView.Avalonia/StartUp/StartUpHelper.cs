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
                _ = Task.Run(async () =>
                {
                    try
                    {
                        vm.PlatformService.InitiateFileAssociationService();
                        Debug.WriteLine($"Processing file association argument: {arg}");
                        await FileAssociationProcessor.ProcessFileAssociationArguments(arg).ConfigureAwait(false);
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
            else if (Settings.UIProperties.OpenInSameWindow)
            {
                if (!ProcessHelper.CheckIfAnotherInstanceIsRunning())
                {
                    WindowFunctions.ImageStartUp(arg, vm, settingsExists, desktop, window);
                }
                else
                {
                    IPC.SendWithArgs(args);
                }
            }
            else
            {
                WindowFunctions.ImageStartUp(arg, vm, settingsExists, desktop, window);
            }
        }
        else
        {
            WindowFunctions.RegularWindowStartUp(vm, settingsExists, desktop, window);
        }
    }
    
    public static void HandleWindowStartUpSettings(CoreViewModel core, bool settingsExists, MainWindow window)
    {
        SettingsUpdater.InitializeSettings(core.MainWindows.ActiveWindow.CurrentValue, settingsExists);

        WindowFunctions.HandleWindowScalingMode(core, window);
        
        ThemeManager.DetermineTheme(Application.Current, settingsExists);
        HandleThemeUpdates(core.MainWindows.ActiveWindow.CurrentValue);
    }

    public static void HandlePostWindowUpdates(CoreViewModel core, IClassicDesktopStyleApplicationLifetime desktop, MainWindow mainWindow)
    {
        var vm = core.MainWindows.ActiveWindow.CurrentValue;
        if (Settings.WindowProperties.Maximized && !Settings.WindowProperties.Fullscreen)
        {
            _ = vm.PlatformWindowService.Maximize(false);
        }
        else if (Settings.WindowProperties.Fullscreen)
        {
            _ = vm.PlatformWindowService.Fullscreen(false);
            Dispatcher.UIThread.Post(() =>
            {
                if (Settings.WindowProperties.Fullscreen)
                {
                    ToggleUIVisibility.FullscreenHideInterface(vm);
                }
                WindowResizing.SetSize(mainWindow, WindowResizeReason.Layout);
                WindowFunctions.CenterWindowOnScreen(true, true, mainWindow);
            },DispatcherPriority.ContextIdle);
        }
        
        SetMemorySettings();
        
        BackGroundLoadings();

        SetWindowEventHandlers(mainWindow);
        Dispatcher.UIThread.Post(() =>
        {
            mainWindow.UIHelper.AddDropDownMenu(mainWindow);
            mainWindow.UIHelper.AddFileMenu(vm);
            mainWindow.UIHelper.AddSettingsMenu(vm);

            vm.ToolTip ??= new ToolTipViewModel();
            TooltipHelper.StartTooltipSubscription(vm.ToolTip, mainWindow);
        
            if (!OperatingSystem.IsMacOS())
            {
                // macOS already handles opening files in the same window,
                // for other platforms we need IPC to open files in the same window
                if (Settings.UIProperties.OpenInSameWindow && !ProcessHelper.CheckIfAnotherInstanceIsRunning())
                {
                    _ = IPC.StartListeningForArguments();
                }
            }
        
            Application.Current.Name = "PicView";
        }, priority: DispatcherPriority.Background);
        
        return;
        
        void BackGroundLoadings()
        {
            _ = Task.Run(() =>
            {
                Debug.Assert(core.PlatformService != null);
                core.MainWindows.ActiveWindow.Value?.Mapper = new FunctionsMapper(vm, mainWindow);
                FileHistoryManager.Initialize();
                HandleWindowControlSettings(core, desktop);
                vm.WindowTabs.SetSortOrder((SortFilesBy)Settings.Sorting.SortPreference);
            });
            _ = Task.Run(() => KeybindingManager.LoadKeybindings(core.PlatformService));
        }
    }

    private static void SetMemorySettings()
    {
        ResourceLimits.LimitMemory(new Percentage(80));
        GCSettings.LatencyMode = GCLatencyMode.LowLatency;
    }

    private static void HandleThemeUpdates(MainWindowViewModel vm)
    {
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

    public static void HandleStartImage(MainWindow mainWindow, CoreViewModel core, string arg)
    {
        _ = Task.Run(() => QuickLoad.QuickLoadAsync(mainWindow, core, arg, continueFromLeftOff: false, isStartup: true));
        if (Settings.WindowProperties.AutoFit)
        {
            Dispatcher.UIThread.Post(() =>
            {
                WindowResizing.FastCenterWindow(mainWindow);
            }, DispatcherPriority.Input);
        }
    }

    public static void StartUpMenuOrLastFile(MainWindow mainWindow, CoreViewModel core)
    {
        if (Settings.StartUp.OpenLastFile)
        {
            if (string.IsNullOrWhiteSpace(Settings.StartUp.LastFile))
            {
                ShowStartUpMenu();
            }
            else
            {
                _ = Task.Run(() => QuickLoad.QuickLoadAsync(mainWindow, core, Settings.StartUp.LastFile, continueFromLeftOff: true, isStartup: true));
                if (Settings.WindowProperties.AutoFit)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        WindowResizing.FastCenterWindow(mainWindow);
                    }, DispatcherPriority.Input);
                }
            }
        }
        else
        {
            ShowStartUpMenu();
            if (Settings.WindowProperties.AutoFit && OperatingSystem.IsMacOS())
            {
                WindowFunctions.CenterWindowOnScreen(mainWindow);
            }
        }

        return;

        void ShowStartUpMenu()
        {
            var vm = core.MainWindows.ActiveWindow.CurrentValue;
            var tab = vm.WindowTabs.ActiveTab.CurrentValue;
            tab.ParentWindowContext = vm;
            var startUpMenu = new StartUpMenu
            {
                Buttons =
                {
                    DataContext = tab
                }
            };
            tab.CurrentView.Value = startUpMenu;
            mainWindow.Show();
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
        await MainKeyboardShortcuts.MainWindow_KeysUpAsync(e, vm).ConfigureAwait(false);
    }
}