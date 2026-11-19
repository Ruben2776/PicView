using PicView.Core.IPlatform;

namespace PicView.Tests.Navigation;

/// <summary>
/// Counts the screensaver calls; does nothing natively.
/// </summary>
internal sealed class CountingPlatform : IPlatformSpecificService
{
    public int Disabled { get; private set; }
    public int Enabled { get; private set; }

    public void DisableScreensaver() => Disabled++;
    public void EnableScreensaver() => Enabled++;

    public void SetTaskbarProgress(ulong progress, ulong maximum) { }
    public void StopTaskbarProgress() { }
    public void SetCursorPos(int x, int y) { }
    public List<FileInfo> GetFiles(FileInfo fileInfo, PicView.Core.FileSorting.FileOrder order) => [];
    public int CompareStrings(string str1, string str2) => string.CompareOrdinal(str1, str2);
    public void OpenWith(string path) { }
    public void LocateOnDisk(string path) { }
    public void ShowFileProperties(string path) { }
    public Task SetAsWallpaper(string path, int wallpaperStyle) => Task.CompletedTask;
    public bool SetAsLockScreen(string path) => false;
    public Task<bool> ExtractWithLocalSoftwareAsync(string path, string tempDirectory) => Task.FromResult(false);
    public string DefaultJsonKeyMap() => "{}";
    public void InitiateFileAssociationService() { }
    public Task<bool> DeleteFile(string path, bool recycle) => Task.FromResult(false);
    public bool CanMoveToRecycleBin(string path) => false;
    public Task<bool> CutFile(string path) => Task.FromResult(false);

    public byte[]? GetShellThumbnail(string path, int width, int height, out int pixelWidth, out int pixelHeight)
    {
        pixelWidth = pixelHeight = 0;
        return null;
    }

    public byte[]? GetPixelsFromNativeImagingComponent(string path, out int pixelWidth, out int pixelHeight)
    {
        pixelWidth = pixelHeight = 0;
        return null;
    }
}
