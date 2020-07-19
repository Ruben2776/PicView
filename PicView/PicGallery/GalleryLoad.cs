using PicView.UILogic.Sizing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static PicView.Library.Fields;
using static PicView.UILogic.HideInterfaceLogic;
using static PicView.UILogic.PicGallery.GalleryFunctions;
using static PicView.UILogic.PicGallery.GalleryScroll;
using static PicView.UILogic.UserControls.UC;

namespace PicView.UILogic.PicGallery
{
    internal static class GalleryLoad
    {
        internal static void PicGallery_Loaded(object sender, RoutedEventArgs e)
        {
            // Add events and set fields, when it's loaded.
            GetPicGallery.Scroller.PreviewMouseWheel += ScrollTo;
            GetPicGallery.Scroller.ScrollChanged += (s, x) => TheMainWindow.Focus(); // Maintain window focus when scrolling manually
            GetPicGallery.grid.MouseLeftButtonDown += (s, x) => TheMainWindow.Focus();
            GetPicGallery.x2.MouseLeftButtonDown += delegate { GalleryToggle.CloseContainedGallery(); };

            SetSize();
        }

        internal static void SetSize()
        {
            if (MonitorInfo.Width > 2100)
            {
                picGalleryItem_Size = 260;
            }
            else if (MonitorInfo.Width > 1700)
            {
                picGalleryItem_Size = 210;
            }
            else if (MonitorInfo.Width > 1200)
            {
                picGalleryItem_Size = 150;
            }
            else
            {
                picGalleryItem_Size = 100;
            }

            picGalleryItem_Size_s = picGalleryItem_Size - 30;
        }

        internal static void LoadLayout()
        {
            if (GetPicGallery == null)
            {
                GetPicGallery = new UserControls.PicGallery
                {
                    Opacity = 0,
                    Visibility = Visibility.Collapsed
                };

                TheMainWindow.ParentContainer.Children.Add(GetPicGallery);
                Panel.SetZIndex(GetPicGallery, 999);
            }

            if (picGalleryItem_Size == 0)
            {
                SetSize();
            }

            if (Properties.Settings.Default.PicGallery == 1)
            {
                if (Properties.Settings.Default.Fullscreen)
                {
                    GetPicGallery.Width = MonitorInfo.Width;
                    GetPicGallery.Height = MonitorInfo.Height;
                }
                else if (Properties.Settings.Default.ShowInterface)
                {
                    GetPicGallery.Width = TheMainWindow.Width - 15;
                    GetPicGallery.Height = TheMainWindow.ActualHeight - 70;
                }
                else
                {
                    GetPicGallery.Width = TheMainWindow.ActualWidth - 2;
                    GetPicGallery.Height = TheMainWindow.ActualHeight - 2; // 2px for borders
                }

                GetPicGallery.HorizontalAlignment = HorizontalAlignment.Stretch;
                GetPicGallery.Scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
                GetPicGallery.Scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                GetPicGallery.x2.Visibility = Visibility.Visible;
                GetPicGallery.Container.Margin = new Thickness(0, 65, 0, 0);
            }
            else
            {
                GetPicGallery.Width = picGalleryItem_Size + 14; // 17 for scrollbar width + 2 for borders
                GetPicGallery.Height = MonitorInfo.WorkArea.Height;

                TheMainWindow.SizeToContent = SizeToContent.WidthAndHeight;
                TheMainWindow.ResizeMode = ResizeMode.CanMinimize;

                WindowLogic.CenterWindowOnScreen();

                GetPicGallery.HorizontalAlignment = HorizontalAlignment.Right;
                GetPicGallery.Scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                GetPicGallery.Scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
                GetPicGallery.x2.Visibility = Visibility.Collapsed;
                GetPicGallery.Container.Margin = new Thickness(0, 0, 0, 0);

                ShowNavigation(false);
                ShowTopandBottom(false);
                ConfigColors.UpdateColor(true);
            }


            GetPicGallery.Visibility = Visibility.Visible;
            GetPicGallery.Opacity = 1;
            GetPicGallery.Container.Orientation = Orientation.Vertical;

            IsOpen = true;
        }

        internal static Task Load()
        {
            IsLoading = true;
            return Task.Run(async () =>
            {
                /// TODO Maybe make this start at at folder index
                /// and get it work with a real sorting method?

                for (int i = 0; i < ChangeImage.Navigation.Pics.Count; i++)
                {
                    var pic = ImageHandling.Thumbnails.GetBitmapSourceThumb(ChangeImage.Navigation.Pics[i]);
                    if (pic != null)
                    {
                        if (!pic.IsFrozen)
                        {
                            pic.Freeze();
                        }

                        await Add(pic, i).ConfigureAwait(false);
                    }
                    // TODO find a placeholder for null images?
                }
                IsLoading = false;
            });
        }
    }
}