using ImageMagick;
using PicView.Core.IPlatform;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.Navigation;

/// <summary>
/// Pins that the services are composed once, kept on the Core, and handed to every tab when it is created.
/// </summary>
[Collection("Sequential")]
public class TabServicesTests
{
    public TabServicesTests()
    {
        SetDefaults();
        TranslationManager.Init();
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
    }

    [Fact]
    public void CreateTab_WithServicesOnCore_HandsThemToTheTab()
    {
        var core = CreateCore();
        core.InitializeNavigation(ServicesFor(core), navigation: null!);
        var vm = new MainWindowViewModel(core, null!, null!, null!, null!);

        var tab = vm.WindowTabs.CreateTab();

        Assert.NotNull(core.Services);
        Assert.Same(core.Services, tab.Services);
    }

    [Fact]
    public void CreateInitialTab_WithServicesOnCore_HandsThemToTheTab()
    {
        var core = CreateCore();
        core.InitializeNavigation(ServicesFor(core), navigation: null!);
        var vm = new MainWindowViewModel(core, null!, null!, null!, null!);

        Assert.Same(core.Services, vm.WindowTabs.ActiveTab.Value.Services);
    }

    [Fact]
    public void InitializeNavigation_Twice_KeepsTheFirstServices()
    {
        var core = CreateCore();
        var first = ServicesFor(core);
        var firstNavigation = NavigationFor(core, first);

        core.InitializeNavigation(first, firstNavigation);
        var second = ServicesFor(core);
        core.InitializeNavigation(second, NavigationFor(core, second));

        Assert.Same(first, core.Services);
        Assert.Same(firstNavigation, core.SharedNavigationService);
        Assert.Same(first.Watcher, core.SharedFileWatcher);
        Assert.Same(first.ThumbnailLoader, core.SharedThumbnailLoader);
    }

    [Fact]
    public void Services_BeforeInitializeNavigation_AreNull()
    {
        var core = CreateCore();

        Assert.Null(core.Services);
        Assert.Null(core.SharedNavigationService);
        Assert.Null(core.SharedFileWatcher);
        Assert.Null(core.SharedThumbnailLoader);
    }

    private static CoreViewModel CreateCore() =>
        new(new MockPlatformSpecificService(), file => ValueTask.FromResult(new ImageModel { FileInfo = file }));

    private static TabServices ServicesFor(CoreViewModel core) =>
        new(core.SharedCache, core.SharedThumbnailCache, new MockThumbnailLoader(), new MockFileWatcherService(),
            new MockImageModelLoader(), core.PlatformService!);

    private static NavigationService NavigationFor(CoreViewModel core, TabServices services) =>
        new(core.PlatformService!.CompareStrings);

    private sealed class MockFileWatcherService : IFileWatcherService
    {
        public void Watch(TabViewModel tab, string? directory = null) { }
        public void Unwatch(TabViewModel tab) { }
        public void RestartWatches() { }
    }

    private sealed class MockImageModelLoader : IImageModelLoader
    {
        public ValueTask<ImageModel> GetImageModelAsync(FileInfo file, CancellationToken ct)
        {
            return ValueTask.FromResult(new ImageModel { FileInfo = file });
        }

        public ValueTask<ImageModel?> GetBase64ImageAsync(FileInfo file, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public ValueTask<ImageModel?> GetBase64ImageAsync(string base64String, CancellationToken ct)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class MockThumbnailLoader : IThumbnailLoader
    {
        public ValueTask<object?> GetThumbnailAsync(FileInfo file, uint size, CoreViewModel? core = null, MagickImage? magick = null) =>
            ValueTask.FromResult<object?>(null);

        public object? GetExifThumbnail(FileInfo file) => null;
        public object? GetThumbQuick(FileInfo file, CoreViewModel? core = null) => null;
    }

    private sealed class MockPlatformSpecificService : IPlatformSpecificService
    {
        public void SetTaskbarProgress(ulong progress, ulong maximum) { }
        public void StopTaskbarProgress() { }
        public void SetCursorPos(int x, int y) { }
        public void DisableScreensaver() { }
        public void EnableScreensaver() { }
        public List<FileInfo> GetFiles(FileInfo fileInfo) => new();
        public int CompareStrings(string str1, string str2) => string.CompareOrdinal(str1, str2);
        public void OpenWith(string path) { }
        public void LocateOnDisk(string path) { }
        public void ShowFileProperties(string path) { }
        public Task SetAsWallpaper(string path, int wallpaperStyle) => Task.CompletedTask;
        public bool SetAsLockScreen(string path) => false;
        public bool CopyFile(string path) => false;
        public Task<bool> CutFile(string path) => throw new NotImplementedException();
        public Task CopyImageToClipboard(object bitmap) => Task.CompletedTask;
        public Task<object?> GetImageFromClipboard() => Task.FromResult<object?>(null);
        public Task<bool> ExtractWithLocalSoftwareAsync(string path, string tempDirectory) => Task.FromResult(false);
        public string DefaultJsonKeyMap() => "{}";
        public void InitiateFileAssociationService() { }
        public Task<bool> DeleteFile(string path, bool recycle) => Task.FromResult(false);
        public bool CanMoveToRecycleBin(string path) => true;

        public byte[]? GetShellThumbnail(string path, int width, int height, out int pixelWidth, out int pixelHeight)
        {
            pixelWidth = 0;
            pixelHeight = 0;
            return null;
        }

        public byte[]? GetPixelsFromNativeImagingComponent(string path, out int pixelWidth, out int pixelHeight)
        {
            pixelWidth = 0;
            pixelHeight = 0;
            return null;
        }
    }

    private sealed class MockFrameProvider : FrameProvider
    {
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) => callback.MoveNext(0);
    }
}
