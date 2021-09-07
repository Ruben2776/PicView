using PicView.UILogic;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static PicView.UILogic.UC;

namespace PicView.PicGallery
{
    internal static class GalleryFunctions
    {
        internal static bool IsOpen;

        internal static async Task Add(BitmapSource pic, int id)
        {
            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                var selected = id == ChangeImage.Navigation.FolderIndex;
                var item = new Views.UserControls.PicGalleryItem(pic, id, selected);
                item.MouseLeftButtonDown += async delegate
                {
                    await GalleryClick.ClickAsync(id).ConfigureAwait(false);
                };
                GetPicGallery.Container.Children.Add(item);
            }));
        }

        internal static void Clear()
        {
            if (GetPicGallery == null)
            {
                return;
            }

            GetPicGallery.Container.Children.Clear();

#if DEBUG
            Trace.WriteLine("Cleared Gallery children");
#endif
        }
    }
}