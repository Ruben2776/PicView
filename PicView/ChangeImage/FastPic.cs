using PicView.ImageHandling;
using System.IO;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Media.Imaging;

namespace PicView.ChangeImage
{
    internal static class FastPic
    {
        private static Timer? _timer;
        private static bool _updateSource;

        internal static async Task Run(int index)
        {
            if (_timer is null)
            {
                _timer = new Timer(450)
                {
                    AutoReset = false,
                    Enabled = true
                };
            }
            else if (_timer.Enabled)
            {
                return;
            }

            Navigation.FolderIndex = index;
            _timer.Start();
            FileInfo? fileInfo = null;
            BitmapSource? pic = null;
            _updateSource = false;

            var preloadValue = Preloader.Get(Navigation.Pics[index]);
            if (preloadValue != null)
            {
                if (preloadValue.IsLoading)
                {
                    await WaitForLoading(preloadValue).ConfigureAwait(false);
                }
                fileInfo = preloadValue.FileInfo;
                pic = preloadValue.BitmapSource;
            }
            else
            {
                fileInfo = new FileInfo(Navigation.Pics[index]);

                if (fileInfo.Length < 4e+6) // Load images that are less than 4mb
                {
                    pic = await ImageDecoder.ReturnBitmapSourceAsync(fileInfo).ConfigureAwait(false);
                }
                else
                {
                    pic = Thumbnails.GetBitmapSourceThumb(fileInfo);
                    _updateSource = true; // Update it when key released
                }
            }

            pic ??= ImageFunctions.ImageErrorMessage();
            await LoadPic.UpdatePicAsync(index, pic, fileInfo).ConfigureAwait(false);
            await Preloader.PreLoadAsync(index).ConfigureAwait(false);
        }

        private static async Task WaitForLoading(Preloader.PreloadValue preloadValue)
        {
            while (preloadValue.IsLoading)
            {
                await Task.Delay(50).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Update after FastPic() was used
        /// </summary>
        internal static async Task FastPicUpdateAsync()
        {
            if (_updateSource is false) { return; }

            // Update picture in case it didn't load. Won't happen normally

            _timer = null;
            BitmapSource? pic = null;
            Preloader.PreloadValue? preloadValue = null;

            if (await Preloader.AddAsync(Navigation.FolderIndex).ConfigureAwait(false))
            {
                preloadValue = Preloader.Get(Navigation.Pics[Navigation.FolderIndex]);
                if (preloadValue is null)
                {
                    await ErrorHandling.ReloadAsync().ConfigureAwait(false);
                    return;
                }

                pic = preloadValue.BitmapSource ?? ImageFunctions.ImageErrorMessage();
            }
            else
            {
                await ErrorHandling.ReloadAsync().ConfigureAwait(false);
                return;
            }

            await LoadPic.UpdatePicAsync(Navigation.FolderIndex, pic, preloadValue.FileInfo).ConfigureAwait(false);
        }
    }
}