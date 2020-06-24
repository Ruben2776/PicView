using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static PicView.Library.Fields;
using static PicView.UI.UserControls.UC;

namespace PicView.UI.PicGallery
{
    internal static class GalleryFunctions
    {
        internal static bool IsLoading { get; set; }

        private static bool Open;

        internal static bool IsOpen
        {
            get { return Open; }
            set
            {
                Open = value;
#if DEBUG
                Trace.WriteLine("IsOpen changed value to: " + IsOpen);
#endif
            }
        }

        internal static async Task Add(BitmapSource pic, int id)
        {
            await TheMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                var selected = id == FolderIndex;
                var item = new UserControls.PicGalleryItem(pic, id, selected);
                item.MouseLeftButtonDown += delegate
                {
                    GalleryClick.Click(id);
                };
                picGallery.Container.Children.Add(item);
            }));
        }

        internal static void Clear()
        {
            if (picGallery == null)
            {
                return;
            }

            IsLoading = false;
            picGallery.Container.Children.Clear();

#if DEBUG
            Trace.WriteLine("Cleared Gallery children");
#endif
        }

        internal static void SetSelected(int x)
        {
            if (x > picGallery.Container.Children.Count) { return; }

            // Select next item
            var nextItem = picGallery.Container.Children[x] as UserControls.PicGalleryItem;
            nextItem.innerborder.BorderBrush = Application.Current.Resources["ChosenColorBrush"] as SolidColorBrush;
            nextItem.innerborder.Width = nextItem.innerborder.Height = picGalleryItem_Size;
        }

        internal static void SetUnselected(int x)
        {
            if (x > picGallery.Container.Children.Count) { return; }

            // Deselect current item
            var prevItem = picGallery.Container.Children[x] as UserControls.PicGalleryItem;
            prevItem.innerborder.BorderBrush = Application.Current.Resources["BorderBrush"] as SolidColorBrush;
            prevItem.innerborder.Width = prevItem.innerborder.Height = picGalleryItem_Size_s;
        }
    }
}