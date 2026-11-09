using PicView.Core.DebugTools;
using PicView.Core.Extensions;

namespace PicView.Core.Navigation;

/// <summary>
/// The side-by-side pair: which image sits beside which, how paired navigation moves, whether forward navigation is
/// allowed, and where an opened file is in the list. Callers pass the
/// side-by-side and looping settings in, so the rules read no stored settings.
/// </summary>
public static class SideBySidePair
{
    /// <summary>
    /// The pair shown when a tab goes to <paramref name="index"/>: the image there and its partner, the image after it.
    /// Without looping, the last image shows in the pair that ends with it, so the primary is the image before it. With
    /// looping, the last image pairs with the first. Side-by-side off, or a list of one image, shows the image alone.
    /// </summary>
    public static (int Primary, int? Partner) PairAt(int index, int count, bool sideBySide, bool looping)
    {
        if (!sideBySide || count < 2 || index < 0 || index >= count)
        {
            return (index, null);
        }

        if (index < count - 1)
        {
            return (index, index + 1);
        }

        return looping ? (index, 0) : (count - 2, count - 1);
    }

    /// <summary>
    /// Where paired navigation goes from <paramref name="index"/>: the index of the pair it ends on, whose partner
    /// <see cref="PairAt"/> gives. Skips move by pairs, and without looping a step ends on a full pair.
    /// </summary>
    /// <returns>
    /// The index, and whether the navigation is reversed. Returns (-1, false) when the count is zero or the navigation
    /// is not valid.
    /// </returns>
    public static (int Index, bool IsReversed) Navigate(int index, int count, NavigateTo navigation,
        SkipAmount skipAmount, bool looping)
    {
        switch (count)
        {
            case 0:
                return (-1, false);
            case 1:
                return (0, false);
        }

        // Each step moves by a pair, so the skip amount is doubled
        var jump = IterationHelper.SkipAmountToInt(skipAmount) * 2;

        switch (navigation)
        {
            case NavigateTo.Next:
            case NavigateTo.Previous:
                var isReversed = navigation == NavigateTo.Previous;
                var stepped = index + (isReversed ? -jump : jump);

                if (looping)
                {
                    var looped = stepped % count;
                    return (looped < 0 ? looped + count : looped, isReversed);
                }

                // Clamped, so the step ends on a full pair at either end of the list
                return (Math.Clamp(stepped, 0, count - 2), isReversed);

            case NavigateTo.First:
                return (0, true);

            case NavigateTo.Last:
                return (count - 2, false);

            default:
#if DEBUG
                DebugHelper.LogDebug(nameof(SideBySidePair), nameof(Navigate), $"{navigation} is not a valid NavigateTo value.");
#endif
                return (-1, false);
        }
    }

    /// <summary>
    /// Whether forward navigation is allowed from <paramref name="index"/>. A pair needs one more image ahead of it.
    /// </summary>
    public static bool CanNavigateForwards(int index, int count, bool isExtracting, bool sideBySide, bool looping) =>
        looping || isExtracting || index < count - (sideBySide ? 2 : 1);

    /// <summary>
    /// The index of <paramref name="file"/> in <paramref name="files"/>, or null when it is not in the list.
    /// The spelling of an opened path can differ from the listing's on-disk spelling on a case-insensitive volume, so
    /// an exact match is tried first, and only when that finds nothing does a case-insensitive match decide.
    /// </summary>
    public static int? IndexOf(IReadOnlyList<FileInfo> files, FileInfo file)
    {
        var index = files.FindIndex(x => x.FullName.Equals(file.FullName, StringComparison.Ordinal));
        if (index >= 0)
        {
            return index;
        }

        index = files.FindIndex(x => x.FullName.Equals(file.FullName, StringComparison.OrdinalIgnoreCase));
        return index >= 0 ? index : null;
    }
}
