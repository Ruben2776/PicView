using System.IO;
using System.Windows.Media.Imaging;
using PicView.ImageHandling;
using PicView.SystemIntegration;
using static PicView.ChangeImage.Navigation;
using Timer = System.Timers.Timer;

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
                _timer = new Timer(TimeSpan.FromSeconds(Properties.Settings.Default.NavSpeed))
                {
                    AutoReset = false,
                    Enabled = true
                };
            }
            else if (_timer.Enabled)
            {
                return;
            }

            FolderIndex = index;
            _timer.Start();
            FileInfo? fileInfo;
            BitmapSource? pic;
            _updateSource = true; // Update it when key released

            var preloadValue = PreLoader.Get(index);

            if (preloadValue != null)
            {
                fileInfo = preloadValue.FileInfo ?? new FileInfo(Pics[index]);
                var showThumb = true;
                while (preloadValue.IsLoading)
                {
                    if (showThumb)
                    {
                        LoadPic.LoadingPreview(fileInfo);
                        showThumb = false;
                    }
                    await Task.Delay(10).ConfigureAwait(false);
                }
                pic = preloadValue.BitmapSource;
            }
            else
            {
                fileInfo = new FileInfo(Pics[index]);
                LoadPic.LoadingPreview(fileInfo);
                await PreLoader.AddAsync(index, fileInfo).ConfigureAwait(false);
                preloadValue = PreLoader.Get(index);
                if (preloadValue is null)
                {
                    await ErrorHandling.ReloadAsync().ConfigureAwait(false);
                    return;
                }

                pic = preloadValue.BitmapSource;
            }

            Taskbar.Progress((double)index / Pics.Count);
            await UpdateImage.UpdateImageAsync(index, pic, fileInfo).ConfigureAwait(false);
            _updateSource = false;
            await PreLoader.PreLoadAsync(index).ConfigureAwait(false);
        }

        internal static async Task FastPicUpdateAsync()
        {
            _timer = null;

            if (_updateSource == false) { return; }

            // Update picture in case it didn't load. Won't happen normally
            
            BitmapSource? pic;
            var preloadValue = PreLoader.Get(FolderIndex);
            if (preloadValue is null)
            {
                await PreLoader.AddAsync(FolderIndex).ConfigureAwait(false);
                preloadValue = PreLoader.Get(FolderIndex);
                if (preloadValue is null)
                {
                    var fileInfo = new FileInfo(Pics[FolderIndex]);
                    var bitmapSource = await ImageDecoder.ReturnBitmapSourceAsync(fileInfo).ConfigureAwait(false) ??
                                         ImageFunctions.ImageErrorMessage();
                    preloadValue = new PreLoader.PreLoadValue(bitmapSource, false, fileInfo);
                }
            }
            while (preloadValue.IsLoading)
            {
                await Task.Delay(10).ConfigureAwait(false);
            }
            pic = preloadValue.BitmapSource;
            await UpdateImage.UpdateImageAsync(FolderIndex, pic, preloadValue.FileInfo).ConfigureAwait(false);
        }
    }
}