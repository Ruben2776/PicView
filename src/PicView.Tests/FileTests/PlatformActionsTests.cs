using PicView.Avalonia.FileSystem;
using PicView.Core.FileSorting;
using PicView.Core.IPlatform;
using PicView.Core.ViewModels;

namespace PicView.Tests.FileTests;

public class PlatformActionsTests
{
    private readonly RecordingPlatform _platform = new();
    private readonly List<string> _reports = [];
    private readonly TabViewModel _tab;

    public PlatformActionsTests()
    {
        SetDefaults();
        _tab = new TabViewModel(new MainWindowViewModel(null!, null!, null!, null!));
    }

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

    [Fact]
    public async Task SetExportedImage_ConvertedFile_ReachesThePlatformAsTheExportedFile()
    {
        _platform.LockScreenResult = true;

        await PlatformActions.SetExportedImage(_tab, _platform, () => new ValueTask<string?>("C:\\temp\\a.png"),
            LockScreen, _reports.Add);

        Assert.Equal(["SetAsLockScreen C:\\temp\\a.png"], _platform.Calls);
        Assert.Empty(_reports);
    }

    [Fact]
    public async Task SetExportedImage_Wallpaper_PassesTheStyle()
    {
        await PlatformActions.SetExportedImage(_tab, _platform, () => new ValueTask<string?>("C:\\a.jpg"),
            PlatformActions.Wallpaper(WallpaperStyle.Fit), _reports.Add);

        Assert.Equal(["SetAsWallpaper C:\\a.jpg Fit"], _platform.Calls);
        Assert.Empty(_reports);
    }

    [Fact]
    public async Task SetExportedImage_PlatformRefuses_ReportsOnce()
    {
        _platform.LockScreenResult = false;

        await PlatformActions.SetExportedImage(_tab, _platform, () => new ValueTask<string?>("C:\\a.jpg"),
            LockScreen, _reports.Add);

        Assert.Single(_reports);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task SetExportedImage_ExportYieldsNoFile_ReportsWithoutAskingThePlatform(string? exported)
    {
        await PlatformActions.SetExportedImage(_tab, _platform, () => new ValueTask<string?>(exported),
            LockScreen, _reports.Add);

        Assert.Empty(_platform.Calls);
        Assert.Single(_reports);
    }

    [Fact]
    public async Task SetExportedImage_ExportThrows_ReportsTheMessage()
    {
        await PlatformActions.SetExportedImage(_tab, _platform,
            () => throw new InvalidOperationException("cannot read"), LockScreen, _reports.Add);

        Assert.Equal(["cannot read"], _reports);
        Assert.False(_tab.IsBusy.Value);
    }

    [Fact]
    public async Task SetExportedImage_PlatformThrows_ReportsTheMessage()
    {
        _platform.WallpaperError = new NotImplementedException("no wallpaper here");

        await PlatformActions.SetExportedImage(_tab, _platform, () => new ValueTask<string?>("C:\\a.jpg"),
            PlatformActions.Wallpaper(WallpaperStyle.Fill), _reports.Add);

        Assert.Equal(["no wallpaper here"], _reports);
    }

    [Fact]
    public async Task SetExportedImage_NoPlatform_DoesNotExportOrReport()
    {
        var isExported = false;

        await PlatformActions.SetExportedImage(_tab, null, () =>
        {
            isExported = true;
            return new ValueTask<string?>("C:\\a.jpg");
        }, LockScreen, _reports.Add);

        Assert.False(isExported);
        Assert.Empty(_reports);
    }

    [Fact]
    public async Task SetExportedImage_TabIsBusyWhileExportingAndFreeAfter()
    {
        var wasBusy = false;

        await PlatformActions.SetExportedImage(_tab, _platform, () =>
        {
            wasBusy = _tab.IsBusy.Value;
            return new ValueTask<string?>("C:\\a.jpg");
        }, LockScreen, _reports.Add);

        Assert.True(wasBusy);
        Assert.False(_tab.IsBusy.Value);
    }

    [Fact]
    public async Task ExportFor_PathOfTheShownFile_ExportsItAsShown()
    {
        _tab.FileInfo.Value = new FileInfo("C:\\pictures\\a.heic");

        var export = PlatformActions.ExportFor(_tab, "C:\\pictures\\a.heic",
            () => new ValueTask<string?>("shown"), path => new ValueTask<string?>("disk " + path));

        Assert.Equal("shown", await export());
    }

    [Fact]
    public async Task ExportFor_PathOfAnotherFile_ExportsTheFileOnDisk()
    {
        _tab.FileInfo.Value = new FileInfo("C:\\pictures\\a.heic");

        var export = PlatformActions.ExportFor(_tab, "C:\\pictures\\b.heic",
            () => new ValueTask<string?>("shown"), path => new ValueTask<string?>("disk " + path));

        Assert.Equal("disk C:\\pictures\\b.heic", await export());
    }

    [Fact]
    public async Task ExportFor_NoShownFile_ExportsTheFileOnDisk()
    {
        var export = PlatformActions.ExportFor(_tab, "C:\\pictures\\b.heic",
            () => new ValueTask<string?>("shown"), path => new ValueTask<string?>("disk " + path));

        Assert.Equal("disk C:\\pictures\\b.heic", await export());
    }

    private static Task<bool> LockScreen(IPlatformSpecificService platform, string file) =>
        platform.SetAsLockScreen(file);

    private sealed class RecordingPlatform : IPlatformSpecificService
    {
        public List<string> Calls { get; } = [];

        public void OpenWith(string path) => Calls.Add($"OpenWith {path}");

        public void LocateOnDisk(string path) => Calls.Add($"LocateOnDisk {path}");

        public bool LockScreenResult { get; set; }

        public async Task<bool> SetAsLockScreen(string path)
        {
            Calls.Add($"SetAsLockScreen {path}");
            return LockScreenResult;
        }

        public Exception? WallpaperError { get; set; }

        public Task SetAsWallpaper(string path, WallpaperStyle style)
        {
            Calls.Add($"SetAsWallpaper {path} {style}");
            return WallpaperError is null ? Task.CompletedTask : Task.FromException(WallpaperError);
        }

        public void SetTaskbarProgress(ulong progress, ulong maximum) { }
        public void StopTaskbarProgress() { }
        public void SetCursorPos(int x, int y) { }
        public List<FileInfo> GetFiles(FileInfo fileInfo, FileOrder order) => [];
        public int CompareStrings(string str1, string str2) => string.CompareOrdinal(str1, str2);
        public void ShowFileProperties(string path) { }
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
