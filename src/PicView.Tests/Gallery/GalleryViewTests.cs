using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Layout;
using Avalonia.Threading;
using ImageMagick;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.UI;
using PicView.Avalonia.Views.Gallery;
using PicView.Avalonia.Views.UC;
using PicView.Core.Gallery;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;
using PicView.Tests.Navigation;
using PicView.Tests.WindowBehavior;
using R3;
using Orientation = Avalonia.Layout.Orientation;

namespace PicView.Tests.Gallery;

/// <summary>
/// How the gallery view shows the items of its tab's gallery as they change.
/// </summary>
[Collection("Sequential")]
public class GalleryViewTests
{
    public GalleryViewTests()
    {
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
        SetDefaults();
        TranslationManager.Init();
    }

    [AvaloniaFact]
    public void Sorted_ShownGallery_ShowsItsItemsInTheNewOrder()
    {
        WithGalleryView((tab, viewer) =>
        {
            var fileA = new FileInfo("a.jpg");
            var fileB = new FileInfo("b.jpg");
            var fileC = new FileInfo("c.jpg");
            var a = new GalleryItemViewModel { FileInfo = fileA };
            var b = new GalleryItemViewModel { FileInfo = fileB };
            var c = new GalleryItemViewModel { FileInfo = fileC };
            tab.Gallery.GalleryItems.AddRange([a, b, c]);
            Dispatcher.UIThread.RunJobs();

            var order = new[] { fileC, fileA, fileB };
            tab.Gallery.GalleryItems.Sort(Comparer<GalleryItemViewModel>.Create((x, y) =>
                Array.IndexOf(order, x.FileInfo).CompareTo(Array.IndexOf(order, y.FileInfo))));
            Dispatcher.UIThread.RunJobs();

            Assert.Equal([c, a, b], viewer.Items.Cast<GalleryItemViewModel>());
        });
    }

    [AvaloniaFact]
    public void Cleared_ShownGallery_ShowsNoItems()
    {
        WithGalleryView((tab, viewer) =>
        {
            tab.Gallery.GalleryItems.AddRange([
                new GalleryItemViewModel { FileInfo = new FileInfo("a.jpg") },
                new GalleryItemViewModel { FileInfo = new FileInfo("b.jpg") }
            ]);
            Dispatcher.UIThread.RunJobs();

            tab.Gallery.GalleryItems.Clear();
            Dispatcher.UIThread.RunJobs();

            Assert.Empty(viewer.Items);
        });
    }

    [AvaloniaFact]
    public void Replaced_ShownGallery_ShowsTheNewItem()
    {
        WithGalleryView((tab, viewer) =>
        {
            var a = new GalleryItemViewModel { FileInfo = new FileInfo("a.jpg") };
            var b = new GalleryItemViewModel { FileInfo = new FileInfo("b.jpg") };
            tab.Gallery.GalleryItems.AddRange([a, b]);
            Dispatcher.UIThread.RunJobs();
            var rebuilt = new GalleryItemViewModel { FileInfo = new FileInfo("b.jpg") };

            tab.Gallery.GalleryItems[1] = rebuilt;
            Dispatcher.UIThread.RunJobs();

            Assert.Equal([a, rebuilt], viewer.Items.Cast<GalleryItemViewModel>());
        });
    }

    [AvaloniaFact]
    public void Initialize_ForATabThatIsNotTheActiveOne_ShowsThatTabsItems()
    {
        WithGalleryView(vm =>
        {
            var first = vm.WindowTabs.ActiveTab.CurrentValue;
            var second = vm.WindowTabs.CreateTab();
            vm.WindowTabs.SelectTab(first);
            return second;
        }, (tab, vm, viewer) =>
        {
            var active = vm.WindowTabs.ActiveTab.CurrentValue;
            Assert.NotSame(tab, active);
            var own = new GalleryItemViewModel { FileInfo = new FileInfo("own.jpg") };

            tab.Gallery.GalleryItems.Add(own);
            active.Gallery.GalleryItems.Add(new GalleryItemViewModel { FileInfo = new FileInfo("other.jpg") });
            Dispatcher.UIThread.RunJobs();

            Assert.Equal([own], viewer.Items.Cast<GalleryItemViewModel>());
        });
    }

    [AvaloniaFact]
    public void Released_ImageViewer_StopsFollowingItsTabsGallery()
    {
        WithImageViewer((tab, imageViewer, viewer) =>
        {
            var a = new GalleryItemViewModel { FileInfo = new FileInfo("a.jpg") };
            var b = new GalleryItemViewModel { FileInfo = new FileInfo("b.jpg") };
            tab.Gallery.GalleryItems.AddRange([a, b]);
            Dispatcher.UIThread.RunJobs();
            tab.ParentWindowContext.WindowTabs.CreateTab();

            tab.ParentWindowContext.WindowTabs.CloseTab(tab);
            tab.Gallery.GalleryItems.Add(new GalleryItemViewModel { FileInfo = new FileInfo("c.jpg") });
            tab.Gallery.NavigateGalleryCommand.Execute(NavigateTo.Last);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal([a, b], viewer.Items.Cast<GalleryItemViewModel>());
            Assert.Equal(-1, viewer.SelectedItemIndex);
        });
    }

    [AvaloniaFact]
    public void CroppedAndBack_ImageViewer_StillFollowsItsTabsGallery()
    {
        WithImageViewer((tab, imageViewer, viewer) =>
        {
            var a = new GalleryItemViewModel { FileInfo = new FileInfo("a.jpg") };
            tab.Gallery.GalleryItems.Add(a);
            Dispatcher.UIThread.RunJobs();
            tab.Crop = new CropViewModel(tab.Zoom, 2, 2);
            tab.Crop = null;
            var b = new GalleryItemViewModel { FileInfo = new FileInfo("b.jpg") };
            tab.Gallery.GalleryItems.Add(b);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal([a, b], viewer.Items.Cast<GalleryItemViewModel>());
        });
    }

    [AvaloniaFact]
    public void StartedDocked_ShowsDockedLook()
    {
        WithShownDockedGallery((_, _, view, panel) =>
        {
            var size = Settings.Gallery.DockedGalleryItemSize;
            Assert.Equal(size, panel.ItemHeight);
            Assert.Equal(GalleryDefaults.GetDockedGalleryHeight(size), view.Height);
            Assert.Equal(Orientation.Horizontal, panel.Orientation);

            Assert.NotEmpty(panel.RealizedItems);
            var container = Assert.IsType<ContentPresenter>(panel.RealizedItems[0].Element);
            var item = Assert.IsType<GalleryItem>(container.Child);
            Assert.Equal(panel.ItemHeight, item.Height);
            Assert.Equal(GalleryThumbnailStretch.ToStretch(Settings.Gallery.DockedGalleryStretchMode),
                item.FindControl<ThumbImage>("GalleryImage")!.Stretch);
        });
    }

    [AvaloniaFact]
    public void DockedItemSizeChanged_WhileDocked_ResizesByTheDefaultsFormula()
    {
        WithShownDockedGallery((core, window, view, panel) =>
        {
            core.GallerySettings.DockedGalleryItemSize.Value = 120;
            RenderFrame(window);

            Assert.Equal(GalleryDefaults.GetDockedGalleryHeight(120), view.Height);
            Assert.Equal(120, panel.ItemHeight);
        });
    }

    [AvaloniaFact]
    public void HiddenByCrop_KeepsItemHeight()
    {
        WithShownDockedGallery((_, window, view, panel) =>
        {
            var tab = (TabViewModel)view.DataContext!;
            var itemHeight = panel.ItemHeight;

            // Crop hides the gallery without animation
            tab.Crop = new CropViewModel(tab.Zoom, 2, 2);
            RenderFrame(window);

            Assert.False(view.IsVisible);
            Assert.Equal(itemHeight, panel.ItemHeight);
            Assert.True(panel.ItemHeight > 0);
        });
    }

    [AvaloniaFact]
    public void StartedDocked_ViewLoadsBeforeInitialize_ShowsDockedGallery()
    {
        // As at startup, where the tab starts docked and its viewer loads before the files are read
        WithShownDockedGallery((core, window, view, panel) =>
        {
            var tab = (TabViewModel)view.DataContext!;
            tab.Gallery.Initialize(core.GallerySettings);
            RenderFrame(window);

            Assert.True(view.IsVisible);
            Assert.Equal(GalleryDefaults.GetDockedGalleryHeight(Settings.Gallery.DockedGalleryItemSize), view.Height);
            Assert.Equal(Settings.Gallery.DockedGalleryItemSize, panel.ItemHeight);
        }, initializeFirst: false);
    }

    [AvaloniaFact]
    public void HiddenWhileLoading_StartedDocked_EndsHidden()
    {
        // A Single Image hides the gallery while the view loads
        WithShownDockedGallery((_, _, view, _) =>
        {
            var tab = (TabViewModel)view.DataContext!;
            Assert.Equal(GalleryMode.Closed, tab.Gallery.ShownMode.Value);
            Assert.False(view.IsVisible);
        }, duringLoad: tab => tab.Gallery.Switcher.SetHidden(true));
    }

    [AvaloniaFact]
    public void ShownWhileLoading_StartedHidden_EndsDocked()
    {
        WithShownDockedGallery((_, _, view, panel) =>
        {
            var tab = (TabViewModel)view.DataContext!;
            Assert.Equal(GalleryMode.Docked, tab.Gallery.ShownMode.Value);
            Assert.True(view.IsVisible);
            Assert.Equal(GalleryDefaults.GetDockedGalleryHeight(Settings.Gallery.DockedGalleryItemSize), view.Height);
            Assert.Equal(Settings.Gallery.DockedGalleryItemSize, panel.ItemHeight);
        }, startHidden: true, duringLoad: tab => tab.Gallery.Switcher.SetHidden(false));
    }

    // Shown in a window, so the view loads, with a tab started docked at the bottom and a few items. duringLoad runs
    // as the view loads, when it sizes itself to nothing before it follows the switcher
    private static void WithShownDockedGallery(Action<CoreViewModel, Window, GalleryView, VirtualizingGallery> test,
        bool initializeFirst = true, bool startHidden = false, Action<TabViewModel>? duringLoad = null)
    {
        Settings.Gallery.DockPosition = GalleryDockPosition.Bottom;
        var core = new CoreViewModel(null, _ => ValueTask.FromResult(new ImageModel()));
        var window = new TestMainWindow(new NoOpModeAdapter());
        var vm = new MainWindowViewModel(core, core.Translation, core.GlobalSettings!, core.GallerySettings);
        window.DataContext = vm;
        var tab = vm.WindowTabs.ActiveTab.Value;
        if (initializeFirst)
        {
            tab.Gallery.Initialize(core.GallerySettings);
        }
        tab.Gallery.Switcher.Start(docked: true);
        if (startHidden)
        {
            tab.Gallery.Switcher.SetHidden(true);
        }

        var application = Application.Current!;
        var previous = application.DataContext;
        application.DataContext = core;
        try
        {
            var view = new GalleryView { DataContext = tab };
            view.Initialize(tab);
            tab.Gallery.GalleryItems.AddRange([
                new GalleryItemViewModel { FileInfo = new FileInfo("a.jpg") },
                new GalleryItemViewModel { FileInfo = new FileInfo("b.jpg") }
            ]);
            if (duringLoad is not null)
            {
                var ran = false;
                view.PropertyChanged += (_, e) =>
                {
                    if (!ran && view.IsLoaded && e.Property == Layoutable.WidthProperty)
                    {
                        ran = true;
                        duringLoad(tab);
                    }
                };
            }
            window.Content = new DockPanel { Children = { view } };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            RenderFrame(window);

            var panel = (VirtualizingGallery)view.FindControl<NavigateAbleItemsViewer>("GalleryItemsControl")!
                .ItemsPanelRoot!;
            test(core, window, view, panel);
        }
        finally
        {
            window.Close();
            application.DataContext = previous;
        }
    }

    private static void RenderFrame(Window window)
    {
        window.InvalidateVisual();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public void ShownPair_MarksBothItems()
    {
        WithGalleryView((tab, viewer) =>
        {
            // As the image viewer gives it the tab
            viewer.DataContext = tab;
            tab.Services!.GlobalSettings.IsSideBySide.Value = true;
            tab.ImageIterator!.Initialize([new FileInfo("a.jpg"), new FileInfo("b.jpg"), new FileInfo("c.jpg")]);

            // The step may wait for the cache off the UI thread, so the UI thread runs its jobs until the step ends
            var step = tab.ImageIterator.IterateToIndexAsync(2, new CancellationTokenSource()).AsTask();
            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (!step.IsCompleted && DateTime.UtcNow < deadline)
            {
                Dispatcher.UIThread.RunJobs();
                Thread.Sleep(10);
            }
            Dispatcher.UIThread.RunJobs();

            // The last image shows in the pair before it, so the items of both are current
            Assert.True(step.IsCompletedSuccessfully);
            Assert.Equal((1, 2), (viewer.CurrentItemIndex, viewer.PartnerItemIndex));
        });
    }

    private static void WithGalleryView(Action<TabViewModel, NavigateAbleItemsViewer> test) =>
        WithGalleryView(static vm => vm.WindowTabs.ActiveTab.CurrentValue, (tab, _, viewer) => test(tab, viewer));

    private static void WithGalleryView(Func<MainWindowViewModel, TabViewModel> tabOf,
        Action<TabViewModel, MainWindowViewModel, NavigateAbleItemsViewer> test) =>
        WithTab(tabOf, (tab, vm) =>
        {
            var view = new GalleryView();
            view.Initialize(tab);
            test(tab, vm, view.FindControl<NavigateAbleItemsViewer>("GalleryItemsControl")!);
        });

    private static void WithImageViewer(Action<TabViewModel, ImageViewer, NavigateAbleItemsViewer> test) =>
        WithTab(static vm => vm.WindowTabs.ActiveTab.CurrentValue, (tab, _) =>
        {
            tab.SetLoading();
            var imageViewer = (ImageViewer)tab.CurrentView.Value!;
            test(tab, imageViewer, imageViewer.GalleryView.FindControl<NavigateAbleItemsViewer>("GalleryItemsControl")!);
        });

    // The gallery view loads its XAML only when the application's DataContext is a CoreViewModel
    private static void WithTab(Func<MainWindowViewModel, TabViewModel> tabOf,
        Action<TabViewModel, MainWindowViewModel> test)
    {
        var core = new CoreViewModel(null, _ => ValueTask.FromResult(new ImageModel()));
        core.InitializeNavigation(
            TestTabServices.For(new SharedImageCache(static f => ValueTask.FromResult(new ImageModel { FileInfo = f })), null),
            new NavigationService(string.CompareOrdinal));
        var window = new TestMainWindow(new NoOpModeAdapter());
        var vm = new MainWindowViewModel(core, core.Translation, core.GlobalSettings!, core.GallerySettings);
        window.DataContext = vm;
        vm.WindowTabs.SetViewFactory(new TabViews(window));
        core.MainWindows.ActiveWindow.Value = vm;
        var tab = tabOf(vm);
        tab.Initialize();

        var application = Application.Current!;
        var previous = application.DataContext;
        application.DataContext = core;
        try
        {
            test(tab, vm);
        }
        finally
        {
            application.DataContext = previous;
            window.Close();
        }
    }

    private sealed class NoOpModeAdapter : IWindowModeAdapter
    {
        public bool SkipsInitialNativeState => false;

        public void EnterNativeState(MainWindow window, WindowModeState state)
        {
        }
    }



    private sealed class MockFrameProvider : FrameProvider
    {
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) => callback.MoveNext(0);
    }
}
