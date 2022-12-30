using PicView.ChangeImage;
using PicView.ChangeTitlebar;
using PicView.SystemIntegration;
using PicView.UILogic;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace PicView.FileHandling
{
    internal static class FileFunctions
    {
        internal static void ShowFileProperties()
        {
            if (ErrorHandling.CheckOutOfRange()) { return; }

            NativeMethods.ShowFileProperties(Navigation.Pics[Navigation.FolderIndex]);
        }

        [DllImport("shell32.dll", SetLastError = true)]
        public static extern int SHOpenFolderAndSelectItems(IntPtr pidlFolder, uint cidl, [In, MarshalAs(UnmanagedType.LPArray)] IntPtr[] apidl, uint dwFlags);

        [DllImport("shell32.dll", SetLastError = true)]
        public static extern void SHParseDisplayName([MarshalAs(UnmanagedType.LPWStr)] string name, IntPtr bindingContext, [Out] out IntPtr pidl, uint sfgaoIn, [Out] out uint psfgaoOut);

        public static void OpenFolderAndSelectItem(string folderPath, string file)
        {
            IntPtr nativeFolder;
            uint psfgaoOut;
            SHParseDisplayName(folderPath, IntPtr.Zero, out nativeFolder, 0, out psfgaoOut);

            if (nativeFolder == IntPtr.Zero)
            {
                // Log error, can't find folder
                return;
            }

            IntPtr nativeFile;
            SHParseDisplayName(Path.Combine(folderPath, file), IntPtr.Zero, out nativeFile, 0, out psfgaoOut);

            IntPtr[] fileArray;
            if (nativeFile == IntPtr.Zero)
            {
                // Open the folder without the file selected if we can't find the file
                fileArray = Array.Empty<IntPtr>();
            }
            else
            {
                fileArray = new[] { nativeFile };
            }

            _ = SHOpenFolderAndSelectItems(nativeFolder, (uint)fileArray.Length, fileArray, 0);

            Marshal.FreeCoTaskMem(nativeFolder);
            if (nativeFile != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(nativeFile);
            }
        }

        /// <summary>
        /// Returns true if directory
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        internal static bool? CheckIfDirectoryOrFile(string path)
        {
            try
            {
                var getAttributes = File.GetAttributes(path);
                return getAttributes.HasFlag(FileAttributes.Directory);
            }
            catch (Exception)
            {
                return null;
            }
        }

        internal static bool RenameFile(string path, string newPath)
        {
            try
            {
                new FileInfo(newPath).Directory.Create(); // create directory if not exists
            }
            catch (Exception e)
            {
#if DEBUG
                Trace.WriteLine(e.Message);
#endif
            }
            try
            {
                File.Move(path, newPath, true);
            }
            catch (Exception e)
            {
#if DEBUG
                Trace.WriteLine(e.Message);
#endif
                return false;
            }
            return true;
        }

        /// <summary>
        /// True if renamed same folder, null if error, false if moved to other dir
        /// </summary>
        /// <param name="newPath"></param>
        /// <returns></returns>
        internal static async Task<bool?> RenameFileWithErrorChecking(string newPath)
        {
            if (!RenameFile(Navigation.Pics[Navigation.FolderIndex], newPath))
            {
                return null;
            }

            // Check if the file is not in the same folder
            if (Path.GetDirectoryName(newPath) != Path.GetDirectoryName(Navigation.Pics[Navigation.FolderIndex]))
            {
                if (Navigation.Pics.Count < 1)
                {
                    await LoadPic.LoadPiFromFileAsync(newPath).ConfigureAwait(false);
                    return false;
                }

                Preloader.Remove(Navigation.FolderIndex);
                if (UC.GetPicGallery is not null && UC.GetPicGallery.Container.Children.Count > Navigation.FolderIndex)
                {
                    UC.GetPicGallery.Container.Children.RemoveAt(Navigation.FolderIndex);
                }
                Navigation.Pics.Remove(Navigation.Pics[Navigation.FolderIndex]);
                await Navigation.NavigateToPicAsync(false).ConfigureAwait(false);
                return false;
            }

            Preloader.Rename(Navigation.Pics[Navigation.FolderIndex], newPath);
            if (UC.GetPicGallery is not null && UC.GetPicGallery.Container.Children.Count > Navigation.FolderIndex)
            {
                UC.GetPicGallery.Container.Children.RemoveAt(Navigation.FolderIndex);
            }
            Navigation.Pics[Navigation.FolderIndex] = newPath;

            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
            {
                var width = ConfigureWindows.GetMainWindow.MainImage.Source.Width;
                var height = ConfigureWindows.GetMainWindow.MainImage.Source.Height;

                SetTitle.SetTitleString((int)width, (int)height, Navigation.FolderIndex, null);
            });

            return true;
        }


        /// <summary>
        /// Returns the human-readable file size for an arbitrary, 64-bit file size
        /// The default format is "0.### XB", e.g. "4.2 KB" or "1.434 GB"
        /// </summary>
        /// <param name="i">FileInfo.Length</param>
        /// <returns></returns>
        /// Credits to http://www.somacon.com/p576.php
        internal static string GetSizeReadable(long i)
        {
            string sign = i < 0 ? "-" : string.Empty;
            char prefix;
            double value;

            switch (i)
            {
                // Gigabyte
                case >= 0x40000000:
                    prefix = 'G';
                    value = i >> 20;
                    break;
                // Megabyte
                case >= 0x100000:
                    prefix = 'M';
                    value = i >> 10;
                    break;
                // Kilobyte
                case >= 0x400:
                    prefix = 'K';
                    value = i;
                    break;
                default:
                    return i.ToString(sign + "0 B", CultureInfo.CurrentCulture); // Byte
            }
            value /= 1024; // Divide by 1024 to get fractional value

            return sign + value.ToString("0.## ", CultureInfo.CurrentCulture) + prefix + 'B';
        }

        internal static string Shorten(string name, int amount)
        {
            if (name.Length < 25) { return name; }

            name = name[..amount];
            name += "...";
            return name;
        }

        internal static string GetDefaultExeConfigPath(ConfigurationUserLevel userLevel)
        {
            try
            {
                var userConfig = ConfigurationManager.OpenExeConfiguration(userLevel);
                return userConfig.FilePath;
            }
            catch (ConfigurationException e)
            {
                return e.Filename;
            }
        }

        internal static string? GetWritingPath()
        {
            return Path.GetDirectoryName(GetDefaultExeConfigPath(ConfigurationUserLevel.PerUserRoamingAndLocal));
        }

        internal static string GetURL(string value)
        {
            try
            {
                var linkParser = new Regex(@"\b(?:https?://|www\.)\S+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                return linkParser.Match(value).ToString();
            }
            catch (Exception e)
            {
#if DEBUG
                Trace.WriteLine(e.Message);
#endif
                return string.Empty;
            }
        }

    }
}