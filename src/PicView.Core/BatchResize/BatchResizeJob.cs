namespace PicView.Core.BatchResize;

/// <summary>
/// The files, output folder, conversion, compression, quality, resize and Batch Thumbnails of one Batch Resize run,
/// fixed when it starts.
/// </summary>
public sealed record BatchResizeJob
{
    public required IReadOnlyList<FileInfo> Files { get; init; }

    public required string OutputFolder { get; init; }

    public ConversionTarget Conversion { get; init; }

    public CompressionMode Compression { get; init; }

    /// <summary>
    /// The quality to write at, or <see langword="null"/> to leave it to the format.
    /// </summary>
    public uint? Quality { get; init; }

    public ResizeMode Resize { get; init; }

    /// <summary>
    /// The width to resize to, used by <see cref="ResizeMode.Width"/> and <see cref="ResizeMode.WidthAndHeight"/>.
    /// </summary>
    public uint Width { get; init; }

    /// <summary>
    /// The height to resize to, used by <see cref="ResizeMode.Height"/> and <see cref="ResizeMode.WidthAndHeight"/>.
    /// </summary>
    public uint Height { get; init; }

    /// <summary>
    /// The percentage to resize to, used by <see cref="ResizeMode.Percentage"/>.
    /// </summary>
    public double Percentage { get; init; } = 100;

    /// <summary>
    /// Whether <see cref="ResizeMode.WidthAndHeight"/> fits the image inside the width and height instead of
    /// stretching it to them.
    /// </summary>
    public bool KeepAspectRatio { get; init; } = true;

    public IReadOnlyList<BatchThumbnail> Thumbnails { get; init; } = [];
}
