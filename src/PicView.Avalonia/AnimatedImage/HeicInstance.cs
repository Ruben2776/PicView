using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ImageMagick;
using PicView.Core.DebugTools;

namespace PicView.Avalonia.AnimatedImage;

public class HeicInstance : IGifInstance
{
    private readonly byte[][] _framePixels;
    private readonly List<TimeSpan> _frameTimes;
    private readonly WriteableBitmap? _targetBitmap;
    private int _currentFrameIndex;
    private uint _iterationCount;
    private TimeSpan _totalTime;

    public HeicInstance(Stream currentStream)
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

        using var collection = new MagickImageCollection(currentStream);

        if (collection.Count == 0)
        {
            throw new InvalidDataException("The HEIC file contains no frames.");
        }

        var first = collection[0];
        var width = (int)first.Width;
        var height = (int)first.Height;
        var pixSize = new PixelSize(width, height);

        _targetBitmap = new WriteableBitmap(pixSize, new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Opaque);
        GifPixelSize = pixSize;

        _totalTime = TimeSpan.Zero;
        _frameTimes = [];
        _framePixels = new byte[collection.Count][];

        for (var i = 0; i < collection.Count; i++)
        {
            var frame = collection[i];

            // AnimationDelay is in centiseconds (1/100th of a second)
            var delay = frame.AnimationDelay > 0
                ? TimeSpan.FromMilliseconds(frame.AnimationDelay * 10)
                : TimeSpan.FromMilliseconds(100); // Default 100ms if no delay specified

            _totalTime = _totalTime.Add(delay);
            _frameTimes.Add(_totalTime);

            // Pre-decode frame to BGRA byte array
            _framePixels[i] = PreDecodeFrame(frame, width, height);
        }

        // Render the first frame into the WriteableBitmap
        CopyFrameToWriteableBitmap(0);
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

        if (_totalTime.Ticks == 0)
        {
            return _targetBitmap;
        }

        var elapsedTicks = stopwatchElapsed.Ticks;
        var timeModulus = TimeSpan.FromTicks(elapsedTicks % _totalTime.Ticks);
        var targetFrame = _frameTimes.FirstOrDefault(x => timeModulus < x);
        var currentFrame = _frameTimes.IndexOf(targetFrame);
        if (currentFrame == -1)
        {
            currentFrame = 0;
        }

        if (_currentFrameIndex == currentFrame)
        {
            return _targetBitmap;
        }

        _iterationCount = (uint)(elapsedTicks / _totalTime.Ticks);

        return ProcessFrameIndex(currentFrame);
    }

    internal WriteableBitmap ProcessFrameIndex(int frameIndex)
    {
        if (_targetBitmap is null)
        {
            throw new InvalidOperationException("The target bitmap is null.");
        }

        CopyFrameToWriteableBitmap(frameIndex);
        _currentFrameIndex = frameIndex;

        return _targetBitmap;
    }

    private static byte[] PreDecodeFrame(IMagickImage frame, int width, int height)
    {
        // Ensure the frame matches expected dimensions
        if ((int)frame.Width != width || (int)frame.Height != height)
        {
            frame.Resize((uint)width, (uint)height);
        }

        // Read pixels as RGBA byte array — MagickImageCollection always contains MagickImage instances
        var magickFrame = (MagickImage)frame;
        using var pixels = magickFrame.GetPixelsUnsafe();
        var rgba = pixels.ToByteArray(PixelMapping.RGBA);

        if (rgba is null)
        {
            throw new InvalidDataException("Failed to extract pixel data from HEIC frame.");
        }

        // Convert RGBA to BGRA (swap R and B channels) for Avalonia's Bgra8888 format
        var bgra = new byte[width * height * 4];
        for (var i = 0; i < bgra.Length; i += 4)
        {
            bgra[i] = rgba[i + 2];     // B <- R
            bgra[i + 1] = rgba[i + 1]; // G <- G
            bgra[i + 2] = rgba[i];     // R <- B
            bgra[i + 3] = rgba[i + 3]; // A <- A
        }

        return bgra;
    }

    private void CopyFrameToWriteableBitmap(int frameIndex)
    {
        if (_targetBitmap is null)
        {
            return;
        }

        try
        {
            using var frameBuffer = _targetBitmap.Lock();
            Marshal.Copy(_framePixels[frameIndex], 0, frameBuffer.Address, _framePixels[frameIndex].Length);
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(HeicInstance), nameof(CopyFrameToWriteableBitmap), e);
        }
    }
}
