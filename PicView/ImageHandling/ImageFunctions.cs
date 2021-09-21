using ImageMagick;
using PicView.UILogic.Sizing;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PicView.ImageHandling
{
    internal static class ImageFunctions
    {
        internal static async Task<bool> OptimizeImageAsync(string file) => await Task.Run(() =>
        {
            switch (Path.GetExtension(file).ToUpperInvariant())
            {
                case ".JPG":
                case ".JPEG":
                case ".PNG":
                case ".ICO":
                    break;
                default: return false;
            }
            ImageOptimizer imageOptimizer = new()
            {
                OptimalCompression = true
            };
            return imageOptimizer.LosslessCompress(file);
        });

        internal static async Task<Size?> ImageSizeAsync(string file)
        {
            using var magick = new MagickImage();
            FileInfo? fileInfo= new FileInfo(file);
            if (fileInfo.Length > 2e+9)
            {
#if DEBUG
                Trace.WriteLine("File size bigger than 2gb");
#endif
                return null;
            }
            try
            {
                await magick.ReadAsync(fileInfo).ConfigureAwait(false);
            }
#if DEBUG
            catch (MagickException e)
            {
                Trace.WriteLine("ImageSize returned " + file + " null, \n" + e.Message);
                return null;
            }
#else
                catch (MagickException) { return null; }
#endif

            return new Size(magick.Width, magick.Height);
        }

        internal static RenderTargetBitmap ImageErrorMessage()
        {
            var w = ScaleImage.XWidth != 0 ? ScaleImage.XWidth : 300 * WindowSizing.MonitorInfo.DpiScaling;
            var h = ScaleImage.XHeight != 0 ? ScaleImage.XHeight : 300 * WindowSizing.MonitorInfo.DpiScaling;
            var rect = new Rect(new Size(w, h));
            var visual = new DrawingVisual();
            using (var ctx = visual.RenderOpen())
            {
                var typeface = new Typeface("/PicView;component/Themes/Resources/fonts/#Tex Gyre Heros");
                //text
                var text = new FormattedText("Unable to render image", CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, 16, (Brush)Application.Current.Resources["MainColorBrush"], WindowSizing.MonitorInfo.DpiScaling)
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
    }
}
