using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Threading;
using PicView.Avalonia.Helpers;
using PicView.Avalonia.MacOS.Views;
using PicView.Avalonia.Services;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;
using PicView.Core.FileHandling;
using PicView.Core.Localization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime;
using System.Threading.Tasks;

namespace PicView.Avalonia.MacOS;

public class App : Application, IPlatformSpecificService
{
    private ExifWindow? _exifWindow;
    private SettingsWindow? _settingsWindow;
    private KeybindingsWindow? _keybindingsWindow;
    private AboutWindow? _aboutWindow;
    private ImageResizeWindow? _imageResizeWindow;
    private MainViewModel? _vm;

    public override void Initialize()
    {
        ProfileOptimization.SetProfileRoot(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/"));
        ProfileOptimization.StartProfile("ProfileOptimization");
        base.OnFrameworkInitializationCompleted();
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
            await Task.Run(() => TranslationHelper.LoadLanguage(SettingsHelper.Settings.UIProperties.UserLanguage));
        }
        catch (TaskCanceledException)
        {
            return;
        }
        var w = desktop.MainWindow = new MacMainWindow();
        _vm = new MainViewModel(this);
        w.DataContext = _vm;
        await StartUpHelper.Start(_vm, settingsExists, desktop, w);

        Dispatcher.UIThread.InvokeAsync(() =>
        {
            w.KeyBindings.Add(new KeyBinding { Command = _vm.ToggleUICommand, Gesture = new KeyGesture(Key.Z, KeyModifiers.Alt) });
        });
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

    public void OpenWith(string path)
    {
        //
    }

    public void LocateOnDisk(string path)
    {
        Process.Start("open", $"-R \"{path}\"");
    }
    
    public void ShowFileProperties(string path)
    {
        // TODO implement show file properties on macOS
    }


    public void ShowAboutWindow()
    {
        if (Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }
        if (_aboutWindow is null)
        {
            _aboutWindow = new AboutWindow
            {
                DataContext = _vm,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };
            _aboutWindow.Show(desktop.MainWindow);
            _aboutWindow.Closing += (s, e) => _aboutWindow = null;
        }
        else
        {
            _aboutWindow.Activate();
        }

        FunctionsHelper.CloseMenus();
    }

    public void ShowExifWindow()
    {
        if (Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }
        if (_exifWindow is null)
        {
            _exifWindow = new ExifWindow
            {
                DataContext = _vm,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };
            _exifWindow.Show(desktop.MainWindow);
            _exifWindow.Closing += (s, e) => _exifWindow = null;
        }
        else
        {
            _exifWindow.Activate();
        }
        FunctionsHelper.CloseMenus();
    }

    public void ShowKeybindingsWindow()
    {
        if (Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }
        if (_keybindingsWindow is null)
        {
            _keybindingsWindow = new KeybindingsWindow
            {
                DataContext = _vm,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };
            _keybindingsWindow.Show(desktop.MainWindow);
            _keybindingsWindow.Closing += (s, e) => _keybindingsWindow = null;
        }
        else
        {
            _keybindingsWindow.Activate();
        }
        FunctionsHelper.CloseMenus();
    }

    public void ShowSettingsWindow()
    {
        if (Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }
        if (_settingsWindow is null)
        {
            _settingsWindow = new SettingsWindow
            {
                DataContext = _vm,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };
            _settingsWindow.Show(desktop.MainWindow);
            _settingsWindow.Closing += (s, e) => _settingsWindow = null;
        }
        else
        {
            _settingsWindow.Activate();
        }
        FunctionsHelper.CloseMenus();
    }

    public void ShowEffectsWindow()
    {
        // TODO: Implement ShowEffectsWindow
    }

    public void ShowResizeWindow()
    {
        // TODO: Implement ShowResizeWindow
    }

    public void Print(string path)
    {
        // TODO: Implement Print
    }
}