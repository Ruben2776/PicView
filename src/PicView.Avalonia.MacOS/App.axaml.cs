using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using PicView.Avalonia.Helpers;
using PicView.Avalonia.MacOS.Views;
using PicView.Avalonia.Services;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;
using PicView.Core.FileHandling;
using PicView.Core.Localization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Threading.Tasks;

namespace PicView.Avalonia.MacOS;

public partial class App : Application, IPlatformSpecificService
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

        bool settingsExists;
        try
        {
            settingsExists = await SettingsHelper.LoadSettingsAsync();
            _ = Task.Run(() => TranslationHelper.LoadLanguage(SettingsHelper.Settings.UIProperties.UserLanguage));
        }
        catch (TaskCanceledException)
        {
            return;
        }
        var w = desktop.MainWindow = new MacMainWindow();
        var vm = new MainViewModel(this);
        w.DataContext = vm;
        if (!settingsExists)
        {
            WindowHelper.CenterWindowOnScreen();
            vm.CanResize = true;
            vm.IsAutoFit = false;
        }
        else
        {
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
        }

        w.Show();
        vm.StartUpTask();
        base.OnFrameworkInitializationCompleted();
    }

    public void SetCursorPos(int x, int y)
    {
        // TODO: Implement SetCursorPos
    }

    public List<string> GetFiles(FileInfo fileInfo)
    {
        var files = FileListHelper.RetrieveFiles(fileInfo);
        return SortHelper.SortIEnumerable(files, this);
    }

    public int CompareStrings(string str1, string str2)
    {
        return string.CompareOrdinal(str1, str2);
    }
}