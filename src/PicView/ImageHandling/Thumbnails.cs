using ImageMagick;
using PicView.ChangeImage;
using PicView.UILogic;
using PicView.Views.UserControls.Gallery;
using System.Diagnostics;
using System.IO;
using System.Windows.Media.Imaging;
using static PicView.ChangeImage.Navigation;
using static PicView.UILogic.UC;

namespace PicView.ImageHandling
{
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
                if (ConfigureWindows.GetMainWindow.CheckAccess() && GetPicGallery != null &&
                    GetPicGallery.Container.Children.Count > 0 && x < GetPicGallery.Container.Children.Count)
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

        internal static BitmapSource GetBitmapSourceThumb(string file, int size, FileInfo? fileInfo = null)
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

                fileInfo ??= new FileInfo(file);
                var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096,
                    fileInfo.Length > 1e+8);
                image.Read(fileStream);
                image.Thumbnail(new MagickGeometry(size, size));
                var bmp = image.ToBitmapSource();
                bmp?.Freeze();
                image.Dispose();
                return bmp ?? ImageFunctions.ShowLogo();
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
}