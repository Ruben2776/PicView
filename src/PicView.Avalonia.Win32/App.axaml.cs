using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
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
    private ExifWindow? _exifWindow;
    private SettingsWindow? _settingsWindow;
    private KeybindingsWindow? _keybindingsWindow;
    private AboutWindow? _aboutWindow;
    private MainViewModel? _vm;

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
            await Task.Run(() => TranslationHelper.LoadLanguage(SettingsHelper.Settings.UIProperties.UserLanguage));
        }
        catch (TaskCanceledException)
        {
            return;
        }

        var w = desktop.MainWindow = new WinMainWindow();
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
        _vm.CloseMenuCommand.Execute(null);
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
        _vm.CloseMenuCommand.Execute(null);
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
        _vm.CloseMenuCommand.Execute(null);
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
        _vm.CloseMenuCommand.Execute(null);
    }
}