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
            ImageType.AnimatedAvif => new AvifAnimationDecoder(stream),
            _ => throw new ArgumentOutOfRangeException(nameof(imageType), imageType, "The image type is not animated.")
        };
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
