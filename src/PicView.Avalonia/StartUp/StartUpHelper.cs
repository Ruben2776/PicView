using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ImageMagick;
using PicView.Avalonia.ColorManagement;
using PicView.Avalonia.Input;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.SettingsManagement;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.FileAssociations;
using PicView.Core.FileHistory;
using PicView.Core.ProcessHandling;
using ImageViewer = PicView.Avalonia.Views.UC.ImageViewer;

namespace PicView.Avalonia.StartUp;

public static class StartUpHelper
{
    public static void StartWithArguments(MainViewModel vm, bool settingsExists,
        IClassicDesktopStyleApplicationLifetime desktop,
        Window window)
    {
        var args = Environment.GetCommandLineArgs();
        if (settingsExists)
        {
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
                else
                {
                    IPC.SendWithArgs(args);
                }
            }
        }

        SettingsUpdater.InitializeSettings(vm);

        HandleWindowScalingMode(vm, window);

        HandleStartUpMenuOrImage(vm, window, args);

        HandlePostWindowUpdates(vm, settingsExists, desktop, window);
    }

    public static void StartWithoutArguments(MainViewModel vm, bool settingsExists,
        IClassicDesktopStyleApplicationLifetime desktop,
        Window window, string? arg = null)
    {
        SettingsUpdater.InitializeSettings(vm);
        
        HandleWindowScalingMode(vm, window);

        HandleStartUpMenuOrImage(vm, window, arg);

        HandlePostWindowUpdates(vm, settingsExists, desktop, window);
    }

    private static void HandleWindowScalingMode(MainViewModel vm, Window window)
    {
        ScreenHelper.UpdateScreenSize(window);

        if (Settings.WindowProperties.Margin < 0)
        {
            Settings.WindowProperties.Margin = 45;
        }

        if (Settings.WindowProperties.AutoFit)
        {
            HandleAutoFit(vm, window);
        }
        else
        {
            HandleNormalWindow(vm, window);
        }
    }

    private static void HandlePostWindowUpdates(MainViewModel vm, bool settingsExists,
        IClassicDesktopStyleApplicationLifetime desktop, Window window)
    {
        SetMemorySettings();

        Task.Run(() => LanguageUpdater.UpdateLanguageAsync(vm.Translation, vm.PicViewer, settingsExists));
        if (settingsExists)
        {
            Task.Run(() => KeybindingManager.LoadKeybindings(vm.PlatformService));
        }
        else
        {
            Task.Run(() =>
            {
                KeybindingManager.SetDefaultKeybindings(vm.PlatformService);
            });
        }

        SetWindowEventHandlers(window);
        HandleThemeUpdates(vm);

        UIHelper.SetControls(desktop);
        Task.Run(() =>
        {
            _ = FileHistoryManager.InitializeAsync();
            HandleWindowControlSettings(vm, desktop);
            SettingsUpdater.ValidateGallerySettings(vm, settingsExists);
        });

       
        vm.MainWindow.LayoutButtonSubscription();
        vm.Gallery.GalleryItemSizeUpdateSubscription(vm);


        if (!Settings.WindowProperties.AutoFit)
        {
            // Need to update the screen size after the window is shown,
            // to avoid rendering error when switching between auto-fit
            ScreenHelper.UpdateScreenSize(window);
        }

        // Need to delay setting fullscreen or maximized until after the window is shown to select the correct monitor
        if (Settings.WindowProperties.Maximized && !Settings.WindowProperties.Fullscreen)
        {
            Dispatcher.UIThread
                .InvokeAsync(() => { vm.PlatformWindowService.Maximize(false); }, DispatcherPriority.Background);
        }
        else if (Settings.WindowProperties.Fullscreen)
        {
            Dispatcher.UIThread.InvokeAsync(() => { vm.PlatformWindowService.Fullscreen(false); },
                DispatcherPriority.Background);
        }

        MenuManager.AddMenus();
        UIHelper.AddHoverBar(vm);
        TooltipHelper.StartTooltipSubscription(vm);
        
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // Windows needs a named pipe server to open files in the same window
            if (Settings.UIProperties.OpenInSameWindow && !ProcessHelper.CheckIfAnotherInstanceIsRunning())
            {
                _ = IPC.StartListeningForArguments(vm);
            }
        }
        
        Application.Current.Name = "PicView";
    }

    private static void SetMemorySettings()
    {
        ResourceLimits.LimitMemory(new Percentage(80));
        GCSettings.LatencyMode = GCLatencyMode.LowLatency;
    }

    private static void HandleThemeUpdates(MainViewModel vm)
    {
        if (Settings.Theme.GlassTheme)
        {
            GlassThemeHelper.GlassThemeUpdates();
        }

        BackgroundManager.SetBackground(vm);
        ColorManager.UpdateAccentColors(Settings.Theme.ColorTheme);
    }

    private static void HandleWindowControlSettings(MainViewModel vm, IClassicDesktopStyleApplicationLifetime desktop)
    {
        if (Settings.Zoom.ScrollEnabled)
        {
            SettingsUpdater.TurnOnScroll(vm);
        }
        else
        {
            vm.MainWindow.ToggleScrollBarVisibility.Value = ScrollBarVisibility.Disabled;
            vm.GlobalSettings.IsScrollingEnabled.Value = false;
        }

        if (Settings.WindowProperties.TopMost)
        {
            desktop.MainWindow.Topmost = true;
        }
    }

    private static void HandleStartUpMenuOrImage(MainViewModel vm, Window window, string[] args)
    {
        vm.ImageViewer = new ImageViewer();

        if (args.Length > 1)
        {
            vm.MainWindow.CurrentView.Value = vm.ImageViewer;
            Task.Run(() => QuickLoad.QuickLoadAsync(vm, args[1], window, false));
        }
        else
        {
            StartUpMenuOrLastFile(vm, window);
        }
    }

    private static void HandleStartUpMenuOrImage(MainViewModel vm, Window window, string? arg = null)
    {
        vm.ImageViewer = new ImageViewer();

        if (arg is not null)
        {
            vm.MainWindow.CurrentView.Value = vm.ImageViewer;
            Task.Run(() => QuickLoad.QuickLoadAsync(vm, arg,  window,false));
        }
        else
        {
            StartUpMenuOrLastFile(vm, window);
        }
    }

    private static void StartUpMenuOrLastFile(MainViewModel vm, Window window)
    {
        if (Settings.StartUp.OpenLastFile)
        {
            if (string.IsNullOrWhiteSpace(Settings.StartUp.LastFile))
            {
                ShowStartUpMenu();
            }
            else
            {
                vm.MainWindow.CurrentView.Value = vm.ImageViewer;
                Task.Run(() => QuickLoad.QuickLoadAsync(vm, Settings.StartUp.LastFile, window, true));
            }
        }
        else
        {
            ShowStartUpMenu();
        }

        return;

        void ShowStartUpMenu()
        {
            
            window.Show();
            
            // Starting it in Dispatcher with post fixes occurrences where the text is not centered or the text is missing
            Dispatcher.UIThread.Post(() => { ErrorHandling.ShowStartUpMenu(vm); });
            Dispatcher.UIThread.Post(() =>
            {
                if (Settings.WindowProperties.AutoFit)
                {
                    WindowFunctions.CenterWindowOnScreen();
                }
            }, DispatcherPriority.Background);
        }
    }

    private static void HandleNormalWindow(MainViewModel vm, Window window)
    {
        vm.MainWindow.CanResize.Value = true;
        vm.GlobalSettings.IsAutoFit.Value = false;
        if (Settings.UIProperties.ShowInterface)
        {
            vm.MainWindow.IsTopToolbarShown.Value = true;
            vm.MainWindow.IsBottomToolbarShown.Value = Settings.UIProperties.ShowBottomNavBar;
        }

        WindowFunctions.InitializeWindowSizeAndPosition(window);
    }

    private static void HandleAutoFit(MainViewModel vm, Window window)
    {
        vm.MainWindow.SizeToContent.Value = SizeToContent.WidthAndHeight;
        vm.MainWindow.CanResize.Value = false;
        vm.GlobalSettings.IsAutoFit.Value = true;
        if (Settings.UIProperties.ShowInterface)
        {
            vm.MainWindow.IsTopToolbarShown.Value = true;
            vm.MainWindow.IsBottomToolbarShown.Value = Settings.UIProperties.ShowBottomNavBar;
        }

        if (Settings.WindowProperties.Fullscreen || Settings.WindowProperties.Maximized)
        {
            window.WindowStartupLocation = WindowStartupLocation.Manual;
        }
        else
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
        
    }

    private static void SetWindowEventHandlers(Window w)
    {
        // Using AddHandler fixes the first keydown event not firing properly
        w.AddHandler(InputElement.KeyDownEvent, MainWindow_KeysDownAsync,
            RoutingStrategies.Tunnel | RoutingStrategies.Bubble | RoutingStrategies.Direct);
        w.AddHandler(InputElement.KeyUpEvent, MainWindow_KeyUp,
            RoutingStrategies.Tunnel | RoutingStrategies.Bubble | RoutingStrategies.Direct);
        w.PointerPressed += async (_, e) => await MouseShortcuts.MainWindow_PointerPressed(e).ConfigureAwait(false);

        w.Deactivated += delegate
        {
            MainKeyboardShortcuts.Reset();
            MainKeyboardShortcuts.ClearKeyDownModifiers();
        };
    }

    private static async Task MainWindow_KeysDownAsync(object? sender, KeyEventArgs e)
    {
        e.Handled = true;
        await MainKeyboardShortcuts.MainWindow_KeysDownAsync(e).ConfigureAwait(false);
    }

    private static void MainWindow_KeyUp(object? sender, KeyEventArgs e)
    {
        e.Handled = true;
        MainKeyboardShortcuts.MainWindow_KeysUp(e);
    }
}