using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static PicView.Fields;
using static PicView.HideInterfaceLogic;
using static PicView.PicGalleryLogic;
using static PicView.PicGalleryScroll;
using static PicView.Thumbnails;



namespace PicView
{
    internal static class PicGalleryLoad
    {
        internal static void PicGallery_Loaded(object sender, RoutedEventArgs e)
        {
            picGallery.Scroller.PreviewMouseWheel += ScrollTo;
            picGallery.Scroller.MouseDown += (s, x) => mainWindow.Focus();
            picGallery.grid.MouseLeftButtonDown += (s, x) => mainWindow.Focus();

             IsLoading = IsOpen = false;
        }

        internal static void LoadLayout()
        {
            if (Properties.Settings.Default.PicGallery == 1)
            {
                if (Properties.Settings.Default.ShowInterface)
                {
                    picGallery.Width = mainWindow.Width - 15;
                    picGallery.Height = mainWindow.ActualHeight - 78;
                }
                else
                {
                    picGallery.Width = mainWindow.Width - 2;
                    picGallery.Height = mainWindow.Height - 2; // 2px for borders
                }

                picGallery.HorizontalAlignment = HorizontalAlignment.Stretch;
                picGallery.Scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
                picGallery.Scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                picGallery.x2.Visibility = Visibility.Visible;
                picGallery.Container.Margin = new Thickness(0, 65, 0, 0);
            }
            else
            {
                picGallery.Width = picGalleryItem_Size + 19; // 17 for scrollbar width + 2 for borders
                picGallery.Height = MonitorInfo.Height;
                picGallery.HorizontalAlignment = HorizontalAlignment.Right;
                picGallery.Scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                picGallery.Scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
                picGallery.x2.Visibility = Visibility.Collapsed;
                picGallery.Container.Margin = new Thickness(0, 0, 0, 0);
                ShowNavigation(true);
                ShowTopandBottom(false);
            }

            picGallery.Visibility = Visibility.Visible;
            picGallery.Opacity = 1;
            picGallery.Container.Orientation = Orientation.Vertical;
        }

        internal static Task Load()
        {
            IsLoading = true;
            return Task.Run(() =>
            {
                for (int i = 0; i < Pics.Count; i++)
                {
                    var pic = GetBitmapSourceThumb(Pics[i]);
                    if (pic != null)
                    { 
                        pic.Freeze();
                        Add(pic, i);
                    }
                }
                IsLoading = false;
            });
        }

    }
}
