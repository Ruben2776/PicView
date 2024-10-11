using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using PicView.Avalonia.ColorManagement;
using PicView.Avalonia.Keybindings;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.SettingsManagement;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views;
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
            // Fixes incorrect window
            w.Height = w.MinHeight;
            w.Width = w.MinWidth;
            
            WindowHelper.CenterWindowOnScreen();
            vm.CanResize = true;
            vm.IsAutoFit = false;
        }
        else
        {
            if (SettingsHelper.Settings.UIProperties.OpenInSameWindow)
            {
                var pipeName = "PicViewPipe";
                
                if (ProcessHelper.CheckIfAnotherInstanceIsRunning())
                {
                    if (args.Length > 1)
                    {
                        // Another instance is running, send arguments and exit
                        _ = IPC.SendArgumentToRunningInstance(args[1], pipeName);
                        Environment.Exit(0);
                    }
                }
            }
            if (SettingsHelper.Settings.WindowProperties.Fullscreen)
            {
                WindowHelper.Fullscreen(vm, desktop);
            }
            else if (SettingsHelper.Settings.WindowProperties.Maximized)
            {
                WindowHelper.Maximize();
            }
            else if (SettingsHelper.Settings.WindowProperties.AutoFit)
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
            else
            {
                vm.CanResize = true;
                vm.IsAutoFit = false;
                WindowHelper.InitializeWindowSizeAndPosition(w);
                if (SettingsHelper.Settings.UIProperties.ShowInterface)
                {
                    vm.IsTopToolbarShown = true;
                    vm.IsBottomToolbarShown = SettingsHelper.Settings.UIProperties.ShowBottomNavBar;
                }
            }
        }

        w.Show();
        vm.IsLoading = true;
        ScreenHelper.UpdateScreenSize(w);
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

        vm.ImageViewer = new ImageViewer();
        
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

        if (!settingsExists)
        {
            if (string.IsNullOrWhiteSpace(SettingsHelper.Settings.Gallery.BottomGalleryStretchMode))
            {
                SettingsHelper.Settings.Gallery.BottomGalleryStretchMode = "UniformToFill";
            }

            if (string.IsNullOrWhiteSpace(SettingsHelper.Settings.Gallery.FullGalleryStretchMode))
            {
                SettingsHelper.Settings.Gallery.FullGalleryStretchMode = "UniformToFill";
            }
        }
        
        BackgroundManager.SetBackground(vm);
        ColorManager.UpdateAccentColors(SettingsHelper.Settings.Theme.ColorTheme);
        
        Task.Run(KeybindingsHelper.LoadKeybindings);
        
        UIHelper.AddMenus();

        w.KeyDown += async (_, e) => await MainKeyboardShortcuts.MainWindow_KeysDownAsync(e).ConfigureAwait(false);
        w.KeyUp += (_, e) => MainKeyboardShortcuts.MainWindow_KeysUp(e);
        w.PointerPressed += async (_, e) => await MouseShortcuts.MainWindow_PointerPressed(e).ConfigureAwait(false);
        
        Application.Current.Name = "PicView";
        
        if (SettingsHelper.Settings.UIProperties.OpenInSameWindow)
        {
            // No other instance is running, create named pipe server
            _ = IPC.StartListeningForArguments("PicViewPipe", vm);
        }
    }
}