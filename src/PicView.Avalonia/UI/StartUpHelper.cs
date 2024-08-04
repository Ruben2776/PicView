using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.Keybindings;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views;
using PicView.Avalonia.Views.UC;
using PicView.Core.Config;
using PicView.Core.Localization;

namespace PicView.Avalonia.UI;

public static class StartUpHelper
{
    public static void Start(MainViewModel vm, bool settingsExists, IClassicDesktopStyleApplicationLifetime desktop, Window w)
    {
        ScreenHelper.ScreenSize = ScreenHelper.GetScreenSize(w);
        UIHelper.SetControls(desktop);
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
            if (SettingsHelper.Settings.WindowProperties.Fullscreen)
            {
                WindowHelper.Fullscreen(vm, desktop);
            }
            else if (SettingsHelper.Settings.WindowProperties.Maximized)
            {
                WindowHelper.Maximize();
            }
            if (SettingsHelper.Settings.WindowProperties.AutoFit)
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
                WindowHelper.InitializeWindowSizeAndPosition(w);
            }
        }
        w.Show();
        vm.IsLoading = true;
        vm.UpdateLanguage();
        vm.GetFlipped = vm.Flip;
        
        if (SettingsHelper.Settings.Zoom.ScrollEnabled)
        {
            vm.ToggleScrollBarVisibility = ScrollBarVisibility.Visible;
            vm.GetScrolling = TranslationHelper.Translation.ScrollingEnabled;
            vm.IsScrollingEnabled = true;
        }
        else
        {
            vm.ToggleScrollBarVisibility = ScrollBarVisibility.Disabled;
            vm.GetScrolling = TranslationHelper.Translation.ScrollingDisabled;
            vm.IsScrollingEnabled = false;
        }
        
        vm.GetBottomGallery = vm.IsGalleryShown ?
            TranslationHelper.Translation.HideBottomGallery :
            TranslationHelper.Translation.ShowBottomGallery;

        if (SettingsHelper.Settings.WindowProperties.TopMost)
        {
            desktop.MainWindow.Topmost = true;
        }

        vm.ImageViewer = new ImageViewer();
        var args = Environment.GetCommandLineArgs();
        if (args.Length > 1)
        {
            _ = QuickLoad.QuickLoadAsync(vm, args[1]);
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
                Task.Run(() => NavigationHelper.LoadPicFromStringAsync(SettingsHelper.Settings.StartUp.LastFile, vm));
            }
        }
        else
        {
            vm.CurrentView = new StartUpMenu();
            vm.IsLoading = false;
        }

        if (!settingsExists)
        {
            vm.GetBottomGalleryItemHeight = Core.Gallery.GalleryDefaults.DefaultBottomGalleryHeight;
            vm.GetFullGalleryItemHeight = Core.Gallery.GalleryDefaults.DefaultFullGalleryHeight;
        }
        // Set default gallery sizes if they are out of range or upgrading from an old version
        if (vm.GetBottomGalleryItemHeight < vm.MinBottomGalleryItemHeight ||
            vm.GetBottomGalleryItemHeight > vm.MaxBottomGalleryItemHeight)
        {
            vm.GetBottomGalleryItemHeight = Core.Gallery.GalleryDefaults.DefaultBottomGalleryHeight;
        }
        if (vm.GetFullGalleryItemHeight < vm.MinFullGalleryItemHeight ||
            vm.GetFullGalleryItemHeight > vm.MaxFullGalleryItemHeight)
        {
            vm.GetFullGalleryItemHeight = Core.Gallery.GalleryDefaults.DefaultFullGalleryHeight;
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
        
        if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
        {
            GalleryFunctions.OpenBottomGallery(vm);
        }
        
        Task.Run(KeybindingsHelper.LoadKeybindings);
        
        vm.GetLooping = SettingsHelper.Settings.UIProperties.Looping
            ? TranslationHelper.Translation.LoopingEnabled
            : TranslationHelper.Translation.LoopingDisabled;
        vm.GetScrolling = SettingsHelper.Settings.Zoom.ScrollEnabled
            ? TranslationHelper.Translation.ScrollingEnabled
            : TranslationHelper.Translation.ScrollingDisabled;
        vm.GetCtrlZoom = SettingsHelper.Settings.Zoom.CtrlZoom
            ? TranslationHelper.Translation.CtrlToZoom
            : TranslationHelper.Translation.ScrollToZoom;
        
        UIHelper.AddMenus();
        UIHelper.AddMToolTipMessage();

        w.KeyDown += async (_, e) => await MainKeyboardShortcuts.MainWindow_KeysDownAsync(e).ConfigureAwait(false);
        w.KeyUp += (_, e) => MainKeyboardShortcuts.MainWindow_KeysUp(e);
        w.PointerPressed += async (_, e) => await MouseShortcuts.MainWindow_PointerPressed(e).ConfigureAwait(false);
    }
}