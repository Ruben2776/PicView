using System.Diagnostics;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.Views.Gallery;
using PicView.Core.Gallery;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;
using PicView.Tests.WindowBehavior;
using R3;

namespace PicView.Tests.Gallery;

/// <summary>How a Gallery Item looks in each gallery, and what realizing Docked Gallery containers costs.</summary>
[Collection("Sequential")]
public class GalleryItemLookTests
{
    private readonly ITestOutputHelper _output;

    public GalleryItemLookTests(ITestOutputHelper output)
    {
        _output = output;
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
        SetDefaults();
        TranslationManager.Init();
    }

    // Docked at the bottom (item size 76) and Expanded Grid (150), as they look before anything moves: alike but for the corners, square when docked and rounded by 6 in Grid, which clips to them
    [AvaloniaTheory]
    [InlineData(false, 76, 74)]
    [InlineData(true, 150, 302)] // a Grid cell: the columns share the width, 304 each here
    public void DockedAndGrid_Item_HasTheBorderedLook(bool expanded, double size, double width)
    {
        WithShownGallery(expanded, (_, _, view, panel) =>
        {
            var item = ItemAt(panel, 0);
            var contentPanel = ContentPanelOf(item);

            Assert.DoesNotContain("tiles", view.Classes);
            Assert.Equal(size, panel.ItemHeight);
            Assert.True(double.IsNaN(item.Width));
            Assert.Equal(size, item.Height);
            Assert.Equal(new Thickness(1), item.Margin);
            Assert.Equal(new Thickness(1), item.BorderThickness);
            Assert.Equal(new CornerRadius(expanded ? 6 : 0), item.CornerRadius);
            Assert.Equal(ResourceColor("GalleryItemBackgroundColor"), ColorOf(item.Background));
            Assert.Equal(ResourceColor("GalleryItemBorderColor"), ColorOf(item.BorderBrush));
            // The container is as wide as the item is high when docked, and a Grid cell when expanded: 1 px free left
            // and right, and the fixed height fills it
            Assert.Equal(new Rect(1, 0, width, size), item.Bounds);
            // The thumbnail's panel is 2 px inside the item: the border, then 1 px of space
            Assert.Equal(new Point(2, 2), contentPanel.TranslatePoint(default, item));
            Assert.Equal(new Size(width - 4, size - 4), contentPanel.Bounds.Size);
            // The thumbnail and the motion photo badge and nothing more, even for the current item (index 0)
            Assert.Equal(2, contentPanel.Children.Count);
            Assert.Equal(8, item.GetVisualDescendants().Count());
            Assert.Equal(expanded, TemplateBorderOf(item).ClipToBounds);
        });
    }

    /// <summary>
    /// Times realizing Docked Gallery containers: each round jumps 200 items, so every container is new.
    /// It asserts no time, so it is skipped: remove Skip, run it in Release and read the line it writes.
    /// Results (Release, microseconds per container, median of five runs, then the five):
    /// before the look moved to styles: 1946.1 (1977.1, 1946.1, 1790.6, 2026.4, 1932.8)
    /// after the base style (Task 12): 1984.5 (2025.8, 1984.5, 1842.6, 1925.3, 2032.7)
    /// after the tile look (Task 13): 1884.3 (2146.0, 1854.1, 1884.3, 2033.0, 1825.0)
    /// </summary>
    [AvaloniaFact(Skip = "Measurement; remove Skip to run by hand in Release")]
    public void Docked_RealizingContainers_ReportsTimePerContainer()
    {
        const int rounds = 50, warmUpRounds = 10, jump = 200, itemCount = 4000;
        WithShownGallery(expanded: false, (tab, window, _, panel) =>
        {
            var perContainer = new List<double>();
            var before = new HashSet<Control>();
            for (var round = 0; round < rounds; round++)
            {
                before.Clear();
                foreach (var realized in panel.RealizedItems)
                {
                    before.Add(realized.Element);
                }

                var started = Stopwatch.GetTimestamp();
                tab.NavigationIndex.Value = (round + 1) * jump % itemCount;
                RenderFrame(window);
                var elapsed = Stopwatch.GetElapsedTime(started);

                var created = panel.RealizedItems.Count(x => !before.Contains(x.Element));
                Assert.True(created > 0, $"Round {round} realized no new container");
                if (round >= warmUpRounds)
                {
                    perContainer.Add(elapsed.TotalMicroseconds / created);
                }
            }

            perContainer.Sort();
            _output.WriteLine($"Docked realization: {perContainer[perContainer.Count / 2]:F1} us per container");
        }, itemCount: itemCount);
    }

    [AvaloniaFact]
    public void ViewStyle_SelectingGalleryItemsByClass_OverridesTheItemsLook()
    {
        WithShownGallery(expanded: false, (_, _, _, panel) =>
        {
            var item = ItemAt(panel, 0);

            Assert.Contains("galleryItem", item.Classes);
            Assert.Equal(new Thickness(7), item.Margin);
            Assert.True(double.IsNaN(item.Height));
        }, beforeShow: view => view.Styles.Add(new Style(x => x.OfType<NavigateAbleItem>().Class("galleryItem"))
        {
            Setters =
            {
                new Setter(Layoutable.MarginProperty, new Thickness(7)),
                new Setter(Layoutable.HeightProperty, double.NaN)
            }
        }));
    }

    [AvaloniaFact]
    public void Masonry_Item_IsARoundedBorderlessTileFilledByItsThumbnail()
    {
        WithMasonry((_, _, view, panel) =>
        {
            var container = panel.RealizedItems.Single(x => x.Index == 0).Element;
            var item = ItemAt(panel, 0);
            var image = item.FindControl<ThumbImage>("GalleryImage")!;
            var border = TemplateBorderOf(item);

            Assert.Contains("tiles", view.Classes);
            Assert.True(item.IsTile);
            Assert.True(double.IsNaN(item.Width));
            Assert.True(double.IsNaN(item.Height));
            Assert.Equal(new Thickness(0), item.Margin);
            Assert.Equal(new Thickness(0), item.Padding);
            Assert.Equal(new Thickness(0), item.BorderThickness);
            Assert.Equal(new CornerRadius(8), item.CornerRadius);
            Assert.Equal(new CornerRadius(8), border.CornerRadius);
            Assert.Equal(new Thickness(0), border.BorderThickness);
            Assert.True(border.ClipToBounds);
            // The item is exactly where the panel placed it, and the thumbnail covers all of it
            Assert.Equal(panel.GetItemBounds(0)!.Value, container.Bounds);
            Assert.Equal(new Rect(container.Bounds.Size), item.Bounds);
            Assert.Equal(Stretch.UniformToFill, image.Stretch);
            Assert.Equal(item.Bounds.Size, image.Bounds.Size);
            // The thumbnail was there when the item was realized, so it shows at once
            Assert.Null(image.Transitions);
        }, items => items[0].Image.Value = Thumbnail());
    }

    [AvaloniaFact]
    public void Masonry_MarkedItems_ShowARingAndTheOthersNone()
    {
        WithMasonry((tab, window, view, panel) =>
        {
            var (first, second, third) = (ItemAt(panel, 0), ItemAt(panel, 1), ItemAt(panel, 2));
            var accent = ResourceColor("AccentColor");
            var secondary = ResourceColor("SecondaryAccentColor");
            var thumbnailPanel = ContentPanelOf(third).Bounds;

            // Index 0 is current: its ring lies over the thumbnail
            var ring = Assert.IsType<Border>(RingOf(first));
            Assert.Same(ring, ContentPanelOf(first).Children[^1]);
            Assert.Equal(new Thickness(2), ring.BorderThickness);
            Assert.Equal(new CornerRadius(8), ring.CornerRadius);
            Assert.False(ring.IsHitTestVisible);
            Assert.Equal(accent, ColorOf(ring.BorderBrush));
            Assert.Null(RingOf(second));
            Assert.Equal(2, ContentPanelOf(second).Children.Count);

            // Selected, and with its context menu open: the secondary accent, on the tile's edge, thumbnail unmoved
            tab.Gallery.SelectedGalleryItemIndex.Value = 1;
            third.SetContextMenuOpen(true);
            RenderFrame(window);
            Assert.Equal(secondary, ColorOf(RingOf(second)!.BorderBrush));
            Assert.Equal(secondary, ColorOf(RingOf(third)!.BorderBrush));
            Assert.Equal(new Rect(third.Bounds.Size), RingOf(third)!.Bounds);
            Assert.Equal(new Thickness(0), TemplateBorderOf(third).BorderThickness);
            Assert.Equal(thumbnailPanel, ContentPanelOf(third).Bounds);

            // Current wins over selected, and the partner of a pair is current too
            tab.Gallery.SelectedGalleryItemIndex.Value = 0;
            view.FindControl<NavigateAbleItemsViewer>("GalleryItemsControl")!.PartnerItemIndex = 1;
            Assert.Equal(accent, ColorOf(RingOf(first)!.BorderBrush));
            Assert.Equal(accent, ColorOf(RingOf(second)!.BorderBrush));

            // No mark left: the ring is removed, not hidden
            third.SetContextMenuOpen(false);
            Assert.Null(RingOf(third));
            Assert.Equal(2, ContentPanelOf(third).Children.Count);
        });
    }

    // The headless window opens no popup, so the tests keep the menu from opening and raise its events themselves
    [AvaloniaFact]
    public void RightClick_OnOneItemThenAnother_GivesTheViewsOneMenuToTheItemClicked()
    {
        WithMasonry((_, window, view, panel) =>
        {
            var (first, second) = (ItemAt(panel, 0), ItemAt(panel, 1));
            // An item builds no menu of its own
            Assert.Null(first.ContextMenu);
            Assert.Empty(first.Resources);

            RightClickWithoutOpening(window, first);

            var menu = Assert.IsType<GalleryItemContextMenu>(first.ContextMenu);
            Assert.Same(view.ItemContextMenu, menu);
            menu.RaiseEvent(new RoutedEventArgs(ContextMenu.OpenedEvent));
            Assert.Contains(":contextMenuOpen", first.Classes);
            Assert.DoesNotContain(":contextMenuOpen", second.Classes);
            menu.RaiseEvent(new RoutedEventArgs(ContextMenu.ClosedEvent));
            Assert.DoesNotContain(":contextMenuOpen", first.Classes);

            RightClickWithoutOpening(window, second);

            Assert.Same(menu, second.ContextMenu);
            Assert.Null(first.ContextMenu);
            menu.RaiseEvent(new RoutedEventArgs(ContextMenu.OpenedEvent));
            Assert.Contains(":contextMenuOpen", second.Classes);
            Assert.DoesNotContain(":contextMenuOpen", first.Classes);
        });
    }

    [AvaloniaFact]
    public void ItemWithTheMenuOpen_RemovedFromTheGallery_GivesTheMenuBack()
    {
        WithMasonry((tab, window, _, panel) =>
        {
            var (first, second) = (ItemAt(panel, 0), ItemAt(panel, 1));
            RightClickWithoutOpening(window, first);
            var menu = Assert.IsType<GalleryItemContextMenu>(first.ContextMenu);
            menu.RaiseEvent(new RoutedEventArgs(ContextMenu.OpenedEvent));

            tab.Gallery.GalleryItems.RemoveAt(0);
            Dispatcher.UIThread.RunJobs();
            RenderFrame(window);

            Assert.Null(first.ContextMenu);
            Assert.DoesNotContain(":contextMenuOpen", first.Classes);
            // The menu no longer answers for the item that left
            menu.RaiseEvent(new RoutedEventArgs(ContextMenu.OpenedEvent));
            Assert.DoesNotContain(":contextMenuOpen", first.Classes);
            Assert.DoesNotContain(":contextMenuOpen", second.Classes);
        });
    }

    [AvaloniaFact]
    public void TilesClassRemovedAndAdded_RealizedItems_FollowAtOnce()
    {
        WithMasonry((_, window, view, panel) =>
        {
            var item = ItemAt(panel, 0);

            view.Classes.Remove("tiles");
            RenderFrame(window);

            Assert.Same(item, ItemAt(panel, 0));
            Assert.False(item.IsTile);
            Assert.Equal(new Thickness(1), item.Margin);
            Assert.Equal(new Thickness(1), item.Padding);
            Assert.Equal(new Thickness(1), item.BorderThickness);
            Assert.Equal(new CornerRadius(0), item.CornerRadius);
            Assert.Equal(panel.ItemHeight, item.Height);
            Assert.False(TemplateBorderOf(item).ClipToBounds);
            Assert.Null(RingOf(item));
            Assert.Equal(8, item.GetVisualDescendants().Count());

            view.Classes.Add("tiles");
            RenderFrame(window);

            Assert.True(double.IsNaN(item.Height));
            Assert.Equal(new CornerRadius(8), item.CornerRadius);
            Assert.NotNull(RingOf(item));
        });
    }

    [AvaloniaTheory]
    [InlineData(true, GalleryDisplayMode.Masonry, true)]
    [InlineData(true, GalleryDisplayMode.Rows, true)]
    [InlineData(true, GalleryDisplayMode.Grid, false)]
    [InlineData(false, GalleryDisplayMode.Masonry, false)]
    public void ThumbnailArrivingAfterTheItemIsShown_FadesInOnlyInTiles(bool expanded, GalleryDisplayMode displayMode,
        bool fades)
    {
        WithShownGallery(expanded, (_, _, _, panel) =>
        {
            var item = ItemAt(panel, 1);
            var image = item.FindControl<ThumbImage>("GalleryImage")!;

            ((GalleryItemViewModel)item.DataContext!).Image.Value = Thumbnail();
            Dispatcher.UIThread.RunJobs();

            Assert.NotNull(image.Source);
            if (!fades)
            {
                Assert.Null(image.Transitions);
                return;
            }

            Assert.Collection(image.Transitions!,
                x =>
                {
                    var opacity = Assert.IsType<DoubleTransition>(x);
                    Assert.Equal(Visual.OpacityProperty, opacity.Property);
                    Assert.Equal(TimeSpan.FromSeconds(0.32), opacity.Duration);
                    Assert.IsType<CubicEaseOut>(opacity.Easing);
                },
                x =>
                {
                    var scale = Assert.IsType<TransformOperationsTransition>(x);
                    Assert.Equal(Visual.RenderTransformProperty, scale.Property);
                    Assert.Equal(TimeSpan.FromSeconds(0.36), scale.Duration);
                    Assert.IsType<CubicEaseOut>(scale.Easing);
                });
        }, displayMode: displayMode);
    }

    private static void WithMasonry(Action<TabViewModel, Window, GalleryView, VirtualizingGallery> test,
        Action<GalleryItemViewModel[]>? prepareItems = null) =>
        WithShownGallery(expanded: true, test, displayMode: GalleryDisplayMode.Masonry, prepareItems: prepareItems);

    private static WriteableBitmap Thumbnail() =>
        new(new PixelSize(40, 20), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);

    private static Border? RingOf(GalleryItem item) =>
        ContentPanelOf(item).Children.OfType<Border>().SingleOrDefault(x => x.Name == "TileRing");

    // Shown in a window, with items that have no thumbnails. Docked at the bottom, or the Expanded Gallery reached
    // without animation: expanded and hidden before the view loads, then shown, as when a Single Image ends
    private static void WithShownGallery(bool expanded,
        Action<TabViewModel, Window, GalleryView, VirtualizingGallery> test, Action<GalleryView>? beforeShow = null,
        int itemCount = 3, GalleryDisplayMode displayMode = GalleryDisplayMode.Grid,
        Action<GalleryItemViewModel[]>? prepareItems = null)
    {
        Settings.Gallery.DockPosition = GalleryDockPosition.Bottom;
        Settings.Gallery.ExpandedGalleryDisplayMode = displayMode;
        var core = new CoreViewModel(null, _ => new ImageModel(),
            saveGallerySettings: static () => ValueTask.FromResult(true));
        var window = new TestMainWindow(new NoOpModeAdapter());
        var vm = new MainWindowViewModel(core, core.Translation, core.GlobalSettings!, core.GallerySettings);
        window.DataContext = vm;
        var tab = vm.WindowTabs.ActiveTab.Value;
        tab.Gallery.Initialize(core.GallerySettings);
        if (expanded)
        {
            tab.Gallery.Switcher.ToggleExpanded();
            tab.Gallery.Switcher.SetHidden(true);
        }
        else
        {
            tab.Gallery.Switcher.Start(docked: true);
        }

        var application = Application.Current!;
        var previous = application.DataContext;
        application.DataContext = core;
        try
        {
            var view = new GalleryView { DataContext = tab };
            view.Initialize(tab);
            beforeShow?.Invoke(view);
            var items = Enumerable.Range(0, itemCount)
                .Select(i => new GalleryItemViewModel { FileInfo = new FileInfo($"{i:D4}.jpg") }).ToArray();
            prepareItems?.Invoke(items);
            tab.Gallery.GalleryItems.AddRange(items);
            window.Content = new DockPanel { Children = { view } };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            RenderFrame(window);
            tab.Gallery.Switcher.SetHidden(false);
            Dispatcher.UIThread.RunJobs();

            var panel = (VirtualizingGallery)view.FindControl<NavigateAbleItemsViewer>("GalleryItemsControl")!
                .ItemsPanelRoot!;
            for (var frame = 0; frame < 10 && panel.RealizedItems.Count is 0; frame++)
            {
                RenderFrame(window);
            }

            test(tab, window, view, panel);
        }
        finally
        {
            window.Close();
            application.DataContext = previous;
        }
    }

    private static GalleryItem ItemAt(VirtualizingGallery panel, int index) => Assert.IsType<GalleryItem>(
        Assert.IsType<ContentPresenter>(panel.RealizedItems.Single(x => x.Index == index).Element).Child);

    private static Panel ContentPanelOf(GalleryItem item) =>
        Assert.IsAssignableFrom<Panel>(item.FindControl<ThumbImage>("GalleryImage")!.Parent);

    private static Border TemplateBorderOf(GalleryItem item) =>
        item.GetVisualDescendants().OfType<Border>().First(x => x.Name == "border");

    private static Color ColorOf(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

    // The accent resources start as colors and become brushes once the color theme is applied
    private static Color ResourceColor(string key)
    {
        var application = Application.Current!;
        Assert.True(application.TryGetResource(key, application.ActualThemeVariant, out var value));
        return value switch
        {
            Color color => color,
            ISolidColorBrush brush => brush.Color,
            _ => throw new InvalidOperationException($"{key} is neither a color nor a solid brush")
        };
    }

    // The release frees the pointer for the next press. Its request for the menu is answered before the menu hears it
    private static void RightClickWithoutOpening(Window window, GalleryItem item)
    {
        EventHandler<ContextRequestedEventArgs> answer = (_, e) => e.Handled = true;
        window.AddHandler(Control.ContextRequestedEvent, answer, RoutingStrategies.Tunnel);
        try
        {
            var point = item.TranslatePoint(new Point(item.Bounds.Width / 2, item.Bounds.Height / 2), window)!.Value;
            window.MouseDown(point, MouseButton.Right);
            window.MouseUp(point, MouseButton.Right);
            Dispatcher.UIThread.RunJobs();
        }
        finally
        {
            window.RemoveHandler(Control.ContextRequestedEvent, answer);
        }
    }

    private static void RenderFrame(Window window)
    {
        window.InvalidateVisual();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
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
