using PicView.Core.Config;

namespace PicView.Core.Sizing;

/// <summary>
///     What a docked compare bar takes from the window around it.
/// </summary>
public static class ComparisonBarDocking
{
    /// <summary>
    ///     The room the docked bar takes from the image: its <paramref name="extent"/> as the width at Left or Right
    ///     and as the height at Top or Bottom. Nothing while the bar is not measured.
    /// </summary>
    /// <param name="extent">The bar's measured thickness across its edge.</param>
    public static (double Width, double Height) ReservedBy(ComparisonBarPosition position, double extent)
    {
        if (extent <= 0)
        {
            return (0, 0);
        }

        return position is ComparisonBarPosition.Left or ComparisonBarPosition.Right ? (extent, 0) : (0, extent);
    }
}
