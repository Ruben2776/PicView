using ImageMagick;
using PicView.ChangeImage;
using PicView.Views.UserControls.Gallery;
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
            try
            {
                using var image = new MagickImage();
                image.Ping(fileInfo);
                var thumb = image.GetExifProfile()?.CreateThumbnail();
                pic = thumb?.ToBitmapSource();
            }
            catch (Exception)
            {
                return null;
            }
        }

        if (pic is { IsFrozen: false })
        {
            pic.Freeze();
        }

        return pic;
    }

    internal class LogoOrThumbHolder
    {
        internal readonly BitmapSource Thumb;

        internal readonly bool isLogo;

        internal const double Size = 256; // Set it to the size of the logo

        public LogoOrThumbHolder(BitmapSource thumb, bool isLogo)
        {
            Thumb = thumb;
            this.isLogo = isLogo;
        }
    }

    internal static LogoOrThumbHolder GetBitmapSourceThumb(FileInfo fileInfo, int size)
    {
        try
        {
            using var image = new MagickImage();
            image.Ping(fileInfo);
            var thumb = image.GetExifProfile()?.CreateThumbnail();
            if (thumb is not null)
            {
                var bitmapThumb = thumb.ToBitmapSource();
                bitmapThumb.Freeze();
                return new LogoOrThumbHolder(bitmapThumb, false);
            }

            if (fileInfo.Length > 5.0e+8)
            {
                return new LogoOrThumbHolder(ImageFunctions.ShowLogo() ?? ImageFunctions.ImageErrorMessage(), true);
            }
            image.Read(fileInfo);
            image.Thumbnail(new MagickGeometry(size, size));
            var bmp = image.ToBitmapSource();
            bmp.Freeze();
            return new LogoOrThumbHolder(bmp, false);
        }
        catch (Exception e)
        {
#if DEBUG
            Trace.WriteLine(nameof(GetBitmapSourceThumb) + " " + e.Message);
#endif
            return new LogoOrThumbHolder(ImageFunctions.ImageErrorMessage(), false);
        }
    }
}