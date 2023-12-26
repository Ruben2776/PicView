using Microsoft.Win32;
using PicView.Core.FileHandling;
using PicView.WPF.ChangeImage;
using PicView.WPF.ChangeTitlebar;
using PicView.WPF.ImageHandling;
using PicView.WPF.UILogic;
using System.Configuration;
using System.IO;
using System.Windows.Threading;

namespace PicView.WPF.FileHandling;

internal static class FileFunctions
{
    /// <summary>
    /// True if renamed same folder, null if error, false if moved to other dir
    /// </summary>
    /// <param name="newPath"></param>
    /// <returns></returns>
    internal static async Task<bool?> RenameFileWithErrorChecking(string newPath, string oldPath)
    {
        if (Path.GetExtension(newPath) != Path.GetExtension(oldPath))
        {
            await SaveImages.SaveImageAsync(oldPath, newPath).ConfigureAwait(false);
            if (ErrorHandling.CheckOutOfRange() == false)
            {
                if (Navigation.Pics[Navigation.FolderIndex] == oldPath)
                {
                    PreLoader.Remove(Navigation.FolderIndex);
                }
            }
            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                () => { SetTitle.SetTitleString(); });
            await Task.Delay(1000); // Fixes "this action can't be completed because the file is open"
            var deleteFile = FileDeletionHelper.DeleteFile(oldPath, false);
            if (!string.IsNullOrWhiteSpace(deleteFile))
            {
                // Show error message to user
                Tooltip.ShowTooltipMessage(deleteFile);
                return null;
            }
        }
        else if (!FileHelper.RenameFile(oldPath, newPath))
        {
            return null;
        }
        else
        {
            if (Navigation.Pics.Count < 1)
            {
                await LoadPic.LoadPiFromFileAsync(newPath).ConfigureAwait(false);
                return false;
            }
            if (ErrorHandling.CheckOutOfRange() == false)
            {
                if (Navigation.Pics[Navigation.FolderIndex] == oldPath)
                {
                    PreLoader.Remove(Navigation.FolderIndex);
                }
            }
        }

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