using PicView.ChangeImage;
using PicView.FileHandling;
using PicView.Properties;
using PicView.UILogic;
using PicView.Views.UserControls.Gallery;
using System.Diagnostics;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
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

    private class TempPics
    {
        internal readonly BitmapSource pic;
        public readonly string name;

        public TempPics(BitmapSource pic, string name)
        {
            this.pic = pic;
            this.name = name;
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
        fileInfo ??= new FileInfo(Navigation.Pics[0]);

        var thumbs = new List<TempPics>();

        await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
        {
            for (int i = 0; i < Navigation.Pics.Count; i++)
            {
                try
                {
                    var picGalleryItem = GetPicGallery.Container.Children[i] as PicGalleryItem;
                    thumbs.Add(new TempPics(picGalleryItem?.ThumbImage?.Source as BitmapSource, Navigation.Pics[i]));
                }
                catch (Exception)
                {
                    //
                }
            }

            Clear();
        }));

        Navigation.Pics.Clear(); // Cancel task if running
        Navigation.Pics = await Task.FromResult(FileLists.FileList(fileInfo)).ConfigureAwait(false);

        try
        {
            thumbs = thumbs.OrderBySequence(Navigation.Pics, pic => pic.name).ToList();
        }
        catch (Exception)
        {
            thumbs = null;
            Clear();
            await GalleryLoad.LoadAsync().ConfigureAwait(false);
            return;
        }

        for (int i = 0; i < Navigation.Pics.Count; i++)
        {
            GalleryLoad.Add(i, Navigation.FolderIndex);
        }

        await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(GalleryNavigation.ScrollToGalleryCenter);

        for (int i = 0; i < Navigation.Pics.Count; i++)
        {
            await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
            {
                GalleryLoad.UpdatePic(i, thumbs[i].pic, new FileInfo(Navigation.Pics[i]));
            }, DispatcherPriority.Render);
        }

        thumbs = null;
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