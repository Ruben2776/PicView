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
            WindowHelper.CenterWindowOnScreen();
            vm.CanResize = true;
            vm.IsAutoFit = false;
        }
        else
        {
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
        _ = Task.Run(vm.UpdateLanguage);

        vm.SetLoadingTitle();

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
        }
        
        UIHelper.AddMenus(desktop);
        
        // Set default gallery sizes if they are out of range or upgrading from an old version
        if (vm.GetBottomGalleryItemSize is < 36 or > 110)
        {
            vm.GetBottomGalleryItemSize = 47;
        }
        if (vm.GetExpandedGalleryItemSize is  < 25 or 110)
        {
            vm.GetExpandedGalleryItemSize = 47;
        }
        
        if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
        {
            GalleryFunctions.OpenBottomGallery(vm);
        }

        Task.Run(KeybindingsHelper.LoadKeybindings);

        w.KeyDown += async (_, e) => await MainKeyboardShortcuts.MainWindow_KeysDownAsync(e).ConfigureAwait(false);
        w.KeyUp += (_, e) => MainKeyboardShortcuts.MainWindow_KeysUp(e);
        w.PointerPressed += async (_, e) => await MouseShortcuts.MainWindow_PointerPressed(e).ConfigureAwait(false);
    }
}