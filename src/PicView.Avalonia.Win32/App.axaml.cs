using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using PicView.Avalonia.Helpers;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Win32.Views;
using PicView.Core.Config;
using PicView.Core.Localization;
using ReactiveUI;

namespace PicView.Avalonia.Win32;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        try
        {
            await SettingsHelper.LoadSettingsAsync();
            _ = Task.Run(() => TranslationHelper.LoadLanguage(SettingsHelper.Settings.UIProperties.UserLanguage));
        }
        catch (TaskCanceledException)
        {
            return;
        }
        var w = desktop.MainWindow = new WinMainWindow();
        var vm = new MainViewModel();
        vm.RetrieveFilesCommand = ReactiveCommand.Create(() =>
        {
            vm.Pics = Windows.FileHandling.GetFileList.Get(vm.FileInfo);
        });
        w.DataContext = vm;
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
            WindowHelper.InitializeWindowSizeAndPosition(desktop);
        }
        w.Show();
        await vm.StartUpTask();
        base.OnFrameworkInitializationCompleted();
    }
}