using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using XamlAnimatedGif.Decoding;
using XamlAnimatedGif.Decompression;
using XamlAnimatedGif.Extensions;

namespace XamlAnimatedGif;

public abstract class Animator : DependencyObject, IDisposable
{
    private readonly Stream _sourceStream;
    private readonly Uri _sourceUri;
    private readonly bool _isSourceStreamOwner;
    private readonly GifDataStream _metadata;
    private readonly Dictionary<int, GifPalette> _palettes;
    private readonly WriteableBitmap _bitmap;
    private readonly int _stride;
    private readonly byte[] _previousBackBuffer;
    private readonly byte[] _indexStreamBuffer;
    private readonly TimingManager _timingManager;
    private readonly bool _cacheFrameDataInMemory;
    private readonly byte[][] _cachedFrameBytes;
    private readonly Task _loadFramesDataTask;

    #region Constructor and factory methods

    internal Animator(Stream sourceStream, Uri sourceUri, GifDataStream metadata, RepeatBehavior repeatBehavior,
        bool cacheFrameDataInMemory)
    {
        _sourceStream = sourceStream;
        _sourceUri = sourceUri;
        _isSourceStreamOwner = sourceUri != null; // stream opened from URI, should close it
        _metadata = metadata;
        _palettes = CreatePalettes(metadata);
        _bitmap = CreateBitmap(metadata);
        var desc = metadata.Header.LogicalScreenDescriptor;
        _stride = 4 * ((desc.Width * 32 + 31) / 32);
        _previousBackBuffer = new byte[desc.Height * _stride];
        _indexStreamBuffer = CreateIndexStreamBuffer(metadata, _sourceStream);
        _timingManager = CreateTimingManager(metadata, repeatBehavior);

        _cacheFrameDataInMemory = cacheFrameDataInMemory;

        if (cacheFrameDataInMemory)
        {
            _cachedFrameBytes = new byte[_metadata.Frames.Count][];
            _loadFramesDataTask = Task.Run(LoadFrames);
        }
    }

    private async Task LoadFrames()
    {
        var biggestFrameSize = 0L;
        for (var frameIndex = 0; frameIndex < _metadata.Frames.Count; frameIndex++)
        {
            var startPosition = _metadata.Frames[frameIndex].ImageData.CompressedDataStartOffset;
            var endPosition = _metadata.Frames.Count == frameIndex + 1
                ? _sourceStream.Length
                : _metadata.Frames[frameIndex + 1].ImageData.CompressedDataStartOffset - 1;
            var size = endPosition - startPosition;
            biggestFrameSize = Math.Max(size, biggestFrameSize);
        }

        var indexCompressedBytes = new byte[biggestFrameSize];
        for (var frameIndex = 0; frameIndex < _metadata.Frames.Count; frameIndex++)
        {
            var frame = _metadata.Frames[frameIndex];
            var frameDesc = _metadata.Frames[frameIndex].Descriptor;
            await GetIndexBytesAsync(frameIndex, indexCompressedBytes);
            await using var indexDecompressedStream =
                new LzwDecompressStream(indexCompressedBytes, frame.ImageData.LzwMinimumCodeSize);
            _cachedFrameBytes[frameIndex] = new byte[frameDesc.Width * frameDesc.Height];

            await indexDecompressedStream.ReadAllAsync(_cachedFrameBytes[frameIndex], 0,
                frameDesc.Width * frameDesc.Height);
        }
    }

    internal static async Task<TAnimator> CreateAsyncCore<TAnimator>(
        Uri sourceUri,
        IProgress<int> progress,
        Func<Stream, GifDataStream, TAnimator> create)
        where TAnimator : Animator
    {
        var stream = await UriLoader.GetStreamFromUriAsync(sourceUri);
        try
        {
            // ReSharper disable once AccessToDisposedClosure
            return await CreateAsyncCore(stream, metadata => create(stream, metadata));
        }
        catch
        {
            stream?.Dispose();
            throw;
        }
    }

    internal static async Task<TAnimator> CreateAsyncCore<TAnimator>(
        Stream sourceStream,
        Func<GifDataStream, TAnimator> create)
        where TAnimator : Animator
    {
        if (!sourceStream.CanSeek)
            throw new ArgumentException("The stream is not seekable");
        sourceStream.Seek(0, SeekOrigin.Begin);
        var metadata = await GifDataStream.ReadAsync(sourceStream);
        return create(metadata);
    }

    #endregion Constructor and factory methods

    #region Animation

    public int FrameCount => _metadata.Frames.Count;

    private bool _isStarted;
    private CancellationTokenSource _cancellationTokenSource;

    public async void Play()
    {
        try
        {
            if (_timingManager.IsComplete)
            {
                _timingManager.Reset();
                _isStarted = false;
            }

            if (!_isStarted)
            {
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = new CancellationTokenSource();
                _isStarted = true;
                OnAnimationStarted();
                if (_timingManager.IsPaused)
                    _timingManager.Resume();
                await RunAsync(_cancellationTokenSource.Token);
            }
            else if (_timingManager.IsPaused)
            {
                _timingManager.Resume();
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            // ignore errors that might occur during Dispose
            if (!_disposing)
                OnError(ex, AnimationErrorKind.Rendering);
        }
    }

    private int _frameIndex;

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        if (_loadFramesDataTask != null)
            await _loadFramesDataTask;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var timing = _timingManager.NextAsync(cancellationToken);
            var rendering = RenderFrameAsync(CurrentFrameIndex, cancellationToken);
            await Task.WhenAll(timing, rendering);
            if (!timing.Result)
                break;
            CurrentFrameIndex = (CurrentFrameIndex + 1) % FrameCount;
        }
    }

    public void Pause()
    {
        _timingManager.Pause();
    }

    public bool IsPaused => _timingManager.IsPaused;

    public bool IsComplete
    {
        get
        {
            if (_isStarted)
                return _timingManager.IsComplete;
            return false;
        }
    }

    public event EventHandler CurrentFrameChanged;

    protected virtual void OnCurrentFrameChanged()
    {
        CurrentFrameChanged?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler<AnimationStartedEventArgs> AnimationStarted;

    protected virtual void OnAnimationStarted()
    {
        AnimationStarted?.Invoke(this, new AnimationStartedEventArgs(AnimationSource));
    }

    public event EventHandler<AnimationCompletedEventArgs> AnimationCompleted;

    protected virtual void OnAnimationCompleted()
    {
        AnimationCompleted?.Invoke(this, new AnimationCompletedEventArgs(AnimationSource));
    }

    public event EventHandler<AnimationErrorEventArgs> Error;

    protected virtual void OnError(Exception ex, AnimationErrorKind kind)
    {
        Error?.Invoke(this, new AnimationErrorEventArgs(AnimationSource, ex, kind));
    }

    public int CurrentFrameIndex
    {
        get => _frameIndex;
        private set
        {
            _frameIndex = value;
            OnCurrentFrameChanged();
        }
    }

    private TimingManager CreateTimingManager(GifDataStream metadata, RepeatBehavior repeatBehavior)
    {
        var actualRepeatBehavior = GetActualRepeatBehavior(metadata, repeatBehavior);

        var manager = new TimingManager(actualRepeatBehavior);
        foreach (var frame in metadata.Frames)
        {
            manager.Add(GetFrameDelay(frame));
        }

        manager.Completed += TimingManagerCompleted;
        return manager;
    }

    private static RepeatBehavior GetActualRepeatBehavior(GifDataStream metadata, RepeatBehavior repeatBehavior)
    {
        return repeatBehavior == default
            ? GetRepeatBehaviorFromGif(metadata)
            : repeatBehavior;
    }

    protected abstract RepeatBehavior GetSpecifiedRepeatBehavior();

    private void TimingManagerCompleted(object sender, EventArgs e)
    {
        OnAnimationCompleted();
    }

    #endregion Animation

    #region Rendering

    private static WriteableBitmap CreateBitmap(GifDataStream metadata)
    {
        var desc = metadata.Header.LogicalScreenDescriptor;
        var bitmap = new WriteableBitmap(desc.Width, desc.Height, 96, 96, PixelFormats.Bgra32, null);
        return bitmap;
    }

    private static Dictionary<int, GifPalette> CreatePalettes(GifDataStream metadata)
    {
        var palettes = new Dictionary<int, GifPalette>();
        Color[] globalColorTable = null;
        if (metadata.Header.LogicalScreenDescriptor.HasGlobalColorTable)
        {
            globalColorTable =
                metadata.GlobalColorTable
                    .Select(gc => Color.FromArgb(0xFF, gc.R, gc.G, gc.B))
                    .ToArray();
        }

        for (var i = 0; i < metadata.Frames.Count; i++)
        {
            var frame = metadata.Frames[i];
            var colorTable = globalColorTable;
            if (frame.Descriptor.HasLocalColorTable)
            {
                colorTable =
                    frame.LocalColorTable
                        .Select(gc => Color.FromArgb(0xFF, gc.R, gc.G, gc.B))
                        .ToArray();
            }

            int? transparencyIndex = null;
            var gce = frame.GraphicControl;
            if (gce is { HasTransparency: true })
            {
                transparencyIndex = gce.TransparencyIndex;
            }

            palettes[i] = new GifPalette(transparencyIndex, colorTable);
        }

        return palettes;
    }

    private static byte[] CreateIndexStreamBuffer(GifDataStream metadata, Stream stream)
    {
        // Find the size of the largest frame pixel data
        // (ignoring the fact that we include the next frame's header)

        var lastSize = stream.Length - metadata.Frames.Last().ImageData.CompressedDataStartOffset;
        var maxSize = lastSize;
        if (metadata.Frames.Count > 1)
        {
            var sizes = metadata.Frames.Zip(metadata.Frames.Skip(1),
                (f1, f2) => f2.ImageData.CompressedDataStartOffset - f1.ImageData.CompressedDataStartOffset);
            maxSize = Math.Max(sizes.Max(), lastSize);
        }

        // Need 4 extra bytes so that BitReader doesn't need to check the size for every read
        return new byte[maxSize + 4];
    }

    private int _previousFrameIndex;
    private GifFrame _previousFrame;

    private async Task RenderFrameAsync(int frameIndex, CancellationToken cancellationToken)
    {
        if (frameIndex < 0)
            return;

        var frame = _metadata.Frames[frameIndex];
        var desc = frame.Descriptor;
        var rect = GetFixedUpFrameRect(desc);

        Stream indexStream = null;
        if (!_cacheFrameDataInMemory)
        {
            indexStream = await GetIndexStreamAsync(frame, cancellationToken);
        }

        await using (indexStream)
        using (_bitmap.LockInScope())
        {
            if (frameIndex < _previousFrameIndex)
                ClearArea(_metadata.Header.LogicalScreenDescriptor);
            else
                DisposePreviousFrame(frame);

            var bufferLength = 4 * rect.Width;
            byte[] indexBuffer;
            var lineBuffer = new byte[bufferLength];

            var palette = _palettes[frameIndex];
            var transparencyIndex = palette.TransparencyIndex ?? -1;

            var rows = desc.Interlace
                ? InterlacedRows(rect.Height)
                : NormalRows(rect.Height);

            if (!_cacheFrameDataInMemory)
            {
                indexBuffer = new byte[desc.Width * desc.Height];
                await indexStream.ReadAllAsync(indexBuffer, 0, indexBuffer.Length, cancellationToken);
            }
            else
            {
                indexBuffer = _cachedFrameBytes[frameIndex];
            }

            foreach (var y in rows)
            {
                var offset = (desc.Top + y) * _stride + desc.Left * 4;

                if (transparencyIndex >= 0)
                {
                    CopyFromBitmap(lineBuffer, _bitmap, offset, bufferLength);
                }

                for (var x = 0; x < rect.Width; x++)
                {
                    var index = indexBuffer[x + y * desc.Width];
                    var i = 4 * x;
                    if (index != transparencyIndex)
                    {
                        WriteColor(lineBuffer, palette[index], i);
                    }
                }

                CopyToBitmap(lineBuffer, _bitmap, offset, bufferLength);
            }

            _bitmap.AddDirtyRect(rect);
        }

        _previousFrame = frame;
        _previousFrameIndex = frameIndex;
    }

    private static IEnumerable<int> NormalRows(int height)
    {
        return Enumerable.Range(0, height);
    }

    private static IEnumerable<int> InterlacedRows(int height)
    {
        /*
         * 4 passes:
         * Pass 1: rows 0, 8, 16, 24...
         * Pass 2: rows 4, 12, 20, 28...
         * Pass 3: rows 2, 6, 10, 14...
         * Pass 4: rows 1, 3, 5, 7...
         * */
        var passes = new[]
        {
            new { Start = 0, Step = 8 },
            new { Start = 4, Step = 8 },
            new { Start = 2, Step = 4 },
            new { Start = 1, Step = 2 }
        };
        foreach (var pass in passes)
        {
            var y = pass.Start;
            while (y < height)
            {
                yield return y;
                y += pass.Step;
            }
        }
    }

    private static void CopyToBitmap(byte[] buffer, WriteableBitmap bitmap, int offset, int length)
    {
        Marshal.Copy(buffer, 0, bitmap.BackBuffer + offset, length);
    }

    private static void CopyFromBitmap(byte[] buffer, WriteableBitmap bitmap, int offset, int length)
    {
        Marshal.Copy(bitmap.BackBuffer + offset, buffer, 0, length);
    }

    private static void WriteColor(byte[] lineBuffer, Color color, int startIndex)
    {
        lineBuffer[startIndex] = color.B;
        lineBuffer[startIndex + 1] = color.G;
        lineBuffer[startIndex + 2] = color.R;
        lineBuffer[startIndex + 3] = color.A;
    }

    private void DisposePreviousFrame(GifFrame currentFrame)
    {
        var pgce = _previousFrame?.GraphicControl;
        if (pgce != null)
        {
            switch (pgce.DisposalMethod)
            {
                case GifFrameDisposalMethod.None:
                case GifFrameDisposalMethod.DoNotDispose:
                    {
                        // Leave previous frame in place
                        break;
                    }
                case GifFrameDisposalMethod.RestoreBackground:
                    {
                        ClearArea(GetFixedUpFrameRect(_previousFrame.Descriptor));
                        break;
                    }
                case GifFrameDisposalMethod.RestorePrevious:
                    {
                        CopyToBitmap(_previousBackBuffer, _bitmap, 0, _previousBackBuffer.Length);
                        var desc = _metadata.Header.LogicalScreenDescriptor;
                        var rect = new Int32Rect(0, 0, desc.Width, desc.Height);
                        _bitmap.AddDirtyRect(rect);
                        break;
                    }
            }
        }

        var gce = currentFrame.GraphicControl;
        if (gce is { DisposalMethod: GifFrameDisposalMethod.RestorePrevious })
        {
            CopyFromBitmap(_previousBackBuffer, _bitmap, 0, _previousBackBuffer.Length);
        }
    }

    private void ClearArea(IGifRect rect)
    {
        ClearArea(new Int32Rect(rect.Left, rect.Top, rect.Width, rect.Height));
    }

    private void ClearArea(Int32Rect rect)
    {
        var bufferLength = 4 * rect.Width;
        var lineBuffer = new byte[bufferLength];
        for (var y = 0; y < rect.Height; y++)
        {
            var offset = (rect.Y + y) * _stride + 4 * rect.X;
            CopyToBitmap(lineBuffer, _bitmap, offset, bufferLength);
        }

        _bitmap.AddDirtyRect(new Int32Rect(rect.X, rect.Y, rect.Width, rect.Height));
    }

    private async Task<Stream> GetIndexStreamAsync(GifFrame frame, CancellationToken cancellationToken)
    {
        var data = frame.ImageData;
        cancellationToken.ThrowIfCancellationRequested();
        _sourceStream.Seek(data.CompressedDataStartOffset, SeekOrigin.Begin);
        using (var ms = new MemoryStream(_indexStreamBuffer))
        {
            await GifHelpers.CopyDataBlocksToStreamAsync(_sourceStream, ms, cancellationToken)
                .ConfigureAwait(false);
        }

        var lzwStream = new LzwDecompressStream(_indexStreamBuffer, data.LzwMinimumCodeSize);
        return lzwStream;
    }

    private async Task GetIndexBytesAsync(int frameIndex, byte[] buffer)
    {
        var startPosition = _metadata.Frames[frameIndex].ImageData.CompressedDataStartOffset;

        _sourceStream.Seek(startPosition, SeekOrigin.Begin);
        using var memoryStream = new MemoryStream(buffer);
        await GifHelpers.CopyDataBlocksToStreamAsync(_sourceStream, memoryStream).ConfigureAwait(false);
    }

    internal BitmapSource Bitmap => _bitmap;

    #endregion Rendering

    #region Helper methods

    private static TimeSpan GetFrameDelay(GifFrame frame)
    {
        var gce = frame.GraphicControl;
        if (gce != null)
        {
            if (gce.Delay != 0)
                return TimeSpan.FromMilliseconds(gce.Delay);
        }

        return TimeSpan.FromMilliseconds(100);
    }

    private static RepeatBehavior GetRepeatBehaviorFromGif(GifDataStream metadata)
    {
        if (metadata.RepeatCount == 0)
            return RepeatBehavior.Forever;
        return new RepeatBehavior(metadata.RepeatCount);
    }

    private Int32Rect GetFixedUpFrameRect(GifImageDescriptor desc)
    {
        var width = Math.Min(desc.Width, _bitmap.PixelWidth - desc.Left);
        var height = Math.Min(desc.Height, _bitmap.PixelHeight - desc.Top);
        return new Int32Rect(desc.Left, desc.Top, width, height);
    }

    #endregion Helper methods

    #region Finalizer and Dispose

    ~Animator()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private volatile bool _disposing;
    private bool _disposed;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            _disposing = true;
            if (_timingManager != null) _timingManager.Completed -= TimingManagerCompleted;
            _cancellationTokenSource?.Cancel();
            if (_isSourceStreamOwner)
            {
                try
                {
                    _sourceStream?.Dispose();
                }
                catch
                {
                    /* ignored */
                }
            }

            _disposed = true;
        }
    }

    #endregion Finalizer and Dispose

    public override string ToString()
    {
        var s = _sourceUri?.ToString() ?? _sourceStream.ToString();
        return "GIF: " + s;
    }

    private class GifPalette
    {
        private readonly Color[] _colors;

        public GifPalette(int? transparencyIndex, Color[] colors)
        {
            TransparencyIndex = transparencyIndex;
            _colors = colors;
        }

        public int? TransparencyIndex { get; }

        public Color this[int i] => _colors[i];
    }

    internal async Task ShowFirstFrameAsync()
    {
        try
        {
            if (_loadFramesDataTask != null)
                await _loadFramesDataTask;
            await RenderFrameAsync(0, CancellationToken.None).ConfigureAwait(false);
            CurrentFrameIndex = 0;
            _timingManager.Pause();
        }
        catch (Exception ex)
        {
            OnError(ex, AnimationErrorKind.Rendering);
        }
    }

    public async void Rewind()
    {
        CurrentFrameIndex = 0;
        var isStopped = _timingManager.IsPaused || _timingManager.IsComplete;
        _timingManager.Reset();
        if (isStopped)
        {
            _timingManager.Pause();
            _isStarted = false;
            try
            {
                await RenderFrameAsync(0, CancellationToken.None);
            }
            catch (Exception ex)
            {
                OnError(ex, AnimationErrorKind.Rendering);
            }
        }
    }

    protected abstract object AnimationSource { get; }

    internal void OnRepeatBehaviorChanged()
    {
        if (_timingManager == null)
            return;

        var newValue = GetSpecifiedRepeatBehavior();
        var newActualValue = GetActualRepeatBehavior(_metadata, newValue);
        if (_timingManager.RepeatBehavior == newActualValue)
            return;

        _timingManager.RepeatBehavior = newActualValue;
        Rewind();
    }
}