using PicView.ImageHandling;
using PicView.SystemIntegration;
using System.IO;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Media.Imaging;
using static PicView.ChangeImage.Navigation;
using static PicView.ChangeImage.Preloader;
using Timer = System.Timers.Timer;

namespace PicView.ChangeImage
{
    internal static class FastPic
    {
        static Timer? timer;
        static bool updateSource;

        internal static async Task Run(int index)
        {
            if (timer is null)
            {
                timer = new Timer(TimeSpan.FromSeconds(.4))
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
            updateSource = true; // Update it when key released

            var preloadValue = Preloader.Get(Pics[FolderIndex]);

            if (preloadValue != null)
            {
                fileInfo = preloadValue.FileInfo ?? new FileInfo(Pics[FolderIndex]);
                var showthumb = true;
                while (preloadValue.IsLoading)
                {
                    if (showthumb)
                    {
                        LoadPic.LoadingPreview(fileInfo);
                        showthumb = false;
                    }
                    await Task.Delay(10).ConfigureAwait(false);
                }
                pic = preloadValue.BitmapSource ?? ImageFunctions.ImageErrorMessage();
            }
            else
            {
                fileInfo = new FileInfo(Pics[FolderIndex]);
                LoadPic.LoadingPreview(fileInfo);
                preloadValue = await Preloader.AddAsync(index, fileInfo).ConfigureAwait(false);
                if (preloadValue is null)
                {
                    await ErrorHandling.ReloadAsync().ConfigureAwait(false);
                    return;
                }

                pic = preloadValue.BitmapSource;
            }

            Taskbar.Progress(FolderIndex);
            await UpdateImage.UpdateImageAsync(FolderIndex, pic, fileInfo).ConfigureAwait(false);
            updateSource = false;
            await Preloader.PreLoadAsync(FolderIndex).ConfigureAwait(false);
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

            preloadValue = await Preloader.AddAsync(FolderIndex).ConfigureAwait(false);
            if (preloadValue is null)
            {
                await ErrorHandling.ReloadAsync().ConfigureAwait(false);
                return;
            }
            while (preloadValue.IsLoading)
            {
                await Task.Delay(20).ConfigureAwait(false);
            }
            pic = preloadValue.BitmapSource ?? ImageFunctions.ImageErrorMessage();
            await UpdateImage.UpdateImageAsync(FolderIndex, pic, preloadValue.FileInfo).ConfigureAwait(false);
        }
    }
}
