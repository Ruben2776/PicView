using System.Collections;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ImageMagick;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.UI;
using PicView.Avalonia.Views.UC;
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
            var viewer = Assert.IsType<ImageViewer>(ViewOf(tab));
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
            var viewer = Assert.IsType<ImageViewer>(ViewOf(tab));
            viewer.ZoomPanControl.SetScaleImmediate(2);

            tab.Crop = new CropViewModel(tab.Zoom, 2, 2);

            var crop = Assert.IsType<CropControl>(ViewOf(tab));
            Assert.Equal(2, crop.PendingZoomView!.Value.Scale);
        });

    [AvaloniaFact]
    public void Crop_Closed_HandsItsZoomBackToTheViewer()
        => WithWindow((_, _, tab) =>
        {
            tab.Display(BitmapModel("a.jpg"));
            var viewer = Assert.IsType<ImageViewer>(ViewOf(tab));
            tab.Crop = new CropViewModel(tab.Zoom, 2, 2);
            Assert.IsType<CropControl>(ViewOf(tab)).SetZoomView(new ZoomView(3, 0.25, 0.75));

            tab.Crop = null;

            Assert.Equal(new ZoomView(3, 0.25, 0.75), viewer.PendingZoomView);
        });

    [AvaloniaFact]
    public void Crop_ClosedAfterTheImageChanged_HandsNothingBack()
        => WithWindow((_, _, tab) =>
        {
            // A crop saved over the shown file displays the new image before the crop closes. With the reset on
            // image change off, nothing else brings the tab's Zoom Factor back to the viewer's scale
            Settings.Zoom.ResetZoomOnChange = false;
            tab.Display(BitmapModel("a.jpg"));
            var viewer = Assert.IsType<ImageViewer>(ViewOf(tab));
            tab.Crop = new CropViewModel(tab.Zoom, 2, 2);
            var crop = Assert.IsType<CropControl>(ViewOf(tab));
            crop.SetZoomView(new ZoomView(3, 0.5, 0.5));
            crop.ZoomPanControl.SetScaleImmediate(3);
            Assert.Equal(3, tab.Zoom.Factor);

            tab.Display(BitmapModel("b.jpg"));
            tab.Crop = null;

            Assert.Null(viewer.PendingZoomView);
            Assert.Equal(viewer.ZoomPanControl.Scale, tab.Zoom.Factor);
        });

    [AvaloniaFact]
    public void Crop_SavedOverTheShownFileFromAZoomedViewer_ReturnsTheViewerUnzoomed()
        => WithWindow((_, _, tab) =>
        {
            // The viewer was zoomed when the crop opened and is not shown while the new image is displayed, so
            // nothing else resets its zoom for the new image
            tab.Display(BitmapModel("a.jpg"));
            var viewer = Assert.IsType<ImageViewer>(ViewOf(tab));
            viewer.ZoomPanControl.SetScaleImmediate(2);
            tab.Crop = new CropViewModel(tab.Zoom, 2, 2);

            tab.Display(BitmapModel("b.jpg"));
            tab.Crop = null;

            Assert.Equal(1, viewer.ZoomPanControl.Scale);
            Assert.Equal(1, tab.Zoom.Factor);
            Assert.Null(viewer.PendingZoomView);
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
            var viewer = (ImageViewer)views.View.Value!;
            var a = new GalleryItemViewModel { FileInfo = new FileInfo("a.jpg") };
            tab.Gallery.GalleryItems.Add(a);
            Dispatcher.UIThread.RunJobs();
            vm.WindowTabs.CloseTab(tab);
            tab.Gallery.GalleryItems.Add(new GalleryItemViewModel { FileInfo = new FileInfo("b.jpg") });
            Dispatcher.UIThread.RunJobs();
            Assert.Null(views.View.Value);
            Assert.Null(tab.Views);
            Assert.Equal([a],
                viewer.GalleryView.FindControl<NavigateAbleItemsViewer>("GalleryItemsControl")!.Items
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
            Assert.IsType<ImageViewer>(ViewOf(tab));
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
    private static int ClickHandlerCount(Button button)
    {
        for (var type = typeof(Button); type is not null; type = type.BaseType)
        {
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
            {
                if (field.GetValue(button) is IDictionary handlers && handlers.Contains(Button.ClickEvent))
                {
                    return ((ICollection)handlers[Button.ClickEvent]!).Count;
                }
            }
        }

        return 0;
    }

    [AvaloniaFact]
    public async Task SetLoading_OnAThreadPoolThread_ShowsTheViewerOnTheUIThread()
        => await WithWindowAsync(async (_, _, tab) =>
        {
            await Task.Run(tab.SetLoading, TestContext.Current.CancellationToken);
            Dispatcher.UIThread.RunJobs();
            Assert.IsType<ImageViewer>(ViewOf(tab));
        });
}
