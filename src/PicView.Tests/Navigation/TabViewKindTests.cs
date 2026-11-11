using ImageMagick;
using PicView.Core.ArchiveHandling;
using PicView.Core.ImageDecoding;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.Navigation;

/// <summary>
/// A tab derives which view it shows from its Crop, its Title Phase and whether it shows anything (ADR-0022).
/// </summary>
[Collection("Sequential")]
public class TabViewKindTests
{
    private readonly TabViewModel _tab;

    public TabViewKindTests()
    {
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
        SetDefaults();
        _tab = new TabViewModel(null!,
            TestTabServices.For(new SharedImageCache(f => ValueTask.FromResult(Model(f.Name))!), new ThumbnailCache(),
                new MockThumbnailLoader()));
    }

    private static ImageModel Model(string name) => new()
    {
        FileInfo = new FileInfo(Path.Combine(Path.GetTempPath(), "PicViewTabViewKindTests", name)),
        Image = new object(),
        PixelWidth = 1,
        PixelHeight = 1
    };

    [Fact]
    public void NewTab_ShowsNothing_IsStartUpMenu()
        => Assert.Equal(TabViewKind.StartUpMenu, _tab.ViewKind.Value);

    [Fact]
    public void SetLoading_OnAFreshTab_IsImageViewer()
    {
        _tab.SetLoading();
        Assert.Equal(TabViewKind.ImageViewer, _tab.ViewKind.Value);
    }

    [Fact]
    public void SetDownloading_OnAFreshTab_IsImageViewer()
    {
        _tab.SetDownloading("1 %");
        Assert.Equal(TabViewKind.ImageViewer, _tab.ViewKind.Value);
    }

    [Fact]
    public void EndLoading_WithItsTicketOnAFreshTab_IsStartUpMenuAgain()
    {
        var ticket = _tab.SetLoading();
        _tab.EndLoading(ticket);
        Assert.Equal(TabViewKind.StartUpMenu, _tab.ViewKind.Value);
    }

    [Fact]
    public void SetError_OnAFreshTab_IsStartUpMenu()
    {
        _tab.SetError("no");
        Assert.Equal(TabViewKind.StartUpMenu, _tab.ViewKind.Value);
    }

    [Fact]
    public void Display_AFile_IsImageViewer()
    {
        _tab.Display(Model("a.jpg"));
        Assert.Equal(TabViewKind.ImageViewer, _tab.ViewKind.Value);
    }

    [Fact]
    public void EndLoading_AfterAFileShows_StaysImageViewer()
    {
        _tab.Display(Model("a.jpg"));
        var ticket = _tab.SetLoading();
        _tab.EndLoading(ticket);
        Assert.Equal(TabViewKind.ImageViewer, _tab.ViewKind.Value);
    }

    [Fact]
    public void DisplaySingleImage_OnAFreshTab_IsImageViewer()
    {
        _tab.DisplaySingleImage(new ImageModel { Image = new object() }, SingleImageType.Clipboard);
        Assert.Equal(TabViewKind.ImageViewer, _tab.ViewKind.Value);
    }

    [Fact]
    public void ShowStandIn_OnAFreshTab_StaysStartUpMenu()
    {
        _tab.ShowStandIn(new object());
        Assert.Equal(TabViewKind.StartUpMenu, _tab.ViewKind.Value);
    }

    [Fact]
    public void Crop_SetAndCleared_IsCropThenImageViewerAgain()
    {
        _tab.Display(Model("a.jpg"));
        _tab.Crop = new CropViewModel(_tab.Zoom, 2, 2);
        Assert.Equal(TabViewKind.Crop, _tab.ViewKind.Value);
        _tab.Crop = null;
        Assert.Equal(TabViewKind.ImageViewer, _tab.ViewKind.Value);
    }

    [Fact]
    public async Task BecomeFreshAsync_AfterAFileShows_IsStartUpMenu()
    {
        _tab.Display(Model("a.jpg"));
        await _tab.BecomeFreshAsync();
        Assert.Equal(TabViewKind.StartUpMenu, _tab.ViewKind.Value);
    }

    [Fact]
    public async Task CloseImageAsync_AfterAFileShows_IsStartUpMenuAndFresh()
    {
        _tab.Display(Model("a.jpg"));
        await _tab.CloseImageAsync();
        Assert.Equal(TabViewKind.StartUpMenu, _tab.ViewKind.Value);
        Assert.Null(_tab.FileInfo.Value);
        Assert.True(_tab.IsFresh);
    }

    [Fact]
    public async Task CloseImageAsync_AfterASingleImageShows_IsStartUpMenu()
    {
        _tab.DisplaySingleImage(new ImageModel { Image = new object() }, SingleImageType.Clipboard);
        await _tab.CloseImageAsync();
        Assert.Equal(TabViewKind.StartUpMenu, _tab.ViewKind.Value);
        Assert.Null(_tab.Image.Value);
    }

    [Fact]
    public async Task CloseImageAsync_OnAFreshTab_NotifiesNothing()
    {
        var changes = 0;
        using var following = _tab.ViewKind.AsObservable().Skip(1).Subscribe(_ => changes++);
        await _tab.CloseImageAsync();
        Assert.Equal(0, changes);
        Assert.Equal(TabViewKind.StartUpMenu, _tab.ViewKind.Value);
    }

    [Fact]
    public async Task CloseImageAsync_WhileCropping_StaysCrop()
    {
        _tab.Display(Model("a.jpg"));
        _tab.Crop = new CropViewModel(_tab.Zoom, 2, 2);
        await _tab.CloseImageAsync();
        Assert.Equal(TabViewKind.Crop, _tab.ViewKind.Value);
        Assert.NotNull(_tab.FileInfo.Value);
    }

    [Fact]
    public void Display_WhileTheViewerShows_NotifiesNothing()
    {
        _tab.Display(Model("a.jpg"));
        var changes = 0;
        using var following = _tab.ViewKind.AsObservable().Skip(1).Subscribe(_ => changes++);
        _tab.Display(Model("b.jpg"));
        Assert.Equal(0, changes);
    }

    private sealed class MockThumbnailLoader : IThumbnailLoader
    {
        public ValueTask<object?> GetThumbnailAsync(FileInfo file, CoreViewModel core) => ValueTask.FromResult<object?>(null);

        public ValueTask<object?> GetThumbnailAsync(FileInfo file, uint size, CoreViewModel? core = null,
            MagickImage? magick = null) => ValueTask.FromResult<object?>(null);

        public object? GetExifThumbnail(FileInfo file) => null;
        public object? GetThumbQuick(FileInfo file, CoreViewModel? core = null) => null;
    }

    private sealed class MockFrameProvider : FrameProvider
    {
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) => callback.MoveNext(0);
    }
}
