using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using PicView.Avalonia.ColorManagement;
using PicView.Avalonia.Keybindings;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.SettingsManagement;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Config;
using PicView.Core.Gallery;
using PicView.Core.ProcessHandling;
using StartUpMenu = PicView.Avalonia.Views.StartUpMenu;

namespace PicView.Avalonia.UI;

public static class StartUpHelper
{
    public static void Start(MainViewModel vm, bool settingsExists, IClassicDesktopStyleApplicationLifetime desktop, Window w)
    {
        var args = Environment.GetCommandLineArgs();
        
        if (!settingsExists)
        {
            InitializeWindowForNoSettings(w, vm);
        }
        else
        {
            HandleWindowStartupSettings(vm, desktop, w, args);
        }

        w.Show();
        InitializePostWindowStartup(vm, desktop, w, args, settingsExists);
    }

    private static void InitializeWindowForNoSettings(Window w, MainViewModel vm)
    {
        w.Height = w.MinHeight;
        w.Width = w.MinWidth;
        WindowFunctions.CenterWindowOnScreen();
        vm.CanResize = true;
        vm.IsAutoFit = false;
    }
    
    private static void HandleWindowStartupSettings(MainViewModel vm, IClassicDesktopStyleApplicationLifetime desktop, Window w, string[] args)
    {
        if (SettingsHelper.Settings.UIProperties.OpenInSameWindow && ProcessHelper.CheckIfAnotherInstanceIsRunning())
        {
            HandleMultipleInstances(args);
        }
        
        if (SettingsHelper.Settings.UIProperties.ShowInterface)
        {
            vm.IsTopToolbarShown = true;
            vm.IsBottomToolbarShown = SettingsHelper.Settings.UIProperties.ShowBottomNavBar;
        }

        ApplyWindowSizeAndPosition(vm, desktop, w);
    }
    
    private static void InitializePostWindowStartup(MainViewModel vm, IClassicDesktopStyleApplicationLifetime desktop, Window w, string[] args, bool settingsExists)
    {
        vm.IsLoading = true;
        ScreenHelper.UpdateScreenSize(w);
        Task.Run(KeybindingsHelper.LoadKeybindings);
        ApplyThemeAndUISettings(vm, desktop);

        LoadInitialContent(vm, args);

        ValidateGallerySettings(vm, settingsExists);

        BackgroundManager.SetBackground(vm);
        ColorManager.UpdateAccentColors(SettingsHelper.Settings.Theme.ColorTheme);

        
        UIHelper.AddMenus();
        SetWindowEventHandlers(w);
        Application.Current.Name = "PicView";

        if (SettingsHelper.Settings.UIProperties.OpenInSameWindow)
        {
            _ = IPC.StartListeningForArguments(IPC.PipeName, w, vm);
        }
    }
    private static void HandleMultipleInstances(string[] args)
    {
        if (args.Length > 1)
        {
            Task.Run(async () =>
            {
                var retries = 0;
                while (!await IPC.SendArgumentToRunningInstance(args[1], IPC.PipeName))
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
    private static void ApplyWindowSizeAndPosition(MainViewModel vm, IClassicDesktopStyleApplicationLifetime desktop, Window w)
    {
        if (SettingsHelper.Settings.WindowProperties.Fullscreen)
        {
            // Need to delay it or it won't render properly
            Dispatcher.UIThread.Post(() =>
            {
                w.Topmost = true;
                WindowFunctions.CenterWindowOnScreen();
                WindowFunctions.Fullscreen(vm, desktop);
            }, DispatcherPriority.ContextIdle);
        }
        else if (SettingsHelper.Settings.WindowProperties.Maximized)
        {
            WindowFunctions.Maximize();
        }
        else if (SettingsHelper.Settings.WindowProperties.AutoFit)
        {
            desktop.MainWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            vm.SizeToContent = SizeToContent.WidthAndHeight;
            vm.CanResize = false;
            vm.IsAutoFit = true;
        }
        else
        {
            vm.CanResize = true;
            vm.IsAutoFit = false;
            WindowFunctions.InitializeWindowSizeAndPosition(w);
        }
    }

    private static void ApplyThemeAndUISettings(MainViewModel vm, IClassicDesktopStyleApplicationLifetime desktop)
    {
        if (SettingsHelper.Settings.Theme.GlassTheme)
        {
            ThemeManager.GlassThemeUpdates();
        }

        UIHelper.SetControls(desktop);
        LanguageUpdater.UpdateLanguage(vm);

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

    private static void LoadInitialContent(MainViewModel vm, string[] args)
    {
        vm.ImageViewer = new ImageViewer();

        if (args.Length > 1)
        {
            var arg = args[1];
            if (arg.StartsWith("lockscreen"))
            {
                var path = arg[(arg.LastIndexOf(',') + 1)..];
                path = Path.GetFullPath(path);
                vm.PlatformService.SetAsLockScreen(path);
                Environment.Exit(0);
            }
            
            vm.CurrentView = vm.ImageViewer;
            Task.Run(() => QuickLoad.QuickLoadAsync(vm, arg));
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
