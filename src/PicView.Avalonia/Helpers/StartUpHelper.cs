using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.Keybindings;
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
            Task.Run(() => vm.LoadPicFromString(args[1]));
        }
        else if (SettingsHelper.Settings.StartUp.OpenLastFile)
        {
            Task.Run(() => vm.LoadPicFromString(SettingsHelper.Settings.StartUp.LastFile));
        }
        else
        {
            vm.CurrentView = new StartUpMenu();
        }
        
        UIHelper.AddMenus(desktop);

        if (vm.GalleryItemSize <= 0)
        {
            GalleryFunctions.SetGalleryItemSize(vm);
        }

        if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
        {
            GalleryFunctions.OpenBottomGallery(vm);
        }

        Task.Run(() => KeybindingsHelper.LoadKeybindings(vm));

        w.KeyDown += async (_, e) => await MainKeyboardShortcuts.MainWindow_KeysDownAsync(e).ConfigureAwait(false);
        w.KeyUp += async (_, e) => await MainKeyboardShortcuts.MainWindow_KeysUp(e).ConfigureAwait(false);
        w.PointerPressed += async (_, e) => await MouseShortcuts.MainWindow_PointerPressed(e).ConfigureAwait(false);
    }
}