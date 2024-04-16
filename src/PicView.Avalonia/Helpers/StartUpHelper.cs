using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using PicView.Avalonia.Keybindings;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views;
using PicView.Avalonia.Views.UC;
using PicView.Core.Config;

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

        vm.IsLoading = false;
        
        await KeybindingsHelper.LoadKeybindings(vm).ConfigureAwait(false);
        w.KeyDown += async (_, e) => await MainKeyboardShortcuts.MainWindow_KeysDownAsync(e).ConfigureAwait(false);
        w.KeyUp += async (_, e) => await MainKeyboardShortcuts.MainWindow_KeysUp(e).ConfigureAwait(false);
    }
}