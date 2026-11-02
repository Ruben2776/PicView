using PicView.Core.ImageDecoding;
using PicView.Core.Models;
using PicView.Core.Navigation.Tiff;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.Navigation;

/// <summary>
/// The tab is the one writer of what it shows: it publishes one Shown change, and the narrow methods beside Display
/// change the shown state without recording or firing it.
/// </summary>
[Collection("Sequential")]
public class TabShownStateTests
{
    private readonly TabViewModel _tab;

    public TabShownStateTests()
    {
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
        SetDefaults();
        _tab = new TabViewModel(null!);
    }

    [Fact]
    public void Display_FiresShownImageOnce_AfterBindablesAndRecording()
    {
        var model = Model("a.jpg");
        var changes = new List<ShownChange>();
        object? imageWhenFired = null;
        var entriesWhenFired = -1;
        using var _ = _tab.Shown.Subscribe(change =>
        {
            changes.Add(change);
            imageWhenFired = _tab.Image.Value;
            entriesWhenFired = _tab.ViewHistory.Entries.Count();
        });

        _tab.Display(model);

        Assert.Equal([ShownChange.Image], changes);
        Assert.Same(model.Image, imageWhenFired);
        Assert.Equal(1, entriesWhenFired);
    }

    [Fact]
    public void DisplaySingleImage_FiresShownImageOnce()
    {
        var changes = new List<ShownChange>();
        using var _ = _tab.Shown.Subscribe(changes.Add);

        _tab.DisplaySingleImage(new ImageModel { Image = new object() }, SingleImageType.Clipboard);

        Assert.Equal([ShownChange.Image], changes);
    }

    [Fact]
    public void ShowPage_OnMultiPageTiff_FiresShownImageOnce()
    {
        var pages = new[] { new object(), new object() };
        var model = Model("a.tif");
        model.Image = pages[0];
        model.TiffNavigation = new TiffNavigationInfo { PageCount = 2, CurrentPage = 0, Pages = pages };
        _tab.Display(model);
        var changes = new List<ShownChange>();
        using var _ = _tab.Shown.Subscribe(changes.Add);

        _tab.ShowPage(1);

        Assert.Equal([ShownChange.Image], changes);
        Assert.Same(pages[1], _tab.Image.Value);
    }

    [Fact]
    public void ShowPage_WithoutTiff_FiresNothing()
    {
        _tab.Display(Model("a.jpg"));
        var changes = new List<ShownChange>();
        using var _ = _tab.Shown.Subscribe(changes.Add);

        _tab.ShowPage(1);

        Assert.Empty(changes);
    }

    private static ImageModel Model(string name, ImageType type = ImageType.Bitmap) => new()
    {
        FileInfo = new FileInfo(name),
        Image = new object(),
        ImageType = type
    };

    private sealed class MockFrameProvider : FrameProvider
    {
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) => callback.MoveNext(0);
    }
}
