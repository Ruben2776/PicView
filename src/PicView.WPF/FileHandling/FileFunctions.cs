using Microsoft.Win32;
using PicView.Core.FileHandling;
using PicView.WPF.ChangeImage;
using PicView.WPF.ChangeTitlebar;
using PicView.WPF.ImageHandling;
using PicView.WPF.UILogic;
using System.Configuration;
using System.IO;
using System.Windows.Threading;
using PicView.Core.Navigation;

namespace PicView.WPF.FileHandling
{
    internal static class FileFunctions
    {
        /// <summary>
        /// True if renamed same folder, null if error, false if moved to other dir
        /// </summary>
        /// <param name="newPath"></param>
        /// <returns></returns>
        internal static async Task<bool?> RenameFileWithErrorChecking(string newPath)
        {
            var extChanged = false;
            if (Path.GetExtension(newPath) != Path.GetExtension(Navigation.Pics[Navigation.FolderIndex]))
            {
                await SaveImages.SaveImageAsync(newPath).ConfigureAwait(false);
                DeleteFiles.TryDeleteFile(Navigation.Pics[Navigation.FolderIndex], false);
                extChanged = true;
            }
            else if (!FileHelper.RenameFile(Navigation.Pics[Navigation.FolderIndex], newPath))
            {
                return null;
            }

            // Check if the file is not in the same folder
            if (!extChanged && Path.GetDirectoryName(newPath) !=
                Path.GetDirectoryName(Navigation.Pics[Navigation.FolderIndex]))
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

            Navigation.Pics[Navigation.FolderIndex] = newPath;
            PreLoader.Rename(Navigation.FolderIndex);
            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                () => { SetTitle.SetTitleString(); });

            return true;
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
            var localMachine = Registry.LocalMachine;
            var fileKey = localMachine.OpenSubKey(
                $@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\{fileName}");
            var result = fileKey?.GetValue(string.Empty);
            if (result == null)
            {
                return null;
            }

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
    }
}