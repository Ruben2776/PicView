using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using PicView.Avalonia.Keybindings;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views;
using PicView.Avalonia.Views.UC;
using PicView.Core.Config;
using PicView.Core.Localization;

namespace PicView.Avalonia.Helpers;

public static class StartUpHelper
{
    public static async Task Start(MainViewModel vm, bool settingsExists, IClassicDesktopStyleApplicationLifetime desktop, Window w)
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
        vm.IsLoading = true;

        vm.GetFlipped = vm.Flip;

        if (SettingsHelper.Settings.Zoom.ScrollEnabled)
        {
            vm.ToggleScrollBarVisibility = ScrollBarVisibility.Visible;
            vm.GetScrolling = TranslationHelper.GetTranslation("ScrollingEnabled");
            vm.IsScrollingEnabled = true;
            SettingsHelper.Settings.Zoom.ScrollEnabled = true;
        }
        else
        {
            vm.ToggleScrollBarVisibility = ScrollBarVisibility.Disabled;
            vm.GetScrolling = TranslationHelper.GetTranslation("ScrollingDisabled");
            vm.IsScrollingEnabled = false;
            SettingsHelper.Settings.Zoom.ScrollEnabled = false;
        }

        if (SettingsHelper.Settings.WindowProperties.TopMost)
        {
            desktop.MainWindow.Topmost = true;
        }

        vm.ImageViewer = new ImageViewer();
        var args = Environment.GetCommandLineArgs();
        if (args.Length > 1)
        {
            await vm.LoadPicFromString(args[1]).ConfigureAwait(false);
        }
        else if (SettingsHelper.Settings.StartUp.OpenLastFile)
        {
            await vm.LoadPicFromString(SettingsHelper.Settings.StartUp.LastFile).ConfigureAwait(false);
        }
        else
        {
            vm.CurrentView = new StartUpMenu();
        }

        if (vm.GalleryItemSize <= 0)
        {
            var screen = ScreenHelper.GetScreen(desktop.MainWindow);
            // ReSharper disable once PossibleLossOfFraction
            vm.GalleryItemSize = screen.WorkingArea.Height / 10;
        }

        vm.IsLoading = false;

        await KeybindingsHelper.LoadKeybindings(vm).ConfigureAwait(false);

        w.KeyDown += async (_, e) => await MainKeyboardShortcuts.MainWindow_KeysDownAsync(e).ConfigureAwait(false);
        w.KeyUp += async (_, e) => await MainKeyboardShortcuts.MainWindow_KeysUp(e).ConfigureAwait(false);
    }
}