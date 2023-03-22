using Microsoft.Win32;
using PicView.ChangeImage;
using PicView.ChangeTitlebar;
using PicView.UILogic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using Windows.UI.StartScreen;

namespace PicView.FileHandling
{
    internal static partial class FileFunctions
    {
        /// <summary>
        /// CheckIfDirectoryOrFile method checks if a given path is a directory or a file.
        /// </summary>
        /// <param name="path">The path to check.</param>
        /// <returns>
        /// A nullable boolean indicating whether the given path is a directory (true), a file (false), or neither (null).
        /// </returns>
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

        /// <summary>
        /// RenameFile method renames a file.
        /// </summary>
        /// <param name="path">The original path of the file.</param>
        /// <param name="newPath">The new path of the file.</param>
        /// <returns>
        /// A boolean indicating whether the file was successfully renamed or not.
        /// </returns>
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

                if (UC.GetPicGallery is not null && UC.GetPicGallery.Container.Children.Count > Navigation.FolderIndex)
                {
                    UC.GetPicGallery.Container.Children.RemoveAt(Navigation.FolderIndex);
                }
                Navigation.Pics.Remove(Navigation.Pics[Navigation.FolderIndex]);
                await Navigation.GoToNextImage(NavigateTo.Next).ConfigureAwait(false);
                return false;
            }

            Preloader.Rename(Navigation.FolderIndex, newPath);
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
        /// The default format is "0.## XB", e.g. "4.2 KB" or "1.43 GB"
        /// </summary>
        /// <param name="fileSize">FileInfo.Length</param>
        /// <returns>E.g. "3.34 MB"</returns>
        /// Credits to http://www.somacon.com/p576.php
        internal static string GetReadableFileSize(this long fileSize)
        {
            const int kilobyte = 1024;
            string sign = fileSize < 0 ? "-" : "";
            double value;
            char prefix;

            switch (fileSize)
            {
                // Gigabyte
                case >= 0x40000000:
                    prefix = 'G';
                    value = fileSize >> 20;
                    break;
                // Megabyte
                case >= 0x100000:
                    prefix = 'M';
                    value = fileSize >> 10;
                    break;
                // Kilobyte
                case >= 0x400:
                    prefix = 'K';
                    value = fileSize;
                    break;

                default:
                    return fileSize.ToString(sign + "0 B", CultureInfo.CurrentCulture); // Byte
            }
            value /= kilobyte; // Divide by 1024 to get fractional value

            return sign + value.ToString($"0.## {prefix}B", CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Shortens the given string `name` to the given `amount` and appends "..." to it.
        /// If the length of `name` is less than 25, returns `name` as it is.
        /// </summary>
        /// <param name="name">The string to shorten</param>
        /// <param name="amount">The length to shorten the string to</param>
        /// <returns>The shortened string</returns>
        internal static string Shorten(this string name, int amount)
        {
            if (name.Length < 25) { return name; }

            name = name[..amount];
            name += "...";
            return name;
        }

        /// <summary>
        /// Returns the file path of the default configuration file for the specified `userLevel`.
        /// If there's a `ConfigurationException` thrown, returns the `Filename` from the exception.
        /// </summary>
        /// <param name="userLevel">The `userLevel` for which to get the configuration file</param>
        /// <returns>The file path of the default configuration file</returns>
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

        /// <summary>
        /// Retrieves the file path for the executable file associated with the specified file name, if any, from the registry key "SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths".
        /// </summary>
        /// <param name="fileName">The file name (with or without extension) of the executable file to retrieve the path for.</param>
        /// <returns>The full path of the executable file, or null if the file is not found in the registry.</returns>
        internal static string? GetPathForExe(string fileName)
        {
            RegistryKey localMachine = Registry.LocalMachine;
            RegistryKey? fileKey = localMachine.OpenSubKey(string.Format(@"{0}\{1}", @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths", fileName));
            if (fileKey == null) { return null; }
            object? result = fileKey.GetValue(string.Empty);
            if (result == null) { return null; }
            fileKey.Close();

            return (string)result;
        }

        /// <summary>
        /// Returns the directory path of the default configuration file for the specified `userLevel` with PerUserRoamingAndLocal value.
        /// </summary>
        /// <returns>The directory path of the default configuration file</returns>
        internal static string? GetWritingPath()
        {
            return Path.GetDirectoryName(GetDefaultExeConfigPath(ConfigurationUserLevel.PerUserRoamingAndLocal));
        }

        [GeneratedRegex("\\b(?:https?://|www\\.)\\S+\\b", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
        private static partial Regex URLregex();

        /// <summary>
        /// Returns the URL contained in the given string `value` by matching it against a regex pattern.
        /// If there's an exception thrown, returns an empty string.
        /// </summary>
        /// <param name="value">The string to find the URL in</param>
        /// <returns>The URL contained in the string, or an empty string if no URL is found or an exception is thrown</returns>
        internal static string GetURL(this string value)
        {
            try
            {
                return URLregex().Match(value).ToString();
            }
            catch (Exception e)
            {
#if DEBUG
                Trace.WriteLine(e.Message);
#endif
                return string.Empty;
            }
        }

        /// <summary>
        /// Returns the URL path if it exists or an empty string if not.
        /// </summary>
        internal static string RetrieveFromURL()
        {
            // Check if from URL and download it
            string url = GetURL(ConfigureWindows.GetMainWindow.TitleText.Text);
            if (!string.IsNullOrEmpty(url))
            {
                if (File.Exists(ArchiveExtraction.TempFilePath))
                {
                    return ArchiveExtraction.TempFilePath;
                }
            }
            return string.Empty;
        }
    }
}