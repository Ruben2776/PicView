using ImageMagick;
using PicView.ChangeImage;
using PicView.FileHandling;
using PicView.Library;
using PicView.UILogic;
using PicView.UILogic.Loading;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
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

        internal static async Task<string> ConvertToBase64(string path)
        {
            var imageArray = await File.ReadAllBytesAsync(path).ConfigureAwait(true);
            return Convert.ToBase64String(imageArray);
        }

        internal static async Task SendToClipboard()
        {
            string path;
            if (Navigation.FolderIndex == 0)
            {
                string url = FileFunctions.GetURL(LoadWindows.GetMainWindow.TitleText.Text);
                if (Uri.IsWellFormedUriString(url, UriKind.Absolute)) // Check if from web
                {
                    using var webclient = new WebClient();
                    var data = await webclient.DownloadDataTaskAsync(new Uri(url)).ConfigureAwait(true);
                    path = Convert.ToBase64String(data);
                }
                else
                {
                    // TODO add method to convert images from clipholder to base64
                    return;
                }
            }
            else
            {
                path = await ConvertToBase64(Navigation.Pics[Navigation.FolderIndex]).ConfigureAwait(true);
            }

            if (!string.IsNullOrWhiteSpace(path))
            {
                Clipboard.SetText(path);
                Tooltip.ShowTooltipMessage(Application.Current.Resources["ConvertedToBase64"]);
            }
        }
    }
}