using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using PicView.Avalonia.ColorManagement;
using PicView.Avalonia.Input;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.SettingsManagement;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views;
using PicView.Avalonia.WindowBehavior;
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
            InitializeWindowForNoSettings(window, vm);
        }
        else
        {
            if (SettingsHelper.Settings.UIProperties.OpenInSameWindow &&
                ProcessHelper.CheckIfAnotherInstanceIsRunning())
            {
                HandleMultipleInstances(args);
            }
        }
        
        vm.IsLoading = true;

        if (SettingsHelper.Settings.WindowProperties.Fullscreen)
        {
            window.Show();
            WindowFunctions.Fullscreen(vm, desktop);
        }
        
        ScreenHelper.UpdateScreenSize(window);
        vm.ImageViewer = new ImageViewer();
        HandleStartUpMenuOrImage(vm, args);

        if (SettingsHelper.Settings.WindowProperties.Maximized)
        {
            WindowFunctions.Maximize();
            window.Show();
        }
        else if (SettingsHelper.Settings.WindowProperties.AutoFit)
        {
            HandleAutoFit(vm, desktop);
            window.Show();
        }
        else if (!SettingsHelper.Settings.WindowProperties.Fullscreen)
        {
            HandleNormalWindow(vm, window);
            window.Show();
        }

        UIHelper.SetControls(desktop);
        LanguageUpdater.UpdateLanguage(vm);

        HandleWindowControlSettings(vm, desktop);

        ValidateGallerySettings(vm, settingsExists);

        HandleThemeUpdates(vm);

        if (settingsExists)
        {
            Task.Run(() => KeybindingManager.LoadKeybindings(vm.PlatformService));
        }
        else
        {
            Task.Run(() => KeybindingManager.SetDefaultKeybindings(vm.PlatformService));
        }
        
        SetWindowEventHandlers(window);

        UIHelper.AddMenus();

        Application.Current.Name = "PicView";

        if (SettingsHelper.Settings.UIProperties.OpenInSameWindow)
        {
            // No other instance is running, create named pipe server
            _ = IPC.StartListeningForArguments(vm);
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

    private static void HandleAutoFit(MainViewModel vm, IClassicDesktopStyleApplicationLifetime desktop)
    {
        desktop.MainWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        vm.SizeToContent = SizeToContent.WidthAndHeight;
        vm.CanResize = false;
        vm.IsAutoFit = true;
        if (SettingsHelper.Settings.UIProperties.ShowInterface)
        {
            vm.IsTopToolbarShown = true;
            vm.IsBottomToolbarShown = SettingsHelper.Settings.UIProperties.ShowBottomNavBar;
        }
    }

    private static void InitializeWindowForNoSettings(Window w, MainViewModel vm)
    {
        // Fixes incorrect window
        w.Height = w.MinHeight;
        w.Width = w.MinWidth;

        WindowFunctions.CenterWindowOnScreen();
        vm.CanResize = true;
        vm.IsAutoFit = false;
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