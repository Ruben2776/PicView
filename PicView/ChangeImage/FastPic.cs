using PicView.SystemIntegration;
using System;
using System.IO;
using System.Windows.Media.Imaging;
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
                _timer = new Timer(TimeSpan.FromSeconds(.7))
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
            FileInfo? fileInfo = null;
            BitmapSource? pic = null;
            _updateSource = true; // Update it when key released

            var preloadValue = Preloader.Get(index);

            if (preloadValue != null)
            {
                fileInfo = preloadValue.FileInfo ?? new FileInfo(Pics[index]);
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
                pic = preloadValue.BitmapSource;
            }
            else
            {
                fileInfo = new FileInfo(Pics[index]);
                LoadPic.LoadingPreview(fileInfo);
                await Preloader.AddAsync(index, fileInfo).ConfigureAwait(false);
                preloadValue = Preloader.Get(index);
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
            await Preloader.PreLoadAsync(index).ConfigureAwait(false);
        }

        internal static async Task FastPicUpdateAsync()
        {
            _timer = null;

            if (_updateSource == false) { return; }

            // Update picture in case it didn't load. Won't happen normally
            
            BitmapSource? pic = null;
            var preloadValue = Preloader.Get(FolderIndex);
            if (preloadValue is null)
            {
                await Preloader.AddAsync(FolderIndex).ConfigureAwait(false);
                preloadValue = Preloader.Get(FolderIndex);
                if (preloadValue is null)
                {
                    await ErrorHandling.ReloadAsync().ConfigureAwait(false);
                    return;
                }
            }
            while (preloadValue.IsLoading)
            {
                await Task.Delay(20).ConfigureAwait(false);
            }
            pic = preloadValue.BitmapSource;
            await UpdateImage.UpdateImageAsync(FolderIndex, pic, preloadValue.FileInfo).ConfigureAwait(false);
        }
    }
}