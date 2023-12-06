using ImageMagick;
using ImageMagick.Formats;
using PicView.UILogic;
using SkiaSharp;
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
    internal static class ImageDecoder
    {
        /// <summary>
        /// Decodes image from file to BitMapSource
        /// </summary>
        /// <param name="fileInfo">Cannot be null</param>
        /// <returns></returns>
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
                    return await GetDefaultBitmapSource(fileInfo, extension).ConfigureAwait(false);
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
        /// Returns Displayed image source to a BitmapFrame
        /// </summary>
        /// <returns></returns>
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
        /// Create MagickImage and make sure its transparent, return it as BitmapSource
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <param name="magickFormat"></param>
        /// <returns></returns>
        private static async Task<BitmapSource> GetMagickSvg(FileInfo fileInfo, MagickFormat magickFormat)
        {
            try
            {
                var magickImage = new MagickImage
                {
                    Quality = 100,
                    ColorSpace = ColorSpace.Transparent,
                    BackgroundColor = MagickColors.Transparent,
                    Format = magickFormat,
                };

                if (fileInfo.Length >=
                    2147483648) // Streams with a length larger than 2GB are not supported, read from file instead
                {
                    await Task.Run(() =>
                    {
                        magickImage = new MagickImage();
                        magickImage.Read(fileInfo);
                    }).ConfigureAwait(false);
                }
                else
                {
                    await using var fileStream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read,
                        FileShare.ReadWrite, 4096, fileInfo.Length > 1e+8);
                    var data = new byte[fileStream.Length];
                    // ReSharper disable once MustUseReturnValue
                    await fileStream.ReadAsync(data.AsMemory(0, (int)fileStream.Length)).ConfigureAwait(false);
                    magickImage.Read(data);
                }

                magickImage.Settings.BackgroundColor = MagickColors.Transparent;
                magickImage.Settings.FillColor = MagickColors.Transparent;
                magickImage.Settings.SetDefine("svg:xml-parse-huge", "true");

                var bitmap = magickImage.ToBitmapSource();
                bitmap.Freeze();
                magickImage.Dispose();
                return bitmap;
            }
            catch (Exception e)
            {
#if DEBUG
                Trace.WriteLine($"{nameof(GetMagickSvg)} {fileInfo.Name} exception, \n {e.Message}");
#endif
                return ImageFunctions.ImageErrorMessage();
            }
        }

        /// <summary>
        /// Asynchronously gets a WriteableBitmap from the given file.
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a <c>WriteableBitmap</c> object if successful; otherwise, it returns null.</returns>
        private static async Task<BitmapSource> GetWriteAbleBitmapAsync(FileInfo fileInfo)
        {
            try
            {
                await using var fileStream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite, 4096, fileInfo.Length > 1e+8);
                var sKBitmap = SKBitmap.Decode(fileStream);
                if (sKBitmap is null)
                {
                    return ImageFunctions.ImageErrorMessage();
                }

                var skPic = sKBitmap.ToWriteableBitmap();
                sKBitmap.Dispose();

                skPic.Freeze();
                return skPic;
            }
            catch (Exception e)
            {
#if DEBUG
                Trace.WriteLine($"{nameof(GetWriteAbleBitmapAsync)} {fileInfo.Name} exception:\n{e.Message}");
#endif
                return ImageFunctions.ImageErrorMessage();
            }
        }

        private static async Task<BitmapSource> GetDefaultBitmapSource(FileInfo fileInfo, string extension)
        {
            try
            {
                using var magickImage = new MagickImage();
                MagickFormat format;

                switch (extension)
                {
                    case ".heic":
                    case ".heif":
                        magickImage.Settings.SetDefines(new HeicReadDefines
                        {
                            PreserveOrientation = true,
                            DepthImage = true,
                        });
                        format = extension is ".heic" ? MagickFormat.Heic : MagickFormat.Heif;
                        break;

                    case ".jp2":
                        magickImage.Settings.SetDefines(new Jp2ReadDefines
                        {
                            QualityLayers = 100,
                        });
                        format = MagickFormat.Jp2;
                        break;

                    case ".tif":
                    case ".tiff":
                        magickImage.Settings.SetDefines(new TiffReadDefines
                        {
                            IgnoreTags = new[]
                            {
                                "34022", // ColorTable
                                "34025", // ImageColorValue
                                "34026", // BackgroundColorValue
                                "32928",
                            },
                        });
                        format = MagickFormat.Tif;
                        break;

                    case ".psd":
                        format = MagickFormat.Psd;
                        break;

                    default:
                        format = MagickFormat.Unknown;
                        break;
                }

                if (fileInfo.Length >= 2147483648)
                {
                    await using var fileStream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read,
                        FileShare.ReadWrite, 4096, true);

                    // Fixes "The file is too long. This operation is currently limited to supporting files less than 2 gigabytes in size."
                    // ReSharper disable once MethodHasAsyncOverload
                    magickImage.Read(fileStream, format);
                }
                else
                {
                    await magickImage.ReadAsync(fileInfo, format).ConfigureAwait(false);
                }

                magickImage?.AutoOrient();
                var bitmapSource = magickImage?.ToBitmapSource();
                bitmapSource?.Freeze();
                magickImage?.Dispose();
                return bitmapSource ?? ImageFunctions.ImageErrorMessage();
            }
            catch (Exception exception)
            {
#if DEBUG
                Trace.WriteLine($"{nameof(GetDefaultBitmapSource)} {fileInfo.Name} exception:\n{exception.Message}");
#endif
                return ImageFunctions.ImageErrorMessage();
            }
        }

        #endregion Private functions
    }
}