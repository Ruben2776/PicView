using PicView.ImageHandling;
using PicView.UILogic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
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
            var image = Application.Current.Resources["Image"] as string; // Show string 'image' fron translation service
            var fileInfo = new FileInfo(Pics[index]);
            BitmapSource? pic = null;
            bool fitImage = true;
            if (fileInfo.Length < 4e+6) // Load images that are less than 4mb
            {
                pic = await ImageDecoder.ReturnBitmapSourceAsync(fileInfo).ConfigureAwait(false);
            }
            else
            {
                pic = Thumbnails.GetBitmapSourceThumb(fileInfo);
                fitImage = false;
            }
            if (pic is null)
            {
                return;
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
        }

        /// <summary>
        /// Update after FastPic() was used
        /// </summary>
        internal static async Task FastPicUpdateAsync()
        {
            timer = null;

            Preloader.PreloadValue? preloadValue;

            preloadValue = Preloader.Get(Navigation.Pics[FolderIndex]);

            if (preloadValue == null) // Error correctiom
            {
                await Preloader.AddAsync(FolderIndex).ConfigureAwait(false);
                preloadValue = Preloader.Get(Navigation.Pics[FolderIndex]);
            }
            while (preloadValue != null && preloadValue.isLoading)
            {
                // Wait for finnished result
                await Task.Delay(5).ConfigureAwait(false);
            }

            if (preloadValue == null || preloadValue.bitmapSource == null)
            {
                await Error_Handling.ReloadAsync().ConfigureAwait(false);
                return;
            }

            LoadPic.UpdatePic(FolderIndex, preloadValue.bitmapSource);
        }
    }
}
