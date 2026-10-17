using PicView.Core.ImageDecoding;
using PicView.Core.Models;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.Navigation;

[Collection("Sequential")]
public class TabDisplayTests
{
    private readonly TabViewModel _tab;

    public TabDisplayTests()
    {
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
        SetDefaults();
        _tab = new TabViewModel(_ => { }, null!);
    }

    [Fact]
    public void Display_Primary_SetsModelAndItsBindables()
    {
        var primary = Model("a.jpg", ImageType.Bitmap);

        _tab.Display(primary);

        Assert.Same(primary, _tab.Model);
        Assert.Same(primary.Image, _tab.Image.Value);
        Assert.Same(primary.FileInfo, _tab.FileInfo.Value);
        Assert.Equal(ImageType.Bitmap, _tab.ImageType.Value);
    }

    [Fact]
    public void Display_WithSecondary_SetsSecondaryAndItsBindables()
    {
        var secondary = Model("b.jpg", ImageType.Svg);

        _tab.Display(Model("a.jpg"), secondary);

        Assert.Same(secondary, _tab.SecondaryModel);
        Assert.Same(secondary.Image, _tab.SecondaryImage.Value);
        Assert.Same(secondary.FileInfo, _tab.SecondaryFileInfo.Value);
        Assert.Equal(ImageType.Svg, _tab.SecondaryImageType.Value);
    }

    [Fact]
    public void Display_WithoutSecondary_ClearsPreviousSecondary()
    {
        _tab.Display(Model("a.jpg"), Model("b.jpg"));

        _tab.Display(Model("c.jpg"));

        Assert.Null(_tab.SecondaryModel);
        Assert.Null(_tab.SecondaryImage.Value);
        Assert.Null(_tab.SecondaryFileInfo.Value);
        Assert.Null(_tab.SecondaryImageType.Value);
    }

    [Fact]
    public void Display_WhenPrimaryChanges_SecondaryIsAlreadyInPlace()
    {
        var secondary = Model("b.jpg");
        ImageModel? secondaryWhenPrimaryChanged = null;
        object? secondaryImageWhenPrimaryChanged = null;
        using var subscription = _tab.Image.Skip(1).Subscribe(_ =>
        {
            secondaryWhenPrimaryChanged = _tab.SecondaryModel;
            secondaryImageWhenPrimaryChanged = _tab.SecondaryImage.Value;
        });

        _tab.Display(Model("a.jpg"), secondary);

        Assert.Same(secondary, secondaryWhenPrimaryChanged);
        Assert.Same(secondary.Image, secondaryImageWhenPrimaryChanged);
    }

    [Fact]
    public void Display_AfterSingleImage_EndsSingleImage()
    {
        _tab.DisplaySingleImage(new ImageModel { Image = new object() }, SingleImageType.Clipboard);

        _tab.Display(Model("a.jpg"));

        Assert.Equal(SingleImageType.None, _tab.SingleImageType);
    }

    [Fact]
    public void DisplaySingleImage_AfterFile_ShowsOwnModelWithoutFile()
    {
        var file = Model("a.jpg");
        file.PixelWidth = 100;
        _tab.Display(file);
        var single = new ImageModel { Image = new object(), PixelWidth = 8 };

        _tab.DisplaySingleImage(single, SingleImageType.Clipboard);

        Assert.Same(single, _tab.Model);
        Assert.Null(_tab.FileInfo.Value);
        Assert.Equal(SingleImageType.Clipboard, _tab.SingleImageType);
        Assert.Equal(100u, file.PixelWidth);
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
