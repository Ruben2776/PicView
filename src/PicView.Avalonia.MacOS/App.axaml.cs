using System.Diagnostics;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using PicView.Avalonia.ColorManagement;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.MacOS.Views;
using PicView.Avalonia.SettingsManagement;
using PicView.Avalonia.StartUp;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.FileAssociations;
using PicView.Core.FileSorting;
using PicView.Core.IPlatform;
using PicView.Core.Localization;
using PicView.Core.MacOS;
using PicView.Core.MacOS.Cursor;
using PicView.Core.MacOS.FileAssociation;
using PicView.Core.MacOS.FileFunctions;
using PicView.Core.MacOS.Wallpaper;
using PicView.Core.ProcessHandling;
using PicView.Core.ViewModels;
using MainWindowViewModel = PicView.Core.ViewModels.MainWindowViewModel;

#pragma warning disable CS0618 // Type or member is obsolete

namespace PicView.Avalonia.MacOS;

public class App : Application, IPlatformSpecificService
{
    private MacMainWindow? _mainWindow;
    private static CoreViewModel? _coreViewModel;
    private static MainWindowViewModel? _mainWindowViewModel;
    
    ///  Flag to track if we are processing the initial startup file
    private bool _isInitialLoad;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

#if DEBUG
        this.AttachDeveloperTools();
#endif
    }

    // The startup procedure for macOS is a bit different than Windows.
    public override void OnFrameworkInitializationCompleted()
    {
        string? startUpFilePath = null;

        if (this.TryGetFeature<IActivatableLifetime>() is { } activatableLifetime)
        {
            activatableLifetime.Activated += async (_, e) =>
            {
                if (e is FileActivatedEventArgs fileArgs)
                {
                    if (fileArgs.Files.Count <= 0)
                    {
                        return;
                    }

                    startUpFilePath = fileArgs.Files[0].Path.LocalPath;
                    await HandleInitialLoadOrConsecutive();
                }
                else if (e is ProtocolActivatedEventArgs protocolArgs)
                {
                    startUpFilePath = protocolArgs.Uri.LocalPath;
                    await HandleInitialLoadOrConsecutive();
                }

            };
        }
        base.OnFrameworkInitializationCompleted();        

        var settingsExists = LoadSettings();
        TranslationManager.Init();

        _coreViewModel = new CoreViewModel(this, GetImageModel.GetImageModelAsync);
        DataContext = _coreViewModel;

        ThemeManager.DetermineTheme(Current, settingsExists);

        _mainWindow = new MacMainWindow();
        _mainWindowViewModel = _mainWindow.DataContext as MainWindowViewModel;
        _coreViewModel.MainWindows.MainWindows.Add(_mainWindowViewModel);
        _coreViewModel.MainWindows.ActiveWindow.Value = _mainWindowViewModel;
        
        TranslationManager.Init();
        SettingsUpdater.InitializeSettings(_mainWindowViewModel, settingsExists);
        WindowFunctions.HandleWindowScalingMode(_coreViewModel, _mainWindow);
        _mainWindow.Show();
        
        var arg = Environment.GetCommandLineArgs();
        if (arg.Length > 1)
        {
            startUpFilePath = arg[1];
        }
        if (startUpFilePath is not null)
        {
            Task.Run(() => QuickLoad.QuickLoadAsync(_mainWindow, _coreViewModel, startUpFilePath, false));
        }
        else
        {
            // Retry again because FileActivatedEventArgs is very fickle #360 
            Dispatcher.UIThread.Post(() =>
            {
                if (startUpFilePath is not null)
                {
                    Task.Run(() => QuickLoad.QuickLoadAsync(_mainWindow, _coreViewModel, startUpFilePath, false));
                }
                else
                {
                    StartUpHelper.StartUpMenuOrLastFile(_mainWindow, _coreViewModel);
                }
            }, DispatcherPriority.Background);
        }
        
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }
        
        StartUpHelper.HandlePostWindowUpdates(_coreViewModel, desktop, _mainWindow);
        
        return;

        async ValueTask HandleInitialLoadOrConsecutive()
        {
            if (!_isInitialLoad)
            {
                _isInitialLoad = true;
                await QuickLoad.QuickLoadAsync(_mainWindow, _coreViewModel, startUpFilePath, true, true).ConfigureAwait(false);
                return;
            }
            if (Settings.UIProperties.OpenInSameWindow)
            {
                Dispatcher.UIThread.Invoke(() => { _mainWindow.Activate(); }, DispatcherPriority.Send);
                await _coreViewModel.MainWindows.ActiveWindow.CurrentValue.WindowTabs.LoadFromStringAsync(startUpFilePath).ConfigureAwait(false);
            }
            else
            {
                ProcessHelper.StartNewProcess(startUpFilePath);
            }
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
                DataContext = _mainWindowViewModel
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

    public async ValueTask Print(string path)
    {
        await _mainWindow.ShowPrintWindow(path);
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

    // TODO: Implement macOS thumbnail extraction (e.g. via Quick Look)
    public byte[]? GetShellThumbnail(string path, int width, int height, out int pixelWidth, out int pixelHeight)
    {
        pixelWidth = 0;
        pixelHeight = 0;
        return null;
    }
    
    #endregion
}