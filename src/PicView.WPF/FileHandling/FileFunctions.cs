using Microsoft.Win32;
using PicView.Core.FileHandling;
using PicView.Core.Gallery;
using PicView.WPF.ChangeImage;
using PicView.WPF.ChangeTitlebar;
using PicView.WPF.ImageHandling;
using PicView.WPF.PicGallery;
using PicView.WPF.UILogic;
using PicView.WPF.Views.UserControls.Gallery;
using System.Diagnostics;
using System.IO;
using PicView.Core.Config;
using PicView.WPF.ConfigureSettings;

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
        IsFileBeingRenamed = true;
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
        Navigation.Pics.Remove(oldPath);
        Navigation.Pics.Add(newPath);
        Navigation.FolderIndex = Navigation.Pics.IndexOf(newPath);

        await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
        {
            var width = ConfigureWindows.GetMainWindow.MainImage.Source?.Width ?? 0;
            var height = ConfigureWindows.GetMainWindow.MainImage.Source?.Height ?? 0;
            try
            {
                SetTitle.SetTitleString((int)width, (int)height, Navigation.FolderIndex, fileInfo);
            }
            catch (Exception exception)
            {
#if DEBUG
                Trace.WriteLine($"{nameof(RenameCurrentFileWithErrorChecking)} update title exception:\n{exception.Message}");
#endif
            }
        });
        await PreLoader.AddAsync(Navigation.FolderIndex, fileInfo, bitmapsource).ConfigureAwait(false);
        await ImageInfo.UpdateValuesAsync(fileInfo).ConfigureAwait(false);

        if (UC.GetPicGallery is not null)
        {
            while (GalleryLoad.IsLoading)
            {
                await Task.Delay(50);
            }
            if (Path.GetFileNameWithoutExtension(newPath) != Path.GetFileNameWithoutExtension(oldPath))
            {
                await UpdateUIValues.ChangeSortingAsync(FileListHelper.GetSordOrder()).ConfigureAwait(false);
            }
            var thumbData = GalleryThumbInfo.GalleryThumbHolder.GetThumbData(Navigation.FolderIndex, null, fileInfo);
            await UC.GetPicGallery?.Dispatcher?.InvokeAsync(() =>
            {
                var item = UC.GetPicGallery.Container.Children.OfType<PicGalleryItem>().FirstOrDefault(x => x.FilePath == oldPath);
                if (item is null)
                {
#if DEBUG
                    Trace.WriteLine($"{nameof(RenameCurrentFileWithErrorChecking)} item null");
#endif
                    return;
                }
                item.UpdateValues(thumbData.FileName, thumbData.FileDate, thumbData.FileSize, thumbData.FileLocation);
            });
        }
        IsFileBeingRenamed = false;
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