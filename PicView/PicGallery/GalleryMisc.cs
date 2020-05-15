using PicView.UserControls;
using System;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static PicView.Fields;


namespace PicView
{
    internal static class GalleryMisc
    {
        internal static bool IsLoading;

        internal static volatile bool IsOpen;

        internal static async void Add(BitmapSource pic, int index)
        {
            await mainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                var selected = index == FolderIndex;
                var item = new PicGalleryItem(pic, index, selected);
                item.MouseLeftButtonUp += (s, x) =>
                {
                    var child = picGallery.Container.Children[FolderIndex] as PicGalleryItem;
                    child.SetSelected(false);
                    GalleryClick.Click(index);
                    item.SetSelected(true);
                };
                picGallery.Container.Children.Add(item);
            }));
        }

        internal static void Clear()
        {
            IsLoading = IsOpen = false;
            picGallery.Container.Children.Clear();
        }

    }
}
