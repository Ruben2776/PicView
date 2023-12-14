using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views;
using System.Runtime.InteropServices;

namespace PicView.Avalonia;

public partial class App : Application
{
    public override void Initialize()
    {
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
                    DataContext = new MainWindowViewModel(),
                };
            }
            else
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}