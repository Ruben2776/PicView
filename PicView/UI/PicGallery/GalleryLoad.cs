using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static PicView.Library.Fields;
using static PicView.UI.PicGallery.GalleryFunctions;
using static PicView.UI.PicGallery.GalleryScroll;
using static PicView.UI.HideInterfaceLogic;
using static PicView.UI.UserControls.UC;
using PicView.Library;

namespace PicView.UI.PicGallery
{
    internal static class GalleryLoad
    {
        internal static void PicGallery_Loaded(object sender, RoutedEventArgs e)
        {
            // Add events and set fields, when it's loaded.
            picGallery.Scroller.PreviewMouseWheel += ScrollTo;
            picGallery.Scroller.ScrollChanged += (s, x) => mainWindow.Focus(); // Maintain window focus when scrolling manually
            picGallery.grid.MouseLeftButtonDown += (s, x) => mainWindow.Focus();
            picGallery.x2.MouseLeftButtonUp += delegate { GalleryToggle.CloseContainedGallery(); };
        }

        internal static void LoadLayout()
        {
            // TODO Make this code more clean and explain what's going on?
            if (Properties.Settings.Default.PicGallery == 1)
            {
                if (Properties.Settings.Default.ShowInterface)
                {
                    picGallery.Width = mainWindow.Width - 15;
                    picGallery.Height = mainWindow.ActualHeight - 70;
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

                ShowNavigation(false);
                ShowTopandBottom(false);
                ConfigColors.UpdateColor(true);

                mainWindow.Margin = new Thickness(0, 0, -picGallery.Width, 0);
            }

            picGallery.Visibility = Visibility.Visible;
            picGallery.Opacity = 1;
            picGallery.Container.Orientation = Orientation.Vertical;

            IsOpen = true;
        }

        internal static Task Load()
        {
            IsLoading = true;
            return Task.Run(() =>
            {
                /// TODO Maybe make this start at at folder index
                /// and get it work with a real sorting method?

                for (int i = 0; i < Pics.Count; i++)
                {
                    var pic = ImageHandling.Thumbnails.GetBitmapSourceThumb(Pics[i]);
                    if (pic != null)
                    {
                        if (!pic.IsFrozen)
                        {
                            pic.Freeze();
                        }

                        Add(pic, i);
                    }
                    // TODO find a placeholder for null images?
                }
                IsLoading = false;
            });
        }
    }
}