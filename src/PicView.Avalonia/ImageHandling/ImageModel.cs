using PicView.Avalonia.Navigation;
using PicView.Core.ImageDecoding;

namespace PicView.Avalonia.ImageHandling;

public class ImageModel
{
    public object? Image { get; set; }
    public FileInfo? FileInfo { get; set; }
    public int PixelWidth { get; set; }
    public int PixelHeight { get; set; }
    public EXIFHelper.EXIFOrientation? EXIFOrientation { get; set; }
    public ImageType ImageType { get; set; }

    public double Rotation
    {
        get
        {
            if (!EXIFOrientation.HasValue)
            {
                return 0;
            }

            return EXIFOrientation switch
            {
                EXIFHelper.EXIFOrientation.None or EXIFHelper.EXIFOrientation.Normal
                    or EXIFHelper.EXIFOrientation.Flipped => 0,
                EXIFHelper.EXIFOrientation.Rotated180 or EXIFHelper.EXIFOrientation.Rotated180Flipped => 180,
                EXIFHelper.EXIFOrientation.Rotated270Flipped or EXIFHelper.EXIFOrientation.Rotated270 => 270,
                EXIFHelper.EXIFOrientation.Rotated90 or EXIFHelper.EXIFOrientation.Rotated90Flipped => 90,
                _ => 0
            };
        }
    }
}