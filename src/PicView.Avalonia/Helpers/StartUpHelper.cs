using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using PicView.Avalonia.Services;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;
using PicView.Core.Localization;

namespace PicView.Avalonia.Helpers;

public static class StartUpHelper
{
    public static async Task Start(IClassicDesktopStyleApplicationLifetime desktop, IPlatformSpecificService platformSpecificService, Window window)
    {
        //bool settingsExists;
        //try
        //{
        //    settingsExists = await SettingsHelper.LoadSettingsAsync();
        //    _ = Task.Run(() => TranslationHelper.LoadLanguage(SettingsHelper.Settings.UIProperties.UserLanguage));
        //}
        //catch (TaskCanceledException)
        //{
        //    return;
        //}
        //var vm = new MainViewModel(platformSpecificService);
        //window.DataContext = vm;
        //if (!settingsExists)
        //{
        //    WindowHelper.CenterWindowOnScreen();
        //    vm.CanResize = true;
        //    vm.IsAutoFit = false;
        //}
        //else
        //{
        //    if (SettingsHelper.Settings.WindowProperties.AutoFit)
        //    {
        //        desktop.MainWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        //        vm.SizeToContent = SizeToContent.WidthAndHeight;
        //        vm.CanResize = false;
        //        vm.IsAutoFit = true;
        //    }
        //    else
        //    {
        //        vm.CanResize = true;
        //        vm.IsAutoFit = false;
        //        WindowHelper.InitializeWindowSizeAndPosition(desktop);
        //    }
        //}

        //window.Show();
        //await vm.StartUpTask();
    }
}