using System.IO;

namespace XamlAnimatedGif.Decoding;

internal abstract class GifExtension : GifBlock
{
    internal const int ExtensionIntroducer = 0x21;

    internal new static async Task<GifExtension> ReadAsync(Stream stream,
        IEnumerable<GifExtension> controlExtensions)
    {
        // Note: at this point, the Extension Introducer (0x21) has already been read

        var label = stream.ReadByte();
        if (label < 0)
            throw new EndOfStreamException();
        return label switch
        {
            GifGraphicControlExtension.ExtensionLabel => await GifGraphicControlExtension.ReadAsync(stream)
                .ConfigureAwait(false),
            GifCommentExtension.ExtensionLabel => await GifCommentExtension.ReadAsync(stream).ConfigureAwait(false),
            GifPlainTextExtension.ExtensionLabel => await GifPlainTextExtension.ReadAsync(stream, controlExtensions)
                .ConfigureAwait(false),
            GifApplicationExtension.ExtensionLabel => await GifApplicationExtension.ReadAsync(stream)
                .ConfigureAwait(false),
            _ => throw GifHelpers.UnknownExtensionTypeException(label),
        };
    }
}