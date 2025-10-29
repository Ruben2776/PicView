namespace PicView.Core.Models;

public class BatchLogEntry
{
    public string FileName { get; set; } = string.Empty;
    public string OldSize { get; set; } = string.Empty;
    public string NewSize { get; set; } = string.Empty;

    public override string ToString() => $"{FileName} {OldSize} -> {NewSize}";
}
