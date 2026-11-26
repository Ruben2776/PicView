using Avalonia;
using Avalonia.Layout;
using BenchmarkDotNet.Attributes;
using PicView.Avalonia.CustomControls;
using PicView.Core.Gallery;
using PicView.Core.ViewModels;

namespace PicView.Benchmarks.Gallery;

/// <summary>
/// What one measure of the gallery panel spends placing every item: <see cref="GalleryLayout.Calculate"/> for the
/// Docked Gallery's strip and for each Gallery Display Mode of the Expanded Gallery, at 150% scaling and the
/// default item sizes and spacings.
/// </summary>
[MemoryDiagnoser]
public class GalleryLayoutBenchmarks
{
    private static readonly GalleryLayoutOptions DockedOptions = new(Orientation.Horizontal, IsExpanded: false,
        ItemHeight: GalleryDefaults.DefaultDockedGalleryHeight, ItemWidth: double.NaN, ItemSpacing: 0,
        LineSpacing: 0, LayoutScale: 1.5);

    private static readonly GalleryLayoutOptions GridOptions = new(Orientation.Vertical, IsExpanded: true,
        ItemHeight: GalleryDefaults.DefaultExpandedGalleryHeight, ItemWidth: double.NaN, ItemSpacing: 25,
        LineSpacing: 20, LayoutScale: 1.5);

    private static readonly GalleryLayoutOptions MasonryOptions =
        GridOptions with { DisplayMode = GalleryDisplayMode.Masonry };

    private static readonly GalleryLayoutOptions RowsOptions =
        GridOptions with { DisplayMode = GalleryDisplayMode.Rows };

    private static readonly Size DockedSize = new(1920, GalleryDefaults.DefaultDockedGalleryHeight);
    private static readonly Size ExpandedSize = new(1890, 1000);

    private readonly GalleryLayout _layout = new();
    private object?[] _items = [];

    [Params(2700, 10000)]
    public int Items { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // Sizes from 200 to 3990 a side, the same every run, with ratios past both ends of the tile clamp
        _items = new object?[Items];
        for (var i = 0; i < Items; i++)
        {
            var item = new GalleryItemViewModel();
            item.SetDetails(new GalleryFileDetails((uint)(200 + i * 37 % 380 * 10),
                (uint)(200 + i * 91 % 380 * 10)));
            _items[i] = item;
        }
    }

    /// <summary>The horizontal Docked Gallery's strip, which can be shown at startup.</summary>
    [Benchmark]
    public Size Docked()
    {
        _layout.Calculate(_items, DockedOptions, DockedSize);
        return _layout.Extent;
    }

    /// <summary>The Expanded Gallery in Grid.</summary>
    [Benchmark]
    public Size Grid()
    {
        _layout.Calculate(_items, GridOptions, ExpandedSize);
        return _layout.Extent;
    }

    /// <summary>The Expanded Gallery in Masonry, every item placed, as on the first measure or at a new width.</summary>
    [Benchmark]
    public Size Masonry()
    {
        _layout.Clear();
        _layout.Calculate(_items, MasonryOptions, ExpandedSize);
        return _layout.Extent;
    }

    /// <summary>The Expanded Gallery in Rows, every item placed, as on the first measure or at a new width.</summary>
    [Benchmark]
    public Size Rows()
    {
        _layout.Clear();
        _layout.Calculate(_items, RowsOptions, ExpandedSize);
        return _layout.Extent;
    }

    /// <summary>Masonry measured again with nothing changed, as at each scroll step: the placement is kept.</summary>
    [Benchmark]
    public Size MasonryScrolled()
    {
        _layout.Calculate(_items, MasonryOptions, ExpandedSize);
        return _layout.Extent;
    }

    /// <summary>
    ///     Masonry measured again after a batch of details arrived that changed no height: every item is read, and
    ///     none is placed again.
    /// </summary>
    [Benchmark]
    public Size MasonryDetailsArrived()
    {
        _layout.InvalidateItems();
        _layout.Calculate(_items, MasonryOptions, ExpandedSize);
        return _layout.Extent;
    }
}
