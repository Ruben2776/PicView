using System.Runtime.InteropServices;
using PicView.Core.Gallery;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.CustomControls;

/// <summary>What the Masonry and Rows placers read. Width and spacings are in DIPs as the panel has them.</summary>
[StructLayout(LayoutKind.Auto)]
internal readonly record struct GalleryPlacementOptions(double Width, double ItemSize, double ItemSpacing,
    double LineSpacing, double Scale)
{
    /// <summary>How far under a whole device pixel a width in DIPs can be and still count as that pixel.</summary>
    internal const double PixelTolerance = 1e-6;

    /// <summary>False for a width of zero, below zero, infinity or NaN: there is nothing to wrap at yet.</summary>
    internal bool HasUsableWidth => double.IsFinite(Width) && Width > 0;

    /// <summary>The whole device pixels the wrap width holds; a part of a pixel is left over.</summary>
    internal double WidthInDevicePixels => Math.Floor(Width * Scale + PixelTolerance);

    /// <summary>A length in DIPs as whole device pixels, rounded as layout rounding rounds it.</summary>
    internal double InDevicePixels(double dips) => Math.Round(dips * Scale);

    /// <summary>Upright width over height, clamped; 1 for an item not read yet or unreadable. Reads Details once.</summary>
    internal static double AspectRatioOf(object? item) =>
        item is GalleryItemViewModel { Details: { PixelWidth: > 0, PixelHeight: > 0 } details }
            ? Math.Clamp((double)details.PixelWidth / details.PixelHeight, GalleryDefaults.MinTileAspectRatio,
                GalleryDefaults.MaxTileAspectRatio)
            : 1;
}
