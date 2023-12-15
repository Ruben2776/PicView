using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views;
using PicView.Core.Config;
using System.Runtime;
using System.Runtime.InteropServices;

namespace PicView.Avalonia;

public partial class App : Application
{
    private ISettingsManager settingsManager;

    public override void Initialize()
    {
        ProfileOptimization.SetProfileRoot(AppDomain.CurrentDomain.BaseDirectory);
        ProfileOptimization.StartProfile("ProfileOptimization");
        settingsManager = new SettingsManager();
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Run MacOS specific style
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                desktop.MainWindow = new MacMainWindow
                {
                    DataContext = new MainViewModel(settingsManager),
                };
            }
            else
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainViewModel(settingsManager),
                };
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}