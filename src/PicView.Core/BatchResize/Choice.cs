namespace PicView.Core.BatchResize;

/// <summary>
/// One option of a drop-down, shown by its label and bound by its value.
/// </summary>
public sealed record Choice<T>(T Value, string Label);
