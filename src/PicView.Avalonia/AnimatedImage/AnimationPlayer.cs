using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PicView.Core.Animation;
using PicView.Core.DebugTools;

namespace PicView.Avalonia.AnimatedImage;

/// <summary>
///     Owns the stream, decoder and target bitmap of one playback. <see cref="Frame" /> is meant to be called once per
///     composition frame and does not allocate when the frame is unchanged.
/// </summary>
internal sealed class AnimationPlayer : IDisposable
{
    private readonly IAnimationDecoder _decoder;
    private readonly Stream? _stream;
    private WriteableBitmap? _bitmap;
    private int _lastDrawn = -1;
    private int _lastFailed = -1;

    /// <summary>
    ///     Attaches a timeline built from the decoder's durations to the playback, makes the target bitmap and draws the
    ///     playback's current frame (frame 0 for a new playback). Takes ownership of the decoder and stream: when
    ///     anything here throws, including drawing that first frame, it disposes them and rethrows.
    /// </summary>
    public AnimationPlayer(AnimationPlayback playback, IAnimationDecoder decoder, Stream? stream = null)
    {
        Playback = playback;
        _decoder = decoder;
        _stream = stream;
        try
        {
            Size = decoder.Size;
            playback.Attach(new FrameTimeline(decoder.FrameDurations));
            _bitmap = new WriteableBitmap(Size, new Vector(96, 96), PixelFormat.Bgra8888, decoder.AlphaFormat);

            if (playback.Timeline is { Count: > 0 })
            {
                var index = playback.CurrentFrame;
                _decoder.RenderFrame(index, _bitmap);
                _lastDrawn = index;
            }
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public AnimationPlayback Playback { get; }

    public PixelSize Size { get; }

    public bool IsDisposed { get; private set; }

    /// <summary>
    ///     Opens the playback's file and builds its decoder. Disposes the stream when the decoder throws, and rethrows.
    /// </summary>
    public static AnimationPlayer Open(AnimationPlayback playback)
    {
        var stream = new FileStream(playback.File.FullName, FileMode.Open, FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);
        IAnimationDecoder decoder;
        try
        {
            decoder = AnimationDecoders.Create(playback.ImageType, stream);
        }
        catch
        {
            stream.Dispose();
            throw;
        }

        // The constructor disposes the decoder and stream itself when it throws
        return new AnimationPlayer(playback, decoder, stream);
    }

    /// <summary>
    ///     Advances the playback, draws its frame when it differs from the last one drawn and returns the bitmap.
    ///     Returns null once disposed.
    /// </summary>
    public WriteableBitmap? Frame(TimeSpan elapsed)
    {
        if (IsDisposed)
        {
            return null;
        }

        var index = Playback.Advance(elapsed);
        // A frame that failed is not asked for again until the playback has moved off it
        if (index != _lastFailed)
        {
            _lastFailed = -1;
        }

        if (index != _lastDrawn && index != _lastFailed && Playback.Timeline is { Count: > 0 })
        {
            Draw(index);
        }

        return _bitmap;
    }

    public void Dispose()
    {
        if (IsDisposed)
        {
            return;
        }

        IsDisposed = true;
        _decoder.Dispose();
        _stream?.Dispose();
        _bitmap?.Dispose();
        _bitmap = null;
    }

    private void Draw(int index)
    {
        try
        {
            _decoder.RenderFrame(index, _bitmap!);
            _lastDrawn = index;
        }
        catch (Exception e)
        {
            _lastFailed = index;
            DebugHelper.LogDebug(nameof(AnimationPlayer), nameof(Draw), e);
        }
    }
}
