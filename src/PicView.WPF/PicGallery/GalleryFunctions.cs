﻿using PicView.Core.Config;
using PicView.WPF.ChangeImage;
using PicView.WPF.UILogic;
using PicView.WPF.Views.UserControls.Gallery;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using PicView.Core.Extensions;
using static PicView.WPF.PicGallery.GalleryLoad;
using static PicView.WPF.UILogic.UC;

namespace PicView.WPF.PicGallery;

internal static class GalleryFunctions
{
    internal static bool IsGalleryOpen { get; set; }

    private struct TempGalleryItem(
        string fileLocation,
        string fileName,
        string fileSize,
        string fileDate,
        BitmapSource source)
    {
        internal string FileLocation = fileLocation;
        internal string FileName = fileName;
        internal string FileSize = fileSize;
        internal string FileDate = fileDate;
        internal BitmapSource Source { get; set; } = source;
    }

    internal static void ReCalculateItemSizes()
    {
        if (GetPicGallery is null)
        {
            return;
        }

        if (GetPicGallery.Container.Children.Count <= 0)
        {
            return;
        }

        var tempItem = (PicGalleryItem)GetPicGallery.Container.Children[0];

        if (Math.Abs(tempItem.OuterBorder.Height - GalleryNavigation.PicGalleryItemSize) < 1)
        {
            return;
        }

        for (var i = 0; i < GetPicGallery.Container.Children.Count; i++)
        {
            var item = (PicGalleryItem)GetPicGallery.Container.Children[i];
            item.InnerBorder.Height = item.InnerBorder.Width = SettingsHelper.Settings.Gallery.IsBottomGalleryShown
                ? GalleryNavigation.PicGalleryItemSize
                : GalleryNavigation.PicGalleryItemSizeS;
            item.OuterBorder.Height = item.OuterBorder.Width = GalleryNavigation.PicGalleryItemSize;
            // Make sure it's deselected
            if (Navigation.Pics.IndexOf(item.Name) == Navigation.FolderIndex)
            {
                item.InnerBorder.BorderBrush = Application.Current.Resources["BorderBrush"] as SolidColorBrush;
            }
        }
    }

    internal static async Task SortGalleryAsync()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        var initialDirectory = Path.GetDirectoryName(Navigation.Pics[0]);
        while (IsLoading)
        {
            await Task.Delay(200, cancellationTokenSource.Token).ConfigureAwait(false);
            if (initialDirectory != Path.GetDirectoryName(Navigation.Pics[Navigation.FolderIndex]))
            {
                // Directory changed, cancel the operation
                await cancellationTokenSource.CancelAsync();
                return;
            }
        }

        var thumbs = new List<TempGalleryItem>();

        for (var i = 0; i < Navigation.Pics.Count; i++)
        {
            try
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
                {
                    if (i > GetPicGallery.Container.Children.Count || i < 0)
                    {
                        return;
                    }
                    var picGalleryItem = GetPicGallery.Container.Children[i] as PicGalleryItem;
                    var thumb = new TempGalleryItem(picGalleryItem.ThumbFileLocation.Text,
                        picGalleryItem.ThumbFileName.Text, picGalleryItem.ThumbFileSize.Text,
                        picGalleryItem.ThumbFileDate.Text, picGalleryItem.ThumbImage.Source as BitmapSource);
                    thumbs.Add(thumb);
                }, DispatcherPriority.Render, cancellationTokenSource.Token);
                if (initialDirectory != Path.GetDirectoryName(Navigation.Pics[0]))
                {
                    // Directory changed, cancel the operation
                    await cancellationTokenSource.CancelAsync();
                    return;
                }
            }
            catch (OperationCanceledException)
            {
                Clear();
            }
            catch (Exception)
            {
                Clear();
                await LoadAsync().ConfigureAwait(false);
                return;
            }
        }
        
        //Clear();

        try
        {
            thumbs = thumbs.OrderBySequence(Navigation.Pics, x => x.FileLocation).ToList();
            for (var i = 0; i < Navigation.Pics.Count; i++)
            {
                var index = i;
                await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
                {
                    UpdatePic(index, (BitmapSource?)thumbs[index].Source, thumbs[index].FileLocation, thumbs[index].FileName,
                        thumbs[index].FileSize, thumbs[index].FileDate);
                    GalleryNavigation.SetSelected(index, index == Navigation.FolderIndex);
                }, DispatcherPriority.Background, cancellationTokenSource.Token);
                if (initialDirectory != Path.GetDirectoryName(Navigation.Pics[0]))
                {
                    // Directory changed, cancel the operation
                    await cancellationTokenSource.CancelAsync();
                    return;
                }
            }
            await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(
                GalleryNavigation.ScrollToGalleryCenter, 
                DispatcherPriority.Render,
                cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            Clear();
        }
        catch (Exception ex)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(SortGalleryAsync)} exception: \n{ex}");
#endif
            Clear();
            await LoadAsync().ConfigureAwait(false);
        }
    }

    internal static void Clear()
    {
        GetPicGallery?.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
        {
            if (GetPicGallery.Container.Children.Count <= 0)
            {
                return;
            }
            GetPicGallery.Container.Children.Clear();

#if DEBUG
            Trace.WriteLine("Cleared Gallery children");
#endif
        }));
    }
}