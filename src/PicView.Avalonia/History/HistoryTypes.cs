using System.Collections.Generic;
using ImageMagick;
using Avalonia.Media.Imaging;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace PicView.Avalonia.History;

public enum EditKind
{
    Open,
    Crop,
    Rotate,
    [Display(Name = "Flip")] FlipH,
    [Display(Name = "Flip")] FlipV,
    Effect,
    Resize,
    Other
}

public sealed class HistoryEntry
{
    public int Index { get; set; }
    public EditKind Kind { get; set; }
    public string Description { get; set; } = "";
    public bool IsLoading { get; set; }
    public Bitmap? Snapshot { get; set; }
}

