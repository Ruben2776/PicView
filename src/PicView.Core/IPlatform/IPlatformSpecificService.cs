using PicView.Core.FileSorting;

namespace PicView.Core.IPlatform;

public interface IPlatformSpecificService
{
    void SetTaskbarProgress(ulong progress, ulong maximum);
    void StopTaskbarProgress();
    void SetCursorPos(int x, int y);

    /// <summary>
    /// The files around <paramref name="fileInfo"/>, in <paramref name="order"/> with this platform's name comparison.
    /// </summary>
    List<FileInfo> GetFiles(FileInfo fileInfo, FileOrder order);

    int CompareStrings(string str1, string str2);

    /// <summary>
    /// The order this platform's file manager shows a folder in, as Windows Explorer's open windows do. Null on a
    /// platform whose file manager is not asked.
    /// </summary>
    IFolderOrderSource? FolderOrders => null;

    /// <summary>
    /// Reads the date this platform's file manager shows for a file, as Windows Explorer's Date column. Null on a
    /// platform where the file system's dates are all there is.
    /// </summary>
    IFileDateSource? FileDates => null;

    void OpenWith(string path);

    void LocateOnDisk(string path);
    
    void ShowFileProperties(string path);
    
    Task SetAsWallpaper(string path, WallpaperStyle style);
    
    Task<bool> SetAsLockScreen(string path);
    
    Task<bool> ExtractWithLocalSoftwareAsync(string path, string tempDirectory);

    string DefaultJsonKeyMap();

    void InitiateFileAssociationService();
    
    Task<bool> DeleteFile(string path, bool recycle);

    /// <summary>
    /// Whether <see cref="DeleteFile"/> can move the file to the recycle bin. Drives without one, such as network
    /// shares, get it deleted permanently instead.
    /// </summary>
    bool CanMoveToRecycleBin(string path);

    /// <summary>
    /// Moves a file to the recycle bin without asking or showing an error. When it cannot go there, the user is not
    /// asked whether to delete it permanently. Used for PicView's own copies, which a dialog would only interrupt.
    /// </summary>
    /// <returns>Whether the file is gone.</returns>
    Task<bool> RecycleQuietlyAsync(string path) => DeleteFile(path, recycle: true);

    Task<bool> CutFile(string path);

    /// <summary>
    /// Gets an OS-level thumbnail as raw BGRA pixel data, which the OS makes if it has none yet.
    /// Implemented on Windows and macOS, returns null on unsupported platforms or on failure.
    /// </summary>
    byte[]? GetShellThumbnail(string path, int width, int height, out int pixelWidth, out int pixelHeight);

    /// <summary>
    /// Like <see cref="GetShellThumbnail"/>, for a caller that can decode the file itself: where the OS making a
    /// thumbnail costs more than that decode, only one it has made already, of at least the size asked for, else null.
    /// On Windows, one in its thumbnail cache. Elsewhere the same as <see cref="GetShellThumbnail"/>.
    /// </summary>
    byte[]? GetCachedShellThumbnail(string path, int width, int height, out int pixelWidth, out int pixelHeight) =>
        GetShellThumbnail(path, width, height, out pixelWidth, out pixelHeight);
    
    byte[]? GetPixelsFromNativeImagingComponent(string path, out int pixelWidth, out int pixelHeight);
}