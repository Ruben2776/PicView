namespace PicView.Core.ImageTransformations;

/// <summary>
/// Maps a rectangle between an image as shown (turned clockwise by the Quarter Turns, then mirrored
/// horizontally if flipped) and the unturned source image.
/// </summary>
public static class OrientedRect
{
    /// <summary>
    /// The size of the image as shown: width and height swap for an odd number of Quarter Turns.
    /// </summary>
    public static (uint Width, uint Height) ShownSize(Orientation orientation, uint sourceWidth, uint sourceHeight)
    {
        return orientation.QuarterTurns % 2 == 0 ? (sourceWidth, sourceHeight) : (sourceHeight, sourceWidth);
    }

    /// <summary>
    /// Maps a rectangle in the source image to the same pixels in the image as shown.
    /// </summary>
    public static PixelArea FromSource(PixelArea source, Orientation orientation, uint sourceWidth, uint sourceHeight)
    {
        long x = source.X, y = source.Y, w = source.Width, h = source.Height;
        long frameWidth = sourceWidth, frameHeight = sourceHeight;

        for (var i = 0; i < orientation.QuarterTurns; i++)
        {
            (x, y, w, h) = (frameHeight - y - h, x, h, w);
            (frameWidth, frameHeight) = (frameHeight, frameWidth);
        }

        if (orientation.IsFlipped)
        {
            x = frameWidth - x - w;
        }

        return new PixelArea((int)x, (int)y, (uint)w, (uint)h);
    }

    /// <summary>
    /// Maps a rectangle in the image as shown back to the same pixels in the source image.
    /// </summary>
    public static PixelArea ToSource(PixelArea shown, Orientation orientation, uint sourceWidth, uint sourceHeight)
    {
        long x = shown.X, y = shown.Y, w = shown.Width, h = shown.Height;
        var (shownWidth, shownHeight) = ShownSize(orientation, sourceWidth, sourceHeight);
        long frameWidth = shownWidth, frameHeight = shownHeight;

        if (orientation.IsFlipped)
        {
            x = frameWidth - x - w;
        }

        for (var i = 0; i < orientation.QuarterTurns; i++)
        {
            (x, y, w, h) = (y, frameWidth - x - w, h, w);
            (frameWidth, frameHeight) = (frameHeight, frameWidth);
        }

        return new PixelArea((int)x, (int)y, (uint)w, (uint)h);
    }
}
