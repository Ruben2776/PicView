using PicView.Core.Gallery;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.ViewModels;

namespace PicView.Tests.Gallery;

/// <summary>
/// Pins that crop and a Single Image hide a tab's gallery from the tab's own state, without changing its mode
/// (ADR-0011).
/// </summary>
[Collection("Sequential")]
public class GalleryHidingTests
{
    public GalleryHidingTests()
    {
        SetDefaults();
        TranslationManager.Init();
    }

    [Fact]
    public void SingleImage_WhileExpanded_HidesAndKeepsMode()
    {
        var tab = new TabViewModel(null!);
        tab.Gallery.Switcher.ToggleExpanded();

        tab.SingleImageType = SingleImageType.Clipboard;

        Assert.Equal(GalleryMode.Closed, tab.Gallery.ShownMode.Value);
        Assert.Equal(GalleryMode.Expanded, tab.Gallery.Mode.Value);
    }

    [Fact]
    public void LeavingSingleImage_GivesBackShownMode()
    {
        var tab = new TabViewModel(null!);
        tab.Gallery.Switcher.ToggleExpanded();
        tab.SingleImageType = SingleImageType.Clipboard;

        tab.SingleImageType = SingleImageType.None;

        Assert.Equal(GalleryMode.Expanded, tab.Gallery.ShownMode.Value);
    }

    [Fact]
    public void Crop_WhileDocked_HidesUntilCropIsCleared()
    {
        var tab = new TabViewModel(null!);
        tab.Gallery.Switcher.Start(true);

        tab.Crop = new CropViewModel();
        Assert.Equal(GalleryMode.Closed, tab.Gallery.ShownMode.Value);

        tab.Crop = null;
        Assert.Equal(GalleryMode.Docked, tab.Gallery.ShownMode.Value);
    }
}
