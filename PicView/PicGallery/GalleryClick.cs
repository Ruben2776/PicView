using PicView.PreLoading;
using PicView.UserControls;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using static PicView.Fields;
using static PicView.GalleryFunctions;
using static PicView.ImageDecoder;
using static PicView.Navigation;
using static PicView.ScaleImage;
using static PicView.Thumbnails;
using static PicView.UC;


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
                    Source = GetThumb(id),
                    Stretch = Stretch.Fill,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                // Need to add border for background to pictures with transparent background
                var border = new Border()
                {
                    Background = Utilities.GetBackgroundColorBrush()
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
                    img = null;
                };

                border.BeginAnimation(FrameworkElement.WidthProperty, da);
                border.BeginAnimation(FrameworkElement.HeightProperty, da0);
            }
            else
            {
                Preloader.PreloaderFix(Pics[id]);
                ItemClick(id);
            }
        }

        internal static void PreviewItemClick(ImageSource source, int id)
        {
            if (!Preloader.Contains(Pics[id]))
            {
                Preloader.PreloaderFix(Pics[id]);
            }

            mainWindow.img.Source = source;
            var size = ImageSize(Pics[id]);
            if (size.HasValue)
            {
                FitImage(size.Value.Width, size.Value.Height);
            }
        }

        internal static void ItemClick(int id)
        {
            // Deselect current item
            SetUnselected(FolderIndex);

            // Change image
            Pic(id);

            if (Properties.Settings.Default.PicGallery == 1)
            {
                picGallery.Visibility = Visibility.Collapsed; // prevent it from popping up again

                // Restore interface elements if needed
                if (!Properties.Settings.Default.ShowInterface)
                {
                    clickArrowLeft.Visibility =
                    clickArrowRight.Visibility =
                    x2.Visibility =
                    minus.Visibility =
                    galleryShortcut.Visibility = Visibility.Visible;
                }
            }

            // Select next item
            SetSelected(id);
        }

    }
}
