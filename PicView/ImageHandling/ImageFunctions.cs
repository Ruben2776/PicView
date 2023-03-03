using ImageMagick;
using PicView.ChangeImage;
using PicView.ChangeTitlebar;
using PicView.FileHandling;
using PicView.UILogic;
using PicView.UILogic.Sizing;
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
        internal static async Task<bool> SetRating(ushort rating) => await Task.Run(() =>
        {
            if (ErrorHandling.CheckOutOfRange())
            {
                return false;
            }

            try
            {
                using MagickImage image = new MagickImage(Navigation.Pics[Navigation.FolderIndex]);
                var profile = image.GetExifProfile();
                profile.SetValue(ExifTag.Rating, rating);

                image.SetProfile(profile);

                image.Write(Navigation.Pics[Navigation.FolderIndex]);
                return true;
            }
            catch (MagickException)
            {
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

            var preloadValue = Preloader.Get(Navigation.Pics[Navigation.FolderIndex]);
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

        internal static async Task<bool> OptimizeImageAsync(string file, bool lossless = true) => await Task.Run(() =>
        {
            ImageOptimizer imageOptimizer = new()
            {
                OptimalCompression = lossless
            };

            if (imageOptimizer.IsSupported(file) == false)
            {
                return false;
            }

            try
            {
                return imageOptimizer.LosslessCompress(file);
            }
            catch (Exception)
            {
                return false;
            }
        });

        internal static RenderTargetBitmap? ImageErrorMessage()
        {
            var brush = Application.Current.TryFindResource("MainColorBrush") as Brush;
            if (brush == null) { return null; }

            var w = ScaleImage.XWidth != 0 ? ScaleImage.XWidth : 300 * WindowSizing.MonitorInfo.DpiScaling;
            var h = ScaleImage.XHeight != 0 ? ScaleImage.XHeight : 300 * WindowSizing.MonitorInfo.DpiScaling;
            var rect = new Rect(new Size(w, h));
            var visual = new DrawingVisual();
            try
            {
                using (var ctx = visual.RenderOpen())
                {
                    var typeface = new Typeface("/PicView;component/Themes/Resources/fonts/#Tex Gyre Heros");
                    //text
                    var text = new FormattedText("Unable to render image", CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, 16, brush, WindowSizing.MonitorInfo.DpiScaling)
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

        internal static BitmapSource? ShowLogo()
        {
           var bitmap = new BitmapImage(new Uri(@"pack://application:,,,/"
                + Assembly.GetExecutingAssembly().GetName().Name
                + ";component/"
                + "Themes/Resources/img/icon__Q6k_icon.ico", UriKind.Absolute));
            bitmap.Freeze();
            return bitmap;
        }
    }
}