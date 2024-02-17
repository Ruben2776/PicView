using Avalonia.Media;
using PicView.Core.ImageDecoding;

namespace PicView.Avalonia.Models;

public class ImageModel
{
    public IImage? Image { get; set; }
    public FileInfo? FileInfo { get; set; }
    public int PixelWidth { get; set; }
    public int PixelHeight { get; set; }
    public EXIFHelper.EXIFOrientation? EXIFOrientation { get; set; }

    public double Rotation
    {
        get
        {
            if (!EXIFOrientation.HasValue)
            {
                return 0;
            }

            switch (EXIFOrientation)
            {
                case EXIFHelper.EXIFOrientation.None:
                case EXIFHelper.EXIFOrientation.Normal:
                case EXIFHelper.EXIFOrientation.Flipped:
                    return 0;

                case EXIFHelper.EXIFOrientation.Rotated180:
                case EXIFHelper.EXIFOrientation.Rotated180Flipped:
                    return 180;

                case EXIFHelper.EXIFOrientation.Rotated270Flipped:
                case EXIFHelper.EXIFOrientation.Rotated270:
                    return 270;

                case EXIFHelper.EXIFOrientation.Rotated90:
                case EXIFHelper.EXIFOrientation.Rotated90Flipped:
                    return 90;
            }
            return 0;
        }
    }
}