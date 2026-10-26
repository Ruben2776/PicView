using PicView.Core.ViewModels;
using R3;

namespace PicView.Core.ImageTransformations;

/// <summary>
/// A tab's Orientation, changed only by turning, flipping and resetting it, and what the views bind to it.
/// </summary>
public sealed class TabOrientation : IDisposable
{
    private readonly ReactiveProperty<Orientation> _current = new(Orientation.Upright);

    /// <param name="translation">Supplies the flip label in the current language; without it the label stays empty.</param>
    public TabOrientation(TranslationViewModel? translation)
    {
        Degrees = _current.Select(static o => o.Degrees).ToReadOnlyBindableReactiveProperty();
        ScaleX = _current.Select(static o => o.IsFlipped ? -1d : 1d).ToReadOnlyBindableReactiveProperty();
        IsRotated0 = IsTurned(0);
        IsRotated90 = IsTurned(1);
        IsRotated180 = IsTurned(2);
        IsRotated270 = IsTurned(3);
        FlipLabel = translation is null
            ? new BindableReactiveProperty<string?>()
            : _current.CombineLatest(translation.Flip, translation.UnFlip,
                    static (o, flip, unflip) => o.IsFlipped ? unflip : flip)
                .ToReadOnlyBindableReactiveProperty(StringComparer.Ordinal);
    }

    public ReadOnlyReactiveProperty<Orientation> Current => _current;

    /// <summary>
    /// The clockwise rotation in degrees, for a rotate transform.
    /// </summary>
    public IReadOnlyBindableReactiveProperty<int> Degrees { get; }

    /// <summary>
    /// -1 when flipped, else 1, for the scale transforms that mirror the image and the flip icons.
    /// </summary>
    public IReadOnlyBindableReactiveProperty<double> ScaleX { get; }

    public IReadOnlyBindableReactiveProperty<bool> IsRotated0 { get; }
    public IReadOnlyBindableReactiveProperty<bool> IsRotated90 { get; }
    public IReadOnlyBindableReactiveProperty<bool> IsRotated180 { get; }
    public IReadOnlyBindableReactiveProperty<bool> IsRotated270 { get; }

    /// <summary>
    /// What the Flip Action would do: "Flip" when the image is not flipped, "Unflip" when it is.
    /// </summary>
    public IReadOnlyBindableReactiveProperty<string?> FlipLabel { get; }

    public void TurnRight() => _current.Value = _current.Value.TurnedRight();

    public void TurnLeft() => _current.Value = _current.Value.TurnedLeft();

    /// <param name="quarterTurns">Clockwise quarter turns from upright.</param>
    public void TurnTo(int quarterTurns) => _current.Value = _current.Value.TurnedTo(quarterTurns);

    public void Flip() => _current.Value = _current.Value.FlippedOver();

    /// <summary>
    /// Turns the image upright and unflips it.
    /// </summary>
    public void Reset() => _current.Value = Orientation.Upright;

    public void Dispose()
    {
        Degrees.Dispose();
        ScaleX.Dispose();
        IsRotated0.Dispose();
        IsRotated90.Dispose();
        IsRotated180.Dispose();
        IsRotated270.Dispose();
        FlipLabel.Dispose();
        _current.Dispose();
    }

    private IReadOnlyBindableReactiveProperty<bool> IsTurned(int quarterTurns) =>
        _current.Select(o => o.QuarterTurns == quarterTurns).ToReadOnlyBindableReactiveProperty();
}
