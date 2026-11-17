using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PicView.Avalonia.AnimatedImage.Decoding;

namespace PicView.Avalonia.AnimatedImage;

internal sealed class GifAnimationDecoder : IAnimationDecoder
{
    private readonly CancellationTokenSource _cts = new();
    private readonly GifDecoder _decoder;

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
        // GifDecoder replays from the first frame when an earlier index is asked for
        _decoder.RenderFrame(index, target);
    }

    public void Dispose()
    {
        _cts.Cancel();
        _decoder.Dispose();
        _cts.Dispose();
    }
}
