using Avalonia.Media.Imaging;

namespace PicView.Avalonia.Interfaces;

public interface IPlatformSpecificService
{
    void SetTaskbarProgress(ulong progress, ulong maximum);
    void StopTaskbarProgress();
    void SetCursorPos(int x, int y);
    
    void DisableScreensaver();
    void EnableScreensaver();

    List<string> GetFiles(FileInfo fileInfo);

    int CompareStrings(string str1, string str2);

    void OpenWith(string path);

    void LocateOnDisk(string path);
    
    void ShowFileProperties(string path);

    void ShowAboutWindow();

    void ShowExifWindow();

    void ShowKeybindingsWindow();

    void ShowSettingsWindow();
    
    void ShowEffectsWindow();
    
    void ShowSingleImageResizeWindow();
    
    void ShowBatchResizeWindow();
    
    void Print(string path);
    
    void SetAsWallpaper(string path, int wallpaperStyle);
    
    bool SetAsLockScreen(string path);
    
    bool CopyFile(string path);
    
    bool CutFile(string path);
    
    Task CopyImageToClipboard(Bitmap bitmap);
    
    Task<Bitmap?> GetImageFromClipboard();
    
    Task<bool> ExtractWithLocalSoftwareAsync(string path, string tempDirectory);

    string DefaultJsonKeyMap();
}