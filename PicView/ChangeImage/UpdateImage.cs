using PicView.FileHandling;
using PicView.ImageHandling;
using PicView.PicGallery;
using PicView.Properties;
using PicView.SystemIntegration;
using PicView.UILogic;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using XamlAnimatedGif;
using static PicView.ChangeImage.ErrorHandling;
using static PicView.ChangeImage.Navigation;
using static PicView.ChangeImage.PreLoader;
using static PicView.ChangeTitlebar.SetTitle;
using static PicView.FileHandling.ArchiveExtraction;
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
    internal static async Task UpdateImageAsync(int index, PreLoadValue preLoadValue)
    {
        preLoadValue.BitmapSource ??= ImageFunctions.ImageErrorMessage();
        var isGif = preLoadValue.FileInfo.Extension.Equals(".gif", StringComparison.OrdinalIgnoreCase);
        
        await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
        {
            if (isGif) // Loads gif from XamlAnimatedGif if necessary   
            {
                AnimationBehavior.SetSourceUri(ConfigureWindows.GetMainWindow.MainImage, new Uri(Pics?[index]));
            }
            else
            {
                ConfigureWindows.GetMainWindow.MainImage.Source = preLoadValue.BitmapSource;
            }
        }, DispatcherPriority.Send);

        var titleString = TitleString(preLoadValue.BitmapSource.PixelWidth, preLoadValue.BitmapSource.PixelHeight,
            index, preLoadValue.FileInfo);
        
        await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
        {
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

            // Reset transforms if needed
            if (!Rotation.IsFlipped && Rotation.RotationAngle == 0) return;
            Rotation.IsFlipped = false;
            Rotation.RotationAngle = 0;
            if (GetImageSettingsMenu is not null)
            {
                GetImageSettingsMenu.FlipButton.IsChecked = false;
            }
            ConfigureWindows.GetMainWindow.FlipButton.IsChecked = false;

            ConfigureWindows.GetMainWindow.MainImage.LayoutTransform = null;
        }, DispatcherPriority.Send);

        if (GalleryFunctions.IsHorizontalFullscreenOpen)
            GalleryNavigation.FullscreenGalleryNavigation();

        if (GetToolTipMessage is { IsVisible: true })
            ConfigureWindows.GetMainWindow.Dispatcher.Invoke(() => GetToolTipMessage.Visibility = Visibility.Hidden);

        if (ConfigureWindows.GetImageInfoWindow is { IsVisible: true })
            await ImageInfo.UpdateValuesAsync(preLoadValue.FileInfo).ConfigureAwait(false);

        if (Pics.Count > 1)
        {
            Taskbar.Progress((double)index / Pics.Count);

            await AddAsync(index, preLoadValue.FileInfo, preLoadValue.BitmapSource).ConfigureAwait(false);
            await PreLoadAsync(index).ConfigureAwait(false);
        }

        // Add recent files, except when browsing archive
        if (string.IsNullOrWhiteSpace(TempZipFile) && Pics.Count > index)
        {
            GetFileHistory ??= new FileHistory();
            GetFileHistory.Add(Pics[index]);
        }
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

        await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
        {
            ToggleStartUpUC(true);

            if (Settings.Default.ScrollEnabled)
            {
                ConfigureWindows.GetMainWindow.Scroller.ScrollToTop();
            }

            if (isGif)
            {
                if (imageSize.HasValue)
                {
                    FitImage(imageSize.Value.Width, imageSize.Value.Height);
                    SetTitleString((int)imageSize.Value.Width, (int)imageSize.Value.Height, name);
                }
                AnimationBehavior.SetSourceUri(ConfigureWindows.GetMainWindow.MainImage, new Uri(file));
            }
            else if (bitmapSource != null)
            {
                ConfigureWindows.GetMainWindow.MainImage.Source = bitmapSource;
                SetTitleString(bitmapSource.PixelWidth, bitmapSource.PixelHeight, name);
                FitImage(bitmapSource.PixelWidth, bitmapSource.PixelHeight);
            }
            else
            {
                UnexpectedError();
            }
        }, DispatcherPriority.Send);

        CloseToolTipMessage();
        Taskbar.NoProgress();
        FolderIndex = 0;

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