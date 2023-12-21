using PicView.Core.Config;
using PicView.WPF.ChangeImage;
using PicView.WPF.ChangeTitlebar;
using PicView.WPF.ImageHandling;
using PicView.WPF.UILogic;
using PicView.WPF.Views.UserControls.Gallery;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using static PicView.WPF.ChangeImage.Navigation;
using static PicView.WPF.UILogic.Sizing.ScaleImage;
using static PicView.WPF.UILogic.UC;

namespace PicView.WPF.PicGallery
{
    /// <summary>
    /// Logic for what happens when user clicks on
    /// thumbnail gallery item
    /// </summary>
    internal static class GalleryClick
    {
        internal static async Task ClickAsync(int id)
        {
            if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown && !GalleryFunctions.IsGalleryOpen)
            {
                await ItemClickAsync(id).ConfigureAwait(false);
                return;
            }

            var imageSize = await Task.FromResult(ImageSizeFunctions.GetImageSize(Pics[id])).ConfigureAwait(false);

            var galleryCloseAnimation = new DoubleAnimation
            {
                FillBehavior = FillBehavior.Stop,
                AccelerationRatio = SettingsHelper.Settings.Gallery.IsBottomGalleryShown ? .6 : 0.5,
                DecelerationRatio = SettingsHelper.Settings.Gallery.IsBottomGalleryShown ? .4 : 0.5,
                From = GetPicGallery.ActualHeight,
                To = GalleryNavigation.PicGalleryItemSize + 22,
                Duration = SettingsHelper.Settings.Gallery.IsBottomGalleryShown ? TimeSpan.FromSeconds(.6) : TimeSpan.FromSeconds(.7)
            };

            galleryCloseAnimation.Completed += async delegate
            {
                GalleryFunctions.IsGalleryOpen = false;
                ConfigureWindows.GetMainWindow.MainImage.Visibility = Visibility.Visible;
                GalleryToggle.ShowBottomGallery();
                await Task.Delay(TimeSpan.FromSeconds(.3)); // await needed to calculate before scroll
                GetPicGallery.Scroller.CanContentScroll = false;
                GalleryNavigation.ScrollToGalleryCenter();
                GetPicGallery.Scroller.CanContentScroll = true;
            };

            await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
            {
                var galleryItem = GetPicGallery.Container.Children[id] as PicGalleryItem;
                ConfigureWindows.GetMainWindow.MainImage.Source = galleryItem.ThumbImage.Source;

                SetTitle.SetLoadingString();
                if (imageSize.HasValue)
                {
                    FitImage(imageSize.Value.Width, imageSize.Value.Height);
                }

                // Show closing animation from bottom gallery
                if (!SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
                {
                    return;
                }

                ConfigureWindows.GetMainWindow.MainImage.Width = XWidth;
                ConfigureWindows.GetMainWindow.MainImage.Height = XHeight;
                GalleryNavigation.SetSize(SettingsHelper.Settings.Gallery.BottomGalleryItemSize);
                GalleryFunctions.ReCalculateItemSizes();
                GetPicGallery.BeginAnimation(FrameworkElement.HeightProperty, galleryCloseAnimation);
            });

            if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
            {
                await ItemClickAsync(id).ConfigureAwait(false);
                return; // Only show width and height animation when bottom gallery is not shown
            }

            if (SettingsHelper.Settings.WindowProperties.AutoFit) // Fix stretching whole screen when auto fitting
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
                {
                    ConfigureWindows.GetMainWindow.SizeToContent = SizeToContent.Manual;
                });
            }

            var fromSize = GalleryNavigation.PicGalleryItemSize;
            var toSize = new[] { XWidth, XHeight };
            const double acceleration = 0.2;
            const double deceleration = 0.4;
            var duration = TimeSpan.FromSeconds(.3);

            var widthAnimation = new DoubleAnimation
            {
                From = fromSize,
                To = toSize[0],
                Duration = duration,
                AccelerationRatio = acceleration,
                DecelerationRatio = deceleration,
                FillBehavior = FillBehavior.Stop,
                AutoReverse = false
            };

            var heightAnimation = new DoubleAnimation
            {
                From = fromSize,
                To = toSize[1],
                Duration = duration,
                AccelerationRatio = acceleration,
                DecelerationRatio = deceleration,
                FillBehavior = FillBehavior.Stop,
                AutoReverse = false
            };

            widthAnimation.Completed += async delegate
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
                {
                    GetPicGallery.Visibility = Visibility.Collapsed; // prevent it from popping up again
                    GalleryFunctions.IsGalleryOpen = false;
                    ConfigureWindows.GetMainWindow.MainImage.Visibility = Visibility.Visible;
                    if (SettingsHelper.Settings.WindowProperties.AutoFit) // Revert back to auto fitting
                    {
                        ConfigureWindows.GetMainWindow.SizeToContent = SizeToContent.WidthAndHeight;
                    }
                });
                await ItemClickAsync(id).ConfigureAwait(false);
            };

            await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
            {
                var opacityAnimation = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = duration,
                    AccelerationRatio = acceleration,
                    DecelerationRatio = deceleration,
                    FillBehavior = FillBehavior.Stop
                };
                GetPicGallery.Container.BeginAnimation(UIElement.OpacityProperty, opacityAnimation);

                GetPicGallery.x2.Visibility = Visibility.Hidden;

                if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown) return;

                ConfigureWindows.GetMainWindow.MainImage.BeginAnimation(FrameworkElement.WidthProperty, widthAnimation);
                ConfigureWindows.GetMainWindow.MainImage.BeginAnimation(FrameworkElement.HeightProperty,
                    heightAnimation);
            }, DispatcherPriority.Send);
        }

        private static async Task ItemClickAsync(int id)
        {
            await GetPicGallery.Dispatcher.InvokeAsync(() =>
            {
                // Deselect current item
                GalleryNavigation.SetSelected(GalleryNavigation.SelectedGalleryItem, false);
                GalleryNavigation.SetSelected(FolderIndex, false);

                // Restore interface elements if needed
                if (!SettingsHelper.Settings.UIProperties.ShowInterface || SettingsHelper.Settings is
                    { WindowProperties.Fullscreen: true, UIProperties.ShowAltInterfaceButtons: true })
                {
                    HideInterfaceLogic.IsNavigationShown(true);
                    HideInterfaceLogic.IsShortcutsShown(true);
                }

                // Select next item
                GalleryNavigation.SetSelected(id, true);
                GalleryNavigation.SelectedGalleryItem = id;
            });
            if (!PreLoader.Contains(id))
            {
                if (Pics.Count > PreLoader.MaxCount) PreLoader.Clear();
            }

            // Change image
            await LoadPic.LoadPicAtIndexAsync(id).ConfigureAwait(false);
        }
    }
}