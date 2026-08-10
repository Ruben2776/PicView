using System.Runtime;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using PicView.Avalonia.ColorManagement;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.StartUp;
using PicView.Avalonia.Win32.Views;
using PicView.Core.FileAssociations;
using PicView.Core.FileSorting;
using PicView.Core.IPlatform;
using PicView.Core.ProcessHandling;
using PicView.Core.ViewModels;
using PicView.Core.WindowsNT;
using PicView.Core.WindowsNT.FileAssociation;
using PicView.Core.WindowsNT.FileHandling;
using PicView.Core.WindowsNT.Taskbar;
using PicView.Core.WindowsNT.Wallpaper;

namespace PicView.Avalonia.Win32;

public class App : Application, IPlatformSpecificService
{
    private static WinMainWindow? _mainWindow;
    private static CoreViewModel? _coreViewModel;
    private static MainWindowViewModel? _mainWindowViewModel;
    private TaskbarProgress? _taskbarProgress;
     
    public override void Initialize()
    {
#if DEBUG
        ProfileOptimization.SetProfileRoot(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/"));
        ProfileOptimization.StartProfile("ProfileOptimization");
#endif
        AvaloniaXamlLoader.Load(this);
#if DEBUG
        this.AttachDeveloperTools();
#endif
    }

    public override void OnFrameworkInitializationCompleted()
    {
        base.OnFrameworkInitializationCompleted();

        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }
        var settingsExists = LoadSettings();
        _coreViewModel = new CoreViewModel(this, GetImageModel.GetImageModelAsync);
        DataContext = _coreViewModel;

        ThemeManager.DetermineTheme(Current, settingsExists);

        _mainWindow = new WinMainWindow();
        _mainWindowViewModel = _mainWindow.DataContext as MainWindowViewModel;
        _coreViewModel.MainWindows.MainWindows.Add(_mainWindowViewModel);
        _coreViewModel.MainWindows.ActiveWindow.Value = _mainWindowViewModel;
        StartUpHelper.StartWithArguments(_coreViewModel, settingsExists, desktop, _mainWindow);
    }

    #region Interface Implementations
    
    public Task<bool> DeleteFile(string path, bool recycle) =>
        Task.Run(() => WinFileHelper.DeleteFile(path, recycle));

    public void SetTaskbarProgress(ulong progress, ulong maximum)
    {
        Dispatcher.Invoke(() =>
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
        });
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

    public List<FileInfo> GetFiles(FileInfo fileInfo)
    {
        return FileListRetriever.RetrieveFiles(fileInfo, CompareStrings);
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

    public async ValueTask Print(string path)
    {
        if (Settings.UIProperties.ShowPrintPreview)
        {
            await _mainWindow.ShowPrintWindow(path);
        }
        else
        {
            ProcessHelper.Print(path);
        }
    }

    public async Task SetAsWallpaper(string path, int wallpaperStyle)
    {
        await Task.Run(() =>
        {
            var style = (WallpaperHelper.WallpaperStyle)wallpaperStyle;
            WallpaperHelper.SetDesktopWallpaper(path, style);
        });
    }

    public bool SetAsLockScreen(string path)
    {
        return false;
        // return LockscreenHelper.SetLockScreenImage(path);
    }

    public async Task<bool> ExtractWithLocalSoftwareAsync(string path, string tempDirectory)
    {
        return await ArchiveExtractionHelper.ExtractWithLocalSoftwareAsync(path, tempDirectory);
    }

    public string DefaultJsonKeyMap()
    {
        return WindowsKeybindings.DefaultKeybindings;
    }

    public void InitiateFileAssociationService()
    {
        var iIFileAssociationService = new WindowsFileAssociationService();
        FileAssociationManager.Initialize(iIFileAssociationService);
    }

    public void DisableScreensaver()
    {
        NativeMethods.DisableScreensaver();
    }

    public void EnableScreensaver()
    {
        NativeMethods.EnableScreensaver();
    }

    public byte[]? GetShellThumbnail(string path, int width, int height, out int pixelWidth, out int pixelHeight)
    {
        return ShellThumbnailNative.GetShellThumbnailBytes(path, width, height, out pixelWidth, out pixelHeight);
    }

    #endregion

}