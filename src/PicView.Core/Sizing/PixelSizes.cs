using System.Runtime.InteropServices;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.ViewModels;

namespace PicView.Core.Sizing;

/// <summary>
/// The pixel size of the image a tab shows, of its second image when a side-by-side pair is shown, and the tab's
/// Quarter Turns: what a <see cref="Fit"/> is calculated from, besides the window.
/// </summary>
/// <param name="SecondWidth">The second image of a side-by-side pair, or 0 when a single image is shown.</param>
/// <param name="SecondHeight">The second image of a side-by-side pair, or 0 when a single image is shown.</param>
[StructLayout(LayoutKind.Auto)]
public readonly record struct PixelSizes(
    double Width,
    double Height,
    double SecondWidth = 0,
    double SecondHeight = 0,
    int QuarterTurns = 0)
{
    /// <summary>
    /// What <paramref name="tab"/> shows, or null while a size is not known yet, so the window waits instead of
    /// resizing for a guess and again once the size arrives.
    /// </summary>
    /// <param name="cache">Where a size is found when the tab's model does not have it yet.</param>
    /// <param name="sideBySide">Whether the tab shows its second image beside the first.</param>
    public static PixelSizes? Of(TabViewModel tab, SharedImageCache? cache, bool sideBySide)
    {
        if (SizeOf(tab.Model, cache) is not { } first)
        {
            return null;
        }

        (double Width, double Height) second = default;
        if (sideBySide && tab.SecondaryModel is { } secondary)
        {
            if (SizeOf(secondary, cache) is not { } pair)
            {
                return null;
            }

            second = pair;
        }

        return new PixelSizes(first.Width, first.Height, second.Width, second.Height,
            (int)Math.Round(tab.RotationAngle.CurrentValue / 90d));
    }

    private static (double Width, double Height)? SizeOf(ImageModel model, SharedImageCache? cache)
    {
        if (model.PixelWidth is not 0 && model.PixelHeight is not 0)
        {
            return (model.PixelWidth, model.PixelHeight);
        }

        if (model.FileInfo is not null && cache is not null && cache.TryGetReady(model.FileInfo, out var ready) &&
            ready.PixelWidth is not 0 && ready.PixelHeight is not 0)
        {
            return (ready.PixelWidth, ready.PixelHeight);
        }

        return null;
    }
}
