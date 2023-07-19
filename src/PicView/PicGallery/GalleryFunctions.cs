using PicView.ChangeImage;
using PicView.FileHandling;
using PicView.Properties;
using PicView.UILogic;
using PicView.Views.UserControls.Gallery;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using PicView.ChangeTitlebar;
using static PicView.PicGallery.GalleryLoad;
using static PicView.UILogic.UC;

namespace PicView.PicGallery;

internal static class GalleryFunctions
{
    internal static bool IsGalleryOpen { get; set; }

    internal static void ReCalculateItemSizes()
    {
        if (GetPicGallery is null) { return; }
        if (GetPicGallery.Container.Children.Count <= 0) { return; }
        var tempItem = (PicGalleryItem)GetPicGallery.Container.Children[0];

        if (Math.Abs(tempItem.OuterBorder.Height - GalleryNavigation.PicGalleryItemSize) < 1) { return; }

        for (int i = 0; i < GetPicGallery.Container.Children.Count; i++)
        {
            var item = (PicGalleryItem)GetPicGallery.Container.Children[i];
            item.InnerBorder.Height = item.InnerBorder.Width = Settings.Default.IsBottomGalleryShown ? GalleryNavigation.PicGalleryItemSize : GalleryNavigation.PicGalleryItemSizeS;
            item.OuterBorder.Height = item.OuterBorder.Width = GalleryNavigation.PicGalleryItemSize;
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

    internal static async Task SortGallery(FileInfo? fileInfo = null)
    {
        await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(SetTitle.SetLoadingString);
        fileInfo ??= new FileInfo(Navigation.Pics[0]);

        var thumbs = new List<GalleryThumbHolder>();

        await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
        {
            for (int i = 0; i < Navigation.Pics.Count; i++)
            {
                try
                {
                    var picGalleryItem = GetPicGallery.Container.Children[i] as PicGalleryItem;
                    thumbs.Add(new GalleryThumbHolder(picGalleryItem.ThumbFileLocation.Text, picGalleryItem.ThumbFileName.Text, picGalleryItem.ThumbFileSize.Text, picGalleryItem.ThumbFileDate.Text, picGalleryItem.ThumbImage?.Source as BitmapSource));
                }
                catch (Exception)
                {
                    thumbs = null;
                    Clear();
                    _ = LoadAsync().ConfigureAwait(false);
                    return;
                }
            }
        }, DispatcherPriority.Render);

        Clear();
        Navigation.Pics.Clear(); // Cancel task if running
        Navigation.Pics = await Task.FromResult(FileLists.FileList(fileInfo)).ConfigureAwait(false);

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

        for (int i = 0; i < Navigation.Pics.Count; i++)
        {
            var i1 = i;
            await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
            {
                Add(i1);
                UpdatePic(i1, thumbs[i1].BitmapSource, thumbs[i1].FileLocation, thumbs[i1].FileName, thumbs[i1].FileSize, thumbs[i1].FileDate);
            }, DispatcherPriority.Background);
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