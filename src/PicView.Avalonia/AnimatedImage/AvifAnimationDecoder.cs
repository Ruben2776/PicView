using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ImageMagick;

namespace PicView.Avalonia.AnimatedImage;

internal sealed class AvifAnimationDecoder : IAnimationDecoder
{
    private const int BytesPerPixel = 4;

    private readonly MagickImageCollection _collection;

    public AvifAnimationDecoder(Stream stream)
    {
        AnimationDecoders.PrepareStream(stream);

        _collection = new MagickImageCollection();
        try
        {
            _collection.Read(stream);

            if (_collection.Count <= 0)
            {
                throw new InvalidDataException("The provided stream does not contain any AVIF frames.");
            }

            // Makes sure every frame is a full-sized image, so they can be rendered independently
            _collection.Coalesce();

            var firstFrame = _collection[0];
            Size = new PixelSize((int)firstFrame.Width, (int)firstFrame.Height);
            FrameDurations = _collection.Select(GetFrameDuration).ToArray();
        }
        catch
        {
            _collection.Dispose();
            throw;
        }
    }

    public PixelSize Size { get; }

    public AlphaFormat AlphaFormat => AlphaFormat.Unpremul;

    public IReadOnlyList<TimeSpan> FrameDurations { get; }

    public unsafe void RenderFrame(int index, WriteableBitmap target)
    {
        var frame = _collection[index];

        if (!frame.HasAlpha)
        {
            frame.Alpha(AlphaAction.Opaque);
        }

        using var pixels = frame.GetPixels();
        var bytes = pixels.ToByteArray(PixelMapping.BGRA);
        if (bytes is null)
        {
            throw new InvalidDataException($"Could not read the pixels of frame {index}.");
        }

        using var frameBuffer = target.Lock();

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

    public void Dispose()
    {
        _collection.Dispose();
    }

    private static TimeSpan GetFrameDuration(IMagickImage<byte> frame)
    {
        var ticksPerSecond = frame.AnimationTicksPerSecond <= 0 ? 100 : frame.AnimationTicksPerSecond;
        return TimeSpan.FromSeconds(frame.AnimationDelay / (double)ticksPerSecond);
    }
}
