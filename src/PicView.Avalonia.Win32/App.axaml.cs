using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using PicView.Avalonia.Helpers;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Win32.Views;

namespace PicView.Avalonia.Win32;

public class App : Application
{
    public override void Initialize()
    {
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