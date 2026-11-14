namespace PicView.Core.Config;

/// <summary>
/// What every remembered Tool Window or App Window saves: where it was and whether it was maximized.
/// A window's own properties derive from this and add only their own fields.
/// </summary>
public class RememberedWindowProperties : IWindowProperties
{
    public int? Top { get; set; }
    public int? Left { get; set; }
    public double? Width { get; set; }
    public double? Height { get; set; }
    public bool Maximized { get; set; }
}
