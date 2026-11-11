using Avalonia;

namespace PicView.Avalonia.CustomControls;

/// <summary>
/// How far a view is zoomed and where it looks: its scale, and the point at the centre of the view as a fraction of
/// the image, 0 to 1 on each axis. A tab's viewer and its crop hand this to each other (ADR-0027); a fraction of the
/// image holds though the two lay the image out at different sizes and places.
/// </summary>
public readonly record struct ZoomView(double Scale, double CentreX, double CentreY)
{
    /// <summary>The image as fitted: not zoomed, looking at its centre.</summary>
    public static ZoomView Fit => new(1, 0.5, 0.5);

    /// <summary>
    /// The view of a <see cref="ZoomPanControl"/> at <paramref name="scale"/> whose centre shows
    /// <paramref name="childCentre"/>, when the image lies at <paramref name="imageRect"/> in the same child space.
    /// An image that is not laid out yet gives its centre.
    /// </summary>
    public static ZoomView From(double scale, Point childCentre, Rect imageRect)
    {
        if (imageRect.Width <= 0 || imageRect.Height <= 0)
        {
            return new ZoomView(scale, 0.5, 0.5);
        }

        return new ZoomView(scale, (childCentre.X - imageRect.X) / imageRect.Width,
            (childCentre.Y - imageRect.Y) / imageRect.Height);
    }

    /// <summary>
    /// The child-space point this view looks at when the image lies at <paramref name="imageRect"/>.
    /// </summary>
    public Point ToChildCenter(Rect imageRect) =>
        new(imageRect.X + CentreX * imageRect.Width, imageRect.Y + CentreY * imageRect.Height);
}
