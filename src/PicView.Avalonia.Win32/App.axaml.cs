using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using PicView.Avalonia.Helpers;
using PicView.Avalonia.Services;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Win32.Views;
using PicView.Core.Config;
using PicView.Core.FileHandling;
using PicView.Core.Localization;
using System.Runtime;
using SortHelper = PicView.Avalonia.Helpers.SortHelper;

namespace PicView.Avalonia.Win32;

public class App : Application, IPlatformSpecificService
{
    public override void Initialize()
    {
        ProfileOptimization.SetProfileRoot(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/"));
        ProfileOptimization.StartProfile("ProfileOptimization");
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        base.OnFrameworkInitializationCompleted();

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
        var w = desktop.MainWindow = new WinMainWindow();
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

        await vm.StartUpTask();
    }

    public void SetCursorPos(int x, int y)
    {
        Windows.NativeMethods.SetCursorPos(x, y);
    }

    public List<string> GetFiles(FileInfo fileInfo)
    {
        var files = FileListHelper.RetrieveFiles(fileInfo);
        return SortHelper.SortIEnumerable(files, this);
    }

    public int CompareStrings(string str1, string str2)
    {
        return Windows.NativeMethods.StrCmpLogicalW(str1, str2);
    }
}