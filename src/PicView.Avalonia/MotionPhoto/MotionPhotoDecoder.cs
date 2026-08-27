using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using PicView.Core.DebugTools;

namespace PicView.Avalonia.MotionPhoto;

/// <summary>
/// Decodes a motion photo video into BGRA32 frames using the bundled picview-ffmpeg
/// native library. A single worker thread demuxes and decodes frames, paces them
/// against a presentation clock and hands buffer pointers to subscribers via
/// <see cref="FrameReady"/>. The consumer copies the frame (typically into a
/// WriteableBitmap on the UI thread) and then returns the buffer with
/// <see cref="ReleaseBuffer"/>.
/// <para>
/// Lifetime rules: <see cref="FrameReady"/> fires on the worker thread. Disposal must
/// happen on the thread that consumes the frames, which guarantees no frame copy can
/// race with the buffer teardown.
/// </para>
/// </summary>
public sealed class MotionPhotoDecoder : IDisposable
{
    /// <summary>Number of display buffers handed to the consumer in rotation.</summary>
    private const int FrameBufferCount = 3;

    /// <summary>Extra slot decoded into when every display buffer is pending; its frame is dropped.</summary>
    private const int OverflowBufferIndex = FrameBufferCount;

    private readonly Stream _stream;
    private readonly GCHandle _callbackHandle;
    private readonly FFmpegService.PvReadCallback _readCallback;
    private readonly FFmpegService.PvSeekCallback _seekCallback;
    private readonly ConcurrentQueue<int> _freeBuffers = new();
    private readonly ManualResetEventSlim _resumeEvent = new(true);
    private readonly Stopwatch _clock = new();

    private IntPtr _session;
    private IntPtr[] _frameBuffers = [];
    private int _frameBufferSize;
    private Thread? _worker;
    private double _startPts = double.NaN;
    private double _pausedAt;
    private double _pausedTotal;
    private volatile bool _stopRequested;
    private volatile bool _isPaused;
    private bool _disposed;

    public int Width { get; }
    public int Height { get; }
    public bool IsPaused => _isPaused;

    /// <summary>
    /// Raised on the worker thread when a frame is ready for display:
    /// (buffer index, pointer to BGRA32 data, byte count, frame width, frame height).
    /// The dimensions are the frame's actual dimensions, which may differ from
    /// <see cref="Width"/>/<see cref="Height"/> (container metadata can disagree with
    /// the decoded size). The buffer stays valid until <see cref="ReleaseBuffer"/> is called.
    /// </summary>
    public event Action<int, IntPtr, int, int, int>? FrameReady;

    /// <summary>Raised on the worker thread when the end of the video is reached.</summary>
    public event EventHandler? Ended;

    /// <summary>Raised on the worker thread when decoding fails irrecoverably.</summary>
    public event EventHandler? Failed;

    /// <summary>
    /// Opens the video carried by <paramref name="stream"/> (ownership stays with the
    /// caller). Returns null when the stream is not a decodable video.
    /// </summary>
    public static MotionPhotoDecoder? Create(Stream stream)
    {
        if (!FFmpegService.TryInitialize())
        {
            return null;
        }

        var decoder = new MotionPhotoDecoder(stream);
        if (decoder._session == IntPtr.Zero)
        {
            decoder.Dispose();
            return null;
        }

        return decoder;
    }

    private MotionPhotoDecoder(Stream stream)
    {
        _stream = stream;
        _readCallback = OnNativeRead;
        _seekCallback = OnNativeSeek;
        _callbackHandle = GCHandle.Alloc(this);
        var opaque = GCHandle.ToIntPtr(_callbackHandle);

        _session = FFmpegService.PvOpen(opaque, _readCallback, _seekCallback, out var info);
        if (_session == IntPtr.Zero)
        {
            return;
        }

        Width = info.Width;
        Height = info.Height;
        _frameBufferSize = Width * Height * 4;
        _frameBuffers = new IntPtr[FrameBufferCount + 1];
        for (var i = 0; i < _frameBuffers.Length; i++)
        {
            _frameBuffers[i] = Marshal.AllocHGlobal(_frameBufferSize);
            if (i < FrameBufferCount)
            {
                _freeBuffers.Enqueue(i);
            }
        }
    }

    /// <summary>Starts the decode worker. The first frame resets the presentation clock.</summary>
    public void Play()
    {
        if (_worker is not null || _session == IntPtr.Zero || _disposed)
        {
            return;
        }

        _worker = new Thread(WorkerLoop) { IsBackground = true, Name = nameof(MotionPhotoDecoder) };
        _worker.Start();
    }

    public void Pause()
    {
        if (_isPaused || _disposed)
        {
            return;
        }

        _pausedAt = _clock.Elapsed.TotalSeconds;
        _isPaused = true;
        _resumeEvent.Reset();
    }

    public void Resume()
    {
        if (!_isPaused || _disposed)
        {
            return;
        }

        _pausedTotal += _clock.Elapsed.TotalSeconds - _pausedAt;
        _isPaused = false;
        _resumeEvent.Set();
    }

    /// <summary>Returns a display buffer to the rotation after its frame has been copied.</summary>
    public void ReleaseBuffer(int index)
    {
        if (index >= 0 && index < FrameBufferCount)
        {
            _freeBuffers.Enqueue(index);
        }
    }

    private void WorkerLoop()
    {
        try
        {
            while (!_stopRequested)
            {
                if (_isPaused)
                {
                    _resumeEvent.Wait();
                    continue;
                }

                if (!_freeBuffers.TryDequeue(out var index))
                {
                    // Every display buffer is pending on the consumer: decode into the
                    // overflow buffer instead and drop the frame.
                    index = OverflowBufferIndex;
                }

                var written = FFmpegService.PvDecodeNext(_session, _frameBuffers[index], _frameBufferSize, out var pts, out var frameWidth, out var frameHeight);
                if (written <= 0)
                {
                    if (index != OverflowBufferIndex)
                    {
                        _freeBuffers.Enqueue(index);
                    }

                    if (written is 0)
                    {
                        Ended?.Invoke(this, EventArgs.Empty);
                    }
                    else
                    {
                        Failed?.Invoke(this, EventArgs.Empty);
                    }

                    break;
                }

                WaitForPresentationTime(pts);
                if (_stopRequested)
                {
                    break;
                }

                if (index == OverflowBufferIndex)
                {
                    continue;
                }

                FrameReady?.Invoke(index, _frameBuffers[index], written, frameWidth, frameHeight);
            }
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(MotionPhotoDecoder), nameof(WorkerLoop), e);
            Failed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Blocks until the frame's presentation time relative to the first frame.
    /// Pausing freezes the clock; stopping aborts the wait.
    /// </summary>
    private void WaitForPresentationTime(double pts)
    {
        if (double.IsNaN(_startPts))
        {
            _startPts = pts;
            _clock.Restart();
            return;
        }

        var deadline = pts - _startPts;
        while (!_stopRequested)
        {
            if (_isPaused)
            {
                _resumeEvent.Wait();
                continue;
            }

            var remaining = deadline - (_clock.Elapsed.TotalSeconds - _pausedTotal);
            if (remaining <= 0)
            {
                return;
            }

            if (remaining > 0.005)
            {
                Thread.Sleep(1);
            }
        }
    }

    private unsafe int OnNativeRead(IntPtr opaque, IntPtr buffer, int size)
    {
        try
        {
            if (GCHandle.FromIntPtr(opaque).Target is not MotionPhotoDecoder decoder || size <= 0)
            {
                return 0;
            }

            return decoder._stream.Read(new Span<byte>(buffer.ToPointer(), size));
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(MotionPhotoDecoder), nameof(OnNativeRead), e);
            return -1;
        }
    }

    private long OnNativeSeek(IntPtr opaque, long offset, int whence)
    {
        try
        {
            if (GCHandle.FromIntPtr(opaque).Target is not MotionPhotoDecoder decoder)
            {
                return -1;
            }

            if (whence == FFmpegService.AvSeekSize)
            {
                return decoder._stream.Length;
            }

            var origin = whence switch
            {
                0 => SeekOrigin.Begin,
                1 => SeekOrigin.Current,
                2 => SeekOrigin.End,
                _ => (SeekOrigin?)null,
            };

            return origin is null ? -1 : decoder._stream.Seek(offset, origin.Value);
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(MotionPhotoDecoder), nameof(OnNativeSeek), e);
            return -1;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _stopRequested = true;
        _resumeEvent.Set();
        _worker?.Join(TimeSpan.FromSeconds(5));

        if (_session != IntPtr.Zero)
        {
            FFmpegService.PvClose(_session);
            _session = IntPtr.Zero;
        }

        foreach (var buffer in _frameBuffers)
        {
            if (buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        _frameBuffers = [];
        if (_callbackHandle.IsAllocated)
        {
            _callbackHandle.Free();
        }

        _resumeEvent.Dispose();
        GC.SuppressFinalize(this);
    }
}
