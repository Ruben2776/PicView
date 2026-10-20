namespace PicView.Core.ImageTransformations;

/// <summary>
/// A tab's Quarter Turn and whether its image is flipped. The default is upright and not flipped.
/// </summary>
public readonly record struct Orientation
{
    /// <summary>
    /// Not turned and not flipped.
    /// </summary>
    public static Orientation Upright => default;

    /// <param name="quarterTurns">Clockwise quarter turns; any number wraps into 0–3.</param>
    /// <param name="isFlipped">Whether the image is mirrored horizontally.</param>
    public Orientation(int quarterTurns, bool isFlipped)
    {
        QuarterTurns = (quarterTurns % 4 + 4) % 4;
        IsFlipped = isFlipped;
    }

    /// <summary>
    /// Clockwise quarter turns, from 0 to 3.
    /// </summary>
    public int QuarterTurns { get; }

    public bool IsFlipped { get; }

    /// <summary>
    /// The clockwise rotation in degrees: 0, 90, 180 or 270.
    /// </summary>
    public int Degrees => QuarterTurns * 90;

    public Orientation TurnedRight() => new(QuarterTurns + 1, IsFlipped);

    public Orientation TurnedLeft() => new(QuarterTurns - 1, IsFlipped);

    public Orientation TurnedTo(int quarterTurns) => new(quarterTurns, IsFlipped);

    public Orientation FlippedOver() => new(QuarterTurns, !IsFlipped);
}
