using Avalonia;

namespace PicView.Avalonia.AnimatedImage;

/// <summary>
///     Hands out the frames of a video one after the other, as BGRA pixels. It can only go forward or start over.
/// </summary>
internal interface IVideoFrameSource : IDisposable
{
    /// <summary>
    ///     The size the frames are declared to have. A buffer for <see cref="TryDecodeNext" /> holds at least this many
    ///     pixels of four bytes each.
    /// </summary>
    PixelSize Size { get; }

    /// <summary>
    ///     Decodes the next frame into <paramref name="destination" />, in rows of <c>frameSize.Width * 4</c> bytes.
    /// </summary>
    /// <returns>False at the end of the video or when the frame cannot be decoded.</returns>
    bool TryDecodeNext(IntPtr destination, int capacity, out PixelSize frameSize);

    /// <summary>
    ///     Goes back to before the first frame.
    /// </summary>
    /// <returns>False when the video cannot be opened again; the source then gives no more frames.</returns>
    bool Restart();
}
