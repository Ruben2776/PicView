using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PicView.Avalonia.Crop;
using PicView.Core.ImageDecoding;
using PicView.Core.ImageTransformations;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;

namespace PicView.Tests.Crop;

public class CropServiceTests
{
    [Theory]
    [InlineData(ImageType.Bitmap, false, true)]
    [InlineData(ImageType.Bitmap, true, false)]
    [InlineData(ImageType.Svg, false, false)]
    [InlineData(ImageType.AnimatedGif, false, false)]
    public void CanCrop_OnlyAStillBitmapNotSideBySide(ImageType type, bool sideBySide, bool expected)
        => Assert.Equal(expected, CropManager.CanCrop(type, sideBySide));

    [Fact]
    public void ShouldResetOrientation_SavedOverTheTabsFile_IsTrue()
    {
        var file = new FileInfo(Path.Combine(Path.GetTempPath(), "tab.jpg"));

        Assert.True(CropService.ShouldResetOrientation(file.FullName, file));
    }

    [Fact]
    public void ShouldResetOrientation_SavedElsewhere_IsFalse()
    {
        var file = new FileInfo(Path.Combine(Path.GetTempPath(), "tab.jpg"));

        Assert.False(CropService.ShouldResetOrientation(Path.Combine(Path.GetTempPath(), "other.jpg"), file));
    }

    [Fact]
    public void ShouldResetOrientation_NoTabFile_IsFalse()
        => Assert.False(CropService.ShouldResetOrientation(Path.Combine(Path.GetTempPath(), "tab.jpg"), null));

    [AvaloniaFact]
    public void RenderSelection_TurnedRight_IsTheSelectionAsShown()
    {
        using var source = new WriteableBitmap(new PixelSize(4, 2), new Vector(96, 96), PixelFormat.Bgra8888,
            AlphaFormat.Premul);
        using var zoom = new TabZoom();
        using var orientation = new TabOrientation(null);
        zoom.SetFitScale(1);
        orientation.TurnRight();
        using var crop = new CropViewModel(zoom, 4, 2, orientation);
        crop.SetSelection(0, 0, 2, 4);

        var result = CropService.RenderSelection(source, crop);

        Assert.Equal(new PixelSize(2, 4), result.PixelSize);
        result.Dispose();
    }
}
