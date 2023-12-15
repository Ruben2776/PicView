using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using PicView.Avalonia.Views;
using PicView.Core.Config;
using System.Runtime;
using System.Runtime.InteropServices;

namespace PicView.Avalonia;

public partial class App : Application
{
    public override void Initialize()
    {
        ProfileOptimization.SetProfileRoot(AppDomain.CurrentDomain.BaseDirectory);
        ProfileOptimization.StartProfile("ProfileOptimization");
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Run MacOS specific style
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                desktop.MainWindow = new MacMainWindow();
            }
            else
            {
                desktop.MainWindow = new MainWindow();
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}