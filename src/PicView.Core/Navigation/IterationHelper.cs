using PicView.Core.DebugTools;

namespace PicView.Core.Navigation;

public static class IterationHelper
{
    public static (int, bool) GetIteration(int index, int count, NavigateTo navigation, SkipAmount skipAmount)
    {
        if (count is 0)
        {
            return (-1, false);
        }

        var skip = SkipAmountToInt(skipAmount);

        switch (navigation)
        {
            case NavigateTo.Next:
            case NavigateTo.Previous:
                var indexChange = navigation == NavigateTo.Next ? skip : -skip;
                var isReversed = navigation == NavigateTo.Previous;

                if (Settings.UIProperties.Looping)
                {
                    var loopedIndex = (index + indexChange) % count;
                    if (loopedIndex < 0)
                    {
                        loopedIndex += count;
                    }
                    return (loopedIndex, isReversed);
                }

                var newIndex = index + indexChange;
                return (Math.Clamp(newIndex, 0, count - 1), isReversed);

            case NavigateTo.First:
                return (0, true);

            case NavigateTo.Last:
                return (count - 1, false);

            default:
#if DEBUG
                DebugHelper.LogDebug(nameof(IterationHelper), nameof(GetIteration), $"{navigation} is not a valid NavigateTo value.");
#endif
                return (-1, false);
        }
    }

    internal static int SkipAmountToInt(SkipAmount skipAmount)
    {
        return skipAmount switch
        {
            SkipAmount.One => 1,
            SkipAmount.Two => 2,
            SkipAmount.Ten => 10,
            SkipAmount.Hundred => 100,
            SkipAmount.None => 0,
            _ => throw new ArgumentOutOfRangeException(nameof(skipAmount), skipAmount, null)
        };
    }
}
