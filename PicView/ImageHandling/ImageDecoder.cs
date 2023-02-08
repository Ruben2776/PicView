using ImageMagick;
using PicView.UILogic;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Rotation = PicView.UILogic.TransformImage.Rotation;

namespace PicView.ImageHandling
{
    internal static class ImageDecoder
    {
        /// <summary>
        /// Decodes image from file to BitMapSource
        /// </summary>
        /// <param name="fileInfo">Cannot be null</param>
        /// <returns></returns>
        internal static async Task<BitmapSource?> ReturnBitmapSourceAsync(FileInfo fileInfo)
        {
            if (fileInfo == null) { return null; }
            if (fileInfo.Length <= 0) { return null; }

            switch (fileInfo.Extension)
            {
                case { } when fileInfo.Extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase):
                case { } when fileInfo.Extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase):
                case { } when fileInfo.Extension.Equals(".jpe", StringComparison.OrdinalIgnoreCase):
                case { } when fileInfo.Extension.Equals(".png", StringComparison.OrdinalIgnoreCase):
                case { } when fileInfo.Extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase):
                case { } when fileInfo.Extension.Equals(".gif", StringComparison.OrdinalIgnoreCase):
                case { } when fileInfo.Extension.Equals(".jfif", StringComparison.OrdinalIgnoreCase):
                case { } when fileInfo.Extension.Equals(".ico", StringComparison.OrdinalIgnoreCase):
                case { } when fileInfo.Extension.Equals(".webp", StringComparison.OrdinalIgnoreCase):
                case { } when fileInfo.Extension.Equals(".wbmp", StringComparison.OrdinalIgnoreCase):
                    return await GetWriteableBitmapAsync(fileInfo).ConfigureAwait(false);

                case { } when fileInfo.Extension.Equals(".tga", StringComparison.OrdinalIgnoreCase): // Make sure to to auto orient tga files https://github.com/Ruben2776/PicView/issues/22
                    return await Task.FromResult(GetDefaultBitmapSource(fileInfo, true)).ConfigureAwait(false);

                case { } when fileInfo.Extension.Equals(".svg", StringComparison.OrdinalIgnoreCase):
                    // TODO convert to drawingimage instead.. maybe
                    // TODO svgz only works in getDefaultBitmapSource, need to figure out how to fix white bg instead of transparent 
                    return await GetTransparentBitmapSourceAsync(fileInfo, MagickFormat.Svg).ConfigureAwait(false);

                case { } when fileInfo.Extension.Equals(".b64", StringComparison.OrdinalIgnoreCase):
                    return await Base64.Base64StringToBitmap(fileInfo).ConfigureAwait(false);

                default:
                    return await Task.FromResult(GetDefaultBitmapSource(fileInfo)).ConfigureAwait(false);
            }
        }

        #region Render Image From Source
        /// <summary>
        /// Returns the currently viewed bitmap image to MagickImage
        /// </summary>
        /// <returns></returns>
        internal static MagickImage? GetRenderedMagickImage()
        {
            try
            {
                var frame = BitmapFrame.Create(GetRenderedBitmapFrame());
                var encoder = new PngBitmapEncoder();

                encoder.Frames.Add(frame);

                var magickImage = new MagickImage();
                using (var stream = new MemoryStream())
                {
                    encoder.Save(stream);
                    magickImage.Read(stream.ToArray());
                }

                magickImage.Quality = 100;

                // Apply transformation values
                if (Rotation.Flipped)
                {
                    magickImage.Flop();
                }

                magickImage.Rotate(Rotation.RotationAngle);

                return magickImage;
            }
            catch (Exception e)
            {
#if DEBUG
                Trace.WriteLine($"{nameof(GetRenderedBitmapFrame)} exception, \n {e.Message}");
#endif
                return null;
            }
        }

        /// <summary>
        /// Returns Displayed image source to a BitmapFrame
        /// </summary>
        /// <returns></returns>
        internal static BitmapFrame? GetRenderedBitmapFrame()
        {
            try
            {
                var sauce = ConfigureWindows.GetMainWindow.MainImage.Source as BitmapSource;

                if (sauce == null)
                {
                    return null;
                }

                var effect = ConfigureWindows.GetMainWindow.MainImage.Effect;

                var rectangle = new Rectangle
                {
                    Fill = new ImageBrush(sauce),
                    Effect = effect
                };

                var sz = new Size(sauce.PixelWidth, sauce.PixelHeight);
                rectangle.Measure(sz);
                rectangle.Arrange(new Rect(sz));

                var rtb = new RenderTargetBitmap(sauce.PixelWidth, sauce.PixelHeight, sauce.DpiX, sauce.DpiY, PixelFormats.Default);
                rtb.Render(rectangle);

                BitmapFrame bitmapFrame = BitmapFrame.Create(rtb);
                bitmapFrame.Freeze();

                return bitmapFrame;
            }
            catch (Exception e)
            {
#if DEBUG
                Trace.WriteLine($"{nameof(GetRenderedBitmapFrame)} exception, \n {e.Message}");
#endif
                return null;
            }
        }

        #endregion

        #region Private functions

        /// <summary>
        /// Create MagickImage and make sure its transparent, return it as BitmapSource
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <param name="magickFormat"></param>
        /// <returns></returns>
        private static async Task<BitmapSource?> GetTransparentBitmapSourceAsync(FileInfo fileInfo, MagickFormat magickFormat)
        {
            FileStream? filestream = null;
            MagickImage magickImage = new()
            {
                Quality = 100,
                ColorSpace = ColorSpace.Transparent,
                BackgroundColor = MagickColors.Transparent,
                Format = magickFormat,
            };
            try
            {
                filestream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, true);
                byte[] data = new byte[filestream.Length];
                await filestream.ReadAsync(data.AsMemory(0, (int)filestream.Length)).ConfigureAwait(false);

                magickImage.Read(data);
                magickImage.Settings.Format = magickFormat;
                magickImage.Settings.BackgroundColor = MagickColors.Transparent;
                magickImage.Settings.FillColor = MagickColors.Transparent;
            }
            catch (Exception e)
            {
                filestream?.Dispose();
                magickImage?.Dispose();
#if DEBUG
                Trace.WriteLine($"{nameof(GetTransparentBitmapSourceAsync)} {fileInfo.Name} exception, \n {e.Message}");
#endif
                return null;
            }

            await filestream.DisposeAsync().ConfigureAwait(false);

            var bitmap = magickImage.ToBitmapSource();
            magickImage.Dispose();
            bitmap.Freeze();
            return bitmap;
        }

        private static async Task<WriteableBitmap?> GetWriteableBitmapAsync(FileInfo fileInfo)
        {
            FileStream? filestream = null; // https://devblogs.microsoft.com/dotnet/file-io-improvements-in-dotnet-6/
            byte[] data;

            try
            {
                filestream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, true);
                data = new byte[filestream.Length];
                await filestream.ReadAsync(data.AsMemory(0, (int)filestream.Length)).ConfigureAwait(false);
                await filestream.DisposeAsync().ConfigureAwait(false);

                var sKBitmap = SKBitmap.Decode(data);
                if (sKBitmap is null) { return null; }

                var skPic = sKBitmap.ToWriteableBitmap();
                skPic.Freeze();
                sKBitmap.Dispose();
                return skPic;
            }
            catch (Exception e)
            {
#if DEBUG
                Trace.WriteLine($"{nameof(GetWriteableBitmapAsync)} {fileInfo.Name} exception, \n {e.Message}");
#endif
                return null;
            }
        }

        private static BitmapSource? GetDefaultBitmapSource(FileInfo fileInfo, bool autoOrient = false)
        {
            var magick = new MagickImage();
            try
            {
                magick.Read(fileInfo);
                if (autoOrient)
                {
                    magick.AutoOrient();
                }
            }
            catch (Exception e)
            {
#if DEBUG
                Trace.WriteLine($"{nameof(GetDefaultBitmapSource)} {fileInfo.Name} exception, \n {e.Message}");
#endif
                return null;
            }

            magick.Quality = 100;

            var pic = magick.ToBitmapSource();
            magick.Dispose();
            pic.Freeze();

            return pic;
        }

        #endregion
    }
}