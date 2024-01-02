using PicView.Core.FileHandling;
using PicView.Windows.Wallpaper;
using PicView.WPF.ChangeImage;
using PicView.WPF.ChangeTitlebar;
using PicView.WPF.ImageHandling;
using PicView.WPF.UILogic;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Rotation = PicView.WPF.UILogic.TransformImage.Rotation;

namespace PicView.WPF.SystemIntegration;

public static class Wallpaper // Taken from a Microsoft sample...
{
    internal static async Task SetWallpaperAsync(WallpaperHelper.WallpaperStyle style, string? path = null)
    {
        var url = string.Empty;
        await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
        {
            url = ConfigureWindows.GetMainWindow.TitleText.Text.GetURL();
            SetTitle.SetLoadingString();
            Application.Current.MainWindow!.Cursor = Cursors.Wait;
        });

        var effectApplied = ConfigureWindows.GetMainWindow.MainImage.Effect != null;
        var rotationAngle = Rotation.RotationAngle;
        var isFlipped = Rotation.IsFlipped;
        var shouldSaveImage = effectApplied || rotationAngle != 0 || isFlipped;
        var checkOutOfRange = ErrorHandling.CheckOutOfRange();

        BitmapSource? bitmapSource = null;
        string? imagePath = null;
        if (Navigation.Pics.Count <= 0)
        {
            shouldSaveImage = true;
        }
        else
        {
            var extension = Path.GetExtension(Navigation.Pics[Navigation.FolderIndex]).ToLowerInvariant();
            switch (extension)
            {
                case ".jpg":
                case ".jpeg":
                case ".png":
                case ".gif":
                case ".bmp":
                    break;

                default:
                    shouldSaveImage = true;
                    break;
            }
        }

        if (shouldSaveImage || checkOutOfRange)
        {
            // Create a temporary directory
            var tempDirectory = Path.GetTempPath();
            var tempFileName = Path.GetRandomFileName();
            var destinationPath = Path.Combine(tempDirectory, tempFileName);

            if (checkOutOfRange)
            {
                bitmapSource = ConfigureWindows.GetMainWindow.MainImage.Source as BitmapSource;
            }
            else
            {
                imagePath = path ?? Navigation.Pics[Navigation.FolderIndex];
            }

            await SaveImages.SaveImageAsync(rotationAngle, isFlipped, bitmapSource, imagePath, destinationPath,
                null, effectApplied).ConfigureAwait(false);
            WallpaperHelper.SetDesktopWallpaper(destinationPath, style);
        }
        else
        {
            WallpaperHelper.SetDesktopWallpaper(Navigation.Pics[Navigation.FolderIndex], style);
        }

        await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
        {
            if (string.IsNullOrWhiteSpace(url))
                SetTitle.SetTitleString();
            else
                SetTitle.SetTitleString(
                    (int)ConfigureWindows.GetMainWindow.MainImage.Source.Width,
                    (int)ConfigureWindows.GetMainWindow.MainImage.Source.Height,
                    !string.IsNullOrWhiteSpace(url)
                        ? url
                        : checkOutOfRange
                            ? Navigation.Pics[Navigation.FolderIndex]
                            : Application.Current.Resources["Image"] as string);
            Application.Current.MainWindow!.Cursor = Cursors.Arrow;
        });
    }
}