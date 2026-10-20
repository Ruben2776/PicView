using Avalonia;
using Avalonia.Media.Imaging;
using Orientation = PicView.Core.ImageTransformations.Orientation;

namespace PicView.Avalonia.ImageTransformations;

/// <summary>
/// Draws a bitmap the way the viewer shows it for an Orientation.
/// </summary>
internal static class OrientedBitmap
{
    /// <summary>
    /// A new bitmap of <paramref name="source"/> turned by <paramref name="orientation"/>.
    /// </summary>
    public static Bitmap Render(Bitmap source, Orientation orientation)
    {
        var size = source.PixelSize;
        var orientedSize = orientation.QuarterTurns % 2 == 0 ? size : new PixelSize(size.Height, size.Width);
        var target = new RenderTargetBitmap(orientedSize, new Vector(96, 96));

        // Turn about the source's centre, mirror the turned image as the view does, then move that centre to the
        // target's centre
        var transform = Matrix.CreateTranslation(-size.Width / 2d, -size.Height / 2d) *
                        Matrix.CreateRotation(Matrix.ToRadians(orientation.Degrees)) *
                        Matrix.CreateScale(orientation.IsFlipped ? -1 : 1, 1) *
                        Matrix.CreateTranslation(orientedSize.Width / 2d, orientedSize.Height / 2d);

        using var context = target.CreateDrawingContext();
        using (context.PushTransform(transform))
        {
            context.DrawImage(source, new Rect(0, 0, size.Width, size.Height));
        }

        return target;
    }
}
