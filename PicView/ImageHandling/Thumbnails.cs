using ImageMagick;
using PicView.Views.UserControls;
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
        internal static BitmapSource? GetThumb(int x)
        {
            if (ChangeImage.Error_Handling.CheckOutOfRange())
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
                pic = GetBitmapSourceThumb(new FileInfo(Pics[x]));
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

        internal static BitmapSource? GetBitmapSourceThumb(FileInfo fileInfo)
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
                case ".webp":
                case ".wbmp":
                    return GetWindowsThumbnail(fileInfo.FullName);
                default:
                    break;
            }

            if (fileInfo.Length > 1e+9)
            {
                return null;
            }

            return GetMagickImageThumb(fileInfo.FullName);
        }

        /// <summary>
        /// Returns BitmapSource at specified quality and pixel size
        /// </summary>
        /// <param name="file"></param>
        /// <param name="quality"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        private static BitmapSource? GetMagickImageThumb(string file, byte quality = 100, short size = 500)
        {
            BitmapSource pic;

            using MagickImage magick = new MagickImage
            {
                Quality = quality,
                ColorSpace = ColorSpace.Transparent
            };
            try
            {
                magick.Read(file);
                magick.AdaptiveResize(size, size);
            }
#if DEBUG
            catch (MagickException e)
            {
                Trace.WriteLine("GetMagickImage returned " + file + " null, \n" + e.Message);
                return null;
            }
#else
                catch (MagickException) { return null; }
#endif
            pic = magick.ToBitmapSource();
            pic.Freeze();
            return pic;
        }

        /// <summary>
        /// Returns a Windows Thumbnail
        /// </summary>
        /// <param name="path">The path to the file</param>
        /// <returns></returns>
        private static BitmapSource GetWindowsThumbnail(string path)
        {
            BitmapSource pic;
            pic = Microsoft.WindowsAPICodePack.Shell.ShellFile.FromFilePath(path).Thumbnail.BitmapSource;
            pic.Freeze();
            return pic;
        }
    }
}