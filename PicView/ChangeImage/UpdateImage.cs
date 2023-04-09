using PicView.FileHandling;
using PicView.ImageHandling;
using PicView.Properties;
using PicView.SystemIntegration;
using PicView.UILogic;
using System.IO;
using System.Security.Policy;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using XamlAnimatedGif;
using static PicView.ChangeImage.ErrorHandling;
using static PicView.ChangeImage.Navigation;
using static PicView.ChangeImage.Preloader;
using static PicView.ChangeTitlebar.SetTitle;
using static PicView.UILogic.Sizing.ScaleImage;
using static PicView.UILogic.Tooltip;
using static PicView.UILogic.UC;
using Rotation = PicView.UILogic.TransformImage.Rotation;

namespace PicView.ChangeImage
{
    internal static class UpdateImage
    {
        /// <summary>
        /// Update picture, size it and set the title from index
        /// </summary>
        /// <param name="index"></param>
        /// <param name="bitmapSource"></param>
        internal static async Task UpdateImageAsync(int index, BitmapSource? bitmapSource, FileInfo? fileInfo = null)
        {
            if (bitmapSource is null)
            {
                bitmapSource = ImageFunctions.ImageErrorMessage();
                if (bitmapSource is null)
                {
                    UnexpectedError();
                    return;
                }
            }

            var ext = fileInfo is null ? Path.GetExtension(Pics[index]) : fileInfo.Extension;
            var isGif = ext is not null && ext.Equals(".gif", StringComparison.OrdinalIgnoreCase);
            await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
            {
                // Scroll to top if scroll enabled
                if (Settings.Default.ScrollEnabled)
                {
                    ConfigureWindows.GetMainWindow.Scroller.ScrollToTop();
                }

                // Reset transforms if needed
                if (Rotation.IsFlipped || Rotation.RotationAngle != 0)
                {
                    Rotation.IsFlipped = false;
                    Rotation.RotationAngle = 0;
                    if (GetQuickSettingsMenu is not null && GetQuickSettingsMenu.FlipButton is not null)
                    {
                        GetQuickSettingsMenu.FlipButton.TheButton.IsChecked = false;
                    }

                    ConfigureWindows.GetMainWindow.MainImage.LayoutTransform = null;
                }
                         
                if (isGif) // Loads gif from XamlAnimatedGif if neccesary   
                {
                    AnimationBehavior.SetSourceUri(ConfigureWindows.GetMainWindow.MainImage, new Uri(Pics?[index]));
                }
                else
                {
                    ConfigureWindows.GetMainWindow.MainImage.Source = bitmapSource;
                }
            }, DispatcherPriority.Send);

            var titleString = TitleString(bitmapSource.PixelWidth, bitmapSource.PixelHeight, index, fileInfo);
            await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
            {
                FitImage(bitmapSource.PixelWidth, bitmapSource.PixelHeight);

                ConfigureWindows.GetMainWindow.Title = titleString[0];
                ConfigureWindows.GetMainWindow.TitleText.Text = titleString[1];
                ConfigureWindows.GetMainWindow.TitleText.ToolTip = titleString[2];
                ConfigureWindows.GetMainWindow.MainImage.Cursor = System.Windows.Input.Cursors.Arrow;
            }, DispatcherPriority.Send);
        }

        /// <summary>
        /// Update picture, size it and set the title from string
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="bitmapSource"></param>
        internal static async Task UpdateImageAsync(string imageName, BitmapSource bitmapSource, bool isGif = false, string? file = null)
        {
            Size? imageSize = null;
            if (isGif)
            {
                if (string.IsNullOrWhiteSpace(file))
                {
                    await ErrorHandling.ReloadAsync().ConfigureAwait(false);
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
                        SetTitleString((int)imageSize.Value.Width, (int)imageSize.Value.Height, imageName);
                    }
                    AnimationBehavior.SetSourceUri(ConfigureWindows.GetMainWindow.MainImage, new Uri(file));
                }
                else if (bitmapSource != null)
                {
                    ConfigureWindows.GetMainWindow.MainImage.Source = bitmapSource;
                    SetTitleString(bitmapSource.PixelWidth, bitmapSource.PixelHeight, imageName);
                    FitImage(bitmapSource.PixelWidth, bitmapSource.PixelHeight);
                }
                else
                {
                    UnexpectedError();
                    return;
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
        /// <param name="pic"></param>
        /// <param name="imageName"></param>
        internal static async Task UpdateImageFromBase64PicAsync(string base64string)
        {
            if (string.IsNullOrEmpty(base64string))
            {
                return;
            }
            var pic = await Base64.Base64StringToBitmapAsync(base64string).ConfigureAwait(false);
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
}
