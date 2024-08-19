using PicView.Core.Config;
using PicView.Core.FileHandling;
using PicView.Core.ImageDecoding;
using PicView.Core.Navigation;
using PicView.WPF.ImageHandling;
using PicView.WPF.PicGallery;
using PicView.WPF.UILogic;
using PicView.WPF.UILogic.TransformImage;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using PicView.Core.Localization;
using XamlAnimatedGif;
using static PicView.WPF.ChangeImage.ErrorHandling;
using static PicView.WPF.ChangeImage.Navigation;
using static PicView.WPF.ChangeTitlebar.SetTitle;
using static PicView.WPF.UILogic.Sizing.ScaleImage;
using static PicView.WPF.UILogic.Tooltip;
using static PicView.WPF.UILogic.UC;
using Rotation = PicView.WPF.UILogic.TransformImage.Rotation;

namespace PicView.WPF.ChangeImage;

internal static class UpdateImage
{
    /// <summary>
    /// Updates the image values asynchronously.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <param name="preLoadValue">The PreLoadValue</param>
    /// <param name="fastPic">Use different loading when key held down.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    internal static async Task UpdateImageValuesAsync(int index, PreLoader.PreLoadValue preLoadValue, bool fastPic = false)
    {
        if (preLoadValue is null)
        {
            await PreLoader.AddAsync(index, new FileInfo(Pics[index]));
            preLoadValue = PreLoader.Get(index)!;
            if (preLoadValue is null)
            {
                await ReloadAsync().ConfigureAwait(false);
                return;
            }
        }

        if (!fastPic)
        {
            var x = 0;
            while (preLoadValue.IsLoading) // Fix occurrences of non-loaded image
            {
                x++;
                await Task.Delay(20);
                if (index != FolderIndex)
                {
                    return;
                }
                // ReSharper disable once InvertIf
                if (x > 20)
                {
                    await PreLoader.AddAsync(index).ConfigureAwait(false);
                    preLoadValue = PreLoader.Get(index)!;
                    if (preLoadValue is null)
                    {
                        return;
                    }
                }
            }
        }

        await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
        {
            if (index != FolderIndex)
            {
                return;
            }
            ConfigureWindows.GetMainWindow.MainImage.Source = preLoadValue?.BitmapSource ?? ImageFunctions.ImageErrorMessage();

            SetOrientation(preLoadValue.Orientation);
            var width = preLoadValue?.BitmapSource?.PixelWidth ??
                        ConfigureWindows.GetMainWindow.MainImage.Source.Width;
            var height = preLoadValue?.BitmapSource?.PixelHeight ??
                         ConfigureWindows.GetMainWindow.MainImage.Source.Height;
            FitImage(width, height);

            // Scroll to top if scroll enabled
            if (SettingsHelper.Settings.Zoom.ScrollEnabled)
            {
                ConfigureWindows.GetMainWindow.Scroller.ScrollToTop();
            }

            if (ZoomLogic.IsZoomed)
            {
                ZoomLogic.ResetZoom(false);
            }
        }, DispatcherPriority.Send);

        if (index != FolderIndex)
        {
            return;
        }

        if (GetPicGallery is not null)
        {
            await GetPicGallery.Dispatcher.InvokeAsync(() =>
            {
                if (index != FolderIndex)
                {
                    return;
                }

                if (!fastPic)
                {
                    GalleryNavigation.SetSelected(FolderIndex, true);
                }
                GalleryNavigation.SelectedGalleryItem = FolderIndex;
                GalleryNavigation.ScrollToGalleryCenter();
            });
        }

        var titleString = TitleHelper.GetTitle(preLoadValue?.BitmapSource?.PixelWidth ?? 0,
            preLoadValue?.BitmapSource?.PixelHeight ?? 0, index, preLoadValue.FileInfo,
            ZoomLogic.ZoomValue, Pics);

        await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
        {
            if (index != FolderIndex)
            {
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
        }, DispatcherPriority.Send);

        preLoadValue.FileInfo ??= new FileInfo(Pics[FolderIndex]);
        if (preLoadValue.FileInfo.Extension.Equals(".gif", StringComparison.OrdinalIgnoreCase))
        {
            var frames = await Task.FromResult(ImageFunctionHelper.GetImageFrames(preLoadValue.FileInfo.FullName))
                .ConfigureAwait(false);
            if (frames > 1)
            {
                var uri = new Uri(preLoadValue.FileInfo.FullName);
                await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
                {
                    if (index != FolderIndex)
                    {
                        return;
                    }

                    AnimationBehavior.SetSourceUri(ConfigureWindows.GetMainWindow.MainImage, uri);
                }, DispatcherPriority.Normal);
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

        PreLoader.Clear();
        GalleryFunctions.Clear();
        XWidth = XHeight = 0;

        FileDeletionHelper.DeleteTempFiles();

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

    internal static void SetOrientation(EXIFHelper.EXIFOrientation? orientation)
    {
        if (orientation is not null)
        {
            switch (orientation)
            {
                default:
                    Rotation.Rotate(0);
                    if (Rotation.IsFlipped)
                    {
                        Rotation.Flip();
                    }

                    break;

                case EXIFHelper.EXIFOrientation.Normal:
                    Rotation.Rotate(0);
                    if (Rotation.IsFlipped)
                    {
                        Rotation.Flip();
                    }

                    break;

                case EXIFHelper.EXIFOrientation.Flipped:
                    Rotation.Rotate(0);
                    if (!Rotation.IsFlipped)
                    {
                        Rotation.Flip();
                    }

                    break;

                case EXIFHelper.EXIFOrientation.Rotated180:
                    Rotation.Rotate(180);
                    if (Rotation.IsFlipped)
                    {
                        Rotation.Flip();
                    }

                    break;

                case EXIFHelper.EXIFOrientation.Rotated180Flipped:
                    Rotation.Rotate(180);
                    if (!Rotation.IsFlipped)
                    {
                        Rotation.Flip();
                    }

                    break;

                case EXIFHelper.EXIFOrientation.Rotated270Flipped:
                    Rotation.Rotate(270);
                    if (!Rotation.IsFlipped)
                    {
                        Rotation.Flip();
                    }

                    break;

                case EXIFHelper.EXIFOrientation.Rotated90:
                    Rotation.Rotate(90);
                    if (Rotation.IsFlipped)
                    {
                        Rotation.Flip();
                    }

                    break;

                case EXIFHelper.EXIFOrientation.Rotated90Flipped:
                    Rotation.Rotate(90);
                    if (!Rotation.IsFlipped)
                    {
                        Rotation.Flip();
                    }

                    break;

                case EXIFHelper.EXIFOrientation.Rotated270:
                    Rotation.Rotate(270);
                    if (Rotation.IsFlipped)
                    {
                        Rotation.Flip();
                    }

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
        await UpdateImageAsync(TranslationHelper.Translation.Base64Image, bitmapSource).ConfigureAwait(false);
    }
}