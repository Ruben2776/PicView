namespace PicView.Core.Metadata;

/// <summary>
/// Generation parameters recovered from an AI-generated image.
/// </summary>
public sealed class SdMetadata
{
    /// <summary>Tool that wrote the metadata, e.g. "Automatic1111" or "ComfyUI".</summary>
    public required string Generator { get; init; }

    /// <summary>Positive prompt, when one could be isolated.</summary>
    public string? Prompt { get; init; }

    /// <summary>Negative prompt, when one could be isolated.</summary>
    public string? NegativePrompt { get; init; }

    /// <summary>Sampler, steps, CFG, seed, model and friends, formatted for display.</summary>
    public string? Settings { get; init; }

    /// <summary>The unmodified payload, so nothing is lost when parsing falls short.</summary>
    public required string Raw { get; init; }

    /// <summary>Which generator format the payload is in.</summary>
    public required SdFormat Format { get; init; }

    /// <summary>Where the payload was found. Stamped by <see cref="SdMetadataReader"/>.</summary>
    public SdSource Source { get; set; }

    /// <summary>The kind of file the payload was found in.</summary>
    public SdContainer Container { get; set; }

    /// <summary>The PNG text chunk keyword the data came from, when <see cref="Source"/> is a PNG chunk.</summary>
    public string? PngKeyword { get; set; }

    /// <summary>For pixel-hidden data, the stealth mode, for example "alpha, compressed".</summary>
    public string? StealthDetail { get; set; }

    /// <summary>
    /// True when the data can be written back: A1111 text in a PNG <c>parameters</c> chunk, or in the
    /// Exif UserComment of a JPEG or WebP.
    /// </summary>
    public bool IsEditable => Format == SdFormat.A1111 &&
                              ((Source == SdSource.PngText && Container == SdContainer.Png &&
                                PngKeyword == "parameters") ||
                               (Source == SdSource.ExifUserComment &&
                                Container is SdContainer.Jpeg or SdContainer.WebP));
}

/// <summary>Where generation data was found in the file.</summary>
public enum SdSource
{
    PngText,
    ExifUserComment,
    ImageComment,
    Pixels
}

/// <summary>The generator format the payload is written in.</summary>
public enum SdFormat
{
    A1111,
    ComfyUi,
    NovelAi,
    InvokeAi
}

/// <summary>The file type, sniffed from its first bytes.</summary>
public enum SdContainer
{
    Png,
    Jpeg,
    WebP,
    Other
}
