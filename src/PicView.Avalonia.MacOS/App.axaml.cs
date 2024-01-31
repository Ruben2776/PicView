using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using PicView.Avalonia.Helpers;
using PicView.Avalonia.MacOS.Views;
using PicView.Avalonia.ViewModels;
using System.Runtime;
using System;
using System.IO;

namespace PicView.Avalonia.MacOS;

public partial class App : Application
{
    public override void Initialize()
    {
        ProfileOptimization.SetProfileRoot(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/"));
        ProfileOptimization.StartProfile("ProfileOptimization");
        StartUpHelper.InitializeSettings();
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var w = desktop.MainWindow = new MacMainWindow();
            w.DataContext = new MainViewModel();
        }

        base.OnFrameworkInitializationCompleted();
    }
}