using PicView.ImageHandling;
using PicView.UILogic.Loading;
using PicView.UILogic.Sizing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static PicView.UILogic.HideInterfaceLogic;
using static PicView.UILogic.PicGallery.GalleryFunctions;
using static PicView.UILogic.PicGallery.GalleryScroll;

namespace PicView.UILogic.PicGallery
{
    internal static class GalleryLoad
    {
        internal static void PicGallery_Loaded(object sender, RoutedEventArgs e)
        {
            // Add events and set fields, when it's loaded.
            UC.GetPicGallery.Scroller.PreviewMouseWheel += ScrollTo;
            UC.GetPicGallery.Scroller.ScrollChanged += (s, x) => LoadWindows.GetMainWindow.Focus(); // Maintain window focus when scrolling manually
            UC.GetPicGallery.grid.MouseLeftButtonDown += (s, x) => LoadWindows.GetMainWindow.Focus();
            UC.GetPicGallery.x2.MouseLeftButtonDown += delegate { GalleryToggle.CloseContainedGallery(); };

            SetSize();
        }

        internal static void SetSize()
        {
            if (WindowLogic.MonitorInfo.Width > 2100)
            {
                picGalleryItem_Size = 260;
            }
            else if (WindowLogic.MonitorInfo.Width > 1700)
            {
                picGalleryItem_Size = 210;
            }
            else if (WindowLogic.MonitorInfo.Width > 1200)
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
            if (UC.GetPicGallery == null)
            {
                UC.GetPicGallery = new Views.UserControls.PicGallery
                {
                    Opacity = 0,
                    Visibility = Visibility.Collapsed
                };

                LoadWindows.GetMainWindow.ParentContainer.Children.Add(UC.GetPicGallery);
                Panel.SetZIndex(UC.GetPicGallery, 999);
            }

            if (picGalleryItem_Size == 0)
            {
                SetSize();
            }

            if (Properties.Settings.Default.PicGallery == 1)
            {
                if (Properties.Settings.Default.Fullscreen)
                {
                    UC.GetPicGallery.Width = WindowLogic.MonitorInfo.Width;
                    UC.GetPicGallery.Height = WindowLogic.MonitorInfo.Height;
                }
                else
                {
                    UC.GetPicGallery.Width = LoadWindows.GetMainWindow.ParentContainer.ActualWidth;
                    UC.GetPicGallery.Height = LoadWindows.GetMainWindow.ParentContainer.ActualHeight;
                }

                UC.GetPicGallery.HorizontalAlignment = HorizontalAlignment.Stretch;
                UC.GetPicGallery.Scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
                UC.GetPicGallery.Scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                UC.GetPicGallery.x2.Visibility = Visibility.Visible;
                UC.GetPicGallery.Container.Margin = new Thickness(0, 65, 0, 0);
            }
            else
            {
                UC.GetPicGallery.Width = (picGalleryItem_Size + 25) * WindowLogic.MonitorInfo.DpiScaling;
                UC.GetPicGallery.Height = WindowLogic.MonitorInfo.WorkArea.Height;

                LoadWindows.GetMainWindow.SizeToContent = SizeToContent.WidthAndHeight;
                LoadWindows.GetMainWindow.ResizeMode = ResizeMode.CanMinimize;

                WindowLogic.CenterWindowOnScreen();

                UC.GetPicGallery.HorizontalAlignment = HorizontalAlignment.Right;
                UC.GetPicGallery.Scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                UC.GetPicGallery.Scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
                UC.GetPicGallery.x2.Visibility = Visibility.Collapsed;
                UC.GetPicGallery.Container.Margin = new Thickness(0, 0, 0, 0);

                ShowNavigation(false);
                ShowTopandBottom(false);
                ConfigureSettings.ConfigColors.UpdateColor(true);
            }

            UC.GetPicGallery.Visibility = Visibility.Visible;
            UC.GetPicGallery.Opacity = 1;
            UC.GetPicGallery.Container.Orientation = Orientation.Vertical;

            IsOpen = true;
        }

        internal static Task Load() => Task.Run(async () =>
        {
            /// TODO Maybe make this start at at folder index
            /// and get it work with a real sorting method?

            for (int i = 0; i < ChangeImage.Navigation.Pics.Count; i++)
            {
                var pic = Thumbnails.GetBitmapSourceThumb(ChangeImage.Navigation.Pics[i]);

                if (pic == null)
                {
                    pic = ImageDecoder.ImageErrorMessage();
                    
                }
                else if (!pic.IsFrozen)
                {
                    pic.Freeze();
                }

                await Add(pic, i).ConfigureAwait(false);
            }
        });
    }
}