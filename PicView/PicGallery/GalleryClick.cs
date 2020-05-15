using PicView.PreLoading;
using PicView.UserControls;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using static PicView.Fields;
using static PicView.GalleryMisc;
using static PicView.ImageDecoder;
using static PicView.Navigation;
using static PicView.Thumbnails;


namespace PicView
{
    internal static class GalleryClick
    {

        internal static void Click(int id)
        {
            mainWindow.Focus();


            if (Properties.Settings.Default.PicGallery == 1)
            {
                if (Preloader.Contains(Pics[id]))
                {
                    PreviewItemClick(Preloader.Load(Pics[id]), id);
                }
                else
                {
                    var z = picGallery.Container.Children[id] as PicGalleryItem;
                    PreviewItemClick(z.img.Source, id);
                }

                picGallery.Width = xWidth;
                picGallery.Height = xHeight;

                var img = new Image()
                {
                    Source = GetBitmapSourceThumb(Pics[id]),
                    Stretch = Stretch.Fill,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                // Need to add border for background to pictures with transparent background
                var border = new Border()
                {
                    Background = Properties.Settings.Default.BgColorWhite ? new SolidColorBrush(Colors.White) : Application.Current.Resources["BackgroundColorBrush"] as SolidColorBrush
                };
                border.Child = img;
                picGallery.grid.Children.Add(border);

                var from = picGalleryItem_Size;
                var to = new double[] { xWidth, xHeight };
                var acceleration = 0.2;
                var deceleration = 0.4;
                var duration = TimeSpan.FromSeconds(.3);

                var da = new DoubleAnimation
                {
                    From = from,
                    To = to[0],
                    Duration = duration,
                    AccelerationRatio = acceleration,
                    DecelerationRatio = deceleration
                };

                var da0 = new DoubleAnimation
                {
                    From = from,
                    To = to[1],
                    Duration = duration,
                    AccelerationRatio = acceleration,
                    DecelerationRatio = deceleration
                };

                da.Completed += delegate
                {
                    ItemClick(id);
                    picGallery.grid.Children.Remove(border);
                    IsOpen = false;
                };

                border.BeginAnimation(FrameworkElement.WidthProperty, da);
                border.BeginAnimation(FrameworkElement.HeightProperty, da0);
            }
            else
            {
                if (!Preloader.Contains(Pics[id]))
                {
                    PreloadCount = 4;
                    Preloader.Clear();
                    Preloader.Add(Pics[id]);
                }

                ItemClick(id);
            }

            IsOpen = false;
        }

        internal static void PreviewItemClick(ImageSource source, int id)
        {
            if (!Preloader.Contains(Pics[id]))
            {
                PreloadCount = 4;
                Preloader.Clear();
                Preloader.Add(Pics[id]);
            }

            mainWindow.img.Source = source;
            var size = ImageSize(Pics[id]);
            if (size.HasValue)
            {
                Resize_and_Zoom.ZoomFit(size.Value.Width, size.Value.Height);
            }
        }

        internal static void ItemClick(int id)
        {
            Pic(id);

            if (Properties.Settings.Default.PicGallery == 1)
            {
                picGallery.Visibility = Visibility.Collapsed;

                if (clickArrowLeft.Visibility != Visibility.Visible)
                {
                    clickArrowLeft.Visibility =
                    clickArrowRight.Visibility =
                    x2.Visibility =
                    minus.Visibility =
                    galleryShortcut.Visibility = Visibility.Visible;
                }
            }
        }

    }
}
