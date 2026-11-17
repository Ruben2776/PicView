using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PicView.Avalonia.AnimatedImage.Decoding;

namespace PicView.Avalonia.AnimatedImage;

internal sealed class GifAnimationDecoder : IAnimationDecoder
{
    private readonly CancellationTokenSource _cts = new();
    private readonly GifDecoder _decoder;
    private int _lastIndex = -1;
    private WriteableBitmap? _lastTarget;

    public GifAnimationDecoder(Stream stream)
    {
        AnimationDecoders.PrepareStream(stream);

        try
        {
            _decoder = new GifDecoder(stream, _cts.Token);
            var dimensions = _decoder.Header!.Dimensions;
            Size = new PixelSize(dimensions.Width, dimensions.Height);
            FrameDurations = _decoder.Frames.Select(frame => frame.FrameDelay).ToArray();
        }
        catch
        {
            _decoder?.Dispose();
            _cts.Dispose();
            throw;
        }
    }

    public PixelSize Size { get; }

    public AlphaFormat AlphaFormat => AlphaFormat.Opaque;

    public IReadOnlyList<TimeSpan> FrameDurations { get; }

    public void RenderFrame(int index, WriteableBitmap target)
    {
        if (index < 0 || index >= FrameDurations.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, "The frame does not exist.");
        }

        // GifDecoder replays from the first frame when an earlier index is asked for, but it draws nothing when the
        // index is the one it drew last. For a different target, step away from that index first so it is drawn again.
        if (index == _lastIndex && !ReferenceEquals(_lastTarget, target) && FrameDurations.Count > 1)
        {
            _decoder.RenderFrame(index == 0 ? 1 : 0, target);
        }

        _decoder.RenderFrame(index, target);
        _lastIndex = index;
        _lastTarget = target;
    }

    public void Dispose()
    {
        _cts.Cancel();
        _decoder.Dispose();
        _cts.Dispose();
    }
}
