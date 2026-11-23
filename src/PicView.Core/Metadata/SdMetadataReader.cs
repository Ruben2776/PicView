using ImageMagick;
using PicView.Core.DebugTools;

namespace PicView.Core.Metadata;

/// <summary>
/// Finds Stable Diffusion generation parameters in an image and decodes them.
/// <para>
/// PNG keeps them in text chunks, which are read straight from the file. JPEG and WebP keep
/// them in the EXIF UserComment, which the caller has already decoded, or in the JPEG comment
/// segment.
/// </para>
/// </summary>
public static class SdMetadataReader
{
    /// <summary>
    /// Reads whatever generation parameters the image carries, or null when it has none.
    /// </summary>
    /// <param name="fileInfo">The image file. PNG text chunks are read from it directly.</param>
    /// <param name="exifUserComment">Decoded EXIF UserComment, where JPEG and WebP output lands.</param>
    /// <param name="imageComment">The image's comment attribute, e.g. the JPEG COM segment.</param>
    public static SdMetadata? Read(FileInfo? fileInfo, string? exifUserComment = null, string? imageComment = null)
    {
        try
        {
            var container = SniffContainer(fileInfo);

            if (fileInfo is { Exists: true })
            {
                using var stream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
                var chunks = PngTextChunkReader.Read(stream);
                if (chunks is not null)
                {
                    var fromChunks = ReadFromPngChunks(chunks);
                    if (fromChunks is not null)
                    {
                        var (metadata, keyword) = fromChunks.Value;
                        metadata.Source = SdSource.PngText;
                        metadata.Container = container;
                        metadata.PngKeyword = keyword;
                        return metadata;
                    }
                }
            }

            var fromExif = ReadFromText(exifUserComment);
            if (fromExif is not null)
            {
                fromExif.Source = SdSource.ExifUserComment;
                fromExif.Container = container;
                return fromExif;
            }

            var fromComment = ReadFromText(imageComment);
            if (fromComment is not null)
            {
                fromComment.Source = SdSource.ImageComment;
                fromComment.Container = container;
            }

            return fromComment;
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(SdMetadataReader), nameof(Read), e);
            return null;
        }
    }

    /// <summary>
    /// Looks for generation parameters hidden in the pixel data. This decodes the image, so it
    /// is far more expensive than <see cref="Read"/> and is meant to run only after that has
    /// come up empty. Returns null when the image carries no hidden payload.
    /// </summary>
    public static SdMetadata? ReadStealth(FileInfo? fileInfo, CancellationToken cancellationToken = default)
    {
        if (fileInfo is not { Exists: true })
        {
            return null;
        }

        try
        {
            using var image = new MagickImage(fileInfo);
            cancellationToken.ThrowIfCancellationRequested();

            var payload = StealthPngInfoReader.Read(image, cancellationToken);
            if (payload is null)
            {
                return null;
            }

            var metadata = ParseUnknownPayload(payload.Text) ??
                           SdMetadataParser.ParseA1111(payload.Text);

            metadata.Source = SdSource.Pixels;
            metadata.Container = SniffContainer(fileInfo);
            metadata.StealthDetail = payload.Compressed ? $"{payload.Mode}, compressed" : payload.Mode;
            return metadata;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(SdMetadataReader), nameof(ReadStealth), e);
            return null;
        }
    }

    private static (SdMetadata Metadata, string Keyword)? ReadFromPngChunks(Dictionary<string, string> chunks)
    {
        // Automatic1111, Forge, SD.Next and most of their forks.
        if (TryGet(chunks, "parameters", out var parameters))
        {
            var fromParameters = ParseUnknownPayload(parameters);
            if (fromParameters is not null)
            {
                return (fromParameters, "parameters");
            }
        }

        // InvokeAI.
        if (TryGet(chunks, "invokeai_metadata", out var invokeAi))
        {
            var parsed = SdMetadataParser.ParseInvokeAi(invokeAi);
            if (parsed is not null)
            {
                return (parsed, "invokeai_metadata");
            }
        }

        // NovelAI splits its metadata across Description and Comment.
        if (TryGet(chunks, "Software", out var software) &&
            software.Contains("NovelAI", StringComparison.OrdinalIgnoreCase))
        {
            chunks.TryGetValue("Description", out var description);
            chunks.TryGetValue("Comment", out var novelAiComment);
            var parsed = SdMetadataParser.ParseNovelAi(description, novelAiComment);
            if (parsed is not null)
            {
                return (parsed, "Description");
            }
        }

        // ComfyUI writes the API graph to "prompt" and the editor graph to "workflow".
        if (TryGet(chunks, "prompt", out var prompt))
        {
            chunks.TryGetValue("workflow", out var workflow);
            var parsed = SdMetadataParser.ParseComfyUi(prompt, workflow);
            if (parsed is not null)
            {
                return (parsed, "prompt");
            }
        }

        // Older InvokeAI, and anything else that dropped a payload in a comment chunk.
        if (TryGet(chunks, "sd-metadata", out var sdMetadata) && ParseUnknownPayload(sdMetadata) is { } legacy)
        {
            return (legacy, "sd-metadata");
        }

        if (TryGet(chunks, "Comment", out var comment) && ParseUnknownPayload(comment) is { } fromComment)
        {
            return (fromComment, "Comment");
        }

        return null;
    }

    /// <summary>
    /// Decodes a payload whose producer is not known from the chunk name it arrived under.
    /// </summary>
    internal static SdMetadata? ParseUnknownPayload(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        if (payload.TrimStart().StartsWith('{'))
        {
            return ParseNovelAiWrapper(payload) ??
                   SdMetadataParser.ParseInvokeAi(payload) ??
                   SdMetadataParser.ParseComfyUi(payload, null);
        }

        return SdMetadataParser.ParseA1111(payload);
    }

    /// <summary>
    /// NovelAI's stealth payload is a JSON object holding its PNG text chunks by name
    /// (Description, Software, Comment...), rather than the chunks themselves.
    /// </summary>
    private static SdMetadata? ParseNovelAiWrapper(string payload)
    {
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(payload);
            var root = document.RootElement;
            if (root.ValueKind != System.Text.Json.JsonValueKind.Object)
            {
                return null;
            }

            var chunks = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var property in root.EnumerateObject())
            {
                if (property.Value.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    chunks[property.Name] = property.Value.GetString() ?? string.Empty;
                }
            }

            if (!TryGet(chunks, "Software", out var software) ||
                !software.Contains("NovelAI", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            chunks.TryGetValue("Description", out var description);
            chunks.TryGetValue("Comment", out var comment);
            return SdMetadataParser.ParseNovelAi(description, comment);
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Decodes an EXIF or JPEG comment. Unlike a PNG chunk these are not SD-specific, so a
    /// payload only counts when it actually looks like generation parameters — otherwise every
    /// camera comment would show up as a prompt.
    /// </summary>
    internal static SdMetadata? ReadFromText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var trimmed = text.TrimStart();

        if (trimmed.StartsWith('{'))
        {
            return SdMetadataParser.ParseInvokeAi(text) ?? SdMetadataParser.ParseComfyUi(text, null);
        }

        if (!text.Contains("Steps:", StringComparison.OrdinalIgnoreCase) &&
            !text.Contains("Negative prompt:", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return SdMetadataParser.ParseA1111(text);
    }

    /// <summary>
    /// True when <paramref name="text"/>, read the way <paramref name="source"/> is read, comes back as
    /// A1111 data holding exactly the given prompt, negative prompt and settings. Used to refuse edits
    /// that would be stored as something other than what the user typed.
    /// </summary>
    internal static bool ReadsBackAs(SdSource source, string text, string? prompt, string? negativePrompt,
        string? settings)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var parsed = source == SdSource.PngText ? ParseUnknownPayload(text) : ReadFromText(text);
        return parsed is { Format: SdFormat.A1111 } &&
               SdMetadataParser.Normalize(parsed.Prompt) == SdMetadataParser.Normalize(prompt) &&
               SdMetadataParser.Normalize(parsed.NegativePrompt) == SdMetadataParser.Normalize(negativePrompt) &&
               SdMetadataParser.Normalize(parsed.Settings) == SdMetadataParser.Normalize(settings);
    }

    /// <summary>Identifies the file type from its first bytes: PNG signature, FF D8 FF, or RIFF....WEBP.</summary>
    private static SdContainer SniffContainer(FileInfo? fileInfo)
    {
        if (fileInfo is not { Exists: true })
        {
            return SdContainer.Other;
        }

        try
        {
            Span<byte> header = stackalloc byte[12];
            using var stream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
            var read = stream.ReadAtLeast(header, header.Length, throwOnEndOfStream: false);
            header = header[..read];

            if (header.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
            {
                return SdContainer.Png;
            }

            if (header.StartsWith(new byte[] { 0xFF, 0xD8, 0xFF }))
            {
                return SdContainer.Jpeg;
            }

            if (header.Length == 12 && header[..4].SequenceEqual("RIFF"u8) && header[8..].SequenceEqual("WEBP"u8))
            {
                return SdContainer.WebP;
            }
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(SdMetadataReader), nameof(SniffContainer), e);
        }

        return SdContainer.Other;
    }

    private static bool TryGet(Dictionary<string, string> chunks, string key, out string value)
    {
        if (chunks.TryGetValue(key, out var found) && !string.IsNullOrWhiteSpace(found))
        {
            value = found;
            return true;
        }

        value = string.Empty;
        return false;
    }
}
