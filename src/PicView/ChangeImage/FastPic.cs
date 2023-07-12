using PicView.ImageHandling;
using System.IO;
using static PicView.ChangeImage.Navigation;
using Timer = System.Timers.Timer;

namespace PicView.ChangeImage;

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
        _updateSource = true; // Update it when key released

        var preLoadValue = PreLoader.Get(index);

        if (preLoadValue != null)
        {
            fileInfo = preLoadValue.FileInfo ?? new FileInfo(Pics[index]);
            var showThumb = true;
            while (preLoadValue.IsLoading)
            {
                if (showThumb)
                {
                    LoadPic.LoadingPreview(fileInfo);
                    showThumb = false;
                }
                await Task.Delay(10).ConfigureAwait(false);
            }
        }
        else
        {
            fileInfo = new FileInfo(Pics[index]);
            LoadPic.LoadingPreview(fileInfo);
            await PreLoader.AddAsync(index, fileInfo).ConfigureAwait(false);
            preLoadValue = PreLoader.Get(index);
            if (preLoadValue is null)
            {
                await ErrorHandling.ReloadAsync().ConfigureAwait(false);
                return;
            }
        }

        await UpdateImage.UpdateImageAsync(index, preLoadValue).ConfigureAwait(false);

        _updateSource = false;
        await PreLoader.PreLoadAsync(index, Pics.Count).ConfigureAwait(false);
    }

    internal static async Task FastPicUpdateAsync()
    {
        _timer = null;

        if (_updateSource == false) { return; }

        // Update picture in case it didn't load. Won't happen normally

        var preLoadValue = PreLoader.Get(FolderIndex);
        if (preLoadValue is null)
        {
            await PreLoader.AddAsync(FolderIndex).ConfigureAwait(false);
            preLoadValue = PreLoader.Get(FolderIndex);
            if (preLoadValue is null)
            {
                var fileInfo = new FileInfo(Pics[FolderIndex]);
                var bitmapSource = await ImageDecoder.ReturnBitmapSourceAsync(fileInfo).ConfigureAwait(false) ??
                                   ImageFunctions.ImageErrorMessage();
                preLoadValue = new PreLoader.PreLoadValue(bitmapSource, false, fileInfo);
            }
        }
        while (preLoadValue.IsLoading)
        {
            await Task.Delay(10).ConfigureAwait(false);
        }
        await UpdateImage.UpdateImageAsync(FolderIndex, preLoadValue).ConfigureAwait(false);
    }
}