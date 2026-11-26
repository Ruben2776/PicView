using R3;

namespace PicView.Core.Sizing;

/// <summary>
/// A tab's Zoom Level, derived from its two inputs (ADR-0016): the Fit Scale, which the window fitter's Fit writes,
/// and the Zoom Factor, which the shown view's zoom control writes as the user zooms (the image viewer's, the crop's,
/// ADR-0027, or the comparison viewer's, ADR-0032) and the tab resets when it publishes an image
/// while zoom resets on image change. <see cref="Level"/> notifies only when the percent changes, so the Titles that
/// subscribe to it mark once per change and never for a same-percent write.
/// </summary>
public sealed class TabZoom : IDisposable
{
    private readonly ReactiveProperty<int> _level = new(100);
    private readonly ReactiveProperty<int> _factorPercent = new(100);
    private readonly ReactiveProperty<double> _factor = new(1);

    /// <summary>The Fit Scale: pixel size to shown size when fitted. 1 until the first Fit.</summary>
    public double FitScale { get; private set; } = 1;

    /// <summary>The Zoom Factor: how far the user has zoomed from the Fit. 1 as fitted.</summary>
    public double Factor => _factor.Value;

    /// <summary>
    /// The Zoom Factor now and each time it changes, on the thread that changed it. The window fitter follows it, so
    /// a window whose zoom resizes it takes its new size (ADR-0047).
    /// </summary>
    public ReadOnlyReactiveProperty<double> FactorValue => _factor;

    /// <summary>The Zoom Level: the percent of its pixel size the image is shown at.</summary>
    public ReadOnlyReactiveProperty<int> Level => _level;

    /// <summary>The Zoom Factor expressed as an integer percentage (100 when fitted).</summary>
    public ReadOnlyReactiveProperty<int> FactorPercent => _factorPercent;

    /// <summary>The Fit wrote a new Fit Scale.</summary>
    public void SetFitScale(double fitScale)
    {
        FitScale = fitScale;
        Recompute();
    }

    /// <summary>The user zoomed, or the control's zoom was reset.</summary>
    public void SetFactor(double factor)
    {
        _factor.Value = factor;
        Recompute();
    }

    /// <summary>Back to the Fit, as when the tab shows another image.</summary>
    public void ResetFactor() => SetFactor(1);

    // Rounded as the title's percent always was
    private void Recompute()
    {
        _level.Value = Convert.ToInt32(FitScale * Factor * 100);
        _factorPercent.Value = Convert.ToInt32(Factor * 100);
    }

    public void Dispose()
    {
        _level.Dispose();
        _factorPercent.Dispose();
        _factor.Dispose();
    }
}
