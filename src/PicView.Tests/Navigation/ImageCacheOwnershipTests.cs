using ImageMagick;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.Navigation;

/// <summary>
/// Pins how tabs own images in the shared image cache, observed through <see cref="TabOverviewViewModel"/> so the
/// tests survive changes to the cache's own interface.
/// </summary>
[Collection("Sequential")]
public class ImageCacheOwnershipTests
{
    private static readonly string TestDirectory = Path.Combine(Path.GetTempPath(), "PicViewCacheOwnershipTests");
    private readonly FileInfo _fileA = new(Path.Combine(TestDirectory, "a.jpg"));
    private readonly FileInfo _fileB = new(Path.Combine(TestDirectory, "b.jpg"));

    private readonly SharedImageCache _cache;
    private readonly TabOverviewViewModel _tabs;

    public ImageCacheOwnershipTests()
    {
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
        SetDefaults();
        TranslationManager.Init();

        var core = new CoreViewModel(null, LoadProbe);
        core.InitializeNavigation(null!, new MockThumbnailLoader(), null!);
        _cache = core.SharedCache;
        _tabs = new MainWindowViewModel(core, core.Translation, null!, core.GlobalSettings!, core.GallerySettings)
            .WindowTabs;
        _tabs.InitializeActiveTab([]);
    }

    [Fact]
    public async Task CloseTab_TabHeldCachedImage_ImageIsDisposed()
    {
        var tab = _tabs.CreateTab(_fileA);
        tab.ImageIterator.Initialize([_fileA]);
        var image = Assert.IsType<ProbeImage>(await Task.Run(() => _tabs.GetSourceFromFile(_fileA), TestContext.Current.CancellationToken));

        _tabs.CloseTab(tab);
        _cache.ForceDisposalQueue();

        Assert.True(image.IsDisposed, "Closing the only tab that referenced the image should release it for disposal");
    }

    [Fact]
    public async Task TearDown_TabHeldCachedImage_ImageIsDisposed()
    {
        var tab = _tabs.CreateTab(_fileA);
        tab.ImageIterator.Initialize([_fileA]);
        var image = Assert.IsType<ProbeImage>(await Task.Run(() => _tabs.GetSourceFromFile(_fileA), TestContext.Current.CancellationToken));

        _tabs.TearDown();
        _cache.ForceDisposalQueue();

        Assert.True(image.IsDisposed, "Closing a Main Window should release the images its tabs held");
    }

    [Fact]
    public async Task GetSourceFromFile_FileNotCached_ReturnsThatFile()
    {
        var tab = _tabs.ActiveTab.Value;
        tab.ImageIterator.Initialize([_fileA, _fileB]);

        var image = Assert.IsType<ProbeImage>(await Task.Run(() => _tabs.GetSourceFromFile(_fileB), TestContext.Current.CancellationToken));

        Assert.Equal(_fileB.FullName, image.File.FullName);
    }

    private static ValueTask<ImageModel> LoadProbe(FileInfo file) =>
        ValueTask.FromResult(new ImageModel { FileInfo = file, Image = new ProbeImage(file) });

    private sealed class ProbeImage(FileInfo file) : IDisposable
    {
        public FileInfo File { get; } = file;
        public bool IsDisposed { get; private set; }
        public void Dispose() => IsDisposed = true;
    }

    private sealed class MockThumbnailLoader : IThumbnailLoader
    {
        public ValueTask<object?> GetThumbnailAsync(FileInfo file, CoreViewModel core) => ValueTask.FromResult<object?>(null);
        public ValueTask<object?> GetThumbnailAsync(FileInfo file, uint size, CoreViewModel? core = null, MagickImage? magick = null) => ValueTask.FromResult<object?>(null);
        public object? GetExifThumbnail(FileInfo file) => null;
        public object? GetThumbQuick(FileInfo file, CoreViewModel? core = null) => null;
    }

    private sealed class MockFrameProvider : FrameProvider
    {
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) => callback.MoveNext(0);
    }
}
