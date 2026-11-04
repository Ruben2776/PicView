using BenchmarkDotNet.Attributes;
using ImageMagick;
using PicView.Core.FileSorting;
using PicView.Core.Gallery;
using PicView.Core.IPlatform;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Benchmarks.Gallery;

/// <summary>
/// Where a gallery load spends its allocations: building one item for each file, or the batches that read the files'
/// details afterwards. <see cref="Load"/> runs a whole load with the details reader replaced, so disk reads don't
/// drown the measurement; <see cref="ItemPerFile"/> is building one item and <see cref="TextsPerFile"/> the texts its
/// tooltip adds; <see cref="PingPerFile"/> is one real details read, for scale.
/// </summary>
[MemoryDiagnoser]
public class GalleryLoadBenchmarks
{
    private static readonly GalleryFileDetails Details = new(200, 100, null);

    private static readonly SharedImageCache Cache =
        new(static file => ValueTask.FromResult(new ImageModel { FileInfo = file }));

    private string _directory = string.Empty;
    private FileInfo[] _files = [];
    private FileInfo? _image;
    private TabViewModel? _tab;

    [Params(100, 1000, 5000)]
    public int Files { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        ObservableSystem.DefaultFrameProvider = new NoFrameProvider();
        SetDefaults();
        if (!TranslationManager.LoadLanguage("en"))
        {
            throw new InvalidOperationException("The English language file was not found");
        }

        _directory = Path.Combine(Path.GetTempPath(), "PicViewGalleryBenchmark_" + Guid.NewGuid());
        Directory.CreateDirectory(_directory);
        for (var i = 0; i < Files; i++)
        {
            File.WriteAllBytes(Path.Combine(_directory, $"{i:D5}.jpg"), []);
        }

        // Listed as the app lists a folder, so each file's size and date are read with the listing
        _files = new DirectoryInfo(_directory).GetFiles();
        Array.Sort(_files, static (a, b) => string.CompareOrdinal(a.Name, b.Name));

        var imagePath = Path.Combine(_directory, "image.jpg");
        using (var magick = new MagickImage(MagickColors.Red, 64, 64))
        {
            magick.Write(imagePath, MagickFormat.Jpeg);
        }
        _image = new FileInfo(imagePath);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _tab?.Dispose();
        Directory.Delete(_directory, true);
    }

    [IterationSetup(Target = nameof(Load))]
    public void CreateTab()
    {
        _tab?.Dispose();
        _tab = new TabViewModel(null!, new TabServices(Cache, new NoThumbnailCache(), new NoThumbnailLoader(),
            new NoFileWatcher(), new NoImageModelLoader(), new OrdinalPlatform(),
            new GlobalSettingsViewModel(static () => ValueTask.FromResult(true))))
        {
            Model = new ImageModel { FileInfo = _files[0] }
        };
        _tab.Initialize();
        _tab.GalleryCoordinator!.ReadDetails = static _ => Details;
        _tab.Gallery.Switcher.Start(true);
    }

    /// <summary>
    /// A whole load from the files being listed to the state reaching Loaded: every item built and added, then every
    /// item's details read in batches outward from the middle.
    /// </summary>
    [Benchmark]
    public int Load()
    {
        var tab = _tab!;
        var gallery = tab.Gallery;
        tab.ImageIterator!.Initialize(_files, Files / 2);

        var deadline = Environment.TickCount64 + 30_000;
        while (gallery.LoadingState is not GalleryLoadingState.Loaded)
        {
            if (Environment.TickCount64 > deadline)
            {
                throw new TimeoutException($"The gallery did not load; state {gallery.LoadingState}");
            }
            Thread.Yield();
        }
        return gallery.GalleryItems.Count;
    }

    /// <summary>The file's name, size and date texts, which an item makes the first time its tooltip reads them.</summary>
    [Benchmark]
    public int TextsPerFile()
    {
        var item = new GalleryItemViewModel { FileInfo = _files[0] };
        return item.FileName.Length + item.FileSize.Length + item.FileDate.Length;
    }

    /// <summary>An item as a load builds it: its three bindable properties and no texts yet.</summary>
    [Benchmark]
    public GalleryItemViewModel ItemPerFile() => new() { FileInfo = _files[0] };

    /// <summary>One real details read, of a small JPEG the OS has cached, so a lower bound on a read in the app.</summary>
    [Benchmark]
    public GalleryFileDetails? PingPerFile() => GalleryFileDetails.Read(_image!);

    private sealed class NoFrameProvider : FrameProvider
    {
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) => callback.MoveNext(0);
    }

    private sealed class NoThumbnailCache : IThumbnailCache
    {
        public void Add(uint ownerId, string path, object thumbnail) { }
        public bool TryGet(string path, out object? thumbnail) { thumbnail = null; return false; }
        public void Remove(string path) { }
        public void RemoveOwner(uint ownerId) { }
        public bool IsEmpty => true;
    }

    private sealed class NoThumbnailLoader : IThumbnailLoader
    {
        public ValueTask<object?> GetThumbnailAsync(FileInfo file, uint size, CoreViewModel? core = null,
            MagickImage? magick = null) => ValueTask.FromResult<object?>(null);
        public object? GetExifThumbnail(FileInfo file) => null;
        public object? GetThumbQuick(FileInfo file, CoreViewModel? core = null) => null;
    }

    private sealed class NoFileWatcher : IFileWatcherService
    {
        public void Watch(TabViewModel tab, string? directory = null) { }
        public void Unwatch(TabViewModel tab) { }
        public void RestartWatches() { }
    }

    private sealed class NoImageModelLoader : IImageModelLoader
    {
        public ValueTask<ImageModel> GetImageModelAsync(FileInfo file, CancellationToken ct) =>
            ValueTask.FromResult(new ImageModel { FileInfo = file });
    }

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
