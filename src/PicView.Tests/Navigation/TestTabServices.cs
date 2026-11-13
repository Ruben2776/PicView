using PicView.Core.FileSorting;
using PicView.Core.IPlatform;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.Thumbnailing;
using PicView.Tests.Thumbnailing;
using PicView.Core.ViewModels;

namespace PicView.Tests.Navigation;

/// <summary>
/// The services a test tab is created with, so its Load and its iterator work: the suite's own fakes, and small no-op
/// ones for the services the suite leaves alone.
/// </summary>
public static class TestTabServices
{
    public static TabServices For(SharedImageCache cache, Thumbnails? thumbnails = null,
        IFileWatcherService? watcher = null, IImageModelLoader? imageLoader = null,
        IPlatformSpecificService? platform = null, GlobalSettingsViewModel? globalSettings = null) =>
        new(cache, thumbnails ?? NoThumbnails(cache), watcher ?? new NoFileWatcher(),
            imageLoader ?? new NoImageModelLoader(), platform ?? new OrdinalPlatform(),
            globalSettings ?? new GlobalSettingsViewModel(static () => ValueTask.FromResult(true)));

    /// <summary>
    /// Thumbnails that are never given, for a suite whose tabs need them but show no gallery.
    /// </summary>
    public static Thumbnails NoThumbnails(SharedImageCache cache) =>
        new(new FakeThumbnailDecoder { HasReadyMade = false, CanDecode = false, CanScaleDown = false }, cache);

    // Watches nothing
    private sealed class NoFileWatcher : IFileWatcherService
    {
        public void Watch(TabViewModel tab, string? directory = null) { }
        public void Unwatch(TabViewModel tab) { }
        public void RestartWatches() { }
    }

    // Gives a model of the file without decoding it
    private sealed class NoImageModelLoader : IImageModelLoader
    {
        public ValueTask<ImageModel> GetImageModelAsync(FileInfo file, CancellationToken ct) =>
            ValueTask.FromResult(new ImageModel { FileInfo = file });
    }

    // Sorts names ordinally and lists a file's folder; does nothing natively
    private sealed class OrdinalPlatform : IPlatformSpecificService
    {
        public void SetTaskbarProgress(ulong progress, ulong maximum) { }
        public void StopTaskbarProgress() { }
        public void SetCursorPos(int x, int y) { }
        public void DisableScreensaver() { }
        public void EnableScreensaver() { }
        public List<FileInfo> GetFiles(FileInfo fileInfo) => FileListRetriever.RetrieveFiles(fileInfo, CompareStrings);
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
}
