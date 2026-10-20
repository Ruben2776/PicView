using PicView.Avalonia.Navigation;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using PicView.Avalonia.ColorManagement;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.MacOS.Views;
using PicView.Avalonia.MacOS.WindowImpl;
using PicView.Avalonia.StartUp;
using PicView.Core.FileAssociations;
using PicView.Core.FileSorting;
using PicView.Core.IPlatform;
using PicView.Core.Localization;
using PicView.Core.MacOS;
using PicView.Core.MacOS.Cursor;
using PicView.Core.MacOS.FileAssociation;
using PicView.Core.MacOS.FileFunctions;
using PicView.Core.MacOS.Imaging;
using PicView.Core.MacOS.Wallpaper;
using PicView.Core.ProcessHandling;
using PicView.Core.ViewModels;
using MainWindowViewModel = PicView.Core.ViewModels.MainWindowViewModel;
using ShellThumbnailNative = PicView.Core.MacOS.Imaging.ShellThumbnailNative;

#pragma warning disable CS0618 // Type or member is obsolete

namespace PicView.Avalonia.MacOS;

public class App : Application, IPlatformSpecificService
{
    private static CoreViewModel? _coreViewModel;
    private AppLifecycle? _app;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

#if DEBUG
        // If this cannot be resolved, run dotnet restore PicView.Avalonia.MacOS.csproj --force
        this.AttachDeveloperTools();
#endif
    }

    // macOS delivers the file PicView was launched with as an activation, which can arrive after App Start begins
    public override void OnFrameworkInitializationCompleted()
    {
        var launchFilesBeforeStart = new List<string>();
        if (this.TryGetFeature<IActivatableLifetime>() is { } activatableLifetime)
        {
            activatableLifetime.Activated += async (_, e) =>
            {
                var path = e switch
                {
                    FileActivatedEventArgs { Files.Count: > 0 } fileArgs => fileArgs.Files[0].Path.LocalPath,
                    ProtocolActivatedEventArgs protocolArgs => protocolArgs.Uri.LocalPath,
                    _ => null
                };
                if (path is null)
                {
                    return;
                }

                if (_app is null)
                {
                    launchFilesBeforeStart.Add(path);
                    return;
                }

                await _app.OpenLaunchFileAsync(path).ConfigureAwait(false);
            };
        }
        base.OnFrameworkInitializationCompleted();        

        var settingsExists = LoadSettings();
        TranslationManager.Init();

        _coreViewModel = new CoreViewModel(this, GetImageModel.GetImageModelAsync);
        DataContext = _coreViewModel;

        _app = new AppLifecycle(_coreViewModel, new MacWindowProvider());
        foreach (var path in launchFilesBeforeStart)
        {
            _ = _app.OpenLaunchFileAsync(path);
        }

        _ = _app.StartAsync(settingsExists, Environment.GetCommandLineArgs(), launchFileMayFollow: true);
    }

   #region Interface implementations

   public async Task<bool> CutFile(string filePath)
   {
       // Cut isn't a supported operation on macOS
       await _coreViewModel.MainWindows.ActiveWindow.CurrentValue.Mapper.CopyFile();
       return true;
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
                DataContext = _coreViewModel?.MainWindows.ActiveWindow.CurrentValue
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

    public bool PrintWithoutPreview(string path) => false;

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

    public byte[]? GetShellThumbnail(string path, int width, int height, out int pixelWidth, out int pixelHeight)
    {
        return ShellThumbnailNative.GetShellThumbnailBytes(path, width, height, out pixelWidth, out pixelHeight);
    }
    
    public byte[]? GetPixelsFromNativeImagingComponent(string path, out int pixelWidth, out int pixelHeight)
    {
        return MacOSImage.GetPixelsFromNativeImagingComponent(path, out pixelWidth, out pixelHeight);
    }
    
    #endregion
}