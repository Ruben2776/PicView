using ImageMagick;
using PicView.ChangeImage;
using PicView.UI;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace PicView.ImageHandling
{
    internal static class Base64
    {
        internal static async Task<BitmapSource> Base64StringToBitmap(string base64String)
        {
            return await Task.Run(() =>
            {
                byte[] binaryData = Convert.FromBase64String(base64String);

                using (MagickImage magick = new MagickImage())
                {
                    var mrs = new MagickReadSettings()
                    {
                        Density = new Density(300, 300),
                        BackgroundColor = MagickColors.Transparent,
                    };

                    try
                    {
                        magick.Read(new MemoryStream(binaryData), mrs);
                    }
#if DEBUG
                    catch (MagickException e)
                    {
                        Trace.WriteLine("Base64StringToBitmap " + base64String + " null, \n" + e.Message);
                        return null;
                    }
#else
                catch (MagickException) { return null; }
#endif
                    // Set values for maximum quality
                    magick.Quality = 100;
                    magick.ColorSpace = ColorSpace.Transparent;

                    var pic = magick.ToBitmapSource();
                    pic.Freeze();
                    return pic;
                }
            }).ConfigureAwait(true);
        }

        internal static bool IsBase64String(string base64)
        {
            Span<byte> buffer = new Span<byte>(new byte[base64.Length]);
            return Convert.TryFromBase64String(base64, buffer, out _);
        }

        internal static string ConvertToBase64(string path)
        {
            byte[] imageArray = File.ReadAllBytes(path);
            return Convert.ToBase64String(imageArray);
        }

        internal static void SendToClipboard()
        {
            var base64 = ConvertToBase64(Navigation.Pics[Navigation.FolderIndex]);
            if (!string.IsNullOrWhiteSpace(base64))
            {
                Clipboard.SetText(base64);
                Tooltip.ShowTooltipMessage(Application.Current.Resources["ConvertedToBase64"]); // TODO add to translation
            }
        }

    }
}