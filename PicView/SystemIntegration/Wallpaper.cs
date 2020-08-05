using Microsoft.Win32;
using PicView.FileHandling;
using PicView.ImageHandling;
using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using static PicView.ChangeImage.Navigation;
using static PicView.UILogic.TransformImage.Rotation;

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
        internal static async void SetWallpaper(WallpaperStyle style)
        {
            if (UILogic.Loading.LoadWindows.GetMainWindow.MainImage.Effect != null || Clipboard.ContainsImage())
            {
                try
                {
                    var SaveImage = ImageDecoder.GetRenderedMagickImage();
                    if (SaveImage == null) { return; }

                    await Task.Run(() =>
                    {
                        // Create temp directory
                        var tempPath = Path.GetTempPath();
                        var randomName = Path.GetRandomFileName();

                        // Write temp file to it
                        using var filestream = new FileStream(tempPath + randomName, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.SequentialScan);
                        SaveImage.Write(filestream);
                        SaveImage.Dispose();
                        filestream.Close();

                        // Use it
                        SetDesktopWallpaper(tempPath + randomName, style);

                        // Clean up
                        File.Delete(tempPath + randomName);
                        using var timer = new Timer(2000);
                        timer.Elapsed += (s, x) => Directory.Delete(tempPath);
                    }).ConfigureAwait(true);

                    SaveImage.Dispose();
                }
                catch { }
            }
            else if (Pics.Count > 0)
            {
                if (FolderIndex < Pics.Count)
                {
                    await Task.Run(() =>
                     {
                         SetDesktopWallpaper(Pics[FolderIndex], style);
                     }).ConfigureAwait(false);
                }
            }
            else
            {
                string wallpaper = FileFunctions.GetURL(UILogic.Loading.LoadWindows.GetMainWindow.TitleText.Text);

                if (Uri.IsWellFormedUriString(wallpaper, UriKind.Absolute)) // Check if from web
                {
                    await Task.Run(() =>
                     {
                        // Create temp directory
                        var tempPath = Path.GetTempPath();
                         var randomName = Path.GetRandomFileName();

                        // Download to it
                        using var webClient = new System.Net.WebClient();
                         Directory.CreateDirectory(tempPath);
                         webClient.DownloadFile(wallpaper, tempPath + randomName);

                        // Use it
                        SetDesktopWallpaper(tempPath + randomName, style);

                        // Clean up
                        File.Delete(tempPath + randomName);
                         using var timer = new Timer(2000);
                         timer.Elapsed += (s, x) => Directory.Delete(tempPath);
                     }).ConfigureAwait(false);

                    return;
                }
                // TODO add Base64 support
            }
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
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true);

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
                SaveImages.TrySaveImage(Rotateint, Flipped, path, dest);
                path = dest;
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