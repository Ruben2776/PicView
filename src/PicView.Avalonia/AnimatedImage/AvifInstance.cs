using Avalonia;
using Avalonia.Animation;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ImageMagick;
using PicView.Core.DebugTools;

namespace PicView.Avalonia.AnimatedImage;

/// <summary>
///     Plays back animated AVIF images by decoding the frames with Magick.NET.
/// </summary>
public class AvifInstance : IGifInstance
{
    private const int BytesPerPixel = 4;

    private readonly MagickImageCollection? _collection;
    private readonly List<TimeSpan> _frameTimes;
    private readonly WriteableBitmap? _targetBitmap;
    private int _currentFrameIndex = -1;
    private uint _iterationCount;
    private TimeSpan _totalTime;

    public AvifInstance(Stream currentStream)
    {
        if (!currentStream.CanSeek)
        {
            throw new InvalidDataException("The provided stream is not seekable.");
        }

        if (!currentStream.CanRead)
        {
            throw new InvalidOperationException("Can't read the stream provided.");
        }

        currentStream.Seek(0, SeekOrigin.Begin);

        CurrentCts = new CancellationTokenSource();

        _collection = new MagickImageCollection();
        _collection.Read(currentStream);

        if (_collection.Count <= 0)
        {
            throw new InvalidDataException("The provided stream does not contain any AVIF frames.");
        }

        // Makes sure every frame is a full-sized image, so they can be rendered independently
        _collection.Coalesce();

        var firstFrame = _collection[0];
        var pixSize = new PixelSize((int)firstFrame.Width, (int)firstFrame.Height);

        _targetBitmap = new WriteableBitmap(pixSize, new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Unpremul);
        GifPixelSize = pixSize;

        _totalTime = TimeSpan.Zero;

        _frameTimes = _collection.Select(frame =>
            {
                _totalTime = _totalTime.Add(GetFrameDelay(frame));
                return _totalTime;
            })
            .ToList();

        ProcessFrameIndex(0);
    }

    public IterationCount IterationCount { get; set; }

    public bool AutoStart => true;

    public CancellationTokenSource CurrentCts { get; }

    public int GifFrameCount => _frameTimes.Count;

    public PixelSize GifPixelSize { get; }

    public bool IsDisposed { get; private set; }

    public void Dispose()
    {
        if (IsDisposed)
        {
            return;
        }

        GC.SuppressFinalize(this);

        IsDisposed = true;
        CurrentCts.Cancel();
        _targetBitmap?.Dispose();
        _collection?.Dispose();
    }

    public WriteableBitmap? ProcessFrameTime(TimeSpan stopwatchElapsed)
    {
        if (!IterationCount.IsInfinite && _iterationCount > IterationCount.Value)
        {
            return null;
        }

        if (CurrentCts.IsCancellationRequested || _targetBitmap is null)
        {
            return null;
        }

        var totalTicks = _totalTime.Ticks;

        if (totalTicks == 0)
        {
            return _currentFrameIndex == 0 ? _targetBitmap : ProcessFrameIndex(0);
        }

        var elapsedTicks = stopwatchElapsed.Ticks;
        var timeModulus = TimeSpan.FromTicks(elapsedTicks % totalTicks);
        var targetFrame = _frameTimes.Find(x => timeModulus < x);
        var currentFrame = _frameTimes.IndexOf(targetFrame);
        if (currentFrame == -1)
        {
            currentFrame = 0;
        }

        if (_currentFrameIndex == currentFrame)
        {
            return _targetBitmap;
        }

        _iterationCount = (uint)(elapsedTicks / totalTicks);

        return ProcessFrameIndex(currentFrame);
    }

    private static TimeSpan GetFrameDelay(IMagickImage<byte> frame)
    {
        var ticksPerSecond = frame.AnimationTicksPerSecond <= 0 ? 100 : frame.AnimationTicksPerSecond;
        var delay = frame.AnimationDelay;
        if (delay <= 0)
        {
            // Fall back to a sensible default, matching what browsers do with zero delays
            delay = (uint)(ticksPerSecond / 10);
        }

        return TimeSpan.FromSeconds(delay / (double)ticksPerSecond);
    }

    internal WriteableBitmap? ProcessFrameIndex(int frameIndex)
    {
        if (_collection is null || _targetBitmap is null)
        {
            return null;
        }

        try
        {
            RenderFrame(_collection[frameIndex], _targetBitmap);
            _currentFrameIndex = frameIndex;
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(AvifInstance), nameof(ProcessFrameIndex), e);
        }

        return _targetBitmap;
    }

    private static unsafe void RenderFrame(IMagickImage<byte> frame, WriteableBitmap targetBitmap)
    {
        if (!frame.HasAlpha)
        {
            frame.Alpha(AlphaOption.Opaque);
        }

        using var pixels = frame.GetPixels();
        var bytes = pixels.ToByteArray(PixelMapping.BGRA);
        if (bytes is null)
        {
            return;
        }

        using var frameBuffer = targetBitmap.Lock();

        var width = Math.Min((int)frame.Width, frameBuffer.Size.Width);
        var height = Math.Min((int)frame.Height, frameBuffer.Size.Height);
        var sourceStride = (int)frame.Width * BytesPerPixel;
        var destination = (byte*)frameBuffer.Address;

        fixed (byte* source = bytes)
        {
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
}
