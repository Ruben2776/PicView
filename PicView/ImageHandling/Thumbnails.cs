using ImageMagick;
using Microsoft.WindowsAPICodePack.Shell;
using PicView.ChangeImage;
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
        /// or full image if preloaded.
        /// </summary>
        /// <returns></returns>
        internal static BitmapSource? GetThumb(int x, FileInfo? fileInfo = null)
        {
            if (ErrorHandling.CheckOutOfRange())
            {
                return null;
            }

            BitmapSource? pic;

            if (GetPicGallery != null
                && GetPicGallery.Container.Children.Count > 0
                && x < GetPicGallery.Container.Children.Count
                && GetPicGallery.Container.Children.Count == Pics.Count)
            {
                var y = GetPicGallery.Container.Children[x] as PicGalleryItem;
                pic = (BitmapSource)y.img.Source;
            }
            else
            {
                fileInfo ??= new FileInfo(Pics[x]);
                pic = GetBitmapSourceThumb(fileInfo);
            }

            if (pic == null)
            {
                return null;
            }

            if (!pic.IsFrozen)
            {
                pic.Freeze();
            }

            return pic;
        }

        internal static BitmapSource? GetBitmapSourceThumb(FileInfo fileInfo, int size = 500)
        {
            if (fileInfo.Length > 2e+7)
            {
                return ImageFunctions.ShowLogo();
            }

            try
            {
                switch (fileInfo.Extension)
                {
                    case ".jpg":
                    case ".jpeg":
                    case ".jpe":
                    case ".png":
                    case ".bmp":
                    case ".gif":
                    case ".ico":
                    case ".jfif":
                    case ".wbmp":
                        return GetWindowsThumbnail(fileInfo.FullName);
                    case ".b64":
                        return ImageFunctions.ShowLogo();
                }

                return GetMagickImageThumb(fileInfo, size) ?? ImageFunctions.ShowLogo();
            }
            catch (Exception e)
            {
#if DEBUG
                Trace.WriteLine(nameof(GetBitmapSourceThumb) + " " + e.Message);
#endif
                return ImageFunctions.ImageErrorMessage();
            }
        }

        /// <summary>
        /// Returns BitmapSource at specified quality and pixel size
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <param name="quality"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        private static BitmapSource? GetMagickImageThumb(FileInfo fileInfo, int size = 500)
        {
            try
            {
                using (MagickImage image = new MagickImage(fileInfo))
                {
                    image.Thumbnail(new MagickGeometry(size, size));
                    var bmp = image.ToBitmapSource();
                    bmp.Freeze();
                    return bmp;
                }
            }
#if DEBUG
            catch (Exception e)
            {
                Trace.WriteLine("GetMagickImage returned " + fileInfo + " null, \n" + e.Message);
                return null;
            }
#else
                catch (Exception) { return null; }
#endif
        }

        /// <summary>
        /// Returns a Windows Thumbnail
        /// </summary>
        /// <param name="path">The path to the file</param>
        /// <returns></returns>
        private static BitmapSource? GetWindowsThumbnail(string path)
        {
            try
            {
                BitmapSource pic = ShellFile.FromFilePath(path).Thumbnail.BitmapSource;
                pic.Freeze();
                return pic;
            }
            catch (Exception e)
            {
#if DEBUG
                Trace.WriteLine(nameof(GetWindowsThumbnail) + " " + e.Message);
#endif
                return null;
            }
        }
    }
}