using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace PicView.Avalonia.AnimatedImage;

/// <summary>
///     Decodes the frames of an animated image. It has no clock: the caller decides which frame to draw and when.
/// </summary>
internal interface IAnimationDecoder : IDisposable
{
    PixelSize Size { get; }

    /// <summary>
    ///     The alpha format the target bitmap must have.
    /// </summary>
    AlphaFormat AlphaFormat { get; }

    /// <summary>
    ///     The frame durations as the file gives them. A duration of zero or less is reported as-is.
    /// </summary>
    IReadOnlyList<TimeSpan> FrameDurations { get; }

    /// <summary>
    ///     Draws the frame into <paramref name="target" />. Any index can be asked for, in any order.
    /// </summary>
    void RenderFrame(int index, WriteableBitmap target);
}
