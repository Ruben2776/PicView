using Microsoft.Win32;
using PicView.ChangeImage;
using PicView.ImageHandling;
using PicView.UILogic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows.Input;
using System.Windows.Media.Imaging;

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

        /// <summary>
        /// NOT thread safe!
        /// </summary>
        /// <param name="style"></param>
        internal static async Task SetWallpaperAsync(WallpaperStyle style)
        {
            await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
            {
                ChangeTitlebar.SetTitle.SetLoadingString();
                System.Windows.Application.Current.MainWindow.Cursor = Cursors.Wait;
            });

            bool hasEffect = ConfigureWindows.GetMainWindow.MainImage.Effect != null;
            double rotationAngle = UILogic.TransformImage.Rotation.RotationAngle;
            bool isFlipped = UILogic.TransformImage.Rotation.IsFlipped;
            bool shouldSaveImage = hasEffect || rotationAngle != 0 || isFlipped;
            bool checkOutOfRange = ErrorHandling.CheckOutOfRange();
            bool effectApplied = ConfigureWindows.GetMainWindow.MainImage.Effect != null;

            BitmapSource? bitmapSource = null;
            string? imagePath = null;

            if (shouldSaveImage || checkOutOfRange)
            {
                // Create a temporary directory
                string tempDirectory = Path.GetTempPath();
                string tempFileName = Path.GetRandomFileName();
                string destinationPath = Path.Combine(tempDirectory, tempFileName);

                if (checkOutOfRange)
                {
                    bitmapSource = ConfigureWindows.GetMainWindow.MainImage.Source as BitmapSource;
                }
                else
                {
                    imagePath = Navigation.Pics[Navigation.FolderIndex];
                }

                await SaveImages.SaveImageAsync(rotationAngle, isFlipped, bitmapSource, imagePath, destinationPath, null, hasEffect).ConfigureAwait(false);
                SetDesktopWallpaper(destinationPath, style);
            }
            else
            {
                SetDesktopWallpaper(Navigation.Pics[Navigation.FolderIndex], style);
            }

            await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
            {
                ChangeTitlebar.SetTitle.SetTitleString();
                System.Windows.Application.Current.MainWindow.Cursor = Cursors.Arrow;
            });
        }

        /// <summary>
        /// Determine if .jpg files are supported as wallpaper in the current
        /// operating system. The .jpg wallpapers are not supported before
        /// Windows Vista.
        /// </summary>
        public static bool SupportJpgAsWallpaper
        {
            get
            {
                return Environment.OSVersion.Version >= new Version(6, 0);
            }
        }

        /// <summary>
        /// Determine if the fit and fill wallpaper styles are supported in
        /// the current operating system. The styles are not supported before
        /// Windows 7.
        /// </summary>
        public static bool SupportFitFillWallpaperStyles
        {
            get
            {
                return Environment.OSVersion.Version >= new Version(6, 1);
            }
        }

        /// <summary>
        /// Set the desktop wallpaper.
        /// </summary>
        /// <param name="path">Path of the wallpaper</param>
        /// <param name="style">Wallpaper style</param>
        public static void SetDesktopWallpaper(string path, WallpaperStyle style)
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
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true);
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

            /// TODO Check if support for execotic file formats can be converted and
            /// works for Windows supported standard images, such as PSD to jpg?

            // If the specified image file is neither .bmp nor .jpg, - or -
            // if the image is a .jpg file but the operating system is Windows Server
            // 2003 or Windows XP/2000 that does not support .jpg as the desktop
            // wallpaper, convert the image file to .bmp and save it to the
            // %appdata%\Microsoft\Windows\Themes folder.
            string ext = Path.GetExtension(path);
            if ((!ext.Equals(".bmp", StringComparison.OrdinalIgnoreCase) &&
                !ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase))
                ||
                (ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase) &&
                !SupportJpgAsWallpaper))
            {
                var dest = string.Format(CultureInfo.CurrentCulture, @"{0}\Microsoft\Windows\Themes\{1}.jpg",
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        Path.GetFileNameWithoutExtension(path));
                if (string.IsNullOrEmpty(dest))
                {
                    return;
                }
            }

            // Set the desktop wallpapaer by calling the Win32 API SystemParametersInfo
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