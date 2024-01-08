using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using PicView.Avalonia.ViewModels;
using System.Threading.Tasks;
using PicView.Avalonia.Win32.Views;
using PicView.Core.Config;
using PicView.Core.Localization;

namespace PicView.Avalonia.Win32;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Task.Run(async () =>
        {
            await SettingsHelper.LoadSettingsAsync();
            await TranslationHelper.LoadLanguage(SettingsHelper.Settings.UIProperties.UserLanguage);
        });
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var w = desktop.MainWindow = new WinMainWindow();
            w.DataContext = new MainViewModel();
        }

        base.OnFrameworkInitializationCompleted();
    }
}