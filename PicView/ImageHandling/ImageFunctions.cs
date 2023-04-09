using ImageMagick;
using PicView.ChangeImage;
using PicView.ChangeTitlebar;
using PicView.FileHandling;
using PicView.UILogic;
using PicView.UILogic.Sizing;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using TextAlignment = System.Windows.TextAlignment;

namespace PicView.ImageHandling
{
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
                using MagickImage image = new MagickImage(Navigation.Pics[Navigation.FolderIndex]);
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
                Trace.WriteLine($"{nameof(ImageFunctions)}::{nameof(SetRating)} caught exception:\n{exception.Message}");
#endif
                Tooltip.ShowTooltipMessage(exception.Message);
                return false;
            }
        });

        internal static async Task OptimizeImageAsyncWithErrorChecking()
        {
            if (ErrorHandling.CheckOutOfRange()) { return; }

            bool toCenter = false;

            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
            {
                toCenter = UC.QuickSettingsMenuOpen;
                if (toCenter is false)
                {
                    toCenter = UC.ToolsAndEffectsMenuOpen;
                }
            });

            Tooltip.ShowTooltipMessage(Application.Current.Resources["Applying"] as string, toCenter);

            var success = await OptimizeImageAsync(Navigation.Pics[Navigation.FolderIndex]).ConfigureAwait(false);

            if (success)
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Render, () =>
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

            var preloadValue = Preloader.Get(Navigation.FolderIndex);
            if (preloadValue == null)
            {
                await Preloader.AddAsync(Navigation.FolderIndex).ConfigureAwait(false);
            }

            var fileInfo = new FileInfo(Navigation.Pics[Navigation.FolderIndex]);
            var readablePrevSize = FileFunctions.GetReadableFileSize(preloadValue.FileInfo.Length);
            var readableNewSize = FileFunctions.GetReadableFileSize(fileInfo.Length);

            var originalValue = preloadValue.FileInfo.Length;
            var decreasedValue = fileInfo.Length;
            if (originalValue != decreasedValue)
            {
                var percentDecrease = ((float)(originalValue - decreasedValue) / decreasedValue) * 100;
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Render, () =>
                {
                    Tooltip.ShowTooltipMessage($"{readablePrevSize} > {readableNewSize} = {percentDecrease.ToString("0.## ", CultureInfo.CurrentCulture)}%", toCenter, TimeSpan.FromSeconds(3.5));
                });
            }
            else
            {
                Tooltip.ShowTooltipMessage("0%", toCenter);
            }
        }

        /// <summary>
        /// Optimizes the image at the specified file path.
        /// </summary>
        /// <param name="file">The file path of the image to optimize.</param>
        /// <param name="lossless">Specifies whether to use lossless compression. Default is true.</param>
        /// <returns>True if the optimization was successful, false otherwise.</returns>
        internal static async Task<bool> OptimizeImageAsync(string file, bool lossless = true) => await Task.Run(() =>
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
                Tooltip.ShowTooltipMessage(Application.Current.Resources["UnsupportedFile"]);

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
                Trace.WriteLine($"{nameof(ImageFunctions)}::{nameof(SetRating)} caught exception:\n{exception.Message}");
#endif
                // Show a tooltip message indicating that an error occurred during compression
                Tooltip.ShowTooltipMessage(exception.Message);

                // Return false to indicate that the optimization was not successful
                return false;
            }
        });

        internal static RenderTargetBitmap? ImageErrorMessage()
        {
            var brush = Application.Current.TryFindResource("MainColorBrush") as Brush;
            if (brush == null)
            {
                return null;
            }

            var w = ScaleImage.XWidth != 0 ? ScaleImage.XWidth : 300 * WindowSizing.MonitorInfo.DpiScaling;
            var h = ScaleImage.XHeight != 0 ? ScaleImage.XHeight : 300 * WindowSizing.MonitorInfo.DpiScaling;
            var rect = new Rect(new Size(w, h));
            var visual = new DrawingVisual();

            try
            {
                using (var ctx = visual.RenderOpen())
                {
                    var typeface = new Typeface(new FontFamily("Tex Gyre Heros"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
                    var text = new FormattedText((string)Application.Current.Resources["UnableToRender"],
                                                 CultureInfo.CurrentUICulture, FlowDirection, typeface, 16, brush, WindowSizing.MonitorInfo.DpiScaling)
                    {
                        TextAlignment = TextAlignment.Center
                    };
                    ctx.DrawText(text, new Point(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2));
                }

                RenderTargetBitmap rtv = new((int)w, (int)h, 96.0, 96.0, PixelFormats.Default);
                rtv.Render(visual);
                rtv.Freeze();
                return rtv;
            }
            catch (Exception)
            {
                return null;
            }
        }
        static FlowDirection FlowDirection => CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

        internal static BitmapSource? ShowLogo()
        {
           var bitmap = new BitmapImage(new Uri(@"pack://application:,,,/"
                + Assembly.GetExecutingAssembly().GetName().Name
                + ";component/"
                + "Themes/Resources/img/icon.png", UriKind.Absolute));
            bitmap.Freeze();
            return bitmap;
        }
    }
}