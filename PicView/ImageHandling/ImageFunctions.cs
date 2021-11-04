using ImageMagick;
using PicView.ChangeImage;
using PicView.UILogic;
using PicView.UILogic.Sizing;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PicView.ImageHandling
{
    internal static class ImageFunctions
    {
        internal static async Task<bool> SetRating(ushort rating) => await Task.Run(() =>
        {
            if (Error_Handling.CheckOutOfRange())
            {
                return false;
            }

            try
            {
                using (MagickImage image = new MagickImage(Navigation.Pics[Navigation.FolderIndex]))
                {
                    var profile = new ExifProfile();
                    profile.SetValue(ExifTag.Rating, rating);

                    image.SetProfile(profile);

                    image.Write(Navigation.Pics[Navigation.FolderIndex]);
                }
                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        });


        internal static async Task OptimizeImageAsyncWithErrorChecking()
        {
            if (Error_Handling.CheckOutOfRange()) { return; }

            var preloadValue = Preloader.Get(Navigation.Pics[Navigation.FolderIndex]);
            if (preloadValue == null)
            {
                await Preloader.AddAsync(Navigation.FolderIndex).ConfigureAwait(false);
            }

            bool toCenter = false;

            var success = await OptimizeImageAsync(Navigation.Pics[Navigation.FolderIndex]).ConfigureAwait(false);

            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, () =>
            {
                toCenter = UC.QuickSettingsMenuOpen;
            });

            Tooltip.ShowTooltipMessage(Application.Current.Resources["Applying"] as string, toCenter);

            if (success)
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render, () =>
                {
                    var width = ConfigureWindows.GetMainWindow.MainImage.Source.Width;
                    var height = ConfigureWindows.GetMainWindow.MainImage.Source.Height;

                    SetTitle.SetTitleString((int)width, (int)height, ChangeImage.Navigation.FolderIndex, null);
                    Tooltip.CloseToolTipMessage();
                });
            }

            var fileInfo = new System.IO.FileInfo(Navigation.Pics[Navigation.FolderIndex]);
            var readablePrevSize = FileHandling.FileFunctions.GetSizeReadable(preloadValue.fileInfo.Length);
            var readableNewSize = FileHandling.FileFunctions.GetSizeReadable(fileInfo.Length);

            var originalValue = preloadValue.fileInfo.Length;
            var decreasedValue = fileInfo.Length;
            if (originalValue != decreasedValue)
            {
                var percentDecrease = ((float)(originalValue - decreasedValue) / decreasedValue) * 100;
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render, () =>
                {
                    Tooltip.ShowTooltipMessage($"{readablePrevSize} > {readableNewSize} = {percentDecrease.ToString("0.## ", CultureInfo.CurrentCulture)}%", toCenter, System.TimeSpan.FromSeconds(3.5));
                });
            }
            else
            {
                Tooltip.ShowTooltipMessage($"0%", toCenter);
            }

        }

        internal static async Task<bool> OptimizeImageAsync(string file) => await Task.Run(() =>
        {
            ImageOptimizer imageOptimizer = new()
            {
                OptimalCompression = true
            };

            if (imageOptimizer.IsSupported(file) == false)
            {
                return false;
            }

            return imageOptimizer.LosslessCompress(file);
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
                        TextAlignment = System.Windows.TextAlignment.Center
                    };

                    ctx.DrawText(text, new Point(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2));
                }
                RenderTargetBitmap rtv = new((int)w, (int)h, 96.0, 96.0, PixelFormats.Default);
                rtv.Render(visual);
                rtv.Freeze();
                return rtv;
            }
            catch (System.Exception)
            {
                return null;
            }
        }
    }
}
