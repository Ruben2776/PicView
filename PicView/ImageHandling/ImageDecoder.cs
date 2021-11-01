using ImageMagick;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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
                    return await getWriteableBitmap(fileInfo).ConfigureAwait(false);

                case ".tga":
                    return getDefaultBitmapSource(fileInfo, true);

                case ".svg": // TODO convert to drawingimage instead
                case ".svgz": // TODO convert to drawingimage instead
                case ".tif":
                case ".tiff":
                case ".dds":
                case ".psd":
                case ".psb":
                case ".xcf":
                    return await getMagicBitmapsource(fileInfo).ConfigureAwait(false);
                default: // some formats cause exceptions when using filestream, so defaulting to reading from file
                    return getDefaultBitmapSource(fileInfo);
            }
        }

        #region Render Image From Source

        internal static MagickImage? GetRenderedMagickImage()
        {
            try
            {
                var frame = BitmapFrame.Create(GetRenderedBitmapFrame());
                var encoder = new PngBitmapEncoder();

                encoder.Frames.Add(frame);

                var SaveImage = new MagickImage();
                using (var stream = new MemoryStream())
                {
                    encoder.Save(stream);
                    SaveImage.Read(stream.ToArray());
                }

                SaveImage.Quality = 100;

                // Apply transformation values
                if (UILogic.TransformImage.Rotation.Flipped)
                {
                    SaveImage.Flop();
                }

                SaveImage.Rotate(UILogic.TransformImage.Rotation.Rotateint);

                return SaveImage;
            }
            catch (Exception e)
            {
#if DEBUG
                Trace.WriteLine($"{nameof(GetRenderedBitmapFrame)} exception, \n {e.Message}");
#endif
                return null;
            }
        }

        internal static BitmapFrame? GetRenderedBitmapFrame()
        {
            try
            {
                var sauce = UILogic.ConfigureWindows.GetMainWindow.MainImage.Source as BitmapSource;

                if (sauce == null)
                {
                    return null;
                }

                var effect = UILogic.ConfigureWindows.GetMainWindow.MainImage.Effect;

                var rectangle = new System.Windows.Shapes.Rectangle
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

        private static async Task<WriteableBitmap?> getWriteableBitmap(FileInfo fileInfo)
        {
            FileStream? filestream = null; // https://devblogs.microsoft.com/dotnet/file-io-improvements-in-dotnet-6/
            byte[] data;

            try
            {
                filestream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
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
                Trace.WriteLine($"{nameof(getWriteableBitmap)} {fileInfo.Name} exception, \n {e.Message}");
#endif
                return null;
            }
        }

        private static async Task<BitmapSource?> getMagicBitmapsource(FileInfo fileInfo)
        {
            FileStream? filestream = null;
            MagickImage magickImage = new()
            {
                Quality = 100,
                ColorSpace = ColorSpace.Transparent
            };
            if (fileInfo.Length >= 2147483647) // Streams with a length larger than 2147483647 are not supported, read from file instead
            {
                try
                {
                    magickImage.Read(fileInfo);
                }
                catch (Exception e)
                {
#if DEBUG
                    Trace.WriteLine($"{nameof(getMagicBitmapsource)} {fileInfo.Name} exception, \n {e.Message}");
#endif
                    return null;
                }
            }
            else
            {
                try
                {
                    filestream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
                    magickImage.Read(filestream);
                }
                catch (Exception e)
                {
#if DEBUG
                    Trace.WriteLine($"{nameof(getMagicBitmapsource)} {fileInfo.Name} exception, \n {e.Message}");
#endif
                    return null;
                }

                await filestream.DisposeAsync().ConfigureAwait(false);
            }

            var bitmap = magickImage.ToBitmapSource();
            magickImage.Dispose();
            bitmap.Freeze();

            return bitmap;
        }

        private static BitmapSource? getDefaultBitmapSource(FileInfo fileInfo, bool autoOrient = false)
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
                Trace.WriteLine($"{nameof(getDefaultBitmapSource)} {fileInfo.Name} exception, \n {e.Message}");
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