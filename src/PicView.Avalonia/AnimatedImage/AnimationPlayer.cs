using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PicView.Core.Animation;
using PicView.Core.DebugTools;

namespace PicView.Avalonia.AnimatedImage;

/// <summary>
///     Owns the stream, decoder and bitmaps of one playback. <see cref="Frame" /> is meant to be called once per
///     composition frame, on the compositor thread: it never decodes, and does not allocate when the frame is
///     unchanged. The frames are decoded on a thread pool thread into a bitmap of their own and copied into the one
///     that is drawn, so a slow frame is shown late instead of holding up everything else on screen.
/// </summary>
internal sealed class AnimationPlayer : IDisposable
{
    private const int None = -1;

    private readonly Action _decodeRequested;
    private readonly IAnimationDecoder _decoder;
    private readonly Lock _gate = new();
    private readonly Action<Action> _schedule;
    private readonly Stream? _stream;

    // Guarded by _gate
    private int _decoding = None;
    private bool _isDecoderBusy;
    private int _lastDrawn = None;
    private int _lastFailed = None;
    private int _requested = None;

    // The decoder draws into _work, which lets it build a frame on the one before; _present is the one handed out
    private WriteableBitmap? _present;
    private WriteableBitmap? _work;

    /// <summary>
    ///     Attaches a timeline built from the decoder's durations to the playback, makes the bitmaps and draws the
    ///     playback's current frame (frame 0 for a new playback). Takes ownership of the decoder and stream: when
    ///     anything here throws, including drawing that first frame, it disposes them and rethrows.
    /// </summary>
    /// <param name="schedule">Runs the decoding of the frames asked for; the thread pool when null. Tests replace it.</param>
    public AnimationPlayer(AnimationPlayback playback, IAnimationDecoder decoder, Stream? stream = null,
        Action<Action>? schedule = null)
    {
        Playback = playback;
        _decoder = decoder;
        _stream = stream;
        _schedule = schedule ?? (static action => _ = Task.Run(action));
        _decodeRequested = DecodeRequested;
        try
        {
            Size = decoder.Size;
            playback.Attach(new FrameTimeline(decoder.FrameDurations));
            _work = NewBitmap(decoder);
            _present = NewBitmap(decoder);

            if (playback.Timeline is { Count: > 0 })
            {
                var index = playback.CurrentFrame;
                _decoder.RenderFrame(index, _work);
                Present();
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
    ///     Advances the playback and returns the bitmap to draw. When the playback has moved to another frame, that
    ///     frame is asked for and shows in the bitmap once it is decoded; a frame asked for while another is still
    ///     being decoded replaces any that was waiting. Returns null once disposed.
    /// </summary>
    public WriteableBitmap? Frame(TimeSpan elapsed)
    {
        var index = Playback.Advance(elapsed);
        bool start;
        lock (_gate)
        {
            if (IsDisposed)
            {
                return null;
            }

            // A frame that failed is not asked for again until the playback has moved off it
            if (index != _lastFailed)
            {
                _lastFailed = None;
            }

            // The frame being decoded shows next, so the one after it is what decides whether this is still wanted
            var upcoming = _decoding is None ? _lastDrawn : _decoding;
            if (index == upcoming || index == _lastFailed || Playback.Timeline is not { Count: > 0 })
            {
                _requested = None;
                return _present;
            }

            _requested = index;
            start = !_isDecoderBusy;
            _isDecoderBusy = true;
        }

        if (start)
        {
            _schedule(_decodeRequested);
        }

        return _present;
    }

    /// <summary>
    ///     Ends the player. The decoder, stream and bitmaps are released right away, or by the thread that is decoding
    ///     a frame once that frame is done.
    /// </summary>
    public void Dispose()
    {
        lock (_gate)
        {
            if (IsDisposed)
            {
                return;
            }

            IsDisposed = true;
            if (_isDecoderBusy)
            {
                return;
            }
        }

        Release();
    }

    /// <summary>
    ///     Decodes the frame asked for, and the ones asked for meanwhile, until none is waiting.
    /// </summary>
    private void DecodeRequested()
    {
        bool release;
        while (true)
        {
            int index;
            lock (_gate)
            {
                if (IsDisposed || _requested is None)
                {
                    _isDecoderBusy = false;
                    // Dispose left the release to this thread when it found a frame being decoded
                    release = IsDisposed;
                    break;
                }

                index = _requested;
                _requested = None;
                _decoding = index;
            }

            var drawn = false;
            try
            {
                _decoder.RenderFrame(index, _work!);
                Present();
                drawn = true;
            }
            catch (Exception e)
            {
                DebugHelper.LogDebug(nameof(AnimationPlayer), nameof(DecodeRequested), e);
            }

            lock (_gate)
            {
                _decoding = None;
                if (drawn)
                {
                    _lastDrawn = index;
                }
                else
                {
                    _lastFailed = index;
                }
            }
        }

        if (release)
        {
            Release();
        }
    }

    /// <summary>
    ///     Copies the frame the decoder drew into the bitmap that is handed out. That bitmap is locked only for the
    ///     copy, never while a frame is decoded.
    /// </summary>
    private unsafe void Present()
    {
        using var source = _work!.Lock();
        using var destination = _present!.Lock();
        var rowBytes = Math.Min(source.RowBytes, destination.RowBytes);
        var height = Math.Min(source.Size.Height, destination.Size.Height);
        if (source.RowBytes == destination.RowBytes)
        {
            var bytes = (long)rowBytes * height;
            Buffer.MemoryCopy((void*)source.Address, (void*)destination.Address, bytes, bytes);
            return;
        }

        for (var y = 0; y < height; y++)
        {
            Buffer.MemoryCopy(
                (byte*)source.Address + (long)y * source.RowBytes,
                (byte*)destination.Address + (long)y * destination.RowBytes,
                rowBytes,
                rowBytes);
        }
    }

    private void Release()
    {
        _decoder.Dispose();
        _stream?.Dispose();
        _work?.Dispose();
        _work = null;
        _present?.Dispose();
        _present = null;
    }

    private static WriteableBitmap NewBitmap(IAnimationDecoder decoder) =>
        new(decoder.Size, new Vector(96, 96), PixelFormat.Bgra8888, decoder.AlphaFormat);
}
