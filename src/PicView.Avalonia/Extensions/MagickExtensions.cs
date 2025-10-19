using System.IO;
using Avalonia.Media.Imaging;
using ImageMagick;
using R3;

namespace PicView.Avalonia.Extensions;

public static class MagickExtensions
{
    public static MagickImage ToMagickImage(this Bitmap bmp)
    {
        using var ms = new MemoryStream();
        bmp.Save(ms);
        ms.Position = 0;
        return new MagickImage(ms);
    }

    public static Bitmap ToAvaloniaBitmap(this MagickImage img)
    {
        using var ms = new MemoryStream();
        img.Write(ms, MagickFormat.Png);
        ms.Position = 0;
        return new Bitmap(ms);
    }

    public static Bitmap ToAvaloniaBitmap(this IMagickImage<byte> img)
    {
        using var ms = new MemoryStream();
        img.Write(ms, MagickFormat.Png);
        ms.Position = 0;
        return new Bitmap(ms);
    }
    
    public static Bitmap ToThumbnail(this MagickImage img, int max)
    {
        using var clone = img.Clone();

        var w = (double)clone.Width;
        var h = (double)clone.Height;

        // Determine the scale factor so the longest side equals 'max'
        var scale = Math.Min(1.0, max / Math.Max(w, h));

        // Only resize if image is larger than 'max'
        if (scale < 1.0)
            clone.Resize((uint)(w * scale), (uint)(h * scale));

        return clone.ToAvaloniaBitmap();
    }

}
