using ImageMagick;
using PicView.ChangeImage;
using PicView.FileHandling;
using PicView.UILogic;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace PicView.ImageHandling
{
    internal static class Base64
    {
        internal static Task<BitmapSource> Base64StringToBitmap(string base64String) => Task.Run(() =>
        {
            byte[] binaryData = Convert.FromBase64String(base64String);

            using MagickImage magick = new MagickImage();
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
        });

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
                string url = FileFunctions.GetURL(ConfigureWindows.GetMainWindow.TitleText.Text);
                if (Uri.IsWellFormedUriString(url, UriKind.Absolute)) // Check if from web
                {
                    using (var client = new HttpClient())
                    {
                        using (var response = await client.GetAsync(url))
                        {
                            byte[] imageBytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(true);
                            path = Convert.ToBase64String(imageBytes);
                        }
                    }
                    
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
                await Tooltip.ShowTooltipMessage(Application.Current.Resources["ConvertedToBase64"]).ConfigureAwait(false);
            }
        }
    }
}