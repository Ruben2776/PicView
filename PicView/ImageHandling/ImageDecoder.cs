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
        /// <param name="file">Absolute path of the file</param>
        /// <returns></returns>
        internal static async Task<BitmapSource?> RenderToBitmapSource(string file)
        {
            var ext = Path.GetExtension(file).ToUpperInvariant();
            FileStream? filestream = null;
            switch (ext)
            {
                case ".JPG":
                case ".JPEG":
                case ".JPE":
                case ".PNG":
                case ".BMP":
                case ".GIF":
                case ".ICO":
                case ".JFIF":
                case ".WEBP":
                case ".WBMP":
                    try
                    {
                        filestream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.SequentialScan);
                    }
                    catch (Exception e)
                    {
#if DEBUG
                        Trace.WriteLine("RenderToBitmapSource Skia returned " + file + " null, \n" + e.Message);
#endif
                        return null;
                    }
                    using (var imgStream = new SKManagedStream(filestream))
                    {
                        using var skData = SKData.Create(filestream);
                        await filestream.DisposeAsync().ConfigureAwait(false);
                        using var codec = SKCodec.Create(skData);
                        var sKBitmap = SKBitmap.Decode(codec);

                        if (sKBitmap == null)
                        {
                            return null;
                        }

                        var skPic = sKBitmap.ToWriteableBitmap();
                        skPic.Freeze();
                        sKBitmap.Dispose();
                        return skPic;
                    }
                case ".TIF":
                case ".TIFF":
                case ".DDS":
                case "TGA": // TODO some tga files are created upside down https://github.com/Ruben2776/PicView/issues/22
                case ".PSD":
                case ".PSB":
                case ".SVG":
                case ".XCF":
                    var fileInfo = new FileInfo(file);
                    if (fileInfo == null)
                    {
                        return null;
                    }
                    MagickImage magickImage = new()
                    {
                        Quality = 100,
                        ColorSpace = ColorSpace.Transparent
                    };
                    if (fileInfo.Length >= 2147483647) // Streams with a length larger than 2147483647 are not supported, read from file instead
                    {
                        magickImage.Read(fileInfo);
                    }
                    else
                    {
                        try
                        {
                            filestream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.SequentialScan);
                            await magickImage.ReadAsync(filestream).ConfigureAwait(false);
                        }
                        catch (Exception e)
                        {
#if DEBUG
                            Trace.WriteLine("RenderToBitmapSource MagickImage returned " + file + " null, \n" + e.Message);
#endif
                            return null;
                        }

                        await filestream.DisposeAsync().ConfigureAwait(false);
                    }

                    var bitmap = magickImage.ToBitmapSource();
                    magickImage.Dispose();
                    bitmap.Freeze();

                    return bitmap;

                default: // some formats cause exceptions when using filestream, so defaulting to reading from file
                    var magick = new MagickImage();
                    try
                    {
                        magick.Read(file);
                    }
                    catch (Exception e)
                    {
#if DEBUG
                        Trace.WriteLine("RenderToBitmapSource MagickImage returned " + file + " null, \n" + e.Message);
#endif
                        return null;
                    }

                    magick.Quality = 100;

                    var pic = magick.ToBitmapSource();
                    magick.Dispose();
                    pic.Freeze();

                    return pic;
            }
        }

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
            catch (Exception) { return null; }
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
            catch (Exception) { return null; }
        }
    }
}