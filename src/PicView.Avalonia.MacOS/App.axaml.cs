using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using PicView.Avalonia.MacOS.Views;
using PicView.Avalonia.ViewModels;
using System.Runtime;
using System;
using System.IO;
using System.Threading.Tasks;
using PicView.Core.Config;
using PicView.Core.Localization;

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
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
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
            w.DataContext = new MainViewModel();
            w.Show();
        }

        base.OnFrameworkInitializationCompleted();
    }
}