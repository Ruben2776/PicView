using Microsoft.Win32;
using PicView.ChangeImage;
using PicView.ChangeTitlebar;
using PicView.FileHandling;
using PicView.ImageHandling;
using PicView.UILogic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Rotation = PicView.UILogic.TransformImage.Rotation;

namespace PicView.SystemIntegration
{
    public static class Wallpaper // Taken from a Microsoft sample...
    {
        public enum WallpaperStyle
        {
            Tile,
            Center,
            Stretch,
            Fit,
            Fill
        }

        internal static async Task SetWallpaperAsync(WallpaperStyle style, string? path = null)
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
                SetDesktopWallpaper(destinationPath, style);
            }
            else
            {
                SetDesktopWallpaper(Navigation.Pics[Navigation.FolderIndex], style);
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

        /// <summary>
        /// Set the desktop wallpaper.
        /// </summary>
        /// <param name="path">Path of the wallpaper</param>
        /// <param name="style">Wallpaper style</param>
        private static void SetDesktopWallpaper(string path, WallpaperStyle style)
        {
            // Set the wallpaper style and tile.
            // Two registry values are set in the Control Panel\Desktop key.
            // TileWallpaper
            //  0: The wallpaper picture should not be tiled
            //  1: The wallpaper picture should be tiled
            // WallpaperStyle
            //  0:  The image is centered if TileWallpaper=0 or tiled if TileWallpaper=1
            //  2:  The image is stretched to fill the screen
            //  6:  The image is resized to fit the screen while maintaining the aspect
            //      ratio. (Windows 7 and later)
            //  10: The image is resized and cropped to fill the screen while
            //      maintaining the aspect ratio. (Windows 7 and later)
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

            switch (style)
            {
                case WallpaperStyle.Tile:
                    key.SetValue(@"WallpaperStyle", "0");
                    key.SetValue(@"TileWallpaper", "1");
                    break;

                case WallpaperStyle.Center:
                    key.SetValue(@"WallpaperStyle", "0");
                    key.SetValue(@"TileWallpaper", "0");
                    break;

                case WallpaperStyle.Stretch:
                    key.SetValue(@"WallpaperStyle", "2");
                    key.SetValue(@"TileWallpaper", "0");
                    break;

                case WallpaperStyle.Fit: // (Windows 7 and later)
                    key.SetValue(@"WallpaperStyle", "6");
                    key.SetValue(@"TileWallpaper", "0");
                    break;

                default:
                case WallpaperStyle.Fill: // (Windows 7 and later)
                    key.SetValue(@"WallpaperStyle", "10");
                    key.SetValue(@"TileWallpaper", "0");
                    break;
            }

            key.Close();

            // Set the desktop wallpaper by calling the Win32 API SystemParametersInfo
            // with the SPI_SETDESKWALLPAPER desktop parameter. The changes should
            // persist, and also be immediately visible.
            if (!NativeMethods.SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, path,
                    SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE))
            {
                throw new Win32Exception();
            }
        }

        private const uint SPI_SETDESKWALLPAPER = 20;
        private const uint SPIF_UPDATEINIFILE = 0x01;
        private const uint SPIF_SENDWININICHANGE = 0x02;
    }
}