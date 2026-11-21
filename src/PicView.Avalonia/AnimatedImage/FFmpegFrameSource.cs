using Avalonia;
using PicView.Avalonia.MotionPhoto;
using PicView.Core.DebugTools;

namespace PicView.Avalonia.AnimatedImage;

/// <summary>
///     Decodes the video track of a stream with the bundled picview-ffmpeg library. The caller keeps ownership of the
///     stream, and uses the source from one thread at a time.
/// </summary>
internal sealed class FFmpegFrameSource : IVideoFrameSource
{
    // The native side calls these for as long as the session is open, so they are kept from being collected
    private readonly FFmpegService.PvReadCallback _read;
    private readonly FFmpegService.PvSeekCallback _seek;
    private readonly Stream _stream;
    private IntPtr _session;

    private FFmpegFrameSource(Stream stream)
    {
        _stream = stream;
        _read = OnNativeRead;
        _seek = OnNativeSeek;
    }

    public PixelSize Size { get; private set; }

    /// <summary>
    ///     Opens the video in <paramref name="stream" />. Returns null when the library is missing, or was built
    ///     without a decoder for what the stream holds.
    /// </summary>
    public static FFmpegFrameSource? TryOpen(Stream stream)
    {
        if (!FFmpegService.TryInitialize())
        {
            return null;
        }

        var source = new FFmpegFrameSource(stream);
        if (source.Open())
        {
            return source;
        }

        source.Dispose();
        return null;
    }

    public bool TryDecodeNext(IntPtr destination, int capacity, out PixelSize frameSize)
    {
        frameSize = default;
        if (_session == IntPtr.Zero)
        {
            return false;
        }

        var written = FFmpegService.PvDecodeNext(_session, destination, capacity, out _, out var width, out var height);
        if (written <= 0)
        {
            return false;
        }

        frameSize = new PixelSize(width, height);
        return true;
    }

    public bool Restart()
    {
        if (_session != IntPtr.Zero && FFmpegService.PvRewind is { } rewind && rewind(_session) is 0)
        {
            return true;
        }

        // A library built before it could rewind, or a stream that cannot seek: the video is opened anew
        var size = Size;
        Close();
        if (Open() && Size == size)
        {
            return true;
        }

        Close();
        return false;
    }

    public void Dispose()
    {
        Close();
    }

    private bool Open()
    {
        try
        {
            _stream.Seek(0, SeekOrigin.Begin);
            _session = FFmpegService.PvOpen(IntPtr.Zero, _read, _seek, out var info);
            if (_session == IntPtr.Zero)
            {
                return false;
            }

            if (info.Width <= 0 || info.Height <= 0)
            {
                Close();
                return false;
            }

            Size = new PixelSize(info.Width, info.Height);
            return true;
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(FFmpegFrameSource), nameof(Open), e);
            return false;
        }
    }

    private void Close()
    {
        if (_session == IntPtr.Zero)
        {
            return;
        }

        FFmpegService.PvClose(_session);
        _session = IntPtr.Zero;
    }

    private unsafe int OnNativeRead(IntPtr opaque, IntPtr buffer, int size)
    {
        try
        {
            return size <= 0 ? 0 : _stream.Read(new Span<byte>(buffer.ToPointer(), size));
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(FFmpegFrameSource), nameof(OnNativeRead), e);
            return -1;
        }
    }

    private long OnNativeSeek(IntPtr opaque, long offset, int whence)
    {
        try
        {
            if (whence == FFmpegService.AvSeekSize)
            {
                return _stream.Length;
            }

            return whence switch
            {
                0 => _stream.Seek(offset, SeekOrigin.Begin),
                1 => _stream.Seek(offset, SeekOrigin.Current),
                2 => _stream.Seek(offset, SeekOrigin.End),
                _ => -1
            };
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(FFmpegFrameSource), nameof(OnNativeSeek), e);
            return -1;
        }
    }
}
