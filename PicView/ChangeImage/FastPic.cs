using PicView.ImageHandling;
using System.IO;
using System.Windows.Media.Imaging;

namespace PicView.ChangeImage
{
    internal static class FastPic
    {
        private static System.Timers.Timer? _timer;
        private static bool _updateSource;

        internal static async Task Run(int index)
        {
            if (_timer is null)
            {
                _timer = new System.Timers.Timer(450)
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
            await UpdateImage.UpdateImageAsync(index, pic, fileInfo).ConfigureAwait(false);
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

            preloadValue = Preloader.Get(Navigation.Pics[Navigation.FolderIndex]) ??
                await Preloader.AddAsync(Navigation.FolderIndex).ConfigureAwait(false);

            await UpdateImage.UpdateImageAsync(Navigation.FolderIndex, pic, preloadValue.FileInfo).ConfigureAwait(false);
        }
    }
}