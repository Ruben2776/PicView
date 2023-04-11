using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Windows.Storage;
using Windows.System.UserProfile;
using PicView.ChangeImage;
using PicView.ChangeTitlebar;
using PicView.ImageHandling;
using PicView.UILogic;
using Rotation = PicView.UILogic.TransformImage.Rotation;

namespace PicView.SystemIntegration
{
    public static class LockScreenHelper
    {
        public static async Task<bool> SetLockScreenImageAsync()
        {
            await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
            {
                SetTitle.SetLoadingString();
                Application.Current.MainWindow!.Cursor = Cursors.Wait;
            });

            string? folderPath, fileName;
            var hasEffect = ConfigureWindows.GetMainWindow.MainImage.Effect != null;
            var rotationAngle = Rotation.RotationAngle;
            var isFlipped = Rotation.IsFlipped;
            var shouldSaveImage = hasEffect || rotationAngle != 0 || isFlipped;
            var checkOutOfRange = ErrorHandling.CheckOutOfRange();

            if (shouldSaveImage || checkOutOfRange)
            {
                // Create a temporary directory
                var tempDirectory = Path.GetTempPath();
                var tempFileName = Path.GetRandomFileName();
                var destinationPath = Path.Combine(tempDirectory, tempFileName);

                BitmapSource? bitmapSource = null;
                string? imagePath = null;

                if (checkOutOfRange)
                {
                    bitmapSource = ConfigureWindows.GetMainWindow.MainImage.Source as BitmapSource;
                }
                else
                {
                    imagePath = Navigation.Pics[Navigation.FolderIndex];
                }

                await SaveImages.SaveImageAsync(rotationAngle, isFlipped, bitmapSource, imagePath, destinationPath, null, hasEffect).ConfigureAwait(false);

                folderPath = Path.GetDirectoryName(destinationPath);
                fileName = Path.GetFileName(destinationPath);
            }
            else
            {
                folderPath = Path.GetDirectoryName(Navigation.Pics[Navigation.FolderIndex]);
                fileName = Path.GetFileName(Navigation.Pics[Navigation.FolderIndex]);
            }

            try
            {
                var storageFolder = await StorageFolder.GetFolderFromPathAsync(folderPath);
                var imageFile = await storageFolder.GetFileAsync(fileName);

                using var stream = await imageFile.OpenAsync(FileAccessMode.Read);
                await LockScreen.SetImageStreamAsync(stream);
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
                SetTitle.SetTitleString();
                Application.Current.MainWindow!.Cursor = Cursors.Arrow;
            });

            return true;
        }
    }
}
