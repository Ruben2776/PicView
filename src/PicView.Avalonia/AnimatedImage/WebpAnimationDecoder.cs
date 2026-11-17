using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SkiaSharp;

namespace PicView.Avalonia.AnimatedImage;

internal sealed class WebpAnimationDecoder : IAnimationDecoder
{
    private readonly SKCodec _codec;
    private readonly SKManagedStream _managedStream;
    private int _lastIndex = -1;
    private WriteableBitmap? _lastTarget;

    public WebpAnimationDecoder(Stream stream)
    {
        AnimationDecoders.PrepareStream(stream);

        _managedStream = new SKManagedStream(stream);
        try
        {
            _codec = SKCodec.Create(_managedStream)
                     ?? throw new InvalidDataException("The provided stream is not a decodable WebP image.");
            Size = new PixelSize(_codec.Info.Width, _codec.Info.Height);
            FrameDurations = _codec.FrameInfo.Select(frame => TimeSpan.FromMilliseconds(frame.Duration)).ToArray();
        }
        catch
        {
            _codec?.Dispose();
            _managedStream.Dispose();
            throw;
        }
    }

    public PixelSize Size { get; }

    public AlphaFormat AlphaFormat => AlphaFormat.Opaque;

    public IReadOnlyList<TimeSpan> FrameDurations { get; }

    public void RenderFrame(int index, WriteableBitmap target)
    {
        _codec.GetFrameInfo(index, out var frameInfo);

        var info = new SKImageInfo(_codec.Info.Width, _codec.Info.Height).WithAlphaType(frameInfo.AlphaType);

        // The next frame in sequence builds on the one the target already holds; any other frame is decoded alone
        var options = index > 0 && _lastIndex == index - 1 && ReferenceEquals(_lastTarget, target)
            ? new SKCodecOptions(index, _lastIndex)
            : new SKCodecOptions(index);

        _lastIndex = -1;
        using (var frameBuffer = target.Lock())
        {
            var result = _codec.GetPixels(info, frameBuffer.Address, options);
            if (result != SKCodecResult.Success)
            {
                throw new InvalidDataException($"Could not decode frame {index} of {_codec.FrameCount}.");
            }
        }

        _lastIndex = index;
        _lastTarget = target;
    }

    public void Dispose()
    {
        _codec.Dispose();
        _managedStream.Dispose();
    }
}
