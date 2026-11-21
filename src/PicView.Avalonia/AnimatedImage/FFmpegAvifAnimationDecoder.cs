using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PicView.Core.DebugTools;
using PicView.Core.ImageDecoding;

namespace PicView.Avalonia.AnimatedImage;

/// <summary>
///     Decodes an animated AVIF as it plays, with the bundled picview-ffmpeg library, so nothing is decoded up front.
///     The frame durations come from the file's container.
///     <para>
///         A worker thread decodes a few frames ahead of the one being shown and starts the video over when it ends,
///         so drawing a frame in playing order only copies it. Any other frame is waited for: the video only goes
///         forward, so the worker decodes its way there, from the start when the frame lies behind.
///     </para>
/// </summary>
internal sealed unsafe class FFmpegAvifAnimationDecoder : IAnimationDecoder
{
    private const int BytesPerPixel = 4;

    // One buffer holds the frame being shown; the others are decoded ahead of it. Frames differ a lot in how long they
    // take, so the ones ahead are what keeps a slow one from being waited for.
    private const int MaxBuffers = 8;
    private const int MinBuffers = 2;
    private const long MaxBufferBytes = 128L * 1024 * 1024;

    // How long a frame is waited for while the worker decodes nothing at all
    private static readonly TimeSpan StallTimeout = TimeSpan.FromSeconds(2);

    private readonly int _capacity;
    private readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Stack<IntPtr> _free = new();
    private readonly object _gate = new();
    private readonly Queue<Frame> _ready = new();
    private readonly IVideoFrameSource _source;

    // Everything below is guarded by _gate

    // The frames from here on could not be decoded: the video ends before the container says it does
    private int _brokenFrom;
    private Frame _current;

    // The index of the frame the source gives next
    private int _position;

    // The frame the worker is asked to decode its way to, throwing away the ones before it. -1 when there is none
    private int _skipTo = -1;
    private bool _stopping;

    /// <summary>
    ///     Takes ownership of <paramref name="source" /> and waits for the first frame.
    /// </summary>
    /// <exception cref="InvalidDataException">The source gives no first frame.</exception>
    internal FFmpegAvifAnimationDecoder(IVideoFrameSource source, IReadOnlyList<TimeSpan> frameDurations)
    {
        _source = source;
        FrameDurations = frameDurations;
        Size = source.Size;
        _brokenFrom = frameDurations.Count;
        try
        {
            _capacity = checked(Size.Width * Size.Height * BytesPerPixel);
            var buffers = (int)Math.Clamp(MaxBufferBytes / Math.Max(1, _capacity), MinBuffers, MaxBuffers);
            for (var i = 0; i < buffers; i++)
            {
                _free.Push((IntPtr)NativeMemory.Alloc((nuint)_capacity));
            }
        }
        catch
        {
            Release();
            throw;
        }

        // From here on the worker is the one that releases the source and the buffers
        new Thread(Work) { IsBackground = true, Name = nameof(FFmpegAvifAnimationDecoder) }.Start();
        if (frameDurations.Count is 0)
        {
            return;
        }

        try
        {
            lock (_gate)
            {
                WaitFor(0);
            }
        }
        catch
        {
            // The worker reads the source, so it has to have ended before another decoder is tried on the file
            Dispose();
            _completion.Task.Wait();
            throw;
        }
    }

    public PixelSize Size { get; }

    // The alpha plane of an AVIF is a track of its own, which is not decoded. TryCreate leaves those files alone.
    public AlphaFormat AlphaFormat => AlphaFormat.Opaque;

    public IReadOnlyList<TimeSpan> FrameDurations { get; }

    /// <summary>
    ///     Completes when the worker has ended after <see cref="Dispose" /> and released the source.
    /// </summary>
    internal Task Completion => _completion.Task;

    /// <summary>
    ///     Opens the animated AVIF in <paramref name="stream" />. The caller keeps ownership of the stream, which is
    ///     read from the worker thread until the decoder is disposed.
    /// </summary>
    /// <returns>
    ///     Null when this decoder is not the one for the file, and another has to take it: the library is missing or
    ///     has no AV1 decoder, the container does not list the frames, or the file has an alpha plane.
    /// </returns>
    public static FFmpegAvifAnimationDecoder? TryCreate(Stream stream)
    {
        try
        {
            var durations = IsoMediaSequence.ReadFrameDurations(stream);
            if (durations is null || IsoMediaSequence.HasAuxiliaryTrack(stream))
            {
                return null;
            }

            var source = FFmpegFrameSource.TryOpen(stream);
            // The constructor releases the source itself when it throws
            return source is null ? null : new FFmpegAvifAnimationDecoder(source, durations);
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(FFmpegAvifAnimationDecoder), nameof(TryCreate), e);
            return null;
        }
    }

    public void RenderFrame(int index, WriteableBitmap target)
    {
        if (index < 0 || index >= FrameDurations.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, "The frame does not exist.");
        }

        // The copy is made under the lock, which keeps the worker from releasing the buffer midway
        lock (_gate)
        {
            WaitFor(index);

            using var frameBuffer = target.Lock();

            var width = Math.Min(_current.Size.Width, frameBuffer.Size.Width);
            var height = Math.Min(_current.Size.Height, frameBuffer.Size.Height);
            var sourceStride = _current.Size.Width * BytesPerPixel;
            var source = (byte*)_current.Buffer;
            var destination = (byte*)frameBuffer.Address;
            for (var y = 0; y < height; y++)
            {
                Buffer.MemoryCopy(
                    source + y * sourceStride,
                    destination + y * frameBuffer.RowBytes,
                    frameBuffer.RowBytes,
                    width * BytesPerPixel);
            }
        }
    }

    /// <summary>
    ///     Tells the worker to end. It releases the source and the buffers once the frame it is decoding is done, so
    ///     this does not wait for it.
    /// </summary>
    public void Dispose()
    {
        lock (_gate)
        {
            _stopping = true;
            Monitor.PulseAll(_gate);
        }
    }

    /// <summary>
    ///     Makes the frame at <paramref name="index" /> the current one, waiting for the worker when it has not decoded
    ///     it yet. Called with the lock held.
    /// </summary>
    private void WaitFor(int index)
    {
        while (true)
        {
            ObjectDisposedException.ThrowIf(_stopping, this);
            if (_current.Buffer != IntPtr.Zero && _current.Index == index)
            {
                return;
            }

            if (index >= _brokenFrom)
            {
                throw new InvalidDataException($"Could not decode frame {index} of {FrameDurations.Count}.");
            }

            if (TryTakeReady(index))
            {
                Monitor.PulseAll(_gate);
                return;
            }

            var isNext = _ready.Count is 0 && _skipTo < 0 && _position == index;
            if (!isNext && _skipTo != index)
            {
                // Not among the frames decoded ahead: they are of no use, and the worker goes for this one
                while (_ready.TryDequeue(out var skipped))
                {
                    _free.Push(skipped.Buffer);
                }

                _skipTo = index;
                Monitor.PulseAll(_gate);
            }

            // The worker pulses after every frame, so this only runs out when it is stuck
            if (!Monitor.Wait(_gate, StallTimeout))
            {
                throw new InvalidDataException($"Timed out decoding frame {index} of {FrameDurations.Count}.");
            }
        }
    }

    /// <summary>
    ///     Takes the frame out of the ones decoded ahead, and gives the buffers of those before it back.
    /// </summary>
    private bool TryTakeReady(int index)
    {
        var found = false;
        foreach (var frame in _ready)
        {
            if (frame.Index == index)
            {
                found = true;
                break;
            }
        }

        if (!found)
        {
            return false;
        }

        if (_current.Buffer != IntPtr.Zero)
        {
            _free.Push(_current.Buffer);
        }

        while (true)
        {
            var frame = _ready.Dequeue();
            if (frame.Index == index)
            {
                _current = frame;
                return true;
            }

            _free.Push(frame.Buffer);
        }
    }

    /// <summary>
    ///     The worker: decodes the frames one after the other into the free buffers, round and round, and waits when
    ///     there is no buffer left.
    /// </summary>
    private void Work()
    {
        try
        {
            while (true)
            {
                IntPtr buffer;
                bool restart;
                lock (_gate)
                {
                    while (!_stopping && (_free.Count is 0 || _brokenFrom is 0))
                    {
                        Monitor.Wait(_gate);
                    }

                    if (_stopping)
                    {
                        return;
                    }

                    // At the end of the video, or when asked for a frame that lies behind
                    restart = _position >= _brokenFrom || (_skipTo >= 0 && _skipTo < _position);
                    buffer = _free.Pop();
                }

                var restarted = !restart || _source.Restart();
                var size = default(PixelSize);
                var decoded = restarted && _source.TryDecodeNext(buffer, _capacity, out size);

                lock (_gate)
                {
                    if (restart)
                    {
                        _position = 0;
                    }

                    if (!decoded)
                    {
                        _free.Push(buffer);
                        // Zero when not even the first frame came, which leaves nothing to play
                        _brokenFrom = _position;
                        _skipTo = -1;
                    }
                    else
                    {
                        var index = _position++;
                        if (_skipTo >= 0 && index != _skipTo)
                        {
                            _free.Push(buffer);
                        }
                        else
                        {
                            _skipTo = -1;
                            _ready.Enqueue(new Frame(index, buffer, size));
                        }
                    }

                    Monitor.PulseAll(_gate);
                }
            }
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(FFmpegAvifAnimationDecoder), nameof(Work), e);
        }
        finally
        {
            lock (_gate)
            {
                _brokenFrom = 0;
                Release();
                Monitor.PulseAll(_gate);
            }

            _completion.TrySetResult();
        }
    }

    private void Release()
    {
        _source.Dispose();
        while (_free.TryPop(out var buffer))
        {
            NativeMemory.Free((void*)buffer);
        }

        while (_ready.TryDequeue(out var frame))
        {
            NativeMemory.Free((void*)frame.Buffer);
        }

        NativeMemory.Free((void*)_current.Buffer);
        _current = default;
    }

    [StructLayout(LayoutKind.Auto)]
    private readonly record struct Frame(int Index, IntPtr Buffer, PixelSize Size);
}
