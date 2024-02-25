using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using PicView.Avalonia.MacOS.Views;
using PicView.Avalonia.ViewModels;
using System.Runtime;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PicView.Core.Config;
using PicView.Core.Localization;
using Avalonia.Controls;
using PicView.Avalonia.Helpers;
using PicView.Core.FileHandling;
using ReactiveUI;

namespace PicView.Avalonia.MacOS;

public partial class App : Application
{
    public override void Initialize()
    {
        ProfileOptimization.SetProfileRoot(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/"));
        ProfileOptimization.StartProfile("ProfileOptimization");
        base.OnFrameworkInitializationCompleted();
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
        var w = desktop.MainWindow = new MacMainWindow();
        var vm = new MainViewModel();
        vm.RetrieveFilesCommand = ReactiveCommand.Create(() =>
        {
            vm.Pics = FileListHelper.RetrieveFiles(vm.FileInfo).ToList();
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