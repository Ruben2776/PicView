using Avalonia.Controls;
using PicView.Avalonia.Input;
using PicView.Avalonia.Views.Gallery;
using PicView.Avalonia.Views.UC;
using PicView.Avalonia.Views.UC.Buttons;
using PicView.Core.Localization;
using PicView.Core.ViewModels;
using static PicView.Tests.UI.TabViewsHarness;

namespace PicView.Tests.UI;

[Collection("Sequential")]
public class NavigableViewTests
{
    public NavigableViewTests()
    {
        SetDefaults();
        TranslationManager.Init();
    }

    [AvaloniaFact]
    public void SetLoading_ShowsANavigableViewHoldingTheImageViewer()
        => WithWindow((_, _, tab) =>
        {
            tab.SetLoading();

            var nav = Assert.IsType<NavigableView>(ViewOf(tab));
            var viewer = Assert.IsType<ImageViewer>(ViewerOf(tab));
            Assert.Same(viewer, nav.Viewer);
            Assert.Same(tab, nav.DataContext);
        });

    [AvaloniaFact]
    public void ImageViewer_HasNoGalleryArrowsOrHoverBar()
        => WithWindow((_, _, tab) =>
        {
            tab.SetLoading();

            var viewer = Assert.IsType<ImageViewer>(ViewerOf(tab));
            Assert.Null(viewer.FindControl<GalleryView>("GalleryView"));
            Assert.Null(viewer.FindControl<ClickArrowLeft>("ClickArrowLeft"));
            Assert.Null(viewer.FindControl<ClickArrowRight>("ClickArrowRight"));
            Assert.Null(viewer.FindControl<HoverBar>("HoverBar"));
            Assert.Null(viewer.FindControl<GalleryShortcut>("GalleryShortcut"));
        });

    [AvaloniaFact]
    public void Crop_SetAndCleared_ReplacesThenShowsTheSameNavigableView()
        => WithWindow((_, _, tab) =>
        {
            tab.Display(BitmapModel("a.jpg"));
            var nav = Assert.IsType<NavigableView>(ViewOf(tab));
            var gallery = nav.GalleryView;

            tab.Crop = new CropViewModel(tab.Zoom, 2, 2);
            Assert.IsType<CropControl>(ViewOf(tab));

            tab.Crop = null;
            Assert.Same(nav, ViewOf(tab));
            Assert.Same(gallery, nav.GalleryView);
        });

    [AvaloniaFact]
    public void ZoomIn_ThroughTheNavigableView_ZoomsTheViewer()
        => WithWindow((window, _, tab) =>
        {
            tab.Display(BitmapModel("a.jpg"));
            var nav = Assert.IsType<NavigableView>(ViewOf(tab));
            var viewer = Assert.IsType<ImageViewer>(ViewerOf(tab));
            Settle(window);

            ((IZoomScrollTarget)nav).ZoomIn();

            Assert.True(viewer.ZoomPanControl.Scale > 1);
        });
}
