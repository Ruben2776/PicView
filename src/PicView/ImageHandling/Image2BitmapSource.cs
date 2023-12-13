using ImageMagick;
using PicView.Core.ImageDecoding;
using PicView.UILogic;
using SkiaSharp.Views.WPF;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Rotation = PicView.UILogic.TransformImage.Rotation;

namespace PicView.ImageHandling
{
    /// <summary>
    /// Provides methods for converting images to WPF's BitmapSource.
    /// </summary>
    internal static class Image2BitmapSource
    {
        /// <summary>
        /// Asynchronously returns a BitmapSource based on the file extension.
        /// </summary>
        /// <param name="fileInfo">The FileInfo representing the image file.</param>
        /// <returns>A Task containing the BitmapSource if successful, otherwise an error image.</returns>
        internal static async Task<BitmapSource> ReturnBitmapSourceAsync(FileInfo fileInfo)
        {
            if (fileInfo is not { Length: > 0 })
            {
                return ImageFunctions.ImageErrorMessage();
            }

            var extension = fileInfo.Extension.ToLowerInvariant();

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
                    return await GetWriteAbleBitmapAsync(fileInfo).ConfigureAwait(false);

                case ".svg":
                    return await GetMagickSvg(fileInfo, MagickFormat.Svg).ConfigureAwait(false);

                case ".svgz":
                    return await GetMagickSvg(fileInfo, MagickFormat.Svgz).ConfigureAwait(false);

                case ".b64":
                    return await Base64.Base64StringToBitmapAsync(fileInfo).ConfigureAwait(false) ??
                           ImageFunctions.ImageErrorMessage();

                default:
                    return await GetDefaultBitmapSource(fileInfo).ConfigureAwait(false);
            }
        }

        #region Render Image From Source

        /// <summary>
        /// Returns the currently viewed bitmap image to MagickImage.
        /// </summary>
        /// <returns>The rendered MagickImage if successful, otherwise null.</returns>
        internal static MagickImage? GetRenderedMagickImage()
        {
            try
            {
                var frame = BitmapFrame.Create(GetRenderedBitmapFrame() ?? throw new InvalidOperationException());
                var encoder = new PngBitmapEncoder();

                encoder.Frames.Add(frame);

                var magickImage = new MagickImage();
                using (var stream = new MemoryStream())
                {
                    encoder.Save(stream);
                    magickImage.Read(stream.ToArray());
                }

                magickImage.Quality = 100;

                // Apply rotation and flip transformations
                if (Rotation.IsFlipped)
                {
                    magickImage.Flop();
                }

                magickImage.Rotate(Rotation.RotationAngle);

                return magickImage;
            }
            catch (Exception e)
            {
#if DEBUG
                Trace.WriteLine($"{nameof(GetRenderedMagickImage)} exception, \n {e.Message}");
#endif
                return null;
            }
        }

        /// <summary>
        /// Returns the displayed image source to a BitmapFrame.
        /// </summary>
        /// <returns>The rendered BitmapFrame if successful, otherwise null.</returns>
        internal static BitmapFrame? GetRenderedBitmapFrame()
        {
            try
            {
                if (ConfigureWindows.GetMainWindow.MainImage.Source is not BitmapSource sourceBitmap)
                {
                    return null;
                }

                var effect = ConfigureWindows.GetMainWindow.MainImage.Effect;

                var rectangle = new Rectangle
                {
                    Fill = new ImageBrush(sourceBitmap),
                    Effect = effect
                };

                var sourceSize = new Size(sourceBitmap.PixelWidth, sourceBitmap.PixelHeight);
                rectangle.Measure(sourceSize);
                rectangle.Arrange(new Rect(sourceSize));

                var renderedBitmap = new RenderTargetBitmap(sourceBitmap.PixelWidth, sourceBitmap.PixelHeight,
                    sourceBitmap.DpiX, sourceBitmap.DpiY, PixelFormats.Default);
                renderedBitmap.Render(rectangle);

                var bitmapFrame = BitmapFrame.Create(renderedBitmap);
                bitmapFrame.Freeze();

                return bitmapFrame;
            }
            catch (Exception exception)
            {
#if DEBUG
                Trace.WriteLine($"{nameof(GetRenderedBitmapFrame)} exception, \n{exception.Message}");
#endif
                return null;
            }
        }

        #endregion Render Image From Source

        #region Private functions

        /// <summary>
        /// Asynchronously returns a BitmapSource for SVG images using MagickImage.
        /// </summary>
        /// <param name="fileInfo">The FileInfo representing the SVG file.</param>
        /// <param name="magickFormat">The MagickFormat for the SVG image.</param>
        /// <returns>A Task containing the BitmapSource for SVG if successful, otherwise an error image.</returns>
        private static async Task<BitmapSource> GetMagickSvg(FileInfo fileInfo, MagickFormat magickFormat)
        {
            var magickImage = await ImageDecoder.GetMagickSvgAsync(fileInfo, magickFormat).ConfigureAwait(false);
            if (magickImage is null)
            {
                return ImageFunctions.ImageErrorMessage();
            }
            var bitmap = magickImage.ToBitmapSource();
            bitmap.Freeze();
            magickImage.Dispose();
            return bitmap;
        }

        /// <summary>
        /// Asynchronously returns a BitmapSource using SKBitmap.
        /// </summary>
        /// <param name="fileInfo">The FileInfo representing the image file.</param>
        /// <returns>A Task containing the BitmapSource if successful, otherwise an error image.</returns>
        private static async Task<BitmapSource> GetWriteAbleBitmapAsync(FileInfo fileInfo)
        {
            using var sKBitmap = await fileInfo.GetSKBitmapAsync();
            if (sKBitmap is null)
            {
                return ImageFunctions.ImageErrorMessage();
            }

            var skPic = sKBitmap.ToWriteableBitmap();
            sKBitmap.Dispose();

            skPic.Freeze();
            return skPic;
        }

        /// <summary>
        /// Asynchronously returns a default BitmapSource using MagickImage.
        /// </summary>
        /// <param name="fileInfo">The FileInfo representing the image file.</param>
        /// <returns>A Task containing the BitmapSource if successful, otherwise an error image.</returns>
        private static async Task<BitmapSource> GetDefaultBitmapSource(FileInfo fileInfo)
        {
            var magickImage = await ImageDecoder.GetMagickImageAsync(fileInfo).ConfigureAwait(false);
            if (magickImage is null)
            {
                return ImageFunctions.ImageErrorMessage();
            }

            var bitmap = magickImage.ToBitmapSource();
            bitmap.Freeze();
            return bitmap;
        }

        #endregion Private functions
    }
}