namespace PicView.Core.Svg;

/// <summary>An sRGB color with straight (non-premultiplied) alpha.</summary>
public readonly record struct SvgColor(byte A, byte R, byte G, byte B)
{
    public static SvgColor Black { get; } = new(255, 0, 0, 0);
}
