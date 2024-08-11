using ImageMagick;
using PicView.Core.FileHandling;
using PicView.Core.ImageDecoding;
using PicView.Core.Localization;
using PicView.WPF.ChangeImage;
using PicView.WPF.ChangeTitlebar;
using PicView.WPF.UILogic;
using PicView.WPF.UILogic.Sizing;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Brush = System.Windows.Media.Brush;
using FontFamily = System.Windows.Media.FontFamily;
using Point = System.Windows.Point;
using Size = System.Windows.Size;
using TextAlignment = System.Windows.TextAlignment;

namespace PicView.WPF.ImageHandling;

internal static class ImageFunctions
{
    /// <summary>
    /// Changes the EXIF rating of an image file to the specified value.
    /// </summary>
    /// <param name="rating">The rating value to set. Must be a value between 0 and 5.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean value indicating whether the operation succeeded or failed.</returns>
    internal static async Task<bool> SetRating(ushort rating)
    {
        if (ErrorHandling.CheckOutOfRange())
        {
            return false;
        }

        try
        {
            return await Task.Run(() => EXIFHelper.SetEXIFRating(Navigation.Pics[Navigation.FolderIndex], rating));
        }
        catch (MagickException exception)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(ImageFunctions)}::{nameof(SetRating)} caught exception:\n{exception.Message}");
#endif
            Tooltip.ShowTooltipMessage(exception.Message, true, TimeSpan.FromSeconds(5));
            return false;
        }
    }

    internal static async Task OptimizeImageAsyncWithErrorChecking()
    {
        if (ErrorHandling.CheckOutOfRange())
        {
            return;
        }

        var toCenter = false;

        await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() => { toCenter = UC.UserControls_Open(); });

        Tooltip.ShowTooltipMessage(TranslationHelper.GetTranslation("Applying"), toCenter);

        await Task.Run(async () =>
        {
            ImageOptimizer imageOptimizer = new()
            {
                OptimalCompression = true
            };

            if (!imageOptimizer.IsSupported(Navigation.Pics[Navigation.FolderIndex]))
            {
                // Show a tooltip message indicating that the file is unsupported
                Tooltip.ShowTooltipMessage(TranslationHelper.GetTranslation("UnsupportedFile"), toCenter);

                // Return false to indicate that the optimization was not successful
                return;
            }

            try
            {
                if (!imageOptimizer.LosslessCompress(Navigation.Pics[Navigation.FolderIndex]))
                {
                    Tooltip.ShowTooltipMessage(TranslationHelper.GetTranslation("NoChange"), toCenter);
                    return;
                }
            }
            catch (MagickException exception)
            {
#if DEBUG
                // Output exception message to Trace in debug mode
                Trace.WriteLine(
                    $"{nameof(ImageFunctions)}::{nameof(SetRating)} caught exception:\n{exception.Message}");
#endif
                // Show a tooltip message indicating that an error occurred during compression
                Tooltip.ShowTooltipMessage(exception.Message, true, TimeSpan.FromSeconds(5));
            }

            var preloadValue = PreLoader.Get(Navigation.FolderIndex);
            var fileInfo = new FileInfo(Navigation.Pics[Navigation.FolderIndex]);
            var readablePrevSize = preloadValue?.FileInfo?.Length.GetReadableFileSize();
            var originalValue = preloadValue?.FileInfo?.Length;
            if (originalValue is null)
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
                {
                    originalValue = FileHelper.GetFileSizeFromString(ConfigureWindows.GetMainWindow.TitleText.Text);
                });
            }
            if (readablePrevSize is null)
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync((() =>
                {
                    readablePrevSize = ConfigureWindows.GetMainWindow.TitleText.Text.ExtractFileSize();
                }));
                if (readablePrevSize is null)
                {
                    preloadValue = PreLoader.Get(Navigation.FolderIndex);
                    if (preloadValue is null)
                    {
                        await PreLoader.AddAsync(Navigation.FolderIndex);
                    }
                    readablePrevSize = preloadValue?.FileInfo?.Length.GetReadableFileSize();
                }
            }
            originalValue ??= preloadValue?.FileInfo?.Length;
            var readableNewSize = fileInfo.Length.GetReadableFileSize();

            try
            {
                var decreasedValue = fileInfo.Length;
                var percentDecrease = ((float)(originalValue - decreasedValue) / decreasedValue) * 100;
                Tooltip.ShowTooltipMessage(
                    $"{readablePrevSize} \u21e2 {readableNewSize} = {percentDecrease.ToString("0.## ", CultureInfo.CurrentCulture)}%",
                    toCenter, TimeSpan.FromSeconds(7));
            }
            catch (Exception exception)
            {
                Tooltip.ShowTooltipMessage(exception);
            }

            // Update title to show new file size
            await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
            {
                var width = ConfigureWindows.GetMainWindow.MainImage.Source.Width;
                var height = ConfigureWindows.GetMainWindow.MainImage.Source.Height;

                SetTitle.SetTitleString((int)width, (int)height, Navigation.FolderIndex, null);
            });
        });
    }

    /// <summary>
    /// Optimizes the image at the specified file path.
    /// </summary>
    /// <param name="file">The file path of the image to optimize.</param>
    /// <param name="lossless">Specifies whether to use lossless compression. Default is true.</param>
    /// <returns>True if the optimization was successful, false otherwise.</returns>
    internal static bool OptimizeImage(string file, bool lossless = true)
    {
        // Create a new ImageOptimizationHelper with the specified lossless compression option
        ImageOptimizer imageOptimizer = new()
        {
            OptimalCompression = lossless
        };

        // Check if the file is supported by the ImageOptimizationHelper
        if (imageOptimizer.IsSupported(file) == false)
        {
            // Show a tooltip message indicating that the file is unsupported
            Tooltip.ShowTooltipMessage(TranslationHelper.GetTranslation("UnsupportedFile"), true, TimeSpan.FromSeconds(5));

            // Return false to indicate that the optimization was not successful
            return false;
        }

        try
        {
            // Compress the image using the ImageOptimizationHelper and return true if successful
            return imageOptimizer.LosslessCompress(file);
        }
        catch (MagickException exception)
        {
#if DEBUG
            // Output exception message to Trace in debug mode
            Trace.WriteLine(
                $"{nameof(ImageFunctions)}::{nameof(SetRating)} caught exception:\n{exception.Message}");
#endif
            // Show a tooltip message indicating that an error occurred during compression
            Tooltip.ShowTooltipMessage(exception.Message, true, TimeSpan.FromSeconds(5));

            // Return false to indicate that the optimization was not successful
            return false;
        }
    }

    /// <summary>
    /// Gets the number of frames in an image.
    /// </summary>
    /// <param name="file">The path to the image file.</param>
    /// <returns>The number of frames in the image. Returns 0 if an error occurs.</returns>
    /// <remarks>
    /// This method uses the Magick.NET library to load the image and retrieve the frame count.
    /// </remarks>
    public static int GetImageFrames(string file)
    {
        try
        {
            // Using statement ensures proper disposal of resources.
            using var magick = new MagickImageCollection(file);
            return magick.Count;
        }
        catch (MagickException ex)
        {
            // Log the exception for debugging purposes.
            Trace.WriteLine($"{nameof(GetImageFrames)} Exception \n{ex}");

            // Return 0 in case of an error.
            return 0;
        }
    }

    internal static RenderTargetBitmap? ImageErrorMessage()
    {
        var brokenBitmapImage = (DrawingImage)Application.Current.Resources["BrokenDrawingImage"];
        var brush = Application.Current.TryFindResource("MainColorBrush") as Brush;

        var w = 333 * WindowSizing.MonitorInfo.DpiScaling;
        var h = 333 * WindowSizing.MonitorInfo.DpiScaling;
        var rect = new Rect(new Size(w, h));
        var visual = new DrawingVisual();

        try
        {
            using (var ctx = visual.RenderOpen())
            {
                var typeface = new Typeface(
                    new FontFamily("/PicView;component/Themes/Resources/fonts/#Roboto Bold"),
                    FontStyles.Normal, FontWeights.Medium, FontStretches.Normal);
                var text = new FormattedText(TranslationHelper.GetTranslation("UnableToRender"),
                    CultureInfo.CurrentUICulture, FlowDirection, typeface, 18, brush,
                    WindowSizing.MonitorInfo.DpiScaling)
                {
                    TextAlignment = TextAlignment.Center
                };
                ctx.DrawImage(brokenBitmapImage, rect);
                ctx.DrawText(text, new Point(rect.Left + rect.Width / 2, h + 5));
            }

            RenderTargetBitmap rtv = new((int)w, (int)h + 45, 96.0, 96.0, PixelFormats.Default);
            rtv.Render(visual);
            rtv.Freeze();
            return rtv;
        }
        catch (Exception exception)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(ImageErrorMessage)} exception:\n{exception.Message}");
#endif
            return null;
        }
    }

    private static FlowDirection FlowDirection => CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft
        ? FlowDirection.RightToLeft
        : FlowDirection.LeftToRight;

    internal static Bitmap BitmapSourceToBitmap(BitmapSource source)
    {
        var bmp = new Bitmap(
            source.PixelWidth,
            source.PixelHeight,
            System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        var data = bmp.LockBits(
            new Rectangle(System.Drawing.Point.Empty, bmp.Size),
            ImageLockMode.WriteOnly,
            System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        source.CopyPixels(
            Int32Rect.Empty,
            data.Scan0,
            data.Height * data.Stride,
            data.Stride);
        bmp.UnlockBits(data);
        return bmp;
    }
    
    /// <summary>
    /// Checks whether a given bitmap has any transparent pixels.
    /// </summary>
    /// <param name="bitmap">The bitmap to check.</param>
    /// <returns>True if the bitmap has any transparent pixels, false otherwise.</returns>
    internal static bool HasTransparentBackground(BitmapSource bitmap)
    {
        // Convert the bitmap to the Bgra32 pixel format if necessary
        if (bitmap.Format != PixelFormats.Bgra32)
        {
            bitmap = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
        }

        // Copy the bitmap pixels into a byte array
        var pixels = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 4];
        bitmap.CopyPixels(pixels, bitmap.PixelWidth * 4, 0);

        // Check each pixel for transparency
        for (var i = 3; i < pixels.Length; i += 4)
        {
            if (pixels[i] < 255)
            {
                return true;
            }
        }

        // If no transparent pixels were found, return false
        return false;
    }
}