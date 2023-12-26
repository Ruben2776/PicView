using PicView.Core.Config;
using PicView.WPF.ChangeImage;
using PicView.WPF.FileHandling;
using PicView.WPF.UILogic;
using PicView.WPF.Views.UserControls.Gallery;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using PicView.Core.Gallery;
using static PicView.WPF.PicGallery.GalleryLoad;
using static PicView.WPF.UILogic.UC;
using PicView.WPF.ImageHandling;

namespace PicView.WPF.PicGallery;

internal static class GalleryFunctions
{
    internal static bool IsGalleryOpen { get; set; }

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

    private static IEnumerable<T> OrderBySequence<T, TId>(this IEnumerable<T> source,
        IEnumerable<TId> order, Func<T, TId> idSelector) where TId : notnull
    {
        var lookup = source?.ToDictionary(idSelector, t => t);
        foreach (var id in order)
        {
            yield return lookup[id];
        }
    }

    internal static async Task SortGalleryAsync(FileInfo? fileInfo = null)
    {
        var cancelToken = new CancellationToken();

        fileInfo ??= new FileInfo(Navigation.Pics[0]);
        Navigation.Pics = await Task.FromResult(FileLists.FileList(fileInfo)).ConfigureAwait(false);

        IsLoading = false; // Hack to cancel loading to prevent crash if it is running. Maybe find a better solution in future

        var thumbs = new List<GalleryThumbInfo.GalleryThumbHolder>();

        for (var i = 0; i < Navigation.Pics.Count; i++)
        {
            try
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
                {
                    var picGalleryItem = GetPicGallery.Container.Children[i] as PicGalleryItem;
                    var thumb = new GalleryThumbInfo.GalleryThumbHolder(picGalleryItem.ThumbFileLocation.Text,
                        picGalleryItem.ThumbFileName.Text, picGalleryItem.ThumbFileSize.Text,
                        picGalleryItem.ThumbFileDate.Text, picGalleryItem.ThumbImage?.Source as BitmapSource);
                    thumbs.Add(thumb);
                }, DispatcherPriority.Render, cancelToken);
            }
            catch (Exception)
            {
                Clear();
                await LoadAsync().ConfigureAwait(false);
                return;
            }
        }

        try
        {
            thumbs = thumbs.OrderBySequence(Navigation.Pics, x => x.FileLocation).ToList();
        }
        catch (Exception)
        {
            Clear();
            await LoadAsync().ConfigureAwait(false);
            return;
        }

        for (var i = 0; i < Navigation.Pics.Count; i++)
        {
            var i1 = i;
            await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
            {
                UpdatePic(i1, (BitmapSource?)thumbs[i1].ImageSource, thumbs[i1].FileLocation, thumbs[i1].FileName,
                    thumbs[i1].FileSize, thumbs[i1].FileDate);
            }, DispatcherPriority.Background, cancelToken);
        }
    }

    internal static void Clear()
    {
        ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
        {
            if (GetPicGallery == null)
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