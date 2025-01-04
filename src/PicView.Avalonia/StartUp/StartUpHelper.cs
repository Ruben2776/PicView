using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using ImageMagick;
using PicView.Avalonia.ColorManagement;
using PicView.Avalonia.Input;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.SettingsManagement;
using PicView.Avalonia.UI;
using PicView.Avalonia.Update;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Calculations;
using PicView.Core.Config;
using PicView.Core.Gallery;
using PicView.Core.ProcessHandling;

namespace PicView.Avalonia.StartUp;

public static class StartUpHelper
{
    public static void Start(MainViewModel vm, bool settingsExists, IClassicDesktopStyleApplicationLifetime desktop,
        Window window)
    {
        var args = Environment.GetCommandLineArgs();

        if (!settingsExists)
        {
            InitializeWindowForNoSettings(vm);
        }
        else
        {
            if (SettingsHelper.Settings.UIProperties.OpenInSameWindow &&
                ProcessHelper.CheckIfAnotherInstanceIsRunning())
            {
                HandleMultipleInstances(args);
            }
            else if (args.Length > 1 && args[1].Equals("update", StringComparison.InvariantCultureIgnoreCase))
            {
                Task.Run(async () => await UpdateManager.UpdateCurrentVersion(vm));
                return;
            }
        }

        vm.IsLoading = true;

        if (SettingsHelper.Settings.WindowProperties.Fullscreen)
        {
            window.Show();
            WindowFunctions.Fullscreen(vm, desktop);
        }

        ScreenHelper.UpdateScreenSize(window);
        
        if (SettingsHelper.Settings.WindowProperties.AutoFit && !SettingsHelper.Settings.WindowProperties.Fullscreen)
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            window.Width = SizeDefaults.WindowMinSize;
            window.Height = SizeDefaults.WindowMinSize;
        }
        window.Show();
        vm.ImageViewer = new ImageViewer();
        ResourceLimits.LimitMemory(new Percentage(90));
        HandleStartUpMenuOrImage(vm, args);
        Task.Run(async () =>
        {
            await LanguageUpdater.UpdateLanguageAsync(vm, settingsExists).ConfigureAwait(false);
            if (settingsExists)
            {
                await KeybindingManager.LoadKeybindings(vm.PlatformService).ConfigureAwait(false);
            }
            else
            {
                await KeybindingManager.SetDefaultKeybindings(vm.PlatformService).ConfigureAwait(false);
            }
        });
        HandleThemeUpdates(vm);
        
        if (settingsExists)
        {
            if (SettingsHelper.Settings.WindowProperties.Maximized && !SettingsHelper.Settings.WindowProperties.Fullscreen)
            {
                WindowFunctions.Maximize();
            }
            else if (SettingsHelper.Settings.WindowProperties.AutoFit && !SettingsHelper.Settings.WindowProperties.Fullscreen)
            {
                HandleAutoFit(vm);
            }
            else if (!SettingsHelper.Settings.WindowProperties.Fullscreen)
            {
                HandleNormalWindow(vm, window);
            }
        }
        
        UIHelper.SetControls(desktop);
        HandleWindowControlSettings(vm, desktop);
        ValidateGallerySettings(vm, settingsExists);
        SetWindowEventHandlers(window);
        UIHelper.AddMenus();

        Application.Current.Name = "PicView";

        if (SettingsHelper.Settings.UIProperties.OpenInSameWindow)
        {
            // No other instance is running, create named pipe server
            _ = IPC.StartListeningForArguments(vm);
        }
        
        // Fixes incorrect fullscreen window
        if (SettingsHelper.Settings.WindowProperties.Fullscreen)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                WindowFunctions.Fullscreen(vm, desktop);
                
            }, DispatcherPriority.ApplicationIdle).Wait();
            WindowFunctions.Fullscreen(vm, desktop);
        }
    }

    private static void HandleThemeUpdates(MainViewModel vm)
    {
        if (SettingsHelper.Settings.Theme.GlassTheme)
        {
            ThemeManager.GlassThemeUpdates();
        }

        BackgroundManager.SetBackground(vm);
        ColorManager.UpdateAccentColors(SettingsHelper.Settings.Theme.ColorTheme);
    }

    private static void HandleWindowControlSettings(MainViewModel vm, IClassicDesktopStyleApplicationLifetime desktop)
    {
        if (SettingsHelper.Settings.Zoom.ScrollEnabled)
        {
            vm.ToggleScrollBarVisibility = ScrollBarVisibility.Visible;
            vm.IsScrollingEnabled = true;
        }
        else
        {
            vm.ToggleScrollBarVisibility = ScrollBarVisibility.Disabled;
            vm.IsScrollingEnabled = false;
        }

        if (SettingsHelper.Settings.WindowProperties.TopMost)
        {
            desktop.MainWindow.Topmost = true;
        }
    }

    private static void HandleStartUpMenuOrImage(MainViewModel vm, string[] args)
    {
        if (args.Length > 1)
        {
            vm.CurrentView = vm.ImageViewer;
            Task.Run(() => QuickLoad.QuickLoadAsync(vm, args[1]));
        }
        else if (SettingsHelper.Settings.StartUp.OpenLastFile)
        {
            if (string.IsNullOrWhiteSpace(SettingsHelper.Settings.StartUp.LastFile))
            {
                vm.CurrentView = new StartUpMenu();
                vm.IsLoading = false;
            }
            else
            {
                vm.CurrentView = vm.ImageViewer;
                Task.Run(() => QuickLoad.QuickLoadAsync(vm, SettingsHelper.Settings.StartUp.LastFile));
            }
        }
        else
        {
            vm.CurrentView = new StartUpMenu();
            vm.IsLoading = false;
        }
    }

    private static void HandleNormalWindow(MainViewModel vm, Window window)
    {
        vm.CanResize = true;
        vm.IsAutoFit = false;
        WindowFunctions.InitializeWindowSizeAndPosition(window);
        if (SettingsHelper.Settings.UIProperties.ShowInterface)
        {
            vm.IsTopToolbarShown = true;
            vm.IsBottomToolbarShown = SettingsHelper.Settings.UIProperties.ShowBottomNavBar;
        }
    }

    private static void HandleAutoFit(MainViewModel vm)
    {
        vm.SizeToContent = SizeToContent.WidthAndHeight;
        vm.CanResize = false;
        vm.IsAutoFit = true;
        if (SettingsHelper.Settings.UIProperties.ShowInterface)
        {
            vm.IsTopToolbarShown = true;
            vm.IsBottomToolbarShown = SettingsHelper.Settings.UIProperties.ShowBottomNavBar;
        }
    }

    private static void InitializeWindowForNoSettings(MainViewModel vm)
    {
        HandleAutoFit(vm);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            SettingsHelper.Settings.Zoom.IsUsingTouchPad = true;
        }
    }

    private static void HandleMultipleInstances(string[] args)
    {
        if (args.Length > 1)
        {
            Task.Run(async () =>
            {
                var retries = 0;
                while (!await IPC.SendArgumentToRunningInstance(args[1]))
                {
                    await Task.Delay(1000);
                    if (++retries > 20)
                    {
                        break;
                    }
                }

                Environment.Exit(0);
            });
        }
    }

    private static void ValidateGallerySettings(MainViewModel vm, bool settingsExists)
    {
        if (!settingsExists)
        {
            vm.GetBottomGalleryItemHeight = GalleryDefaults.DefaultBottomGalleryHeight;
            vm.GetFullGalleryItemHeight = GalleryDefaults.DefaultFullGalleryHeight;
        }

        // Set default gallery sizes if they are out of range or upgrading from an old version
        if (vm.GetBottomGalleryItemHeight < vm.MinBottomGalleryItemHeight ||
            vm.GetBottomGalleryItemHeight > vm.MaxBottomGalleryItemHeight)
        {
            vm.GetBottomGalleryItemHeight = GalleryDefaults.DefaultBottomGalleryHeight;
        }

        if (vm.GetFullGalleryItemHeight < vm.MinFullGalleryItemHeight ||
            vm.GetFullGalleryItemHeight > vm.MaxFullGalleryItemHeight)
        {
            vm.GetFullGalleryItemHeight = GalleryDefaults.DefaultFullGalleryHeight;
        }

        if (settingsExists)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(SettingsHelper.Settings.Gallery.BottomGalleryStretchMode))
        {
            SettingsHelper.Settings.Gallery.BottomGalleryStretchMode = "UniformToFill";
        }

        if (string.IsNullOrWhiteSpace(SettingsHelper.Settings.Gallery.FullGalleryStretchMode))
        {
            SettingsHelper.Settings.Gallery.FullGalleryStretchMode = "UniformToFill";
        }
    }

    private static void SetWindowEventHandlers(Window w)
    {
        w.KeyDown += async (_, e) => await MainKeyboardShortcuts.MainWindow_KeysDownAsync(e).ConfigureAwait(false);
        w.KeyUp += (_, e) => MainKeyboardShortcuts.MainWindow_KeysUp(e);
        w.PointerPressed += async (_, e) => await MouseShortcuts.MainWindow_PointerPressed(e).ConfigureAwait(false);
    }
}