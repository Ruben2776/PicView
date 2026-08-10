using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Controls.ApplicationLifetimes;
using PicView.Avalonia.ColorManagement;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.Linux.Views;
using PicView.Avalonia.StartUp;
using PicView.Core.FileSorting;
using PicView.Core.IPlatform;
using PicView.Core.ViewModels;
using MainWindowViewModel = PicView.Core.ViewModels.MainWindowViewModel;

#pragma warning disable CS0618 // Type or member is obsolete

namespace PicView.Avalonia.Linux;


public class App : Application, IPlatformSpecificService
{
    private LinuxMainWindow? _mainWindow;
    private static CoreViewModel? _coreViewModel;
    private static MainWindowViewModel? _mainWindowViewModel;

    public override void Initialize()
    {
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

        _mainWindow = new LinuxMainWindow();
        _mainWindowViewModel = _mainWindow.DataContext as MainWindowViewModel;
        _coreViewModel.MainWindows.MainWindows.Add(_mainWindowViewModel);
        _coreViewModel.MainWindows.ActiveWindow.Value = _mainWindowViewModel;
        StartUpHelper.StartWithArguments(_coreViewModel, settingsExists, desktop, _mainWindow);

        desktop.MainWindow = _mainWindow;
    }

 #region Interface Implementations
    
    public Task<bool> DeleteFile(string path, bool recycle) =>
        throw new NotImplementedException();

    public void SetTaskbarProgress(ulong progress, ulong maximum)
    {
        // TODO
    }

    public void StopTaskbarProgress()
    {
        throw new NotImplementedException();
    }

    public void SetCursorPos(int x, int y)
    {
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }

    public void LocateOnDisk(string path)
    {
        throw new NotImplementedException();
    }

    public void ShowFileProperties(string path)
    {
        throw new NotImplementedException();
    }

    public async ValueTask Print(string path)
    {
        await _mainWindow.ShowPrintWindow(path);   
    }

    public async Task SetAsWallpaper(string path, int wallpaperStyle)
    {
        throw new NotImplementedException();
    }

    public bool SetAsLockScreen(string path)
    {
        return false;
        // return LockscreenHelper.SetLockScreenImage(path);
    }

    public bool CopyFile(string path)
    {
        throw new NotImplementedException();
    }

    public bool CutFile(string path)
    {
        throw new NotImplementedException();
    }

    public async Task CopyImageToClipboard(object bitmap)
    {
        throw new NotImplementedException();
    }

    public async Task<object?> GetImageFromClipboard()
    {
        return false;
    }

    public async Task<bool> ExtractWithLocalSoftwareAsync(string path, string tempDirectory)
    {
        return false;
    }

    public string DefaultJsonKeyMap()
    {
        return Core.Linux.LinuxKeybindings.DefaultKeybindings;
    }

    public void InitiateFileAssociationService()
    {
    }

    public void DisableScreensaver()
    {
    }

    public void EnableScreensaver()
    {
    }

    // TODO: Implement Linux thumbnail extraction (e.g. via freedesktop thumbnail spec)
    public byte[]? GetShellThumbnail(string path, int width, int height, out int pixelWidth, out int pixelHeight)
    {
        pixelWidth = 0;
        pixelHeight = 0;
        return null;
    }

    #endregion
}