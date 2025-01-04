using System.Runtime;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using PicView.Avalonia.ColorManagement;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.StartUp;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Win32.Views;
using PicView.Core.Config;
using PicView.Core.FileHandling;
using PicView.Core.Localization;
using PicView.Core.ProcessHandling;
using PicView.WindowsNT;
using PicView.WindowsNT.FileHandling;
using PicView.WindowsNT.Lockscreen;
using PicView.WindowsNT.Taskbar;
using PicView.WindowsNT.Wallpaper;
using Dispatcher = Avalonia.Threading.Dispatcher;
using Win32Clipboard = PicView.WindowsNT.Copy.Win32Clipboard;

namespace PicView.Avalonia.Win32;

public partial class App : Application, IPlatformSpecificService
{
    private WinMainWindow? _mainWindow;
    private ExifWindow? _exifWindow;
    private SettingsWindow? _settingsWindow;
    private KeybindingsWindow? _keybindingsWindow;
    private AboutWindow? _aboutWindow;
    private SingleImageResizeWindow? _singleImageResizeWindow;
    private BatchResizeWindow? _batchResizeWindow;
    private EffectsWindow? _effectsWindow;
    private MainViewModel? _vm;
    
    private TaskbarProgress? _taskbarProgress;

    public override void Initialize()
    {
        ProfileOptimization.SetProfileRoot(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/"));
        ProfileOptimization.StartProfile("ProfileOptimization");
        AvaloniaXamlLoader.Load(this);
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

            bool settingsExists;
            try
            {
                settingsExists = await SettingsHelper.LoadSettingsAsync().ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                return;
            }
        
            TranslationHelper.Init();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ThemeManager.DetermineTheme(Current, settingsExists);

                _mainWindow = new WinMainWindow();
                desktop.MainWindow = _mainWindow;
            });
        
            _vm = new MainViewModel(this);
        
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _mainWindow.DataContext = _vm;
                StartUpHelper.Start(_vm, settingsExists, desktop, _mainWindow);
            });
        }
        catch (Exception e)
        {
            #if DEBUG
            Console.WriteLine(e);
            #endif
        }
    }
    
    #region Interface Implementations
    
    public void SetTaskbarProgress(ulong progress, ulong maximum)
    {
        if (_taskbarProgress is null)
        {
            var handle = _mainWindow?.TryGetPlatformHandle()?.Handle;
    
            // Ensure the handle is valid before proceeding
            if (handle == IntPtr.Zero || handle is null)
            {
                return;
            }
            _taskbarProgress = new TaskbarProgress(handle.Value);
        }

        _taskbarProgress.SetProgress(progress, maximum);
    }
    
    public void StopTaskbarProgress()
    {
        var handle = _mainWindow?.TryGetPlatformHandle()?.Handle;
    
        // Ensure the handle is valid before proceeding
        if (handle == IntPtr.Zero || handle is null)
        {
            return;
        }
        _taskbarProgress?.StopProgress();
        
        _taskbarProgress = null;
    }

    public void SetCursorPos(int x, int y)
    {
        NativeMethods.SetCursorPos(x, y);
    }

    public List<string> GetFiles(FileInfo fileInfo)
    {
        var files = FileListHelper.RetrieveFiles(fileInfo);
        return FileListManager.SortIEnumerable(files, this);
    }

    public int CompareStrings(string str1, string str2)
    {
        return NativeMethods.StrCmpLogicalW(str1, str2);
    }

    public void OpenWith(string path)
    {
        ProcessHelper.OpenWith(path);
    }

    public void LocateOnDisk(string path)
    {
        var folder = Path.GetDirectoryName(path);
        FileExplorer.OpenFolderAndSelectFile(folder, path);
    }

    public void ShowFileProperties(string path)
    {
        FileExplorer.ShowFileProperties(path);
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
                _aboutWindow.Activate();
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
                _exifWindow.Activate();
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
                _keybindingsWindow.Activate();
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
                _settingsWindow.Activate();
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
                _singleImageResizeWindow.Activate();                
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
                _batchResizeWindow.Show();
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
                _effectsWindow.Show();
            }
            _= FunctionsHelper.CloseMenus();
        }
    }

    public void Print(string path)
    {
        ProcessHelper.Print(path);
    }

    public void SetAsWallpaper(string path, int wallpaperStyle)
    {
        var style = (WallpaperHelper.WallpaperStyle)wallpaperStyle;
        WallpaperHelper.SetDesktopWallpaper(path, style);
    }
    
    public bool SetAsLockScreen(string path)
    {
        return LockscreenHelper.SetLockScreenImage(path);
    }

    public bool CopyFile(string path)
    {
        return Win32Clipboard.CopyFileToClipboard(false, path);
    }
    
    public bool CutFile(string path)
    {
        return Win32Clipboard.CopyFileToClipboard(true, path);
    }

    public Task CopyImageToClipboard()
    {
        return Task.CompletedTask;
    }
    
    public async Task<bool> ExtractWithLocalSoftwareAsync(string path, string tempDirectory)
    {
        return await ArchiveExtractionHelper.ExtractWithLocalSoftwareAsync(path, tempDirectory);
    }

    public string DefaultJsonKeyMap()
    {
        return WindowsKeybindings.DefaultKeybindings;
    }

    public void DisableScreensaver()
    {
        NativeMethods.DisableScreensaver();
    }
    
    public void EnableScreensaver()
    {
        NativeMethods.EnableScreensaver();
    }
    
    #endregion
}