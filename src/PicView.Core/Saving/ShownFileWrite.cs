using PicView.Core.ImageTransformations;

namespace PicView.Core.Saving;

/// <summary>
/// What a write of the tab's shown image starts from.
/// </summary>
public enum WriteSource
{
    /// <summary>The Save Source: the Effects Render of the tab's file, else the file, else the Single Image pixels.</summary>
    SaveSource,

    /// <summary>The file on disk, else the Single Image pixels; never effects.</summary>
    Unedited
}

/// <summary>
/// The edits a write applies on top of the tab's turn and flip.
/// </summary>
/// <param name="Cut">The area to cut out, in the image as the viewer shows it before its turn; null keeps it whole.</param>
/// <param name="Width">The width to resize to; 0 keeps the aspect from <paramref name="Height"/>.</param>
/// <param name="Height">The height to resize to; 0 keeps the aspect from <paramref name="Width"/>. Both 0 is no resize.</param>
/// <param name="Quality">The encoder quality, or null for the format's default.</param>
/// <param name="LosslessOptimize">Optimize the written file losslessly; applies no turn.</param>
public readonly record struct WriteEdits(
    PixelArea? Cut = null,
    uint Width = 0,
    uint Height = 0,
    uint? Quality = null,
    bool LosslessOptimize = false);

/// <summary>
/// Where a write lands.
/// </summary>
public abstract record WriteDestination
{
    /// <summary>Over the tab's own file.</summary>
    public sealed record OverItself : WriteDestination;

    /// <summary>A new file that takes the place of the tab's file.</summary>
    public sealed record Replace(string NewPath) : WriteDestination;

    /// <summary>Another file; the tab keeps showing its own.</summary>
    public sealed record Copy(string Path) : WriteDestination;
}

/// <summary>
/// One write of the tab's shown image.
/// </summary>
public sealed record ShownFileWrite(WriteSource Source, WriteEdits Edits, WriteDestination Destination);

public enum WriteOutcome
{
    Written,
    NameTaken,
    NothingToWrite,
    Failed
}

/// <summary>
/// How a write went.
/// </summary>
/// <param name="WrittenPath">The file written, or null when nothing was.</param>
/// <param name="TurnApplied">The image was turned or flipped to match a tab that was not upright.</param>
/// <param name="EffectsApplied">An Effects Render was what got written.</param>
public readonly record struct WriteResult(
    WriteOutcome Outcome,
    string? WrittenPath,
    bool TurnApplied,
    bool EffectsApplied);
