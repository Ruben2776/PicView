using PicView.Core.ImageDecoding;

namespace PicView.Avalonia.AnimatedImage;

internal static class AnimationDecoders
{
    /// <summary>
    ///     Creates the decoder for an animated image type. The caller keeps ownership of <paramref name="stream" />.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The image type is not animated.</exception>
    public static IAnimationDecoder Create(ImageType imageType, Stream stream)
    {
        return imageType switch
        {
            ImageType.AnimatedGif => new GifAnimationDecoder(stream),
            ImageType.AnimatedWebp => new WebpAnimationDecoder(stream),
            ImageType.AnimatedAvif => CreateAvif(stream),
            _ => throw new ArgumentOutOfRangeException(nameof(imageType), imageType, "The image type is not animated.")
        };
    }

    /// <summary>
    ///     An animated AVIF is decoded as it plays when the bundled FFmpeg library can do it. Otherwise ImageMagick
    ///     decodes every frame before the first one is shown.
    /// </summary>
    private static IAnimationDecoder CreateAvif(Stream stream)
    {
        PrepareStream(stream);
        return FFmpegAvifAnimationDecoder.TryCreate(stream) ?? (IAnimationDecoder)new AvifAnimationDecoder(stream);
    }

    internal static void PrepareStream(Stream stream)
    {
        if (!stream.CanSeek)
        {
            throw new InvalidDataException("The provided stream is not seekable.");
        }

        if (!stream.CanRead)
        {
            throw new InvalidOperationException("Can't read the stream provided.");
        }

        stream.Seek(0, SeekOrigin.Begin);
    }
}
