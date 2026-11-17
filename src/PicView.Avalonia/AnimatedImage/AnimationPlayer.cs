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

    /// <summary>
    ///     Attaches a timeline built from the decoder's durations to the playback, makes the target bitmap and draws the
    ///     playback's current frame (frame 0 for a new playback).
    /// </summary>
    public AnimationPlayer(AnimationPlayback playback, IAnimationDecoder decoder, Stream? stream = null)
    {
        Playback = playback;
        _decoder = decoder;
        _stream = stream;
        Size = decoder.Size;

        playback.Attach(new FrameTimeline(decoder.FrameDurations));
        _bitmap = new WriteableBitmap(Size, new Vector(96, 96), PixelFormat.Bgra8888, decoder.AlphaFormat);

        if (playback.Timeline is { Count: > 0 })
        {
            Draw(playback.CurrentFrame);
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
        IAnimationDecoder? decoder = null;
        try
        {
            decoder = AnimationDecoders.Create(playback.ImageType, stream);
            return new AnimationPlayer(playback, decoder, stream);
        }
        catch
        {
            decoder?.Dispose();
            stream.Dispose();
            throw;
        }
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
        if (index != _lastDrawn && Playback.Timeline is { Count: > 0 })
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
            DebugHelper.LogDebug(nameof(AnimationPlayer), nameof(Draw), e);
        }
    }
}
