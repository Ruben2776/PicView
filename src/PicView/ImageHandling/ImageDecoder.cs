﻿using ImageMagick;
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

namespace PicView.ImageHandling;

internal static class ImageDecoder
{
    /// <summary>
    /// Decodes image from file to BitMapSource
    /// </summary>
    /// <param name="fileInfo">Cannot be null</param>
    /// <returns></returns>
    internal static async Task<BitmapSource?> ReturnBitmapSourceAsync(FileInfo fileInfo)
    {
        if (fileInfo is not { Length: > 0 }) { return null; }

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

            case ".b64":
                return await Base64.Base64StringToBitmapAsync(fileInfo).ConfigureAwait(false);

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

            var renderedBitmap = new RenderTargetBitmap(sourceBitmap.PixelWidth, sourceBitmap.PixelHeight, sourceBitmap.DpiX, sourceBitmap.DpiY, PixelFormats.Default);
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
    private static async Task<BitmapSource?> GetMagickSvg(FileInfo fileInfo, MagickFormat magickFormat)
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

            if (fileInfo.Length >= 2147483648) // Streams with a length larger than 2GB are not supported, read from file instead
            {
                await Task.Run(() =>
                {
                    magickImage = new MagickImage();
                    magickImage.Read(fileInfo);
                }).ConfigureAwait(false);
            }
            else
            {
                await using var fileStream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize: 4096, useAsync: true);
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
            Tooltip.ShowTooltipMessage(e);
            return null;
        }
    }

    /// <summary>
    /// Asynchronously gets a WriteableBitmap from the given file.
    /// If the file size is equal to or greater than 2 GB, it returns the default bitmap source.
    /// </summary>
    /// <param name="fileInfo"></param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a <c>WriteableBitmap</c> object if successful; otherwise, it returns null.</returns>
    private static async Task<WriteableBitmap?> GetWriteAbleBitmapAsync(FileInfo fileInfo)
    {
        if (fileInfo.Length >= 2147483648)
            return (WriteableBitmap?)await Task.FromResult(GetDefaultBitmapSource(fileInfo)).ConfigureAwait(false);

        try
        {
            await using var fileStream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize: 4096, useAsync: true);
            var sKBitmap = SKBitmap.Decode(fileStream);
            if (sKBitmap is null) { return null; }

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
            return null;
        }
    }

    private static BitmapSource? GetDefaultBitmapSource(FileInfo fileInfo)
    {
        try
        {
            var magickImage = new MagickImage();
            magickImage.Read(fileInfo);

            var extension = fileInfo.Extension;
            var settings = new MagickReadSettings
            {
                SyncImageWithExifProfile = true,
                SyncImageWithTiffProperties = true,
            };

            if (extension.Equals(".HEIC", StringComparison.OrdinalIgnoreCase) || extension.Equals(".HEIF", StringComparison.OrdinalIgnoreCase))
            {
                settings.SetDefines(new HeicReadDefines
                {
                    PreserveOrientation = true,
                    DepthImage = true,
                });
            }
            else if (extension.Equals(".JP2", StringComparison.OrdinalIgnoreCase))
            {
                settings.SetDefines(new Jp2ReadDefines
                {
                    QualityLayers = 100,
                });
            }
            else if (extension.Equals(".TIF", StringComparison.OrdinalIgnoreCase) || extension.Equals(".TIFF", StringComparison.OrdinalIgnoreCase))
            {
                settings.SetDefines(new TiffReadDefines
                {
                    IgnoreTags = new[] {
                        "34022", // ColorTable
                        "34025", // ImageColorValue
                        "34026", // BackgroundColorValue
                        "32928",
                    },
                });
            }

            magickImage?.AutoOrient();
            var bitmapSource = magickImage?.ToBitmapSource();
            bitmapSource?.Freeze();
            magickImage?.Dispose();
            return bitmapSource;
        }
        catch (Exception exception)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(GetDefaultBitmapSource)} {fileInfo.Name} exception:\n{exception.Message}");
#endif
            Tooltip.ShowTooltipMessage(exception);
            return null;
        }
    }

    #endregion Private functions
}