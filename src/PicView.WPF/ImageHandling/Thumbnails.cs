using System.Diagnostics;
using System.IO;
using System.Windows.Media.Imaging;
using ImageMagick;
using PicView.WPF.ChangeImage;
using PicView.WPF.UILogic;
using PicView.WPF.Views.UserControls.Gallery;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using static PicView.WPF.ChangeImage.Navigation;
using static PicView.WPF.UILogic.UC;

namespace PicView.WPF.ImageHandling;

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

    internal static async Task<BitmapSource> GetBitmapSourceThumbAsync(string file, int size, FileInfo? fileInfo = null)
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
            await using var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096,
                fileInfo.Length > 1e+8);

            switch (fileInfo.Extension.ToLowerInvariant())
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
                    var skImage = SKBitmap.Decode(fileStream);
                    if (skImage is null)
                    {
                        return ImageFunctions.ImageErrorMessage();
                    }
                    var resized = skImage.Resize(new SKImageInfo(size, size), SKFilterQuality.Medium);
                    var writeableBitmap = resized.ToWriteableBitmap();
                    writeableBitmap.Freeze();
                    return writeableBitmap;
            }

            if (fileInfo.Length >= 2147483648)
            {
                // Fixes "The file is too long. This operation is currently limited to supporting files less than 2 gigabytes in size."
                // ReSharper disable once MethodHasAsyncOverload
                image?.Read(fileStream);
            }
            else
            {
                await image.ReadAsync(fileStream).ConfigureAwait(false);
            }
            image?.Thumbnail(new MagickGeometry(size, size));
            var bmp = image?.ToBitmapSource();
            bmp?.Freeze();
            image?.Dispose();
            return bmp ?? ImageFunctions.ImageErrorMessage();
        }
        catch (Exception e)
        {
#if DEBUG
            Trace.WriteLine(nameof(GetBitmapSourceThumbAsync) + " " + e.Message);
#endif
            return ImageFunctions.ImageErrorMessage();
        }
    }
}