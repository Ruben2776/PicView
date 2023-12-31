using Microsoft.Win32;
using PicView.Core.FileHandling;
using PicView.WPF.ChangeImage;
using PicView.WPF.ImageHandling;
using PicView.WPF.UILogic;
using System.IO;

namespace PicView.WPF.FileHandling;

internal static class FileFunctions
{
    internal static bool IsFileBeingRenamed { get; private set; }

    /// <summary>
    /// True if renamed same folder, null if error, false if moved to other dir
    /// </summary>
    /// <param name="newPath"></param>
    /// <returns></returns>
    internal static async Task<bool?> RenameCurrentFileWithErrorChecking(string newPath, string oldPath)
    {
        if (File.Exists(newPath))
        {
            return null;
        }
        IsFileBeingRenamed = true;

        if (Path.GetDirectoryName(oldPath) != Path.GetDirectoryName(newPath))
        {
            IsFileBeingRenamed = false;
            if (!FileHelper.RenameFile(oldPath, newPath))
            {
                return null;
            }

            return true;
        }

        var oldIndex = Navigation.FolderIndex;
        if (Path.GetExtension(newPath) != Path.GetExtension(oldPath))
        {
            await SaveImages.SaveImageAsync(oldPath, newPath).ConfigureAwait(false);
            while (FileHelper.IsFileInUse(oldPath))
            {
                await Task.Delay(50); // Fixes "this action can't be completed because the file is open"
            }

            var deleteMsg = FileDeletionHelper.DeleteFileWithErrorMsg(oldPath, false);
            if (!string.IsNullOrWhiteSpace(deleteMsg))
            {
                // Show error message to user
                Tooltip.ShowTooltipMessage(deleteMsg);
                return null;
            }
        }
        else if (!FileHelper.RenameFile(oldPath, newPath))
        {
            IsFileBeingRenamed = false;
            return null;
        }
        var bitmapsource = PreLoader.Get(oldIndex)?.BitmapSource;
        PreLoader.Remove(oldIndex);
        var fileInfo = new FileInfo(newPath);
        if (fileInfo.Exists == false) { return null; }

        var newList = await Task.FromResult(FileLists.FileList(fileInfo)).ConfigureAwait(false);
        if (newList.Count == 0) { return null; }

        Navigation.Pics = newList;
        Navigation.FolderIndex = Navigation.Pics.IndexOf(fileInfo.FullName);
        if (Navigation.FolderIndex < 0)
        {
            await ErrorHandling.ReloadAsync().ConfigureAwait(false);
            return null;
        }
        IsFileBeingRenamed = false;

        await FileUpdateNavigation.UpdateTitle(Navigation.FolderIndex);
        await FileUpdateNavigation.UpdateGalleryAsync(Navigation.FolderIndex, oldIndex, fileInfo, oldPath, newPath, true).ConfigureAwait(false);
        await PreLoader.AddAsync(Navigation.FolderIndex, fileInfo, bitmapsource).ConfigureAwait(false);
        await ImageInfo.UpdateValuesAsync(fileInfo).ConfigureAwait(false);

        FileHistoryNavigation.Rename(oldPath, newPath);
        return true;
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