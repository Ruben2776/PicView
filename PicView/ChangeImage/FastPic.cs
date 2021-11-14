using PicView.ImageHandling;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using static PicView.ChangeImage.Navigation;

namespace PicView.ChangeImage
{
    internal static class FastPic
    {
        static System.Timers.Timer? timer;
        static bool updateSource;

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
            updateSource = false;

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
                    updateSource = true; // Update it when key released
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
            LoadPic.UpdatePic(FolderIndex, pic);

            if (Preloader.IsRunning is false)
            {
                await Preloader.PreLoad(FolderIndex).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Update after FastPic() was used
        /// </summary>
        internal static async Task FastPicUpdateAsync()
        {
            if (updateSource == false) { return; }

            // Update picture in case it didn't load. Won't happen normally

            timer = null;
            BitmapSource? pic = null;
            Preloader.PreloadValue? preloadValue = null;

            var exists = await Preloader.AddAsync(FolderIndex).ConfigureAwait(false);
            if (exists)
            {
                preloadValue = Preloader.Get(Pics[FolderIndex]);
                if (preloadValue is null)
                {
                    await Error_Handling.ReloadAsync().ConfigureAwait(false);
                    return;
                }
                if (preloadValue.bitmapSource is not null)
                {
                    pic = preloadValue.bitmapSource;
                }
                else
                {
                    pic = ImageFunctions.ImageErrorMessage();
                }
            }
            else
            {
                await Error_Handling.ReloadAsync().ConfigureAwait(false);
                return;
            }

            LoadPic.UpdatePic(FolderIndex, pic, preloadValue.fileInfo);
        }
    }
}
