﻿using PicView.Core.Config;
using PicView.WPF.ImageHandling;
using PicView.WPF.PicGallery;
using PicView.WPF.UILogic;
using System.Diagnostics;
using System.IO;
using static PicView.WPF.ChangeImage.Navigation;
using Timer = System.Timers.Timer;

namespace PicView.WPF.ChangeImage;

internal static class FastPic
{
    private static Timer? _timer;
    private static bool _updateSource;

    internal static async Task Run(int index)
    {
        if (_timer is null)
        {
            _timer = new Timer(TimeSpan.FromSeconds(SettingsHelper.Settings.UIProperties.NavSpeed))
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

                await Task.Delay(10);
            }
        }
        else
        {
            fileInfo = new FileInfo(Pics[index]);
            if (fileInfo.Exists == false)
            {
                return;
            }
            LoadPic.LoadingPreview(fileInfo);
            await PreLoader.AddAsync(index, fileInfo).ConfigureAwait(false);
            preLoadValue = PreLoader.Get(index);
            if (preLoadValue is null)
            {
                if (FolderIndex == index)
                {
                    await ErrorHandling.ReloadAsync();
                }

                return;
            }
        }

        await UpdateImage.UpdateImageValuesAsync(index, preLoadValue, true).ConfigureAwait(false);

        _updateSource = false;
        await PreLoader.PreLoadAsync(index, Pics.Count, false).ConfigureAwait(false);
    }

    internal static async Task FastPicUpdateAsync()
    {
        _timer = null;

        if (_updateSource == false)
        {
            return;
        }

        // Update picture in case it didn't load. Won't happen normally
        
        if (UC.GetPicGallery is not null)
        {
            await UC.GetPicGallery.Dispatcher.InvokeAsync(() =>
            {
                // Select next item
                GalleryNavigation.SetSelected(FolderIndex, true);
                GalleryNavigation.SelectedGalleryItem = FolderIndex;
                GalleryNavigation.ScrollToGalleryCenter();
            });
        }

        var preLoadValue = PreLoader.Get(FolderIndex);
        if (preLoadValue is null)
        {
            await PreLoader.AddAsync(FolderIndex).ConfigureAwait(false);
            preLoadValue = PreLoader.Get(FolderIndex);
            if (preLoadValue is null)
            {
                var fileInfo = new FileInfo(Pics[FolderIndex]);
                var bitmapSource = await Image2BitmapSource.ReturnBitmapSourceAsync(fileInfo).ConfigureAwait(false);
                preLoadValue = new PreLoader.PreLoadValue(bitmapSource, fileInfo, null);
            }
        }

        while (preLoadValue.BitmapSource is null)
        {
            await Task.Delay(10).ConfigureAwait(false);
        }
        try
        {
            await UpdateImage.UpdateImageValuesAsync(FolderIndex, preLoadValue).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(FastPicUpdateAsync)} cancelled:\n");
#endif
        }
        catch (Exception ex)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(FastPicUpdateAsync)} exception:\n{ex.Message}");
#endif
            Tooltip.ShowTooltipMessage(ex.Message, true, TimeSpan.FromSeconds(5));
        }
    }
}