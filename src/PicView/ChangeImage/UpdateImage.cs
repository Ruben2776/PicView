using PicView.FileHandling;
using PicView.ImageHandling;
using PicView.PicGallery;
using PicView.Properties;
using PicView.UILogic;
using PicView.UILogic.Sizing;
using PicView.UILogic.TransformImage;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using XamlAnimatedGif;
using static PicView.ChangeImage.ErrorHandling;
using static PicView.ChangeImage.Navigation;
using static PicView.ChangeImage.PreLoader;
using static PicView.ChangeTitlebar.SetTitle;
using static PicView.UILogic.Sizing.ScaleImage;
using static PicView.UILogic.Tooltip;
using static PicView.UILogic.UC;
using Rotation = PicView.UILogic.TransformImage.Rotation;

namespace PicView.ChangeImage;

internal static class UpdateImage
{
    /// <summary>
    /// Update picture, size it and set the title from index
    /// </summary>
    /// <param name="index"></param>
    /// <param name="preLoadValue"></param>
    internal static async Task UpdateImageValuesAsync(int index, PreLoadValue preLoadValue)
    {
        preLoadValue.BitmapSource ??= ImageFunctions.ImageErrorMessage();

        await ConfigureWindows.GetMainWindow.MainImage.Dispatcher.InvokeAsync(() =>
        {
            ConfigureWindows.GetMainWindow.MainImage.Source = preLoadValue.BitmapSource;
        }, DispatcherPriority.Send);

        var titleString = TitleString(preLoadValue.BitmapSource.PixelWidth, preLoadValue.BitmapSource.PixelHeight,
            index, preLoadValue.FileInfo);

        await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
        {
            if (Rotation.RotationAngle is not 0)
            {
                Rotation.Rotate(0);
            }
            FitImage(preLoadValue.BitmapSource.PixelWidth, preLoadValue.BitmapSource.PixelHeight);

            ConfigureWindows.GetMainWindow.Title = titleString[0];
            ConfigureWindows.GetMainWindow.TitleText.Text = titleString[1];
            ConfigureWindows.GetMainWindow.TitleText.ToolTip = titleString[2];
            ConfigureWindows.GetMainWindow.MainImage.Cursor = Cursors.Arrow;

            // Scroll to top if scroll enabled
            if (Settings.Default.ScrollEnabled)
            {
                ConfigureWindows.GetMainWindow.Scroller.ScrollToTop();
            }

            // Reset transforms
            if (ZoomLogic.IsZoomed)
            {
                ZoomLogic.ResetZoom(false);
            }

            if (Rotation.IsFlipped)
            {
                Rotation.Flip();
            }

            if (GetSpinWaiter is { IsVisible: true })
            {
                GetSpinWaiter.Visibility = Visibility.Collapsed;
            }
        }, DispatcherPriority.Send);

        if (preLoadValue.FileInfo.Extension.Equals(".gif", StringComparison.OrdinalIgnoreCase))
        {
            var frames = ImageFunctions.GetImageFrames(Pics[index]);
            if (frames > 0)
            {
                var uri = new Uri(Pics[index]);
                AnimationBehavior.SetSourceUri(ConfigureWindows.GetMainWindow.MainImage, uri);
            }
        }

        if (GetToolTipMessage is { IsVisible: true })
            ConfigureWindows.GetMainWindow.Dispatcher.Invoke(() => GetToolTipMessage.Visibility = Visibility.Hidden);
    }

    /// <summary>
    /// Update picture, size it and set the title from string
    /// </summary>
    /// <param name="name"></param>
    /// <param name="bitmapSource"></param>
    /// <param name="isGif"></param>
    /// <param name="file"></param>
    internal static async Task UpdateImageAsync(string name, BitmapSource? bitmapSource, bool isGif = false, string? file = null)
    {
        if (bitmapSource is null)
        {
            UnexpectedError();
            return;
        }
        if (GetPicGallery is not null)
        {
            await GetPicGallery.Dispatcher.InvokeAsync(() =>
            {
                GetPicGallery.Visibility = Visibility.Collapsed;
            }, DispatcherPriority.Send);
        }

        await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
        {
            ToggleStartUpUC(true);

            if (Settings.Default.ScrollEnabled)
            {
                ConfigureWindows.GetMainWindow.Scroller.ScrollToTop();
            }

            ConfigureWindows.GetMainWindow.MainImage.Source = bitmapSource;
            SetTitleString(bitmapSource.PixelWidth, bitmapSource.PixelHeight, name);
            FitImage(bitmapSource.PixelWidth, bitmapSource.PixelHeight);

            if (GetSpinWaiter is { IsVisible: true })
            {
                GetSpinWaiter.Visibility = Visibility.Collapsed;
            }
        }, DispatcherPriority.Send);

        Size? imageSize = null;
        if (isGif)
        {
            if (string.IsNullOrWhiteSpace(file))
            {
                await ReloadAsync().ConfigureAwait(false);
                return;
            }
            imageSize = ImageSizeFunctions.GetImageSize(file);
        }

        if (isGif)
        {
            await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
            {
                if (imageSize.HasValue)
                {
                    FitImage(imageSize.Value.Width, imageSize.Value.Height);
                    SetTitleString((int)imageSize.Value.Width, (int)imageSize.Value.Height, name);
                }
                AnimationBehavior.SetSourceUri(ConfigureWindows.GetMainWindow.MainImage, new Uri(file));
            }, DispatcherPriority.Normal);
        }

        CloseToolTipMessage();
        Pics?.Clear();

        PreLoader.Clear();
        GalleryFunctions.Clear();
        ScaleImage.XWidth = ScaleImage.XHeight = 0;

        DeleteFiles.DeleteTempFiles();

        if (ConfigureWindows.GetImageInfoWindow is not null)
        {
            await ImageInfo.UpdateValuesAsync(null).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Load a picture from a base64
    /// </summary>
    internal static async Task UpdateImageFromBase64PicAsync(string base64String)
    {
        if (string.IsNullOrEmpty(base64String))
        {
            return;
        }
        var pic = await Base64.Base64StringToBitmapAsync(base64String).ConfigureAwait(false);
        if (pic == null)
        {
            return;
        }
        if (Application.Current.Resources["Base64Image"] is not string b64)
        {
            return;
        }

        await UpdateImageAsync(b64, pic).ConfigureAwait(false);
    }
}