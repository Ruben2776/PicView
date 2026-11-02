using PicView.Avalonia.FileSystem;
using PicView.Core.IPlatform;

namespace PicView.Tests.FileTests;

public class PlatformActionsTests
{
    private readonly RecordingPlatform _platform = new();

    [Fact]
    public async Task OpenWith_Path_PassesPathToPlatform()
    {
        await PlatformActions.OpenWith(_platform, "C:\\a.jpg");

        Assert.Equal(["OpenWith C:\\a.jpg"], _platform.Calls);
    }

    [Fact]
    public async Task OpenWith_NoFileOpen_PassesNullPathThrough()
    {
        await PlatformActions.OpenWith(_platform, null);

        Assert.Equal(["OpenWith "], _platform.Calls);
    }

    [Fact]
    public async Task OpenWith_NoPlatform_DoesNothing()
    {
        await PlatformActions.OpenWith(null, "C:\\a.jpg");

        Assert.Empty(_platform.Calls);
    }

    [Fact]
    public async Task OpenInExplorer_Path_CallsLocateOnDisk()
    {
        await PlatformActions.OpenInExplorer(_platform, "C:\\a.jpg");

        Assert.Equal(["LocateOnDisk C:\\a.jpg"], _platform.Calls);
    }

    [Fact]
    public async Task ShowFile_Path_OpensItWithThePlatform()
    {
        await PlatformActions.ShowFile(_platform, "C:\\settings.json");

        Assert.Equal(["OpenWith C:\\settings.json"], _platform.Calls);
    }

    private sealed class RecordingPlatform : IPlatformSpecificService
    {
        public List<string> Calls { get; } = [];

        public void OpenWith(string path) => Calls.Add($"OpenWith {path}");

        public void LocateOnDisk(string path) => Calls.Add($"LocateOnDisk {path}");

        public bool SetAsLockScreen(string path) => false;
        public void SetTaskbarProgress(ulong progress, ulong maximum) { }
        public void StopTaskbarProgress() { }
        public void SetCursorPos(int x, int y) { }
        public void DisableScreensaver() { }
        public void EnableScreensaver() { }
        public List<FileInfo> GetFiles(FileInfo fileInfo) => [];
        public int CompareStrings(string str1, string str2) => string.CompareOrdinal(str1, str2);
        public void ShowFileProperties(string path) { }
        public Task SetAsWallpaper(string path, int wallpaperStyle) => Task.CompletedTask;
        public Task<bool> ExtractWithLocalSoftwareAsync(string path, string tempDirectory) => Task.FromResult(false);
        public string DefaultJsonKeyMap() => "{}";
        public void InitiateFileAssociationService() { }
        public Task<bool> DeleteFile(string path, bool recycle) => Task.FromResult(false);
        public bool CanMoveToRecycleBin(string path) => true;
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
}
