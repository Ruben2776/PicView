using PicView.UILogic;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using static PicView.ChangeImage.Navigation;
using static PicView.ImageHandling.Thumbnails;
using static PicView.PicGallery.GalleryFunctions;
using static PicView.UILogic.Sizing.ScaleImage;
using static PicView.UILogic.UC;

namespace PicView.PicGallery
{
    /// <summary>
    /// Logick for what happens when user clicks on
    /// thumbnail gallery item
    /// </summary>
    internal static class GalleryClick
    {
        internal static async Task ClickAsync(int id)
        {
            ConfigureWindows.GetMainWindow.Focus();

            if (GalleryNavigation.ShouldHorizontalNavigate() == false)
            {
                await ItemClickAsync(id).ConfigureAwait(false);
                return;
            }

            ConfigureWindows.GetMainWindow.MainImage.Visibility = Visibility.Hidden;

            var z = GetPicGallery.Container.Children[id] as Views.UserControls.PicGalleryItem;
            ConfigureWindows.GetMainWindow.MainImage.Source = z.img.Source;
            Border? border = null;
            Image? image = null;
            var size = await ImageHandling.ImageFunctions.ImageSizeAsync(Pics[id]).ConfigureAwait(true);
            if (size.HasValue)
            {
                GalleryFunctions.IsOpen = false;
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Send, () =>
                {
                    FitImage(size.Value.Width, size.Value.Height);
                });
            }

            var from = GalleryNavigation.PicGalleryItem_Size;
            var to = new double[] { XWidth, XHeight };
            var acceleration = 0.2;
            var deceleration = 0.4;
            var duration = TimeSpan.FromSeconds(.3);

            var da = new DoubleAnimation
            {
                From = 0,
                To = to[0],
                Duration = duration,
                AccelerationRatio = acceleration,
                DecelerationRatio = deceleration,
                FillBehavior = FillBehavior.Stop
            };

            var da0 = new DoubleAnimation
            {
                From = 0,
                To = to[1],
                Duration = duration,
                AccelerationRatio = acceleration,
                DecelerationRatio = deceleration,
                FillBehavior = FillBehavior.Stop
            };

            da.Completed += async delegate
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, () =>
                {
                    border.Opacity = 0;
                    GetPicGallery.grid.Children.Remove(border);
                    image = null;
                    IsOpen = false;
                    GetPicGallery.Visibility = Visibility.Collapsed; // prevent it from popping up again
                    ConfigureWindows.GetMainWindow.MainImage.Visibility = Visibility.Visible;
                });
                await ItemClickAsync(id, false).ConfigureAwait(false);
            };

            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, () =>
            {
                if (Properties.Settings.Default.AutoFitWindow)
                {
                    GetPicGallery.Width = XWidth;
                    GetPicGallery.Height = XHeight;
                }
                else
                {
                    var da3 = new DoubleAnimation
                    {
                        From = 1,
                        To = 0,
                        Duration = duration,
                        AccelerationRatio = acceleration,
                        DecelerationRatio = deceleration,
                        FillBehavior = FillBehavior.Stop
                    };
                    GetPicGallery.Container.BeginAnimation(FrameworkElement.OpacityProperty, da3);
                }

                GetPicGallery.x2.Visibility = Visibility.Hidden;

                image = new Image()
                {
                    Source = GetThumb(id),
                    Stretch = Stretch.Fill,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                // Need to add border for background to pictures with transparent background
                border = new Border()
                {
                    Background = ConfigureSettings.ConfigColors.BackgroundColorBrush
                };
                border.Child = image;
                GetPicGallery.grid.Children.Add(border);

                border.BeginAnimation(FrameworkElement.WidthProperty, da);
                border.BeginAnimation(FrameworkElement.HeightProperty, da0);
            });
        }

        internal static async Task ItemClickAsync(int id, bool resize = true)
        {
            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, () =>
            {
                // Deselect current item
                GalleryNavigation.SetSelected(GalleryNavigation.SelectedGalleryItem, false);
                GalleryNavigation.SetSelected(FolderIndex, false);

                if (Properties.Settings.Default.FullscreenGalleryHorizontal == false && Properties.Settings.Default.FullscreenGalleryVertical == false)
                {
                    // Restore interface elements if needed
                    if (!Properties.Settings.Default.ShowInterface || Properties.Settings.Default.Fullscreen)
                    {
                        HideInterfaceLogic.ShowNavigation(true);
                        HideInterfaceLogic.ShowShortcuts(true);
                    }
                }

                // Select next item
                GalleryNavigation.SetSelected(id, true);
                GalleryNavigation.SelectedGalleryItem = id;
            });
            // Change image
            await LoadPicAtIndexAsync(id, resize).ConfigureAwait(false);
        }
    }
}