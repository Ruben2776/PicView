using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.Sizing;

[Collection("Sequential")]
public class PixelSizesTests
{
    private const uint Owner = 1;

    private readonly TabViewModel _tab;

    public PixelSizesTests()
    {
        ObservableSystem.DefaultFrameProvider = new FakeFrameProvider();
        SetDefaults();
        _tab = new TabViewModel(_ => { }, null!);
    }

    [Fact]
    public void Of_ModelWithItsSize_IsThatSize()
    {
        _tab.Display(Model("a.jpg", 400, 300));

        Assert.Equal(new PixelSizes(400, 300), PixelSizes.Of(_tab, null, sideBySide: false));
    }

    [Fact]
    public void Of_SizeNotKnownYet_WaitsInsteadOfGuessing()
    {
        _tab.Display(Model("a.jpg", 0, 0));

        Assert.Null(PixelSizes.Of(_tab, null, sideBySide: false));
    }

    [Fact]
    public async Task Of_SizeOnlyInTheCache_TakesItFromTheCache()
    {
        var file = new FileInfo("a.jpg");
        var cache = new SharedImageCache(f => ValueTask.FromResult(Model(f.Name, 400, 300)));
        cache.SetFiles(Owner, [file]);
        await cache.GetAsync(Owner, file, TestContext.Current.CancellationToken);
        _tab.Display(new ImageModel { FileInfo = file });

        Assert.Equal(new PixelSizes(400, 300), PixelSizes.Of(_tab, cache, sideBySide: false));
    }

    [Fact]
    public void Of_SingleImage_IsItsOwnSize()
    {
        _tab.DisplaySingleImage(new ImageModel { Image = new object(), PixelWidth = 640, PixelHeight = 480 },
            SingleImageType.Clipboard);

        Assert.Equal(new PixelSizes(640, 480), PixelSizes.Of(_tab, null, sideBySide: true));
    }

    [Fact]
    public void Of_SideBySidePair_HasBothSizes()
    {
        _tab.Display(Model("a.jpg", 400, 300), Model("b.jpg", 200, 100));

        Assert.Equal(new PixelSizes(400, 300, 200, 100), PixelSizes.Of(_tab, null, sideBySide: true));
    }

    [Fact]
    public void Of_SideBySideOff_IgnoresTheSecondImage()
    {
        _tab.Display(Model("a.jpg", 400, 300), Model("b.jpg", 200, 100));

        Assert.Equal(new PixelSizes(400, 300), PixelSizes.Of(_tab, null, sideBySide: false));
    }

    [Fact]
    public void Of_PairWhoseSecondSizeIsNotKnownYet_WaitsInsteadOfFittingOneImage()
    {
        _tab.Display(Model("a.jpg", 400, 300), Model("b.jpg", 0, 0));

        Assert.Null(PixelSizes.Of(_tab, null, sideBySide: true));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(90, 1)]
    [InlineData(180, 2)]
    [InlineData(270, 3)]
    public void Of_RotatedTab_HasItsQuarterTurns(int angle, int quarterTurns)
    {
        _tab.Display(Model("a.jpg", 400, 300));
        _tab.Orientation.TurnTo(angle / 90);

        Assert.Equal(quarterTurns, PixelSizes.Of(_tab, null, sideBySide: false)?.QuarterTurns);
    }

    private static ImageModel Model(string name, uint width, uint height) =>
        new() { FileInfo = new FileInfo(name), PixelWidth = width, PixelHeight = height };
}
