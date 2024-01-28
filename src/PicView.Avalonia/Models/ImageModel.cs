using Avalonia.Media;
using Avalonia.Media.Imaging;
using ImageMagick;
using PicView.Core.FileHandling;
using PicView.Core.ImageDecoding;

namespace PicView.Avalonia.Models;

public class ImageModel
{
    public IImage? Image { get; set; }
    public FileInfo? FileInfo { get; set; }
    public int PixelWidth { get; set; }
    public int PixelHeight { get; set; }
    public EXIFHelper.EXIFOrientation? EXIFOrientation { get; set; }
    public bool IsAnimated { get; private set; }
    public bool IsFlipped { get; set; }

    private int _rotation;

    public int Rotation
    {
        get
        {
            if (EXIFOrientation.HasValue)
            {
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
            }
            return 0;
        }
        set { _rotation = value; }
    }

    public static async Task LoadImageAsync(ImageModel imageModel)
    {
        if (imageModel?.FileInfo is not { Length: > 0 })
        {
            return;
        }

        var extension = imageModel.FileInfo.Extension.ToLowerInvariant();
        var bytes = await FileHelper.GetBytesFromFile(imageModel.FileInfo.FullName).ConfigureAwait(false);
        using var memoryStream = new MemoryStream(bytes);

        switch (extension)
        {
            case ".gif":
            case ".png":
            case ".webp":
                using (var magick = new MagickImageCollection(imageModel.FileInfo))
                {
                    imageModel.IsAnimated = magick.Count > 1;
                }
                goto default;

            case ".svg":
                break;

            case ".svgz":
                break;

            case ".b64":
                break;

            default:
                var bmp = new Bitmap(memoryStream);
                imageModel.Image = bmp;
                imageModel.PixelWidth = bmp.PixelSize.Width;
                imageModel.PixelHeight = bmp.PixelSize.Height;
                break;
        }
    }
}