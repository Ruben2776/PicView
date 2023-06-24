using PicView.ChangeImage;
using PicView.ChangeTitlebar;
using PicView.ConfigureSettings;
using PicView.ImageHandling;
using PicView.Properties;
using PicView.UILogic;
using PicView.Views.UserControls.Gallery;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using static PicView.ChangeImage.Navigation;
using static PicView.ImageHandling.Thumbnails;
using static PicView.UILogic.Sizing.ScaleImage;
using static PicView.UILogic.UC;

namespace PicView.PicGallery;

/// <summary>
/// Logic for what happens when user clicks on
/// thumbnail gallery item
/// </summary>
internal static class GalleryClick
{
    internal static async Task ClickAsync(int id)
    {
        if (Settings.Default.FullscreenGallery || Settings.Default.IsBottomGalleryShown && !GalleryFunctions.IsGalleryOpen)
        {
            await ItemClickAsync(id).ConfigureAwait(false);
            return;
        }

        await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
        {
            ConfigureWindows.GetMainWindow.Focus();
            ConfigureWindows.GetMainWindow.MainImage.Visibility = Visibility.Hidden;
            var galleryItem = GetPicGallery.Container.Children[id] as PicGalleryItem;
            ConfigureWindows.GetMainWindow.MainImage.Source = galleryItem.ThumbImage.Source;
        });

        var imageSize = ImageSizeFunctions.GetImageSize(Pics[id]);
        DoubleAnimation? galleryCloseAnimation;
        var image = new Image
        {
            Source = GetThumb(id) ?? ImageFunctions.ShowLogo() ?? ImageFunctions.ImageErrorMessage(),
            Stretch = Stretch.Fill,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        // Need to add border for background to pictures with transparent background
        var border = new Border
        {
            Background = ConfigColors.BackgroundColorBrush,
            Child = image
        };
        ConfigureWindows.GetMainWindow.ParentContainer.Children.Add(border);

        if (imageSize.HasValue)
        {
            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Send, () =>
            {
                SetTitle.SetLoadingString();
                FitImage(imageSize.Value.Width, imageSize.Value.Height);
                if (!Settings.Default.IsBottomGalleryShown)
                {
                    return;
                }

                border.Width = XWidth;
                border.Height = XHeight;
                galleryCloseAnimation = new DoubleAnimation
                {
                    FillBehavior = FillBehavior.Stop,
                    AccelerationRatio = 0.5,
                    DecelerationRatio = 0.5,
                    From = GetPicGallery.ActualHeight,
                    To = GalleryNavigation.PicGalleryItemSize + 22,
                    Duration = TimeSpan.FromSeconds(.5)
                };
                GalleryNavigation.SetSize(Settings.Default.BottomGalleryItems);
                for (int i = 0; i < GetPicGallery.Container.Children.Count; i++)
                {
                    var item = (PicGalleryItem)GetPicGallery.Container.Children[i];
                    item.InnerBorder.Height = item.InnerBorder.Width = GalleryNavigation.PicGalleryItemSize;
                    item.OuterBorder.Height = item.OuterBorder.Width = GalleryNavigation.PicGalleryItemSize;
                }
                galleryCloseAnimation.Completed += async delegate
                {
                    border.Opacity = 0;
                    ConfigureWindows.GetMainWindow.ParentContainer.Children.Remove(border);
                    image = null;
                    GalleryFunctions.IsGalleryOpen = false;
                    ConfigureWindows.GetMainWindow.MainImage.Visibility = Visibility.Visible;
                    GalleryLoad.LoadBottomGallery();
                    await ItemClickAsync(id).ConfigureAwait(false);
                };

                GetPicGallery.BeginAnimation(FrameworkElement.HeightProperty, galleryCloseAnimation);
            });
        }
        if (Settings.Default.IsBottomGalleryShown) return;

        var fromSize = GalleryNavigation.PicGalleryItemSize;
        var toSize = new[] { XWidth <= 0 ? ConfigureWindows.GetMainWindow.ActualWidth : XWidth, XHeight <= 0 ? ConfigureWindows.GetMainWindow.ActualHeight : XHeight };
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
            FillBehavior = FillBehavior.Stop
        };

        var heightAnimation = new DoubleAnimation
        {
            From = fromSize,
            To = toSize[1],
            Duration = duration,
            AccelerationRatio = acceleration,
            DecelerationRatio = deceleration,
            FillBehavior = FillBehavior.Stop
        };

        widthAnimation.Completed += async delegate
        {
            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
            {
                if (!Settings.Default.IsBottomGalleryShown)
                {
                }
                GetPicGallery.Visibility = Visibility.Collapsed; // prevent it from popping up again
                border.Opacity = 0;
                ConfigureWindows.GetMainWindow.ParentContainer.Children.Remove(border);
                image = null;
                GalleryFunctions.IsGalleryOpen = false;
                ConfigureWindows.GetMainWindow.MainImage.Visibility = Visibility.Visible;
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

            if (!Settings.Default.IsBottomGalleryShown)
            {
                border.BeginAnimation(FrameworkElement.WidthProperty, widthAnimation);
                border.BeginAnimation(FrameworkElement.HeightProperty, heightAnimation);
            }
        }, DispatcherPriority.Send);
    }

    private static async Task ItemClickAsync(int id)
    {
        await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
        {
            GetPicGallery.Scroller.CanContentScroll = false; // Enable animations
            // Deselect current item
            GalleryNavigation.SetSelected(GalleryNavigation.SelectedGalleryItem, false);
            GalleryNavigation.SetSelected(FolderIndex, false);

            // Restore interface elements if needed
            if (!Settings.Default.ShowInterface || Settings.Default.Fullscreen && Settings.Default.ShowAltInterfaceButtons)
            {
                HideInterfaceLogic.ShowNavigation(true);
                HideInterfaceLogic.ShowShortcuts(true);
            }

            // Select next item
            GalleryNavigation.SetSelected(id, true);
            GalleryNavigation.SelectedGalleryItem = id;
            GalleryNavigation.ScrollToGalleryCenter();
        });
        // Change image
        await LoadPic.LoadPicAtIndexAsync(id).ConfigureAwait(false);
    }
}