using System;
using System.Runtime;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using PicView.Avalonia.Helpers;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Win32.Views;
using ReactiveUI;

namespace PicView.Avalonia.Win32;

public class App : Application
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
            var w = desktop.MainWindow = new WinMainWindow();
            w.DataContext = new MainViewModel();
        }

        base.OnFrameworkInitializationCompleted();
    }
}