using PicView.ImageHandling;
using PicView.UILogic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static PicView.ChangeImage.Navigation;

namespace PicView.ChangeImage
{
    internal static class FastPic
    {
        static System.Timers.Timer? timer;

        internal static async Task Run(int index)
        {
            if (timer is null)
            {
                timer = new System.Timers.Timer(450)
                {
                    AutoReset = false,
                    Enabled = true
                };
            }
            else if (timer.Enabled)
            {
                return;
            }

            FolderIndex = index;
            timer.Start();
            FileInfo? fileInfo = null;
            BitmapSource? pic = null;
            bool fitImage = true;

            var preloadValue = Preloader.Get(Pics[index]);
            if (preloadValue != null)
            {
                if (preloadValue.isLoading)
                {
                    do
                    {
                        await Task.Delay(50);
                    } while (preloadValue.isLoading);
                }
                if (preloadValue.bitmapSource is not null)
                {
                    pic = preloadValue.bitmapSource;
                }
                if (preloadValue.fileInfo is not null)
                {
                    fileInfo = preloadValue.fileInfo;
                }
            }
            else
            {
                fileInfo = new FileInfo(Pics[index]);

                if (fileInfo.Length < 4e+6) // Load images that are less than 4mb
                {
                    pic = await ImageDecoder.ReturnBitmapSourceAsync(fileInfo).ConfigureAwait(false);
                }
                else
                {
                    pic = Thumbnails.GetBitmapSourceThumb(fileInfo);
                    fitImage = false;
                }
            }

            if (pic is null)
            {
                pic = ImageFunctions.ImageErrorMessage();
                if (pic is null)
                {
                    Error_Handling.UnexpectedError();
                    return;
                }
            }

            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Render, () =>
            {
                SetTitle.SetTitleString(pic.PixelWidth, pic.PixelHeight, index, fileInfo);
                ConfigureWindows.GetMainWindow.MainImage.Source = pic;

                if (fitImage)
                {
                    UILogic.Sizing.ScaleImage.FitImage(pic.PixelWidth, pic.PixelHeight);
                }
            });

            if (Preloader.IsRunning is false)
            {
                await Preloader.PreLoad(FolderIndex).ConfigureAwait(false);
            }
        }
    }
}
