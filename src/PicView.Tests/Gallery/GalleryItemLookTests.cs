using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Interfaces;
using PicView.Avalonia.UI;
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

    // Docked at the bottom (item size 76) and Expanded Columns (150), as they look before anything moves
    [AvaloniaTheory]
    [InlineData(false, 76, 74)]
    [InlineData(true, 150, 148)]
    public void DockedAndColumns_Item_HasTheBorderedLook(bool expanded, double size, double width)
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
            Assert.Equal(new CornerRadius(0), item.CornerRadius);
            Assert.Equal(ResourceColor("GalleryItemBackgroundColor"), ColorOf(item.Background));
            Assert.Equal(ResourceColor("GalleryItemBorderColor"), ColorOf(item.BorderBrush));
            // The container is size x size: 1 px free left and right, and the fixed height fills it
            Assert.Equal(new Rect(1, 0, width, size), item.Bounds);
            // The thumbnail's panel is 2 px inside the item: the border, then 1 px of space
            Assert.Equal(new Point(2, 2), contentPanel.TranslatePoint(default, item));
            Assert.Equal(new Size(width - 4, size - 4), contentPanel.Bounds.Size);
            // The thumbnail and the motion photo badge and nothing more, even for the current item (index 0)
            Assert.Equal(2, contentPanel.Children.Count);
            Assert.Equal(8, item.GetVisualDescendants().Count());
            Assert.False(TemplateBorderOf(item).ClipToBounds);
        });
    }

    /// <summary>
    /// Times realizing Docked Gallery containers: each round jumps 200 items, so every container is new.
    /// It asserts no time. Run it in Release and read the line it writes.
    /// Results (Release, microseconds per container, median of five runs, then the five):
    /// before the look moved to styles: 1946.1 (1977.1, 1946.1, 1790.6, 2026.4, 1932.8)
    /// after the base style (Task 12): 1984.5 (2025.8, 1984.5, 1842.6, 1925.3, 2032.7)
    /// </summary>
    [AvaloniaFact]
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

    // Shown in a window, with items that have no thumbnails. Docked at the bottom, or the Expanded Gallery reached
    // without animation: expanded and hidden before the view loads, then shown, as when a Single Image ends
    private static void WithShownGallery(bool expanded,
        Action<TabViewModel, Window, GalleryView, VirtualizingGallery> test, Action<GalleryView>? beforeShow = null,
        int itemCount = 3)
    {
        Settings.Gallery.DockPosition = GalleryDockPosition.Bottom;
        var core = new CoreViewModel(null, _ => ValueTask.FromResult(new ImageModel()));
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
            tab.Gallery.GalleryItems.AddRange(Enumerable.Range(0, itemCount)
                .Select(i => new GalleryItemViewModel { FileInfo = new FileInfo($"{i:D4}.jpg") }).ToArray());
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
