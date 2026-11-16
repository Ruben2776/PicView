using System.Collections;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ImageMagick;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.CustomControls.Zoom;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.UI;
using PicView.Avalonia.Views.UC;
using PicView.Avalonia.Views.Menu;
using PicView.Avalonia.ImageTransformations;
using PicView.Avalonia.Input;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Config;
using PicView.Core.Keybindings;
using PicView.Tests.Input;
using Avalonia.Headless;
using MouseButton = Avalonia.Input.MouseButton;
using PicView.Core.ImageDecoding;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.Localization;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;
using PicView.Tests.Navigation;
using PicView.Tests.WindowBehavior;
using static PicView.Tests.UI.TabViewsHarness;

namespace PicView.Tests.UI;

[Collection("Sequential")]
public class TabViewsTests
{
    public TabViewsTests()
    {
        SetDefaults();
        TranslationManager.Init();
    }

    [AvaloniaFact]
    public void Attach_NewTab_ShowsAMenuWithTheTabAsDataContext()
        => WithWindow((_, _, tab) =>
        {
            var menu = Assert.IsType<StartUpMenu>(ViewOf(tab));
            Assert.Same(tab, menu.DataContext);
        });

    [AvaloniaFact]
    public void SetLoading_ShowsAViewerWithTheTabAsDataContext()
        => WithWindow((_, _, tab) =>
        {
            tab.SetLoading();
            var nav = Assert.IsType<NavigableView>(ViewOf(tab));
            var viewer = Assert.IsType<ImageViewer>(ViewerOf(tab));
            Assert.Same(tab, nav.DataContext);
            Assert.Same(tab, viewer.DataContext);
        });

    [AvaloniaFact]
    public void Crop_SetAndCleared_ShowsACropControlThenTheSameViewer()
        => WithWindow((_, _, tab) =>
        {
            tab.Display(BitmapModel("a.jpg"));
            var viewer = ViewOf(tab);
            tab.Crop = new CropViewModel(tab.Zoom, 2, 2);
            Assert.IsType<CropControl>(ViewOf(tab));
            tab.Crop = null;
            Assert.Same(viewer, ViewOf(tab));
        });

    [AvaloniaFact]
    public void Crop_OpenedOnAZoomedViewer_StartsAtTheViewersScale()
        => WithWindow((_, _, tab) =>
        {
            tab.Display(BitmapModel("a.jpg"));
            Assert.IsType<NavigableView>(ViewOf(tab));
            var viewer = Assert.IsType<ImageViewer>(ViewerOf(tab));
            viewer.ZoomPanControl.SetScaleImmediate(2);

            tab.Crop = new CropViewModel(tab.Zoom, 2, 2);

            var crop = Assert.IsType<CropControl>(ViewOf(tab));
            Assert.Equal(2, crop.PendingZoomView!.Value.Scale);
        });

    [AvaloniaFact]
    public void Crop_ZoomedAndClosed_ReturnsTheViewerToItsOwnZoom()
        => WithWindow((window, _, tab) =>
        {
            // The crop's zoom is its own: the viewer comes back as it was left, with no zoom preview
            tab.Display(BitmapModel("a.jpg"));
            Settle(window);
            Assert.IsType<NavigableView>(ViewOf(tab));
            var viewer = Assert.IsType<ImageViewer>(ViewerOf(tab));
            tab.Crop = new CropViewModel(tab.Zoom, 2, 2);
            Settle(window);
            Assert.IsType<CropControl>(ViewOf(tab)).ZoomPanControl.SetScaleImmediate(3);
            Assert.Equal(3, tab.Zoom.Factor);

            tab.Crop = null;
            Settle(window);

            Assert.Equal(1, viewer.ZoomPanControl.Scale);
            Assert.Equal(1, tab.Zoom.Factor);
            Assert.False(viewer.ZoomPreview.IsVisible);
        });

    [AvaloniaFact]
    public void Crop_ClosedOnAZoomedViewerWithResetOnChange_ReturnsTheViewerUnzoomed()
        => WithWindow((window, _, tab) =>
        {
            Settings.Zoom.ResetZoomOnChange = true;
            tab.Display(BitmapModel("a.jpg"));
            Settle(window);
            Assert.IsType<NavigableView>(ViewOf(tab));
            var viewer = Assert.IsType<ImageViewer>(ViewerOf(tab));
            viewer.ZoomPanControl.SetScaleImmediate(2);
            tab.Crop = new CropViewModel(tab.Zoom, 2, 2);
            Settle(window);

            tab.Crop = null;
            Settle(window);

            Assert.Equal(1, viewer.ZoomPanControl.Scale);
            Assert.Equal(1, tab.Zoom.Factor);
        });

    [AvaloniaFact]
    public void Crop_ZoomedAndClosedOnAZoomedViewerWithoutResetOnChange_KeepsTheViewersZoom()
        => WithWindow((window, _, tab) =>
        {
            Settings.Zoom.ResetZoomOnChange = false;
            tab.Display(BitmapModel("a.jpg"));
            Settle(window);
            Assert.IsType<NavigableView>(ViewOf(tab));
            var viewer = Assert.IsType<ImageViewer>(ViewerOf(tab));
            viewer.ZoomPanControl.SetScaleImmediate(2);
            tab.Crop = new CropViewModel(tab.Zoom, 2, 2);
            Settle(window);
            Assert.IsType<CropControl>(ViewOf(tab)).ZoomPanControl.SetScaleImmediate(3);

            tab.Crop = null;
            Settle(window);

            Assert.Equal(2, viewer.ZoomPanControl.Scale);
            Assert.Equal(2, tab.Zoom.Factor);
        });

    [AvaloniaFact]
    public void Crop_ClosedAfterTheImageChanged_WritesTheViewersScaleAsTheZoomFactor()
        => WithWindow((_, _, tab) =>
        {
            // A crop saved over the shown file displays the new image before the crop closes. With the reset on
            // image change off, nothing else brings the tab's Zoom Factor back to the viewer's scale
            Settings.Zoom.ResetZoomOnChange = false;
            tab.Display(BitmapModel("a.jpg"));
            Assert.IsType<NavigableView>(ViewOf(tab));
            var viewer = Assert.IsType<ImageViewer>(ViewerOf(tab));
            tab.Crop = new CropViewModel(tab.Zoom, 2, 2);
            var crop = Assert.IsType<CropControl>(ViewOf(tab));
            crop.SetZoomView(new ZoomView(3, 0.5, 0.5));
            crop.ZoomPanControl.SetScaleImmediate(3);
            Assert.Equal(3, tab.Zoom.Factor);

            tab.Display(BitmapModel("b.jpg"));
            tab.Crop = null;

            Assert.Equal(viewer.ZoomPanControl.Scale, tab.Zoom.Factor);
        });

    [AvaloniaFact]
    public void Crop_SavedOverTheShownFileFromAZoomedViewer_ReturnsTheViewerUnzoomed()
        => WithWindow((_, _, tab) =>
        {
            // The viewer was zoomed when the crop opened and is not shown while the new image is displayed, so
            // nothing else resets its zoom for the new image
            tab.Display(BitmapModel("a.jpg"));
            Assert.IsType<NavigableView>(ViewOf(tab));
            var viewer = Assert.IsType<ImageViewer>(ViewerOf(tab));
            viewer.ZoomPanControl.SetScaleImmediate(2);
            tab.Crop = new CropViewModel(tab.Zoom, 2, 2);

            tab.Display(BitmapModel("b.jpg"));
            tab.Crop = null;

            Assert.Equal(1, viewer.ZoomPanControl.Scale);
            Assert.Equal(1, tab.Zoom.Factor);
        });

    [AvaloniaFact]
    public void CloseTab_WhileCropping_DoesNotThrow()
        => WithWindow((_, vm, _) =>
        {
            var tab = vm.WindowTabs.CreateTab();
            tab.Display(BitmapModel("a.jpg"));
            tab.Crop = new CropViewModel(tab.Zoom, 2, 2);
            Assert.IsType<CropControl>(tab.Views!.View.Value);

            vm.WindowTabs.CloseTab(tab);
            Dispatcher.UIThread.RunJobs();

            Assert.Null(tab.Views);
        });

    [AvaloniaFact]
    public void EndLoading_OnAFreshTab_ShowsTheKeptMenuAgain()
        => WithWindow((_, _, tab) =>
        {
            var menu = ViewOf(tab);
            var ticket = tab.SetLoading();
            tab.EndLoading(ticket);
            Assert.Same(menu, ViewOf(tab));
        });

    [AvaloniaFact]
    public void Reorder_KeepsTheSameViewer()
        => WithWindow((_, vm, tab) =>
        {
            tab.SetLoading();
            var viewer = ViewOf(tab);
            vm.WindowTabs.CreateTab();
            vm.WindowTabs.Reorder(tab, 1);
            Dispatcher.UIThread.RunJobs();
            Assert.Same(viewer, ViewOf(tab));
        });

    [AvaloniaFact]
    public void CloseTab_DisposesTheViewerAndClearsTheView()
        => WithWindow((_, vm, _) =>
        {
            var tab = vm.WindowTabs.CreateTab();
            tab.SetLoading();
            var views = tab.Views!;
            var nav = (NavigableView)views.View.Value!;
            var a = new GalleryItemViewModel { FileInfo = new FileInfo("a.jpg") };
            tab.Gallery.GalleryItems.Add(a);
            Dispatcher.UIThread.RunJobs();
            vm.WindowTabs.CloseTab(tab);
            tab.Gallery.GalleryItems.Add(new GalleryItemViewModel { FileInfo = new FileInfo("b.jpg") });
            Dispatcher.UIThread.RunJobs();
            Assert.Null(views.View.Value);
            Assert.Null(tab.Views);
            Assert.Equal([a],
                nav.GalleryView.FindControl<NavigateAbleItemsViewer>("GalleryItemsControl")!.Items
                    .Cast<GalleryItemViewModel>());
        });

    [AvaloniaFact]
    public async Task Attach_OffTheUIThread_MakesTheMenuOnTheUIThread()
    {
        // Consecutive pasted files create their tabs off the UI thread
        await WithWindowAsync(async (_, vm, _) =>
        {
            // Made on a thread of its own and joined, so the UI thread cannot run the posted reaction before the
            // assertion, and a waited-for pool task cannot be run inline on the UI thread
            TabViewModel? tab = null;
            var thread = new Thread(() => tab = vm.WindowTabs.CreateTab());
            thread.Start();
            thread.Join();
            Assert.Null(ViewOf(tab!));
            Dispatcher.UIThread.RunJobs();
            Assert.IsType<StartUpMenu>(ViewOf(tab!));
        });
    }

    [AvaloniaFact]
    public void StartUpMenu_ReattachedAfterAViewer_HandlesAClickOnce()
        => WithWindow((window, _, tab) =>
        {
            // The test window has no main view, so a host presents whatever the tab shows, as the main view does
            var host = new ContentControl();
            window.Content = host;
            var menu = Assert.IsType<StartUpMenu>(ViewOf(tab));
            host.Content = menu;
            Settle(window);
            Assert.True(menu.IsLoaded);

            // The viewer replaces the menu, which detaches it; ending the load on a fresh tab shows the same menu again
            var ticket = tab.SetLoading();
            Assert.IsType<NavigableView>(ViewOf(tab));
            Assert.IsType<ImageViewer>(ViewerOf(tab));
            // The viewer's own Loaded needs the full window, so the host is left empty while it shows
            host.Content = null;
            Settle(window);
            Assert.False(menu.IsLoaded);
            tab.EndLoading(ticket);
            host.Content = ViewOf(tab);
            Settle(window);
            Assert.Same(menu, ViewOf(tab));
            Assert.True(menu.IsLoaded);

            // The click's effect opens the last file, so count what the button holds for Click instead
            Assert.Equal(1, ClickHandlerCount(menu.OpenLastFileButton));
        });

    // Interactive keeps its routed event handlers in a dictionary keyed by event
    private static int ClickHandlerCount(Button button) => HandlerCount(button, Button.ClickEvent);

    private static int HandlerCount(Interactive control, RoutedEvent routedEvent)
    {
        for (var type = control.GetType(); type is not null; type = type.BaseType)
        {
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic |
                                                 BindingFlags.DeclaredOnly))
            {
                if (field.GetValue(control) is IDictionary handlers && handlers.Contains(routedEvent))
                {
                    return ((ICollection)handlers[routedEvent]!).Count;
                }
            }
        }

        return 0;
    }

    [AvaloniaFact]
    public void Loaded_RaisedAgain_AddsTheWheelAndPinchHandlersOnce()
        => WithWindow((_, _, tab) =>
        {
            // The kept view is loaded again each time it is shown after a crop or the menu. Handlers added on
            // every load made one wheel step zoom twice after one crop, and three times after two. The navigable
            // view routes them, so a wheel over its chrome reaches the viewer
            tab.SetLoading();
            var nav = Assert.IsType<NavigableView>(ViewOf(tab));
            var viewer = Assert.IsType<ImageViewer>(ViewerOf(tab));

            nav.RaiseEvent(new RoutedEventArgs(Control.LoadedEvent));
            nav.RaiseEvent(new RoutedEventArgs(Control.LoadedEvent));
            viewer.RaiseEvent(new RoutedEventArgs(Control.LoadedEvent));

            Assert.Equal(1, HandlerCount(nav.ContentPanel, InputElement.PointerWheelChangedEvent));
            Assert.Equal(1, HandlerCount(nav.ContentPanel, InputElement.PinchEvent));
            Assert.Equal(0, HandlerCount(viewer, InputElement.PointerWheelChangedEvent));
        });

    [AvaloniaFact]
    public async Task SetLoading_OnAThreadPoolThread_ShowsTheViewerOnTheUIThread()
        => await WithWindowAsync(async (_, _, tab) =>
        {
            await Task.Run(tab.SetLoading, TestContext.Current.CancellationToken);
            Dispatcher.UIThread.RunJobs();
            Assert.IsType<NavigableView>(ViewOf(tab));
            Assert.IsType<ImageViewer>(ViewerOf(tab));
        });

    [AvaloniaFact]
    public void Comparing_On_ShowsTheSameNavigableViewWithAComparisonViewer()
        => WithWindow((_, _, tab) =>
        {
            tab.Display(BitmapModel("a.jpg"), BitmapModel("b.jpg"));
            var nav = Assert.IsType<NavigableView>(ViewOf(tab));

            tab.SetComparing(true);

            Assert.Same(nav, ViewOf(tab));
            var viewer = Assert.IsType<ComparisonViewer>(ViewerOf(tab));
            Assert.Same(tab.Comparison, viewer.Comparison);
        });

    [AvaloniaFact]
    public void Comparing_OnAndOff_KeepsTheSameGallery()
        => WithWindow((_, _, tab) =>
        {
            tab.Display(BitmapModel("a.jpg"), BitmapModel("b.jpg"));
            var nav = Assert.IsType<NavigableView>(ViewOf(tab));
            var a = new GalleryItemViewModel { FileInfo = new FileInfo("a.jpg") };
            tab.Gallery.GalleryItems.Add(a);
            Dispatcher.UIThread.RunJobs();
            var galleryBefore = nav.GalleryView;

            tab.SetComparing(true);
            tab.SetComparing(false);
            Dispatcher.UIThread.RunJobs();

            Assert.Same(nav, ViewOf(tab));
            Assert.Same(galleryBefore, nav.GalleryView);
            Assert.Equal([a],
                nav.GalleryView.FindControl<NavigateAbleItemsViewer>("GalleryItemsControl")!.Items
                    .Cast<GalleryItemViewModel>());
        });

    [AvaloniaFact]
    public void Comparing_Off_ShowsTheKeptImageViewerAgain()
        => WithWindow((_, _, tab) =>
        {
            tab.Display(BitmapModel("a.jpg"), BitmapModel("b.jpg"));
            var viewer = Assert.IsType<ImageViewer>(ViewerOf(tab));
            tab.SetComparing(true);
            var comparisonViewer = Assert.IsType<ComparisonViewer>(ViewerOf(tab));
            var preview = ((INavigableContent)comparisonViewer).ZoomPreview!;

            tab.SetComparing(false);

            Assert.Same(viewer, ViewerOf(tab));
            // The comparison viewer left the slot, and its zoom preview left the view's overlay with it
            var nav = Assert.IsType<NavigableView>(ViewOf(tab));
            Assert.DoesNotContain(preview, nav.OverlayHost.Children);
            Assert.True(IsDisposed(comparisonViewer));
        });

    [AvaloniaFact]
    public void Comparing_OnAfterZooming_StartsTheComparisonAtTheTabsFactor()
        => WithWindow((window, vm, tab) =>
        {
            Settings.Zoom.IsZoomAnimated = false;
            var viewer = ShowPair(window, vm, tab);
            viewer.ZoomIn();
            viewer.ZoomIn();
            Settle(window);
            Assert.NotEqual(1, tab.Zoom.Factor);

            tab.SetComparing(true);
            Settle(window);

            var comparison = Assert.IsType<ComparisonViewer>(ViewerOf(tab));
            Assert.Equal(tab.Zoom.Factor, ZoomOf(comparison).Scale, 6);
        });

    [AvaloniaFact]
    public void Comparing_OnUnzoomed_LeavesTheComparisonUnpanned()
        => WithWindow((window, vm, tab) =>
        {
            // Setting the scale it already has would re-centre its content, which offsets the comparison
            ShowPair(window, vm, tab);

            tab.SetComparing(true);
            Settle(window);

            var zoom = ZoomOf(Assert.IsType<ComparisonViewer>(ViewerOf(tab)));
            Assert.Equal(1, zoom.Scale);
            Assert.Equal(0, zoom.TranslateX);
            Assert.Equal(0, zoom.TranslateY);
        });

    [AvaloniaFact]
    public void Comparing_OffAfterZooming_StartsTheViewerAtTheTabsFactor()
        => WithWindow((window, vm, tab) =>
        {
            Settings.Zoom.IsZoomAnimated = false;
            var viewer = ShowPair(window, vm, tab);
            tab.SetComparing(true);
            Settle(window);
            var comparison = Assert.IsType<ComparisonViewer>(ViewerOf(tab));
            ((IZoomScrollTarget)comparison).ZoomIn();
            ((IZoomScrollTarget)comparison).ZoomIn();
            Settle(window);
            Assert.NotEqual(1, tab.Zoom.Factor);

            tab.SetComparing(false);
            Settle(window);

            Assert.Same(viewer, ViewerOf(tab));
            Assert.Equal(tab.Zoom.Factor, viewer.ZoomPanControl.Scale, 6);
        });

    [AvaloniaFact]
    public void CloseTab_WhileComparing_DisposesTheComparisonViewer()
        => WithWindow((_, vm, _) =>
        {
            var tab = vm.WindowTabs.CreateTab();
            tab.Display(BitmapModel("a.jpg"), BitmapModel("b.jpg"));
            tab.SetComparing(true);
            var comparison = tab.Comparison!;
            var viewer = Assert.IsType<ComparisonViewer>(ViewerOf(tab));

            vm.WindowTabs.CloseTab(tab);
            Dispatcher.UIThread.RunJobs();

            Assert.True(comparison.IsDisposed);
            Assert.True(IsDisposed(viewer));
        });

    [AvaloniaFact]
    public void Comparing_FromTheStartUpMenu_ShowsANavigableViewWithAComparisonViewer()
        => WithWindow((_, _, tab) =>
        {
            Assert.IsType<StartUpMenu>(ViewOf(tab));
            tab.SetComparing(true);

            tab.Display(BitmapModel("a.jpg"), BitmapModel("b.jpg"));

            Assert.IsType<NavigableView>(ViewOf(tab));
            Assert.IsType<ComparisonViewer>(ViewerOf(tab));
        });

    [AvaloniaFact]
    public async Task Comparing_AfterClosingTheImage_ShowsANewComparisonViewer()
        => await WithWindowAsync(async (_, _, tab) =>
        {
            tab.Display(BitmapModel("a.jpg"), BitmapModel("b.jpg"));
            tab.SetComparing(true);
            var old = Assert.IsType<ComparisonViewer>(ViewerOf(tab));
            await tab.CloseImageAsync().ConfigureAwait(true);
            Dispatcher.UIThread.RunJobs();

            tab.Display(BitmapModel("c.jpg"), BitmapModel("d.jpg"));

            var viewer = Assert.IsType<ComparisonViewer>(ViewerOf(tab));
            Assert.NotSame(old, viewer);
            Assert.Same(tab.Comparison, viewer.Comparison);
            Assert.True(IsDisposed(old));
            var nav = Assert.IsType<NavigableView>(ViewOf(tab));
            Assert.DoesNotContain(((INavigableContent)old).ZoomPreview!, nav.OverlayHost.Children);
        });

    [AvaloniaFact]
    public void Comparing_ResetZoom_ResetsTheComparisonScale()
        => WithWindow((window, vm, tab) =>
        {
            Settings.Zoom.IsZoomAnimated = false;
            ShowPair(window, vm, tab);
            tab.SetComparing(true);
            Settle(window);
            var comparison = Assert.IsType<ComparisonViewer>(ViewerOf(tab));
            ((IZoomScrollTarget)comparison).ZoomIn();
            ((IZoomScrollTarget)comparison).ZoomIn();
            Settle(window);
            Assert.NotEqual(1, ZoomOf(comparison).Scale);

            RotationManager.ResetZoom(vm);
            Settle(window);

            Assert.Equal(1, ZoomOf(comparison).Scale);
            Assert.Equal(1, tab.Zoom.Factor);
        });

    [AvaloniaFact]
    public async Task Comparing_WindowModeChanged_ResetsTheComparisonZoomSlim()
        => await WithWindowAsync(async (window, vm, tab) =>
        {
            Settings.Zoom.IsZoomAnimated = false;
            ShowPair(window, vm, tab);
            tab.SetComparing(true);
            Settle(window);
            var comparison = Assert.IsType<ComparisonViewer>(ViewerOf(tab));
            ((IZoomScrollTarget)comparison).ZoomIn();
            Settle(window);
            Assert.NotEqual(1, ZoomOf(comparison).Scale);
            var switcher = new WindowModeSwitcher(window, new NoNativeState(), static () => ValueTask.FromResult(true));

            await switcher.ToggleFullscreen().ConfigureAwait(true);
            Settle(window);

            Assert.Equal(1, ZoomOf(comparison).Scale);
        });

    [AvaloniaFact]
    public void Comparing_ShouldCloseImageBeVisible_IsTrue()
        => WithWindow((_, _, tab) =>
        {
            tab.Display(BitmapModel("a.jpg"), BitmapModel("b.jpg"));
            tab.SetComparing(true);

            // The main view's context menu shows Close image while the tab's view is a navigable view
            Assert.IsType<NavigableView>(tab.CurrentView.Value);
        });

    [AvaloniaFact]
    public void Comparing_ClickedOnTheRightHalf_NavigatesNext()
        => WithWindow((window, vm, tab) =>
        {
            Settings.Navigation.IsNavigatingByClicking = true;
            ShowPair(window, vm, tab);
            tab.SetComparing(true);
            tab.Comparison!.SetMode(ComparisonMode.Split);
            window.SharedTitleBar = new CustomTitleBar();
            window.SharedBottomBar = new BottomBar();
            var actions = new RecordingActionRunner();
            vm.ActionRunner = actions;
            window.AddHandler(InputElement.PointerPressedEvent,
                (_, e) => _ = ImageViewerClickBehavior.PointerPressedAsync(e, window), RoutingStrategies.Tunnel);
            Settle(window);
            var nav = Assert.IsType<NavigableView>(ViewOf(tab));
            var comparison = Assert.IsType<ComparisonViewer>(ViewerOf(tab));

            // A press on the split handle drags the split, so it does not navigate
            Click(window, CenterOf(comparison.FindControl<Border>("SplitHandle")!, window));
            Assert.Empty(actions.Calls);

            // On the image, right of the split handle in the middle
            var panel = nav.ContentPanel;
            Click(window,
                panel.TranslatePoint(new Point(panel.Bounds.Width * 0.6, panel.Bounds.Height / 2), window)!.Value);

            Assert.Equal([Actions.Next], actions.Calls);
        });

    // Shows a pair in the tab's navigable view, hosted in the window so its viewer loads, and returns its image viewer
    private static ImageViewer ShowPair(TestMainWindow window, MainWindowViewModel vm, TabViewModel tab)
    {
        window.Width = 800;
        window.Height = 600;
        vm.ImageWidth.Value = 400;
        vm.ImageHeight.Value = 300;
        tab.Display(BitmapModel("a.jpg"), BitmapModel("b.jpg"));
        window.Content = Assert.IsType<NavigableView>(ViewOf(tab));
        Settle(window);
        return Assert.IsType<ImageViewer>(ViewerOf(tab));
    }

    private static ZoomPanControl ZoomOf(ComparisonViewer viewer) =>
        viewer.FindControl<ZoomPanControl>("ZoomPanControl")!;

    private static bool IsDisposed(ComparisonViewer viewer) =>
        (bool)typeof(ComparisonViewer).GetField("_isDisposed", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(viewer)!;

    private static Point CenterOf(Control control, Window window) =>
        control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;

    private static void Click(TestMainWindow window, Point at)
    {
        window.MouseDown(at, MouseButton.Left);
        window.MouseUp(at, MouseButton.Left);
        Settle(window);
    }

    private sealed class NoNativeState : IWindowModeAdapter
    {
        public bool SkipsInitialNativeState => false;

        public void EnterNativeState(MainWindow window, WindowModeState state)
        {
        }
    }
}
