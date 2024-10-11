using PicView.Core.ImageDecoding;

namespace PicView.Avalonia.ImageHandling;

public record ImageModel
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
                EXIFHelper.EXIFOrientation.None or EXIFHelper.EXIFOrientation.Horizontal
                    or EXIFHelper.EXIFOrientation.MirrorHorizontal => 0,
                EXIFHelper.EXIFOrientation.Rotate180 or EXIFHelper.EXIFOrientation.MirrorVertical => 180,
                EXIFHelper.EXIFOrientation.MirrorHorizontalRotate270Cw or EXIFHelper.EXIFOrientation.Rotated270Cw => 90,
                EXIFHelper.EXIFOrientation.Rotate90Cw => 90,
                EXIFHelper.EXIFOrientation.MirrorHorizontalRotate90Cw => 270,
                _ => 0
            };
        }
    }
}