using PicView.ImageHandling;
using PicView.UILogic;
using PicView.UILogic.Sizing;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static PicView.PicGallery.GalleryFunctions;
using static PicView.UILogic.HideInterfaceLogic;

namespace PicView.PicGallery
{
    internal static class GalleryLoad
    {
        internal static void PicGallery_Loaded(object sender, RoutedEventArgs e)
        {
            // Add events and set fields, when it's loaded.
            UC.GetPicGallery.Scroller.PreviewMouseWheel += GalleryNavigation.ScrollTo;
            UC.GetPicGallery.Scroller.ScrollChanged += (s, x) => ConfigureWindows.GetMainWindow.Focus(); // Maintain window focus when scrolling manually
            UC.GetPicGallery.grid.MouseLeftButtonDown += (s, x) => ConfigureWindows.GetMainWindow.Focus();
            UC.GetPicGallery.x2.MouseLeftButtonDown += delegate { GalleryToggle.CloseHorizontalGallery(); };

            SetSize();
        }

        internal static void SetSize()
        {
            if (WindowSizing.MonitorInfo.Width > 2100)
            {
                GalleryNavigation.PicGalleryItem_Size = 150;
            }
            else if (WindowSizing.MonitorInfo.Width > 1700)
            {
                GalleryNavigation.PicGalleryItem_Size = 130;
            }
            else if (WindowSizing.MonitorInfo.Width > 1200)
            {
                GalleryNavigation.PicGalleryItem_Size = 100;
            }
            else
            {
                GalleryNavigation.PicGalleryItem_Size = 80;
            }

            GalleryNavigation.PicGalleryItem_Size_s = GalleryNavigation.PicGalleryItem_Size - 30;
        }

        internal static void LoadLayout(bool fullscreen)
        {
            if (UC.GetPicGallery == null)
            {
                UC.GetPicGallery = new Views.UserControls.PicGallery
                {
                    Opacity = 0
                };

                ConfigureWindows.GetMainWindow.ParentContainer.Children.Add(UC.GetPicGallery);
                Panel.SetZIndex(UC.GetPicGallery, 999);
            }

            if (GalleryNavigation.PicGalleryItem_Size == 0)
            {
                SetSize();
            }

            if (fullscreen)
            {
                if (Properties.Settings.Default.FullscreenGalleryHorizontal)
                {
                    UC.GetPicGallery.Width = WindowSizing.MonitorInfo.WorkArea.Width;
                    UC.GetPicGallery.Height = (GalleryNavigation.PicGalleryItem_Size + 25) * WindowSizing.MonitorInfo.DpiScaling;

                    UC.GetPicGallery.HorizontalAlignment = HorizontalAlignment.Center;
                    UC.GetPicGallery.VerticalAlignment = VerticalAlignment.Bottom;
                    UC.GetPicGallery.Scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                    UC.GetPicGallery.Scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;

                    UC.GetPicGallery.Container.Orientation = Orientation.Horizontal;
                    UC.GetPicGallery.Margin = new Thickness(0, 0, 0, 10);
                    UC.GetPicGallery.border.BorderThickness = new Thickness(0, 0, 0, 0);
                    UC.GetPicGallery.border.Background = new SolidColorBrush(Colors.Transparent);

                    UC.GetPicGallery.Scroller.CanContentScroll = true;

                    GalleryFunctions.IsHorizontalOpen = false;
                    GalleryFunctions.IsHorizontalFullscreenOpen = true;
                    GalleryFunctions.IsVerticalFullscreenOpen = false;
                }
                else
                {
                    UC.GetPicGallery.Width = (GalleryNavigation.PicGalleryItem_Size + 25) * WindowSizing.MonitorInfo.DpiScaling;
                    UC.GetPicGallery.Height = WindowSizing.MonitorInfo.WorkArea.Height;

                    UC.GetPicGallery.HorizontalAlignment = HorizontalAlignment.Right;
                    UC.GetPicGallery.Scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                    UC.GetPicGallery.Scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;

                    UC.GetPicGallery.Container.Orientation = Orientation.Vertical;

                    GalleryFunctions.IsHorizontalOpen = false;
                    GalleryFunctions.IsHorizontalFullscreenOpen = false;
                    GalleryFunctions.IsVerticalFullscreenOpen = true;
                }

                ConfigureWindows.GetMainWindow.SizeToContent = SizeToContent.WidthAndHeight;
                ConfigureWindows.GetMainWindow.ResizeMode = ResizeMode.CanMinimize;


                UC.GetPicGallery.x2.Visibility = Visibility.Collapsed;
                UC.GetPicGallery.Container.Margin = new Thickness(0, 0, 0, 0);

                ShowNavigation(false);
                ShowTopandBottom(false);
                ConfigureSettings.ConfigColors.UpdateColor(true);
                ConfigureWindows.GetMainWindow.Focus();
            }
            else
            {
                if (Properties.Settings.Default.Fullscreen)
                {
                    UC.GetPicGallery.Width = WindowSizing.MonitorInfo.Width;
                    UC.GetPicGallery.Height = WindowSizing.MonitorInfo.Height;
                }
                else
                {
                    UC.GetPicGallery.Width = ConfigureWindows.GetMainWindow.ParentContainer.ActualWidth;
                    UC.GetPicGallery.Height = ConfigureWindows.GetMainWindow.ParentContainer.ActualHeight;
                }

                UC.GetPicGallery.HorizontalAlignment = HorizontalAlignment.Stretch;
                UC.GetPicGallery.Scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
                UC.GetPicGallery.Scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                UC.GetPicGallery.x2.Visibility = Visibility.Visible;
                UC.GetPicGallery.Container.Margin = new Thickness(0, 65 * WindowSizing.MonitorInfo.DpiScaling, 0, 0);

                UC.GetPicGallery.Container.Orientation = Orientation.Vertical;

                GalleryFunctions.IsHorizontalOpen = true;
                GalleryFunctions.IsHorizontalFullscreenOpen = false;
                GalleryFunctions.IsVerticalFullscreenOpen = false;
            }

            UC.GetPicGallery.Opacity = 1;
        }

        internal static Task Load() => Task.Run(async () =>
        {
            /// TODO Maybe make this start at at folder index
            /// and get it work with a real sorting method?

            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, (Action)(() =>
            {
                if (UC.GetPicGallery == null)
                {
                    return;
                }

                else if (UC.GetPicGallery.Container.Children.Count > 0)
                {
                    return;
                }
            }));


            for (int i = 0; i < ChangeImage.Navigation.Pics.Count; i++)
            {
                var pic = Thumbnails.GetBitmapSourceThumb(ChangeImage.Navigation.Pics[i]);

                if (pic == null)
                {
                    pic = ImageFunctions.ImageErrorMessage();
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