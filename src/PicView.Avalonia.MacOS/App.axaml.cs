using System.Diagnostics;
using System.Runtime;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using PicView.Avalonia.ColorManagement;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.MacOS.Views;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.StartUp;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Config;
using PicView.Core.FileHandling;
using PicView.Core.Localization;

namespace PicView.Avalonia.MacOS;

public class App : Application, IPlatformSpecificService
{
    private MacMainWindow? _mainWindow;
    private ExifWindow? _exifWindow;
    private SettingsWindow? _settingsWindow;
    private KeybindingsWindow? _keybindingsWindow;
    private AboutWindow? _aboutWindow;
    private SingleImageResizeWindow? _singleImageResizeWindow;
    private BatchResizeWindow? _batchResizeWindow;
    private EffectsWindow? _effectsWindow;
    private MainViewModel? _vm;

    public override void Initialize()
    {
        ProfileOptimization.SetProfileRoot(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/"));
        ProfileOptimization.StartProfile("ProfileOptimization");
        base.OnFrameworkInitializationCompleted();
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        try
        {
            base.OnFrameworkInitializationCompleted();

            if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            {
                return;
            }

            var settingsExists = await SettingsHelper.LoadSettingsAsync().ConfigureAwait(false);
        
            TranslationHelper.Init();
        
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ThemeManager.DetermineTheme(Current, settingsExists);
            
                _mainWindow = new MacMainWindow();
                desktop.MainWindow = _mainWindow;
            },DispatcherPriority.Send);
        
            _vm = new MainViewModel(this);
        
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _mainWindow.DataContext = _vm;
                StartUpHelper.Start(_vm, settingsExists, desktop, _mainWindow);
            },DispatcherPriority.Send);
        }
        catch (Exception)
        {
            //
        }
    }

    public void SetTaskbarProgress(ulong progress, ulong maximum)
    {
        // TODO: Implement SetTaskbarProgress
        // https://github.com/carina-studio/AppSuiteBase/blob/master/Core/AppSuiteApplication.MacOS.cs#L365
    }

    public void StopTaskbarProgress()
    {
        
    }

    public void SetCursorPos(int x, int y)
    {
        // TODO: Implement SetCursorPos
    }

    public List<string> GetFiles(FileInfo fileInfo)
    {
        var files = FileListHelper.RetrieveFiles(fileInfo);
        return FileListManager.SortIEnumerable(files, this);
    }

    public int CompareStrings(string str1, string str2)
    {
        return string.CompareOrdinal(str1, str2);
    }

    public void OpenWith(string path)
    {
        // TODO: Implement OpenWith on macOS
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
        if (Dispatcher.UIThread.CheckAccess())
        {
            Set();
        }
        else
        {
            Dispatcher.UIThread.InvokeAsync(Set);
        }
        return;

        void Set()
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
                if (_aboutWindow.WindowState == WindowState.Minimized)
                {
                    WindowFunctions.ShowMinimizedWindow(_aboutWindow);
                }
                else
                {
                    _aboutWindow.Show();
                }       
            }

            _ = FunctionsHelper.CloseMenus();
        }
    }

    public void ShowExifWindow()
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            Set();
        }
        else
        {
            Dispatcher.UIThread.InvokeAsync(Set);
        }
        return;

        void Set()
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
                if (_exifWindow.WindowState == WindowState.Minimized)
                {
                    WindowFunctions.ShowMinimizedWindow(_exifWindow);
                }
                else
                {
                    _exifWindow.Show();
                }      
            }

            _ = FunctionsHelper.CloseMenus();
        }
    }

    public void ShowKeybindingsWindow()
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            Set();
        }
        else
        {
            Dispatcher.UIThread.InvokeAsync(Set);
        }
        return;

        void Set()
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
                if (_keybindingsWindow.WindowState == WindowState.Minimized)
                {
                    WindowFunctions.ShowMinimizedWindow(_keybindingsWindow);
                }
                else
                {
                    _keybindingsWindow.Show();
                }      
            }

            _ = FunctionsHelper.CloseMenus();
        }
    }

    public void ShowSettingsWindow()
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            Set();
        }
        else
        {
            Dispatcher.UIThread.InvokeAsync(Set);
        }
        return;
        void Set()
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
                if (_settingsWindow.WindowState == WindowState.Minimized)
                {
                    WindowFunctions.ShowMinimizedWindow(_settingsWindow);
                }
                else
                {
                    _settingsWindow.Show();
                }     
            }
            _= FunctionsHelper.CloseMenus();
            
        }
    }

    public void ShowEffectsWindow()
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            Set();
        }
        else
        {
            Dispatcher.UIThread.InvokeAsync(Set);
        }
        return;
        void Set()
        {
            if (Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            {
                return;
            }
            if (_effectsWindow is null)
            {
                _effectsWindow = new EffectsWindow
                {
                    DataContext = _vm,    
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                };
                _effectsWindow.Show(desktop.MainWindow);
                _effectsWindow.Closing += (s, e) => _effectsWindow = null;
            }
            else
            {
                if (_effectsWindow.WindowState == WindowState.Minimized)
                {
                    WindowFunctions.ShowMinimizedWindow(_effectsWindow);
                }
                else
                {
                    _effectsWindow.Show();
                }   
            }
            _= FunctionsHelper.CloseMenus();
        }
    }

    public void ShowSingleImageResizeWindow()
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            Set();
        }
        else
        {
            Dispatcher.UIThread.InvokeAsync(Set);
        }
        return;
        void Set()
        {
            if (Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            {
                return;
            }
            if (_singleImageResizeWindow is null)
            {
                _singleImageResizeWindow = new SingleImageResizeWindow
                {
                    DataContext = _vm,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                };
                _singleImageResizeWindow.Show(desktop.MainWindow);
                _singleImageResizeWindow.Closing += (s, e) => _singleImageResizeWindow = null;
            }
            else
            {
                if (_singleImageResizeWindow.WindowState == WindowState.Minimized)
                {
                    WindowFunctions.ShowMinimizedWindow(_singleImageResizeWindow);
                }
                else
                {
                    _singleImageResizeWindow.Show();
                }  
            }
            _= FunctionsHelper.CloseMenus();
        }
    }

    public void ShowBatchResizeWindow()
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            Set();
        }
        else
        {
            Dispatcher.UIThread.InvokeAsync(Set);
        }
        return;
        void Set()
        {
            if (Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            {
                return;
            }
            if (_batchResizeWindow is null)
            {
                _batchResizeWindow = new BatchResizeWindow
                {
                    DataContext = _vm,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                };
                _batchResizeWindow.Show(desktop.MainWindow);
                _batchResizeWindow.Closing += (s, e) => _batchResizeWindow = null;
            }
            else
            {
                if (_batchResizeWindow.WindowState == WindowState.Minimized)
                {
                    WindowFunctions.ShowMinimizedWindow(_batchResizeWindow);
                }
                else
                {
                    _batchResizeWindow.Show();
                }  
            }
            _= FunctionsHelper.CloseMenus();
        }   
    }

    public void Print(string path)
    {
        // TODO: Implement Print
    }

    public void SetAsWallpaper(string path, int wallpaperStyle)
    {
        // TODO: Implement SetAsWallpaper
    }

    public bool SetAsLockScreen(string path)
    {
        // TODO: Implement SetAsLockScreen
        return false;
    }
    
    public bool CopyFile(string path)
    {
        // TODO: Implement CopyFile
        return false;
    }
    
    public bool CutFile(string path)
    {
        // TODO: Implement CutFile
        return false;
    }

    public Task CopyImageToClipboard(Bitmap bitmap)
    {
        return Task.CompletedTask;
    }

    public Task<Bitmap?> GetImageFromClipboard()
    {
        return null;
    }

    public Task<bool> ExtractWithLocalSoftwareAsync(string path, string tempDirectory)
    {
        // TODO: Implement ExtractWithLocalSoftwareAsync
        return Task.FromResult(false);
    }
    
    public void DisableScreensaver()
    {
        // TODO: Implement DisableScreensaver
    }
    
    public void EnableScreensaver()
    {
        // TODO: Implement EnableScreensaver
    }

    public string DefaultJsonKeyMap()
    {
     return   MacOsKeybindings.DefaultKeybindings;
    }
}