using PicView.Core.Localization;

namespace PicView.Core.BatchResize;

/// <summary>
/// An extra, smaller copy of each file a Batch Resize Job writes, resized from the source file.
/// </summary>
/// <param name="Subfolder">The folder inside the job's output folder it is written to.</param>
/// <param name="Size">What <paramref name="Value"/> resizes by.</param>
/// <param name="Value">A percentage, or a width or height in pixels.</param>
public sealed record BatchThumbnail(string Subfolder, ThumbnailSize Size, double Value)
{
    /// <summary>
    /// The most Batch Thumbnails a job has.
    /// </summary>
    public const int Max = 7;

    /// <summary>
    /// The Batch Thumbnails a job starts with when the user asks for this many, largest first.
    /// </summary>
    public static IReadOnlyList<BatchThumbnail> Defaults(int count) => count switch
    {
        1 => [Percent(TranslationManager.Translation.Thumbnail ?? "Thumb", 30)],
        2 => [Percent("medium", 50), Percent("small", 30)],
        3 => [Percent("large", 70), Percent("medium", 50), Percent("small", 30)],
        4 => [Percent("large", 70), Percent("medium", 50), Percent("small", 30), Percent("xs", 15)],
        5 => [Percent("xl", 80), Percent("large", 70), Percent("medium", 50), Percent("small", 30), Percent("xs", 15)],
        6 =>
        [
            Percent("xl", 80), Percent("large", 70), Percent("medium", 60), Percent("small", 50), Percent("xs", 40),
            Percent("xxs", 30)
        ],
        7 =>
        [
            Percent("xxl", 85), Percent("xl", 75), Percent("large", 65), Percent("medium", 50), Percent("small", 40),
            Percent("xs", 30), Percent("xxs", 20)
        ],
        _ => []
    };

    private static BatchThumbnail Percent(string subfolder, double value) =>
        new(subfolder, ThumbnailSize.Percentage, value);
}

/// <summary>
/// What a Batch Thumbnail is resized by.
/// </summary>
public enum ThumbnailSize
{
    Percentage,
    Width,
    Height
}
