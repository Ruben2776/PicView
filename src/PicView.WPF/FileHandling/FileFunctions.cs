using Microsoft.Win32;
using PicView.Core.FileHandling;
using PicView.WPF.ChangeImage;
using PicView.WPF.ChangeTitlebar;
using PicView.WPF.ImageHandling;
using PicView.WPF.UILogic;
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

            await Task.Delay(700); // Fixes "this action can't be completed because the file is open"
            var deleteMsg = FileDeletionHelper.DeleteFileWithErrorMsg(oldPath, false);
            if (!string.IsNullOrWhiteSpace(deleteMsg))
            {
                // Show error message to user
                Tooltip.ShowTooltipMessage(deleteMsg);
                return null;
            }
            Refresh();
            await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(
                SetTitle.SetTitleString, DispatcherPriority.Background);
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
        }
        Refresh();
        return true;

        void Refresh()
        {
            if (ErrorHandling.CheckOutOfRange() == false)
            {
                Navigation.Pics.Remove(oldPath);
                PreLoader.Clear(); // Need cache to be refreshed
            }
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
}