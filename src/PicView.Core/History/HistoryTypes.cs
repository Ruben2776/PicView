using System.Collections.Generic;
using ImageMagick;

namespace PicView.Core.History;

public enum EditKind
{
    Crop,
    Rotate,
    Flip,
    Effect,
    Resize,
    Other
}

public sealed class EffectSettings
{
    public string? EffectName { get; set; }
    public Dictionary<string, double>? Parameters { get; set; }
}

public sealed class HistoryEntry
{
    public required EditKind Kind { get; init; }
    public string? Description { get; init; } 
    public EffectSettings? Settings { get; init; }
    public required MagickImage Snapshot { get; init; }
}
