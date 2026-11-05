using PicView.Core.Sizing;
using PicView.Core.ViewModels;

namespace PicView.Tests.Crop;

public class CropViewModelTests
{
    [Fact]
    public void AspectRatio_IsTheTabsFitScale()
    {
        using var zoom = new TabZoom();
        using var crop = new CropViewModel(zoom);

        zoom.SetFitScale(0.5);
        Assert.Equal(0.5, crop.AspectRatio);

        zoom.SetFitScale(2);
        Assert.Equal(2, crop.AspectRatio);
    }
}
