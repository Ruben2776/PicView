using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using PicView.Avalonia.Input;
using PicView.Avalonia.Views.Gallery;
using PicView.Avalonia.Views.UC;
using PicView.Avalonia.Views.UC.Buttons;
using PicView.Core.Localization;
using PicView.Core.ViewModels;
using PicView.Tests.WindowBehavior;
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
    public void ImageViewer_HasNoComparisonControls()
        => WithWindow((window, _, tab) =>
        {
            tab.Display(BitmapModel("a.jpg"));
            Settle(window);

            var viewer = Assert.IsType<ImageViewer>(ViewerOf(tab));
            foreach (var name in new[] { "CompareImage", "CompareBar", "SplitOverlay", "SyncedSideBySideGrid", "AlignMarkersCanvas" })
            {
                Assert.Null(viewer.FindControl<Control>(name));
            }
        });

    [AvaloniaFact]
    public void ImageViewer_HasNoDockedBar()
        => WithWindow((window, _, tab) =>
        {
            var (nav, viewer) = ShowIn(window, tab);

            Assert.Empty(nav.BarHost.Children);
            Assert.Null(((INavigableContent)viewer).DockedBar);
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

    [AvaloniaFact]
    public void Wheel_OverTheHoverBar_ZoomsTheViewer()
        => WithWheelZoom((nav, viewer) =>
        {
            Assert.True(WheelUpOver(nav.HoverBar));

            Assert.True(viewer.ZoomPanControl.Scale > 1);
        });

    [AvaloniaFact]
    public void Wheel_OverAClickArrow_ZoomsTheViewer()
        => WithWheelZoom((nav, viewer) =>
        {
            Assert.True(WheelUpOver(nav.ClickArrowRight));

            Assert.True(viewer.ZoomPanControl.Scale > 1);
        });

    [AvaloniaFact]
    public void Wheel_OverTheGallery_LeavesTheViewer()
        => WithWheelZoom((nav, viewer) =>
        {
            WheelUpOver(nav.GalleryView);

            Assert.Equal(1, viewer.ZoomPanControl.Scale);
        });

    [AvaloniaFact]
    public void Magnify_OverTheHoverBar_ZoomsTheViewer()
        => WithWindow((window, _, tab) =>
        {
            var (nav, viewer) = ShowIn(window, tab);

            nav.HoverBar.RaiseEvent(new PointerDeltaEventArgs(InputElement.PointerTouchPadGestureMagnifyEvent,
                nav.HoverBar, new Pointer(0, PointerType.Touch, true), window, default,
                (ulong)Environment.TickCount64, new PointerPointProperties(), KeyModifiers.None, new Vector(0, 1)));
            Dispatcher.UIThread.RunJobs();

            Assert.True(viewer.ZoomPanControl.Scale > 1);
        });

    [AvaloniaFact]
    public void ZoomPreview_WhereTheHoverBarIs_TakesThePress()
        => WithWindow((window, _, tab) =>
        {
            var (nav, viewer) = ShowIn(window, tab);
            var preview = viewer.FindControl<ZoomPreviewer>("ZoomPreview")!;
            preview.IsVisible = true;
            Settle(window);

            var shared = BoundsIn(nav, preview).Intersect(BoundsIn(nav, nav.HoverBar));
            Assert.True(shared is { Width: > 0, Height: > 0 });
            // Hit testing reads the rendered scene, so the shown preview is rendered first
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            var hit = nav.InputHitTest(shared.Center) as Visual;
            Assert.NotNull(hit?.FindAncestorOfType<ZoomPreviewer>(includeSelf: true));
        });

    // The window hosts the tab's view, so the view is in a Main Window that shows the active tab
    private static (NavigableView Nav, ImageViewer Viewer) ShowIn(TestMainWindow window, TabViewModel tab)
    {
        // The hover bar shows, which it does while the bottom bar is hidden, and takes the pointer, as with its fade
        // buttons on
        tab.ParentWindowContext.GlobalSettings.ShowBottomBar.Value = false;
        tab.ParentWindowContext.GlobalSettings.ShowHoverBar.Value = true;
        tab.ParentWindowContext.GlobalSettings.ShowFadeButtons.Value = true;
        tab.Display(BitmapModel("a.jpg"));
        var nav = Assert.IsType<NavigableView>(ViewOf(tab));
        window.Width = 800;
        window.Height = 600;
        window.Content = nav;
        Settle(window);
        return (nav, Assert.IsType<ImageViewer>(ViewerOf(tab)));
    }

    private static Rect BoundsIn(Visual root, Control control) =>
        new(control.TranslatePoint(default, root)!.Value, control.Bounds.Size);

    // Wheel up zooms in, as Ctrl+wheel does by default
    private static void WithWheelZoom(Action<NavigableView, ImageViewer> test)
        => WithWindow((window, _, tab) =>
        {
            var previous = Keymaps.Current;
            Keymaps.Replace(Keymap.Empty.Assign(new Keybind(PicView.Avalonia.Input.MouseButton.WheelUp), "ZoomIn"));
            try
            {
                var (nav, viewer) = ShowIn(window, tab);

                test(nav, viewer);
            }
            finally
            {
                Keymaps.Replace(previous);
            }
        });

    // Raised on the control, so it routes through its ancestors as a wheel over it does. Returns whether it was used
    private static bool WheelUpOver(Control control)
    {
        var e = new PointerWheelEventArgs(control, new Pointer(0, PointerType.Mouse, true),
            (Visual)TopLevel.GetTopLevel(control)!, default, (ulong)Environment.TickCount64,
            new PointerPointProperties(), KeyModifiers.None, new Vector(0, 1));
        control.RaiseEvent(e);
        Dispatcher.UIThread.RunJobs();
        return e.Handled;
    }
}
