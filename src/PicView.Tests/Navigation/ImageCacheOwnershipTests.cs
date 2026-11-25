using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.Navigation;
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
        core.InitializeNavigation(
            TestTabServices.For(core.SharedCache, null),
            navigation: null!);
        _cache = core.SharedCache;
        _tabs = new MainWindowViewModel(core, core.Translation, core.GlobalSettings!, core.GallerySettings)
            .WindowTabs;
        _tabs.ActiveTab.Value.Initialize();
    }

    [Fact]
    public async Task CloseTab_TabHeldCachedImage_ImageIsDisposed()
    {
        var tab = _tabs.CreateTab();
        tab.Initialize();
        tab.ImageIterator.Initialize([_fileA]);
        var image = Assert.IsType<ProbeImage>(await Task.Run(() => _tabs.GetSourceFromFile(_fileA), TestContext.Current.CancellationToken));

        _tabs.CloseTab(tab);
        _cache.ForceDisposalQueue();

        Assert.True(image.IsDisposed, "Closing the only tab that referenced the image should release it for disposal");
    }

    [Fact]
    public async Task TearDown_TabHeldCachedImage_ImageIsDisposed()
    {
        var tab = _tabs.CreateTab();
        tab.Initialize();
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

    [Fact]
    public async Task ShownImage_OwnerNoLongerReferencesIt_IsNotDisposed()
    {
        var tab = _tabs.ActiveTab.Value;
        var image = await ShowAsync(tab, 0);

        // As when the cache evicts the shown image from the tab's preload window, or a jump releases the window while
        // the next image loads. Losing focus then disposes every image nothing references
        _cache.ReleaseImages(tab.Id);
        _cache.ForceDisposalQueue();

        Assert.False(image.IsDisposed, "The image a tab shows must stay alive whatever its preload window holds");
    }

    [Fact]
    public async Task ShownImage_AnotherImageShownInItsPlace_IsReleased()
    {
        var tab = _tabs.ActiveTab.Value;
        var image = await ShowAsync(tab, 0);

        // Far enough that the preload around it does not reach the first image again
        await ShowAsync(tab, ManyFiles.Length / 2);
        _cache.ReleaseImages(tab.Id);
        _cache.ForceDisposalQueue();

        Assert.True(image.IsDisposed, "An image the tab no longer shows should not be kept alive by it");
    }

    [Fact]
    public async Task CloseTab_TabShowsCachedImage_ImageIsDisposed()
    {
        var tab = _tabs.CreateTab();
        tab.Initialize();
        var image = await ShowAsync(tab, 0);

        _tabs.CloseTab(tab);
        _cache.ForceDisposalQueue();

        Assert.True(image.IsDisposed, "Closing the tab that showed the image should release it for disposal");
    }

    // More files than the preload reaches around an index, so a preload never wraps round to the shown image
    private static readonly FileInfo[] ManyFiles = Enumerable.Range(0, 40)
        .Select(i => new FileInfo(Path.Combine(TestDirectory, $"{i:00}.jpg")))
        .ToArray();

    private static async Task<ProbeImage> ShowAsync(TabViewModel tab, int index)
    {
        if (tab.ImageIterator!.Files.Count is 0)
        {
            tab.ImageIterator.Initialize(ManyFiles);
        }

        await tab.ImageIterator.IterateToIndexAsync(index, new CancellationTokenSource());
        Assert.Equal(ManyFiles[index].FullName, tab.Model.FileInfo!.FullName);
        return Assert.IsType<ProbeImage>(tab.Model.Image);
    }

    private static ImageModel LoadProbe(FileInfo file) =>
        new ImageModel { FileInfo = file, Image = new ProbeImage(file) };

    private sealed class ProbeImage(FileInfo file) : IDisposable
    {
        public FileInfo File { get; } = file;
        public bool IsDisposed { get; private set; }
        public void Dispose() => IsDisposed = true;
    }


    private sealed class MockFrameProvider : FrameProvider
    {
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) => callback.MoveNext(0);
    }
}
