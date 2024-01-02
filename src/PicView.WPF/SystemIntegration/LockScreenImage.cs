using Microsoft.Win32;
using PicView.Core.FileHandling;
using PicView.Core.Localization;
using PicView.WPF.ChangeImage;
using PicView.WPF.ChangeTitlebar;
using PicView.WPF.ImageHandling;
using PicView.WPF.ProcessHandling;
using PicView.WPF.UILogic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Rotation = PicView.WPF.UILogic.TransformImage.Rotation;

namespace PicView.WPF.SystemIntegration;

public static class LockScreenHelper
{
    public static async Task<bool> SetLockScreenImageAsync(string? path = null)
    {
        var url = string.Empty;
        await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
        {
            url = ConfigureWindows.GetMainWindow.TitleText.Text.GetURL();
            SetTitle.SetLoadingString();
            Application.Current.MainWindow!.Cursor = Cursors.Wait;
        });

        string destinationPath;
        var hasEffect = ConfigureWindows.GetMainWindow.MainImage.Effect != null;
        var rotationAngle = Rotation.RotationAngle;
        var isFlipped = Rotation.IsFlipped;
        var shouldSaveImage = hasEffect || rotationAngle != 0 || isFlipped;
        var checkOutOfRange = ErrorHandling.CheckOutOfRange();

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
            destinationPath = Path.Combine(tempDirectory, Path.GetFileNameWithoutExtension(path) + ".jpg");

            BitmapSource? bitmapSource = null;
            string? imagePath = null;

            if (checkOutOfRange)
            {
                bitmapSource = ConfigureWindows.GetMainWindow.MainImage.Source as BitmapSource;
            }
            else
            {
                imagePath = path ?? Navigation.Pics[Navigation.FolderIndex];
            }

            await SaveImages
                .SaveImageAsync(rotationAngle, isFlipped, bitmapSource, imagePath, destinationPath, null, hasEffect)
                .ConfigureAwait(false);
            path = destinationPath;
        }
        else
        {
            path = Navigation.Pics[Navigation.FolderIndex];
        }

        try
        {
            Tooltip.ShowTooltipMessage(TranslationHelper.GetTranslation("Applying"));

            ProcessLogic.RunElevated("PicView.exe", "lockscreen," + path);
        }
        catch (Exception ex)
        {
#if DEBUG
            Trace.WriteLine(
                $"{nameof(LockScreenHelper)}::{nameof(SetLockScreenImageAsync)} exception:\n{ex.Message}");
#endif
            Tooltip.ShowTooltipMessage(ex);
            await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
            {
                SetTitle.SetTitleString();
                ConfigureWindows.GetMainWindow.Cursor = Cursors.Arrow;
            });
            return false;
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
            ConfigureWindows.GetMainWindow.Cursor = Cursors.Arrow;
        });

        return true;
    }
}