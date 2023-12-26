using PicView.Core.Config;
using PicView.WPF.FileHandling;
using PicView.WPF.ImageHandling;
using PicView.WPF.PicGallery;
using PicView.WPF.UILogic;
using PicView.WPF.UILogic.TransformImage;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using PicView.Core.Navigation;
using XamlAnimatedGif;
using static PicView.WPF.ChangeImage.ErrorHandling;
using static PicView.WPF.ChangeImage.Navigation;
using static PicView.WPF.ChangeImage.PreLoader;
using static PicView.WPF.ChangeTitlebar.SetTitle;
using static PicView.WPF.UILogic.Sizing.ScaleImage;
using static PicView.WPF.UILogic.Tooltip;
using static PicView.WPF.UILogic.UC;
using Rotation = PicView.WPF.UILogic.TransformImage.Rotation;

namespace PicView.WPF.ChangeImage;

internal static class UpdateImage
{
    /// <summary>
    /// Update picture, size it and set the title from index
    /// </summary>
    /// <param name="index"></param>
    /// <param name="preLoadValue"></param>
    internal static async Task UpdateImageValuesAsync(int index, PreLoadValue preLoadValue)
    {
        if (preLoadValue is null)
        {
            return;
        }
        preLoadValue.BitmapSource ??= ImageFunctions.ImageErrorMessage();
        var source = new CancellationTokenSource();

        await ConfigureWindows.GetMainWindow.MainImage.Dispatcher.InvokeAsync(() =>
        {
            if (index != FolderIndex)
            {
                source.Cancel();
                return;
            }

            ConfigureWindows.GetMainWindow.MainImage.Source = preLoadValue.BitmapSource;

            if (preLoadValue.Orientation is not 0)
            {
                // 0 = none
                // 1 = 0 degrees
                // 2 = 0 degrees, flipped
                // 3 = 180 degrees
                // 4 = 180 degrees, flipped
                // 5 = 270 degrees, flipped
                // 6 = 90 degrees
                // 7 = 90 degrees, flipped
                // 8 = 270 degrees, flipped
                switch (preLoadValue.Orientation)
                {
                    case 0:
                    case 1:
                        Rotation.Rotate(0);
                        if (Rotation.IsFlipped)
                        {
                            Rotation.Flip();
                        }
                        break;

                    case 2:
                        if (Rotation.IsFlipped == false)
                        {
                            Rotation.Flip();
                        }
                        Rotation.Rotate(0);
                        break;

                    case 3:
                        if (Rotation.IsFlipped)
                        {
                            Rotation.Flip();
                        }
                        Rotation.Rotate(180);
                        break;

                    case 4:
                        if (Rotation.IsFlipped == false)
                        {
                            Rotation.Flip();
                        }
                        Rotation.Rotate(180);
                        break;

                    case 5:
                        if (Rotation.IsFlipped == false)
                        {
                            Rotation.Flip();
                        }
                        Rotation.Rotate(270);
                        break;

                    case 6:
                        if (Rotation.IsFlipped)
                        {
                            Rotation.Flip();
                        }
                        Rotation.Rotate(90);
                        break;

                    case 7:
                        if (Rotation.IsFlipped == false)
                        {
                            Rotation.Flip();
                        }
                        Rotation.Rotate(90);
                        break;

                    case 8:
                        if (Rotation.IsFlipped)
                        {
                            Rotation.Flip();
                        }
                        Rotation.Rotate(270);
                        break;
                }
            }
            else
            {
                Rotation.Rotate(0);
                if (Rotation.IsFlipped)
                {
                    Rotation.Flip();
                }
            }

            FitImage(preLoadValue.BitmapSource.PixelWidth, preLoadValue.BitmapSource.PixelHeight);

            // Scroll to top if scroll enabled
            if (SettingsHelper.Settings.Zoom.ScrollEnabled)
            {
                ConfigureWindows.GetMainWindow.Scroller.ScrollToTop();
            }

            if (ZoomLogic.IsZoomed)
            {
                ZoomLogic.ResetZoom(false);
            }
        }, DispatcherPriority.Send, source.Token);

        var titleString = await Task.FromResult(TitleHelper.GetTitle(preLoadValue.BitmapSource.PixelWidth,
            preLoadValue.BitmapSource.PixelHeight, index, preLoadValue.FileInfo,
            ZoomLogic.ZoomValue, Pics)).ConfigureAwait(false);
        if (source.IsCancellationRequested)
        {
            return;
        }

        await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
        {
            if (index != FolderIndex)
            {
                source.Cancel();
                return;
            }

            ConfigureWindows.GetMainWindow.Title = titleString[0];
            ConfigureWindows.GetMainWindow.TitleText.Text = titleString[1];
            ConfigureWindows.GetMainWindow.TitleText.ToolTip = titleString[2];
            ConfigureWindows.GetMainWindow.MainImage.Cursor = Cursors.Arrow;

            if (GetSpinWaiter is { IsVisible: true })
            {
                GetSpinWaiter.Visibility = Visibility.Collapsed;
            }
        }, DispatcherPriority.Send, source.Token);

        if (source.IsCancellationRequested)
        {
            return;
        }

        preLoadValue.FileInfo ??= new FileInfo(Pics[FolderIndex]);
        if (preLoadValue.FileInfo.Extension.Equals(".gif", StringComparison.OrdinalIgnoreCase))
        {
            var frames = await Task.FromResult(ImageFunctions.GetImageFrames(preLoadValue.FileInfo.FullName))
                .ConfigureAwait(false);
            if (frames > 1)
            {
                var uri = new Uri(preLoadValue.FileInfo.FullName);
                await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
                {
                    if (index != FolderIndex)
                    {
                        source.Cancel();
                        return;
                    }

                    AnimationBehavior.SetSourceUri(ConfigureWindows.GetMainWindow.MainImage, uri);
                }, DispatcherPriority.Normal, source.Token);
            }
        }

        if (GetToolTipMessage is { IsVisible: true })
            ConfigureWindows.GetMainWindow.Dispatcher.Invoke(() =>
                GetToolTipMessage.Visibility = Visibility.Hidden);
    }

    /// <summary>
    /// Update picture, size it and set the title from string
    /// </summary>
    /// <param name="name"></param>
    /// <param name="bitmapSource"></param>
    /// <param name="isGif"></param>
    /// <param name="file"></param>
    internal static async Task UpdateImageAsync(string name, BitmapSource? bitmapSource, bool isGif = false,
        string? file = null)
    {
        if (bitmapSource is null)
        {
            UnexpectedError();
            return;
        }

        if (GetPicGallery is not null)
        {
            await GetPicGallery.Dispatcher.InvokeAsync(() => { GetPicGallery.Visibility = Visibility.Collapsed; },
                DispatcherPriority.Send);
        }

        await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
        {
            ToggleStartUpUC(true);

            if (SettingsHelper.Settings.Zoom.ScrollEnabled)
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

        Clear();
        GalleryFunctions.Clear();
        XWidth = XHeight = 0;

        DeleteFiles.DeleteTempFiles();

        if (ConfigureWindows.GetImageInfoWindow is not null)
        {
            await ImageInfo.UpdateValuesAsync(null).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Load a picture from a base64
    /// </summary>
    internal static async Task UpdateImageFromBase64PicAsync(FileInfo fileInfo)
    {
        var bitmapSource = await Image2BitmapSource.GetMagickBase64(fileInfo).ConfigureAwait(false);
        await UpdateImageFromBase64PicAsync(bitmapSource);
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

        var bitmapSource = await Image2BitmapSource.GetMagickBase64(base64String).ConfigureAwait(false);
        await UpdateImageFromBase64PicAsync(bitmapSource);
    }

    private static async Task UpdateImageFromBase64PicAsync(BitmapSource bitmapSource)
    {
        // Add a string to explain to the user that it is a base64 image
        if (Application.Current.Resources["Base64Image"] is not string b64)
        {
            return;
        }

        await UpdateImageAsync(b64, bitmapSource).ConfigureAwait(false);
    }
}