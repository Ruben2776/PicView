using ImageMagick;
using PicView.WPF.ChangeImage;
using PicView.WPF.Views.UserControls.Gallery;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System.Diagnostics;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static PicView.WPF.ChangeImage.Navigation;
using static PicView.WPF.UILogic.UC;

namespace PicView.WPF.ImageHandling;

internal static class Thumbnails
{
    internal struct ThumbHolder(double? width, double? height, BitmapSource? bitmapSource)
    {
        internal double? OriginalWidth = width;
        internal double? OriginalHeight = height;
        internal BitmapSource? BitmapSource = bitmapSource;
    }

    /// <summary>
    /// Loads the thumbnail at the provided index.
    /// </summary>
    /// <param name="x">The index of the thumbnail to load.</param>
    /// <param name="fileInfo">The <see cref="FileInfo"/> associated with the thumbnail (optional).</param>
    /// <returns>A <see cref="ThumbHolder"/> containing thumbnail information, or <c>null</c> if loading fails.</returns>
    internal static ThumbHolder? GetThumb(int x, FileInfo? fileInfo = null)
    {
        BitmapSource? pic;
        try
        {
            if (GetPicGallery != null)
            {
                GetPicGallery.Dispatcher.Invoke(DispatcherPriority.Render, () =>
                {
                    if (GetPicGallery.Container.Children.Count <= 0 || x >= GetPicGallery.Container.Children.Count)
                    {
                        return GetIt();
                    }

                    var y = GetPicGallery.Container.Children[x] as PicGalleryItem;
                    pic = (BitmapSource)y.ThumbImage.Source;
                    if (pic is { IsFrozen: false })
                    {
                        pic.Freeze();
                    }

                    return new ThumbHolder(null, null, pic);
                });
            }
            else
            {
                return GetIt();
            }
        }
        catch (Exception)
        {
            return null;
        }

        return null;

        ThumbHolder GetIt()
        {
            var preLoadValue = PreLoader.Get(x);
            if (preLoadValue != null)
            {
                if (preLoadValue.BitmapSource is not null)
                {
                    return new ThumbHolder(preLoadValue.BitmapSource.Width, preLoadValue.BitmapSource.Height, preLoadValue.BitmapSource);
                }
                fileInfo = preLoadValue.FileInfo;
            }
            fileInfo ??= new FileInfo(Pics[x]);
            using var image = new MagickImage();
            image.Ping(fileInfo);
            var thumb = image.GetExifProfile()?.CreateThumbnail();
            pic = thumb?.ToBitmapSource();
            if (pic is { IsFrozen: false })
            {
                pic.Freeze();
            }
            return new ThumbHolder(image?.Width, image?.Height, pic);
        }
    }

    /// <summary>
    /// Asynchronously gets a <see cref="BitmapSource"/> thumbnail for the specified file.
    /// </summary>
    /// <param name="file">The path of the file for which to generate the thumbnail.</param>
    /// <param name="size">The size of the thumbnail (width and height).</param>
    /// <param name="fileInfo">The <see cref="FileInfo"/> associated with the file (optional).</param>
    /// <returns>
    /// A task representing the asynchronous operation that returns a <see cref="BitmapSource"/> thumbnail.
    /// </returns>
    internal static async Task<BitmapSource?> GetBitmapSourceThumbAsync(string file, int size, FileInfo? fileInfo = null)
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