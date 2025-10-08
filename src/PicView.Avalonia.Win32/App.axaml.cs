using System.Runtime;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Clowd.Clipboard;
using PicView.Avalonia.ColorManagement;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.StartUp;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Win32.Views;
using PicView.Avalonia.Win32.WindowImpl;
using PicView.Core.DebugTools;
using PicView.Core.FileAssociations;
using PicView.Core.FileSorting;
using PicView.Core.Localization;
using PicView.Core.ProcessHandling;
using PicView.Core.WindowsNT;
using PicView.Core.WindowsNT.FileAssociation;
using PicView.Core.WindowsNT.FileHandling;
using PicView.Core.WindowsNT.Taskbar;
using PicView.Core.WindowsNT.Wallpaper;
using Dispatcher = Avalonia.Threading.Dispatcher;
using Win32Clipboard = PicView.Core.WindowsNT.Copy.Win32Clipboard;

namespace PicView.Avalonia.Win32;

public class App : Application, IPlatformSpecificService, IPlatformWindowService
{
    private static WinMainWindow? _mainWindow;
    private static WindowInitializer? _windowInitializer;
    private TaskbarProgress? _taskbarProgress;
    private MainViewModel? _vm;
     
    public override void Initialize()
    {
#if DEBUG
        ProfileOptimization.SetProfileRoot(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/"));
        ProfileOptimization.StartProfile("ProfileOptimization");
#endif
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

            var settingsExists= await LoadSettingsAsync().ConfigureAwait(false);

            TranslationManager.Init();
            _vm = new MainViewModel(this, this)
            {
                MainWindow =
                {
                    TopTitlebarViewModel = new TopTitlebarViewModel()
                }
            };

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ThemeManager.DetermineTheme(Current, settingsExists);
                
                _mainWindow = new WinMainWindow();
                desktop.MainWindow = _mainWindow;
                _mainWindow.DataContext = _vm;
                StartUpHelper.StartWithArguments(_vm, settingsExists, desktop, _mainWindow);
                _windowInitializer = new WindowInitializer();
            }, DispatcherPriority.Send);
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(App), nameof(OnFrameworkInitializationCompleted), e);
        }
    }

    public int CombinedTitleButtonsWidth
    {
        get => (int)(Settings.WindowProperties.Maximized && !Settings.WindowProperties.Fullscreen
            ? _mainWindow?.OffScreenMargin.Left + _mainWindow?.OffScreenMargin.Right + field ?? field
            : field);
        set;
    } = 185;

    #region Interface Implementations
    
    public Task<bool> DeleteFile(string path, bool recycle) =>
        Task.Run(() => WinFileHelper.DeleteFile(path, recycle));

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

    public void Print(string path)
    {
        if (Settings.UIProperties.ShowPrintPreview)
            _windowInitializer?.ShowPrintPreviewWindow(_vm);
        else
            ProcessHelper.Print(path);
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

    public bool CopyFile(string path)
    {
        return Win32Clipboard.CopyFileToClipboard(false, path);
    }

    public bool CutFile(string path)
    {
        return Win32Clipboard.CopyFileToClipboard(true, path);
    }

    public async Task CopyImageToClipboard(Bitmap bitmap)
    {
        await ClipboardAvalonia.SetImageAsync(bitmap).ConfigureAwait(false);
    }

    public async Task<Bitmap?> GetImageFromClipboard()
    {
        return await ClipboardAvalonia.GetImageAsync().ConfigureAwait(false);
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

    #endregion

    #region Window interface implementations
    
    public void ShowAboutWindow() =>
        _windowInitializer?.ShowAboutWindow(_vm);

    public async Task ShowImageInfoWindow() =>
        await _windowInitializer?.ShowImageInfoWindow(_vm);

    public void ShowKeybindingsWindow() =>
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
        await Win32Window.Maximize(_mainWindow, _vm, saveSetting);
    
    /// <inheritdoc />
    public async Task MaximizeRestore(bool saveSetting = true) =>
        await Win32Window.ToggleMaximize(_mainWindow, _vm, saveSetting);

    /// <inheritdoc />
    public async Task Fullscreen(bool saveSetting = true) =>
        await Win32Window.Fullscreen(_mainWindow, _vm, saveSetting);
    
    /// <inheritdoc />
    public async Task ToggleFullscreen(bool saveSetting = true) =>
        await Win32Window.ToggleFullscreen(_mainWindow, _vm, saveSetting);
    
    /// <inheritdoc />
    public async Task Restore() =>
        await Win32Window.Restore(_mainWindow, _vm);

    #endregion

}