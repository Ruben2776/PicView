using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Svg.Skia;
using PicView.Avalonia.Navigation;
using PicView.Core.ImageDecoding;

namespace PicView.Avalonia.ImageHandling;

public static class ImageFunctions
{
    public static void SetImage(object image, Image imageControl, ImageType imageType)
    {
        imageControl.Source = imageType switch
        {
            ImageType.Svg => new SvgImage { Source = SvgSource.Load(image as string) },
            ImageType.Bitmap => image as Bitmap,
            _ => imageControl.Source
        };
    }

    public static bool IsAnimated(FileInfo fileInfo)
    {
        var frames = ImageFunctionHelper.GetImageFrames(fileInfo.FullName);
        return frames > 1;
    }

    public static bool HasTransparentBackground(object imageSource)
    {
        // TODO implement
        return true;
    }
}
