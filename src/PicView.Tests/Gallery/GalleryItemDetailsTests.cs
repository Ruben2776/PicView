using PicView.Core.Extensions;
using PicView.Core.Gallery;
using PicView.Core.ViewModels;

namespace PicView.Tests.Gallery;

public class GalleryItemDetailsTests
{
    [Fact]
    public void SetDetails_NewDetails_ReplacesThemAndTheSizeText()
    {
        var item = new GalleryItemViewModel();
        var details = new GalleryFileDetails(300, 200, null);

        item.SetDetails(details);

        Assert.Same(details, item.Details);
        Assert.Equal(StringExtensions.CombineSize(300, 200), item.ImageSize.Value);
    }
}
