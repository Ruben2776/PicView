using Avalonia.Media.Imaging;

namespace PicView.Avalonia.Interfaces;

public interface IPlatformSpecificService
{
    void SetTaskbarProgress(ulong progress, ulong maximum);
    void StopTaskbarProgress();
    void SetCursorPos(int x, int y);
    
    void DisableScreensaver();
    void EnableScreensaver();

    List<FileInfo> GetFiles(FileInfo fileInfo);

    int CompareStrings(string str1, string str2);

    void OpenWith(string path);

    void LocateOnDisk(string path);
    
    void ShowFileProperties(string path);
    
    void Print(string path);
    
    Task SetAsWallpaper(string path, int wallpaperStyle);
    
    bool SetAsLockScreen(string path);
    
    bool CopyFile(string path);
    
    bool CutFile(string path);
    
    Task CopyImageToClipboard(Bitmap bitmap);
    
    Task<Bitmap?> GetImageFromClipboard();
    
    Task<bool> ExtractWithLocalSoftwareAsync(string path, string tempDirectory);

    string DefaultJsonKeyMap();

    void InitiateFileAssociationService();
    
    Task<bool> DeleteFile(string path, bool recycle);
    Task<bool> SaveFile(string path);
}