using ImageMagick;
using PicView.ChangeImage;
using PicView.Views.UserControls.Gallery;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System.Diagnostics;
using System.IO;
using System.Windows.Media.Imaging;
using static PicView.ChangeImage.Navigation;
using static PicView.UILogic.UC;

namespace PicView.ImageHandling;

internal static class Thumbnails
{
    /// <summary>
    /// Load thumbnail at provided index
    /// </summary>
    /// <returns></returns>
    internal static BitmapSource? GetThumb(int x, FileInfo? fileInfo = null)
    {
        BitmapSource? pic;
        try
        {
            if (GetPicGallery != null && GetPicGallery.Container.Children.Count > 0 && x < GetPicGallery.Container.Children.Count)
            {
                var y = GetPicGallery.Container.Children[x] as PicGalleryItem;
                pic = (BitmapSource)y.ThumbImage.Source;
            }
            else
            {
                if (fileInfo is null)
                {
                    var preLoadValue = PreLoader.Get(x);
                    if (preLoadValue is null)
                    {
                        fileInfo = new FileInfo(Pics[x]);
                    }
                    else
                    {
                        return preLoadValue.BitmapSource;
                    }
                }

                using var image = new MagickImage();
                image.Ping(fileInfo);
                var thumb = image.GetExifProfile()?.CreateThumbnail();
                pic = thumb?.ToBitmapSource();
            }
        }
        catch (Exception)
        {
            return null;
        }

        if (pic is { IsFrozen: false })
        {
            pic.Freeze();
        }

        return pic;
    }

    internal static BitmapSource GetBitmapSourceThumb(string file, int size)
    {
        try
        {
            using var image = new MagickImage();
            image.Ping(file);
            var thumb = image.GetExifProfile()?.CreateThumbnail();
            var bitmapThumb = thumb?.ToBitmapSource();
            if (bitmapThumb != null)
            {
                bitmapThumb.Freeze();
                return bitmapThumb;
            }
        }
        catch (Exception e)
        {
#if DEBUG
            Trace.WriteLine(nameof(GetBitmapSourceThumb) + " " + e.Message);
#endif
        }

        try
        {
            var fileInfo = new FileInfo(file);
            var extension = fileInfo.Extension.ToLowerInvariant();
            var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize: 4096,
                useAsync: fileInfo.Length > 1e+8);
            switch (extension)
            {
                case ".jpg":
                case ".jpeg":
                case ".jpe":
                case ".png":
                case ".bmp":
                case ".gif":
                case ".jfif":
                case ".ico":
                case ".webp":
                case ".wbmp":

                    var sKBitmap = SKBitmap.Decode(fileStream);
                    if (sKBitmap is null)
                    {
                        return ImageFunctions.ShowLogo() ?? ImageFunctions.ImageErrorMessage();
                    }

                    var skPic = sKBitmap.Resize(new SKImageInfo(size, size, SKColorType.Rgba8888), SKFilterQuality.High)
                        .ToWriteableBitmap();
                    sKBitmap.Dispose();
                    fileStream.Dispose();
                    skPic.Freeze();
                    return skPic;

                default:
                    return ImageFunctions.ShowLogo() ?? ImageFunctions.ImageErrorMessage();
            }
        }
        catch (Exception e)
        {
#if DEBUG
            Trace.WriteLine(nameof(GetBitmapSourceThumb) + " " + e.Message);
#endif
            return ImageFunctions.ImageErrorMessage();
        }
    }
}