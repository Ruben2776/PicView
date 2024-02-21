using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Win32.Views;
using PicView.Core.Config;
using PicView.Core.Localization;

namespace PicView.Avalonia.Win32;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
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
            var w = desktop.MainWindow = new WinMainWindow();
            w.DataContext = new MainViewModel();
            w.Show();
        }

        base.OnFrameworkInitializationCompleted();
    }
}