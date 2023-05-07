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
        if (GalleryFunctions.IsHorizontalOpen == false)
        {
            await ItemClickAsync(id).ConfigureAwait(false);
            return;
        }

        await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
        {
            ConfigureWindows.GetMainWindow.Focus();
            ConfigureWindows.GetMainWindow.MainImage.Visibility = Visibility.Hidden;
            var z = GetPicGallery.Container.Children[id] as PicGalleryItem;
            ConfigureWindows.GetMainWindow.MainImage.Source = z.ThumbImage.Source;
        });

        Border? border = null;
        Image? image = null;

        var size = ImageSizeFunctions.GetImageSize(Pics[id]);
        if (size.HasValue)
        {
            GalleryFunctions.IsHorizontalOpen = false;
            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Send, () =>
            {
                SetTitle.SetLoadingString();
                FitImage(size.Value.Width, size.Value.Height);
            });
        }

        var from = GalleryNavigation.PicGalleryItemSize;
        var to = new[] { XWidth, XHeight };
        var acceleration = 0.2;
        var deceleration = 0.4;
        var duration = TimeSpan.FromSeconds(.3);

        var da = new DoubleAnimation
        {
            From = from,
            To = to[0],
            Duration = duration,
            AccelerationRatio = acceleration,
            DecelerationRatio = deceleration,
            FillBehavior = FillBehavior.Stop
        };

        var da0 = new DoubleAnimation
        {
            From = from,
            To = to[1],
            Duration = duration,
            AccelerationRatio = acceleration,
            DecelerationRatio = deceleration,
            FillBehavior = FillBehavior.Stop
        };

        da.Completed += async delegate
        {
            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
            {
                border.Opacity = 0;
                GetPicGallery.grid.Children.Remove(border);
                image = null;
                GalleryFunctions.IsHorizontalOpen = false;
                GetPicGallery.Visibility = Visibility.Collapsed; // prevent it from popping up again
                ConfigureWindows.GetMainWindow.MainImage.Visibility = Visibility.Visible;
            });
            await ItemClickAsync(id).ConfigureAwait(false);
        };

        image = new Image
        {
            Source = GetThumb(id) ?? ImageFunctions.ShowLogo() ?? ImageFunctions.ImageErrorMessage(),
            Stretch = Stretch.Fill,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        // Need to add border for background to pictures with transparent background
        border = new Border
        {
            Background = ConfigColors.BackgroundColorBrush,
            Child = image
        };

        await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
        {
            if (Settings.Default.AutoFitWindow)
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
                GetPicGallery.Container.BeginAnimation(UIElement.OpacityProperty, da3);
            }

            GetPicGallery.x2.Visibility = Visibility.Hidden;
            GetPicGallery.grid.Children.Add(border);

            border.BeginAnimation(FrameworkElement.WidthProperty, da);
            border.BeginAnimation(FrameworkElement.HeightProperty, da0);
        }, DispatcherPriority.Send);
    }

    private static async Task ItemClickAsync(int id)
    {
        await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
        {
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
        });
        // Change image
        await LoadPic.LoadPicAtIndexAsync(id).ConfigureAwait(false);
    }
}