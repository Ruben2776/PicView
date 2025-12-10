using System.Diagnostics;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using PicView.Avalonia.ColorManagement;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.MacOS.Views;
using PicView.Avalonia.MacOS.WindowImpl;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.StartUp;
using PicView.Avalonia.ViewModels;
using PicView.Core.FileAssociations;
using PicView.Core.FileSorting;
using PicView.Core.Localization;
using PicView.Core.MacOS;
using PicView.Core.MacOS.Cursor;
using PicView.Core.MacOS.FileAssociation;
using PicView.Core.MacOS.FileFunctions;
using PicView.Core.MacOS.Wallpaper;
using PicView.Core.ProcessHandling;

#pragma warning disable CS0618 // Type or member is obsolete

namespace PicView.Avalonia.MacOS;

public class App : Application, IPlatformSpecificService, IPlatformWindowService
{
    private MacMainWindow? _mainWindow;
    private static WindowInitializer? _windowInitializer;
    private MainViewModel? _vm;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        
#if DEBUG
        this.AttachDeveloperTools();
#endif
    }

    public override void OnFrameworkInitializationCompleted()
    {
        try
        {
            base.OnFrameworkInitializationCompleted();
            
            string? startUpFilePath = null;
            EventHandler<UrlOpenedEventArgs> handler = (_, e) => { startUpFilePath = e.Urls[0]; };
            Current.UrlsOpened += handler;

            if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            {
                return;
            }

            var settingsExists = LoadSettings();
            _vm = new MainViewModel(this, this);
        
            TranslationManager.Init();

            DataContext = _vm;
            ThemeManager.DetermineTheme(Current, settingsExists);

            _mainWindow = new MacMainWindow();
            desktop.MainWindow = _mainWindow;

            _mainWindow.DataContext = _vm;
            if (string.IsNullOrWhiteSpace(startUpFilePath))
            {
                StartUpHelper.StartWithoutArguments(_vm, settingsExists, desktop, _mainWindow);
            }
            else
            {
                StartUpHelper.StartUpBlank(_vm, settingsExists, desktop, _mainWindow);
            }
            _windowInitializer = new WindowInitializer();
            
            // Register for macOS file opening
            Current.UrlsOpened += async (_, e) =>
            {
                if (Settings.UIProperties.OpenInSameWindow)
                {
                    Dispatcher.UIThread.Invoke(() => 
                    {
                        _mainWindow.Activate();
                    }, DispatcherPriority.Send);
                    await NavigationManager.LoadPicFromStringAsync(e.Urls[0], _vm);
                }
                else
                {
                    ProcessHelper.StartNewProcess(e.Urls[0]);
                }
            };
            Current.UrlsOpened -= handler;
        }
        catch (Exception)
        {
            //
        }
    }

    #region Interface implementations
    
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
        MacOSCursor.SetCursorPos(x, y);
    }

    public List<FileInfo> GetFiles(FileInfo fileInfo)
    {
        return FileListRetriever.RetrieveFiles(fileInfo, CompareStrings);
    }

    public int CompareStrings(string str1, string str2)
    {
        return string.CompareOrdinal(str1, str2);
    }

    public void OpenWith(string path)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var openWithView = new OpenWithView(path)
            {
                DataContext = _vm
            };
            openWithView.Show();
        }, DispatcherPriority.Input);
        
    }

    public void LocateOnDisk(string path)
    {
        Process.Start("open", $"-R \"{path}\"");
    }
    
    public void ShowFileProperties(string path)
    {
        _ = FileProperties.ShowFilePropertiesAsync(path);
        // TODO: make interface async
    }

    public void Print(string path)
    {
        _windowInitializer?.ShowPrintPreviewWindow(_vm, path);
    }

    public async Task SetAsWallpaper(string path, int wallpaperStyle)
    {
         await WallpaperHelper.SetWallpaper(path);
    }

    public bool SetAsLockScreen(string path)
    {
        // wallpaper and lockscreen are the same in macOS
        return false;
    }
    
    public bool CopyFile(string path)
    {
        // TODO: Implement copying file to clipboard
        return false;
    }
    
    public bool CutFile(string path)
    {
        // TODO: Implement cutting file to clipboard
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
        return MacOsKeybindings.DefaultKeybindings;
    }

    public void InitiateFileAssociationService()
    {
        var iIFileAssociationService = new MacFileAssociationService();
        FileAssociationManager.Initialize(iIFileAssociationService);
    }

    public async Task<bool> DeleteFile(string path, bool recycle)
    {
        if (recycle)
        {
            return await Task.Run(() => OsxFileHelper.MoveFileToRecycleBinAsync(path));
        }
        await Task.Run(() => File.Delete(path));
        return !File.Exists(path); 
    }
    
    #endregion
    
    #region Window interface implementations

    public int CombinedTitleButtonsWidth { get; set; } = 165;
    
    public void ShowAboutWindow() =>
        _windowInitializer?.ShowAboutWindow(_vm);

    public async Task ShowImageInfoWindow() =>
        await _windowInitializer?.ShowImageInfoWindow(_vm);

    public async Task ShowKeybindingsWindow() =>
        _windowInitializer?.ShowKeybindingsWindow(_vm);

    public async Task ShowSettingsWindow() =>
        await _windowInitializer?.ShowSettingsWindow(_vm);

    public void ShowSingleImageResizeWindow() =>
        _windowInitializer?.ShowSingleImageResizeWindow(_vm);

    public async Task ShowBatchResizeWindow() =>
        await _windowInitializer?.ShowBatchResizeWindow(_vm);

    public void ShowEffectsWindow() =>
        _windowInitializer?.ShowEffectsWindow(_vm);

    public void ShowConvertWindow() =>
        _windowInitializer?.ShowConvertWindow(_vm);

    /// <inheritdoc />
    public async Task Maximize(bool saveSetting = true) =>
        await MacOSWindow.Maximize(_mainWindow, _vm, saveSetting);
    
    /// <inheritdoc />
    public async Task MaximizeRestore(bool saveSetting = true) =>
        await MacOSWindow.ToggleMaximize(_mainWindow, _vm, saveSetting);

    /// <inheritdoc />
    public async Task Fullscreen(bool saveSetting = true) =>
        await MacOSWindow.Fullscreen(_mainWindow, _vm, saveSetting);
    
    /// <inheritdoc />
    public async Task ToggleFullscreen(bool saveSetting = true) =>
        await MacOSWindow.ToggleFullscreen(_mainWindow, _vm, saveSetting);
    
    /// <inheritdoc />
    public async Task Restore() =>
        await MacOSWindow.Restore(_mainWindow, _vm);
    
    #endregion
}