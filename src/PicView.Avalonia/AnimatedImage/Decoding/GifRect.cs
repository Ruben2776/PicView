using System.Runtime.InteropServices;

namespace PicView.Avalonia.AnimatedImage.Decoding;

[StructLayout(LayoutKind.Auto)]
public readonly struct GifRect(int x, int y, int width, int height) : IEquatable<GifRect>
{
    public int X { get; } = x;
    public int Y { get; } = y;
    public int Width { get; } = width;
    public int Height { get; } = height;
    public int TotalPixels { get; } = width * height;

    public static bool operator ==(GifRect a, GifRect b)
    {
        return a.X == b.X && a.Y == b.Y && a.Width == b.Width && a.Height == b.Height;
    }

    public static bool operator !=(GifRect a, GifRect b)
    {
        return !(a == b);
    }

    public override bool Equals(object? obj)
    {
        if (obj == null || GetType() != obj.GetType())
            return false;

        return this == (GifRect)obj;
    }

    public override int GetHashCode()
    {
        return X.GetHashCode() ^ Y.GetHashCode() | Width.GetHashCode() ^ Height.GetHashCode();
    }

    public bool Equals(GifRect other)
    {
        return X == other.X && Y == other.Y && Width == other.Width && Height == other.Height && TotalPixels == other.TotalPixels;
    }
}