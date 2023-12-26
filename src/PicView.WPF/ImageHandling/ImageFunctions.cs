using ImageMagick;
using PicView.Core.FileHandling;
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
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
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
    internal static async Task<bool> SetRating(ushort rating) => await Task.Run(() =>
    {
        if (ErrorHandling.CheckOutOfRange())
        {
            return false;
        }

        try
        {
            using var image = new MagickImage(Navigation.Pics[Navigation.FolderIndex]);
            var profile = image?.GetExifProfile();
            if (profile is null)
            {
                profile = new ExifProfile(Navigation.Pics[Navigation.FolderIndex]);
                if (profile is null || image is null)
                    return false;
            }
            else if (image is null)
                return false;

            profile.SetValue(ExifTag.Rating, rating);
            image.SetProfile(profile);

            image.Write(Navigation.Pics[Navigation.FolderIndex]);
            return true;
        }
        catch (MagickException exception)
        {
#if DEBUG
            Trace.WriteLine(
                $"{nameof(ImageFunctions)}::{nameof(SetRating)} caught exception:\n{exception.Message}");
#endif
            Tooltip.ShowTooltipMessage(exception.Message, true, TimeSpan.FromSeconds(5));
            return false;
        }
    });

    internal static async Task OptimizeImageAsyncWithErrorChecking()
    {
        if (ErrorHandling.CheckOutOfRange())
        {
            return;
        }

        await Task.Run(async () =>
        {
            var toCenter = false;

            await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() => { toCenter = UC.UserControls_Open(); });

            Tooltip.ShowTooltipMessage(Application.Current.Resources["Applying"] as string, toCenter);

            var success = OptimizeImage(Navigation.Pics[Navigation.FolderIndex]);

            // Update title to show new file size
            if (success)
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
                {
                    var width = ConfigureWindows.GetMainWindow.MainImage.Source.Width;
                    var height = ConfigureWindows.GetMainWindow.MainImage.Source.Height;

                    SetTitle.SetTitleString((int)width, (int)height, Navigation.FolderIndex, null);
                    Tooltip.CloseToolTipMessage();
                });
            }
            else
            {
                Tooltip.ShowTooltipMessage("0%", toCenter);
                return;
            }

            var preloadValue = PreLoader.Get(Navigation.FolderIndex);
            if (preloadValue == null)
            {
                await PreLoader.AddAsync(Navigation.FolderIndex).ConfigureAwait(false);
            }

            var fileInfo = new FileInfo(Navigation.Pics[Navigation.FolderIndex]);
            var readablePrevSize = preloadValue.FileInfo.Length.GetReadableFileSize();
            var readableNewSize = fileInfo.Length.GetReadableFileSize();

            var originalValue = preloadValue.FileInfo.Length;
            var decreasedValue = fileInfo.Length;
            if (originalValue != decreasedValue)
            {
                var percentDecrease = ((float)(originalValue - decreasedValue) / decreasedValue) * 100;
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Render,
                    () =>
                    {
                        Tooltip.ShowTooltipMessage(
                            $"{readablePrevSize} > {readableNewSize} = {percentDecrease.ToString("0.## ", CultureInfo.CurrentCulture)}%",
                            toCenter, TimeSpan.FromSeconds(3.5));
                    });
            }
            else
            {
                Tooltip.ShowTooltipMessage("0%", toCenter);
            }
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
        // Create a new ImageOptimizer with the specified lossless compression option
        ImageOptimizer imageOptimizer = new()
        {
            OptimalCompression = lossless
        };

        // Check if the file is supported by the ImageOptimizer
        if (imageOptimizer.IsSupported(file) == false)
        {
            // Show a tooltip message indicating that the file is unsupported
            Tooltip.ShowTooltipMessage(Application.Current.Resources["UnsupportedFile"], true, TimeSpan.FromSeconds(5));

            // Return false to indicate that the optimization was not successful
            return false;
        }

        try
        {
            // Compress the image using the ImageOptimizer and return true if successful
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

    internal static BitmapSource ShowLogo()
    {
        var bitmap = new BitmapImage(new Uri(@"pack://application:,,,/"
                                             + Assembly.GetExecutingAssembly().GetName().Name
                                             + ";component/"
                                             + "Themes/Resources/img/icon.png", UriKind.Absolute));
        bitmap.Freeze();
        return bitmap;
    }

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
}