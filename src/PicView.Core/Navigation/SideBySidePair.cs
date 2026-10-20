using PicView.Core.DebugTools;
using PicView.Core.Extensions;

namespace PicView.Core.Navigation;

/// <summary>
/// The side-by-side pair: which image sits beside which, how paired navigation moves, whether forward navigation is
/// allowed, how many images are shown at once, and where an opened file is in the list. Reads the side-by-side and
/// looping settings itself, so callers ask for an outcome instead of reading the settings.
/// </summary>
public static class SideBySidePair
{
    /// <summary>
    /// Whether images are shown side by side.
    /// </summary>
    public static bool IsEnabled => Settings.ImageScaling.ShowImageSideBySide;

    /// <summary>
    /// The index of the image shown beside the one at <paramref name="index"/>, or null when there is none: side-by-side
    /// is off, the index is out of range, or there is no other image to show (a one-file list, or the last image
    /// without looping).
    /// </summary>
    public static int? PartnerOf(int index, int count)
    {
        if (!IsEnabled || index < 0 || index >= count || count < 2)
        {
            return null;
        }

        var next = index + 1;
        if (next < count)
        {
            return next;
        }

        return Settings.UIProperties.Looping ? 0 : null;
    }

    /// <summary>
    /// Where paired navigation goes from <paramref name="index"/>. Skips move by pairs.
    /// </summary>
    /// <returns>
    /// The primary index, the secondary index (null when there is no other image), and whether the navigation is
    /// reversed. Returns (-1, null, false) when the count is zero or the navigation is not valid.
    /// </returns>
    public static (int Primary, int? Secondary, bool IsReversed) Navigate(int index, int count, NavigateTo navigation,
        SkipAmount skipAmount)
    {
        switch (count)
        {
            case 0:
                return (-1, null, false);
            case 1:
                return (0, null, false);
        }

        // Each step moves by a pair, so the skip amount is doubled
        var jump = IterationHelper.SkipAmountToInt(skipAmount) * 2;

        switch (navigation)
        {
            case NavigateTo.Next:
            case NavigateTo.Previous:
                var indexChange = navigation == NavigateTo.Next ? jump : -jump;
                var isReversed = navigation == NavigateTo.Previous;

                if (Settings.UIProperties.Looping)
                {
                    // Calculate the first index with wrap-around logic
                    var looped = (index + indexChange) % count;
                    if (looped < 0)
                    {
                        looped += count;
                    }

                    // The second index is just the next image, also wrapped
                    return (looped, (looped + 1) % count, isReversed);
                }

                // Calculate raw indices without wrapping
                var first = index + indexChange;
                var second = first + 1;

                // Clamp to the beginning of the list if we go too far back
                if (first < 0)
                {
                    return (0, 1, isReversed);
                }

                // Clamp to the end of the list if the second index goes out of bounds
                if (second >= count)
                {
                    return (count - 2, count - 1, isReversed);
                }

                return (first, second, isReversed);

            case NavigateTo.First:
                return (0, 1, true);

            case NavigateTo.Last:
                return (count - 2, count - 1, false);

            default:
#if DEBUG
                DebugHelper.LogDebug(nameof(SideBySidePair), nameof(Navigate), $"{navigation} is not a valid NavigateTo value.");
#endif
                return (-1, null, false);
        }
    }

    /// <summary>
    /// Whether forward navigation is allowed from <paramref name="index"/>. A pair needs one more image ahead of it.
    /// </summary>
    public static bool CanNavigateForwards(int index, int count, bool isExtracting)
    {
        var isLooping = Settings.UIProperties.Looping;
        return isLooping || isExtracting || index < count - (IsEnabled ? 2 : 1);
    }

    /// <summary>
    /// The number of images shown at once from a list of <paramref name="entryCount"/> entries: two when side-by-side
    /// is on and there is more than one entry, otherwise one.
    /// </summary>
    public static int ShownCount(int entryCount) => IsEnabled && entryCount > 1 ? 2 : 1;

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
