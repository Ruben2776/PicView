namespace PicView.Core.IPlatform;

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
    
    ValueTask Print(string path);
    
    Task SetAsWallpaper(string path, int wallpaperStyle);
    
    bool SetAsLockScreen(string path);
    
    Task<bool> ExtractWithLocalSoftwareAsync(string path, string tempDirectory);

    string DefaultJsonKeyMap();

    void InitiateFileAssociationService();
    
    Task<bool> DeleteFile(string path, bool recycle);

    /// <summary>
    /// Gets an OS-level thumbnail as raw BGRA pixel data.
    /// Implemented on Windows and macOS, returns null on unsupported platforms or on failure.
    /// </summary>
    byte[]? GetShellThumbnail(string path, int width, int height, out int pixelWidth, out int pixelHeight);
}