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

namespace PicView.Avalonia.Helpers;

public static class StartUpHelper
{
    public static void Start(MainViewModel vm, bool settingsExists, IClassicDesktopStyleApplicationLifetime desktop, Window w)
    {
        vm.ScreenSize = ScreenHelper.GetScreenSize(w);
        
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
        vm.SetLoadingTitle();
        vm.IsLoading = true;
        _ = Task.Run(vm.UpdateLanguage);

        vm.GetFlipped = vm.Flip;

        if (SettingsHelper.Settings.Zoom.ScrollEnabled)
        {
            vm.ToggleScrollBarVisibility = ScrollBarVisibility.Visible;
            vm.GetScrolling = TranslationHelper.GetTranslation("ScrollingEnabled");
            vm.IsScrollingEnabled = true;
        }
        else
        {
            vm.ToggleScrollBarVisibility = ScrollBarVisibility.Disabled;
            vm.GetScrolling = TranslationHelper.GetTranslation("ScrollingDisabled");
            vm.IsScrollingEnabled = false;
        }
        
        vm.GetBottomGallery = vm.IsBottomGalleryShown ?
            TranslationHelper.GetTranslation("HideBottomGallery") :
            TranslationHelper.GetTranslation("ShowBottomGallery");

        if (SettingsHelper.Settings.WindowProperties.TopMost)
        {
            desktop.MainWindow.Topmost = true;
        }

        vm.ImageViewer = new ImageViewer();
        var args = Environment.GetCommandLineArgs();
        if (args.Length > 1)
        {
            Task.Run(() => NavigationHelper.LoadPicFromString(args[1], vm));
        }
        else if (SettingsHelper.Settings.StartUp.OpenLastFile)
        {
            Task.Run(() => NavigationHelper.LoadPicFromString(SettingsHelper.Settings.StartUp.LastFile, vm));
        }
        else
        {
            vm.CurrentView = new StartUpMenu();
            vm.IsLoading = false;
        }

        
        const int defaultBottomGalleryHeight = 53;
        const int defaultFullGalleryHeight = 73;

        if (!settingsExists)
        {
            vm.GetBottomGalleryItemHeight = defaultBottomGalleryHeight;
            vm.GetFullGalleryItemHeight = defaultFullGalleryHeight;
        }
        // Set default gallery sizes if they are out of range or upgrading from an old version
        if (vm.GetBottomGalleryItemHeight < vm.MinBottomGalleryItemHeight ||
            vm.GetBottomGalleryItemHeight > vm.MaxBottomGalleryItemHeight)
        {
            vm.GetBottomGalleryItemHeight = defaultBottomGalleryHeight;
        }
        if (vm.GetFullGalleryItemHeight  < vm.MaxFullGalleryItemHeight ||
            vm.GetFullGalleryItemHeight > vm.MaxFullGalleryItemHeight)
        {
            vm.GetFullGalleryItemHeight = defaultFullGalleryHeight;
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
        
        UIHelper.AddMenus(desktop);
        UIHelper.AddMToolTipMessage(desktop);

        Task.Run(KeybindingsHelper.LoadKeybindings);

        w.KeyDown += async (_, e) => await MainKeyboardShortcuts.MainWindow_KeysDownAsync(e).ConfigureAwait(false);
        w.KeyUp += (_, e) => MainKeyboardShortcuts.MainWindow_KeysUp(e);
        w.PointerPressed += async (_, e) => await MouseShortcuts.MainWindow_PointerPressed(e).ConfigureAwait(false);
    }
}