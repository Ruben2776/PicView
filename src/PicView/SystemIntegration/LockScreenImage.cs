using Microsoft.Win32;
using PicView.ChangeImage;
using PicView.ChangeTitlebar;
using PicView.FileHandling;
using PicView.ImageHandling;
using PicView.UILogic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using PicView.ProcessHandling;

//using Windows.Storage;
//using Windows.System.UserProfile;
using Rotation = PicView.UILogic.TransformImage.Rotation;

namespace PicView.SystemIntegration;

public static class LockScreenHelper
{
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool Wow64DisableWow64FsRedirection(ref IntPtr ptr); //If on 64 bit, C# will replace "System32" with "SysWOW64". This disables that.

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

            await SaveImages.SaveImageAsync(rotationAngle, isFlipped, bitmapSource, imagePath, destinationPath, null, hasEffect).ConfigureAwait(false);
            path = destinationPath;
        }
        else
        {
            path = Navigation.Pics[Navigation.FolderIndex];
        }

        try
        {
            Tooltip.ShowTooltipMessage(Application.Current.Resources["Applying"]);

            ProcessLogic.RunElevated("PicView.Tools.exe", "lockscreen," + path);
        }
        catch (Exception ex)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(LockScreenHelper)}::{nameof(SetLockScreenImageAsync)} exception:\n{ex.Message}");
#endif
            Tooltip.ShowTooltipMessage(ex);
            await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
            {
                SetTitle.SetTitleString();
                Application.Current.MainWindow!.Cursor = Cursors.Arrow;
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
                    !string.IsNullOrWhiteSpace(url) ? url : checkOutOfRange
                    ? Navigation.Pics[Navigation.FolderIndex] : Application.Current.Resources["Image"] as string);
            Application.Current.MainWindow!.Cursor = Cursors.Arrow;
        });

        return true;
    }
}