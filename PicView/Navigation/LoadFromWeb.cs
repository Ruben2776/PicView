using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static PicView.AjaxLoader;
using static PicView.Error_Handling;
using static PicView.Fields;
using static PicView.ImageDecoder;
using static PicView.SetTitle;
using static PicView.Thumbnails;
using static PicView.Tooltip;
using static PicView.Navigation;

namespace PicView
{
    internal static class LoadFromWeb
    {
        /// <summary>
        /// Attemps to download image and display it
        /// </summary>
        /// <param name="path"></param>
        internal static async void PicWeb(string path)
        {
            if (ajaxLoading.Opacity != 1)
                AjaxLoadingStart();

            mainWindow.Bar.Text = Loading;

            BitmapSource pic;
            try
            {
                pic = await LoadImageWebAsync(path).ConfigureAwait(true);
            }
#if DEBUG
            catch (Exception e)
            {
                Trace.WriteLine("PicWeb caught exception, message = " + e.Message);
                pic = null;
            }
#else
            catch (Exception) { pic = null; }
#endif
            if (pic == null)
            {
                Reload(true);
                ToolTipStyle("Unable to load image");
                AjaxLoadingEnd();
                return;
            }

            Pic(pic, path);
            RecentFiles.Add(path);
            canNavigate = false;
        }

        /// <summary>
        /// Downloads image from web and returns as BitmapSource
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        internal static async Task<BitmapSource> LoadImageWebAsync(string address)
        {
            canNavigate = false;
            BitmapSource pic = null;
            await Task.Run(async () =>
            {
                var client = new WebClient();
                client.DownloadProgressChanged += (sender, e) =>
                mainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    mainWindow.Title = mainWindow.Bar.Text = e.BytesReceived + "/" + e.TotalBytesToReceive + ". " + e.ProgressPercentage + "% complete...";
                    mainWindow.Bar.ToolTip = mainWindow.Title;
                }));
                client.DownloadDataCompleted += (sender, e) =>
                mainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    if (pic != null)
                        SetTitleString(pic.PixelWidth, pic.PixelHeight, address);
                    canNavigate = false;
                }));

                var bytes = await client.DownloadDataTaskAsync(new Uri(address)).ConfigureAwait(false);
                var stream = new MemoryStream(bytes);
                pic = GetMagickImage(stream);
            }).ConfigureAwait(false);
            return pic;
        }
    }
}
