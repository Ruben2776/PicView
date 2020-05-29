using PicView.UserControls;
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static PicView.Fields;


namespace PicView
{
    internal static class GalleryMisc
    {
        internal static bool IsLoading;

        internal static bool IsOpen;

        internal static async void Add(BitmapSource pic, int id)
        {
            await mainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                var selected = id == FolderIndex;
                var item = new PicGalleryItem(pic, id, selected);
                item.MouseLeftButtonUp += (s, x) =>
                {
                    GalleryClick.Click(id);
                };
                picGallery.Container.Children.Add(item);
            }));
        }

        internal static void Clear()
        {
            IsLoading = IsOpen = false;
            picGallery.Container.Children.Clear();
        }

        internal static void SetSelected(int x)
        {
            // Select next item
            var nextItem = picGallery.Container.Children[x] as PicGalleryItem;
            nextItem.innerborder.BorderBrush = Application.Current.Resources["ChosenColorBrush"] as SolidColorBrush;
            nextItem.innerborder.Width = nextItem.innerborder.Height = picGalleryItem_Size;
        }

        internal static void SetUnselected(int x)
        {
            // Deselect current item
            var prevItem = picGallery.Container.Children[x] as PicGalleryItem;
            prevItem.innerborder.BorderBrush = Application.Current.Resources["BorderBrush"] as SolidColorBrush;
            prevItem.innerborder.Width = prevItem.innerborder.Height = picGalleryItem_Size_s;
        }

    }
}
