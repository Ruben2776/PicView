using Microsoft.VisualBasic.FileIO;
using PicView.ChangeImage;
using PicView.UILogic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using PicView.PicGallery;
using static PicView.ChangeImage.Navigation;
using static PicView.UILogic.Tooltip;

namespace PicView.FileHandling;

internal static class DeleteFiles
{
    /// <summary>
    /// Deletes the temporary files when an archived file has been opened
    /// </summary>
    internal static void DeleteTempFiles()
    {
        if (!Directory.Exists(ArchiveExtraction.TempFilePath))
        {
            return;
        }

        try
        {
            Array.ForEach(Directory.GetFiles(ArchiveExtraction.TempFilePath), File.Delete);
#if DEBUG
            Trace.WriteLine("Temp zip files deleted");
#endif
        }
        catch (Exception)
        {
            return;
        }

        try
        {
            Directory.Delete(ArchiveExtraction.TempFilePath);
#if DEBUG
            Trace.WriteLine("Temp zip folder " + ArchiveExtraction.TempFilePath + " deleted");
#endif
        }
        catch (Exception)
        {
            return;
        }

        ArchiveExtraction.TempZipFile = ArchiveExtraction.TempFilePath = null;
    }

    /// <summary>
    /// Deletes file or send it to recycle bin
    /// </summary>
    /// <param name="file"></param>
    /// <param name="recycle"></param>
    /// <returns></returns>
    internal static bool TryDeleteFile(string file, bool recycle)
    {
        if (!File.Exists(file))
        {
            return false;
        }

        try
        {
            var toRecycleOption = recycle ? RecycleOption.SendToRecycleBin : RecycleOption.DeletePermanently;
            FileSystem.DeleteFile(file, UIOption.OnlyErrorDialogs, toRecycleOption);
        }
        catch (Exception e)
        {
#if DEBUG
            Trace.WriteLine("Delete exception \n" + e.Message);
#endif
            ShowTooltipMessage(e.Message);
            return false;
        }
        return true;
    }

    /// <summary>
    /// Delete file or move it to recycle bin, navigate to next pic
    /// and display information
    /// </summary>
    /// <param name="recycle"></param>
    internal static async Task DeleteFileAsync(bool recycle, string fileName)
    {
        if (!TryDeleteFile(fileName, recycle))
        {
            return;
        }

        await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
        {
            var index = Pics.IndexOf(fileName);
            if (index < 0)
            {
                return;
            }
            // Sync with gallery
            if (UC.GetPicGallery is not null && UC.GetPicGallery.Container.Children.Count > index)
            {
                UC.GetPicGallery.Container.Children.RemoveAt(index);
            }
        });

        Pics.Remove(fileName);
        if (Pics.Count <= 0)
        {
            ErrorHandling.UnexpectedError();
            return;
        }
        ShowTooltipMessage(recycle ? Application.Current.Resources["SentFileToRecycleBin"] : Application.Current.Resources["Deleted"]);

        FolderIndex = GetNextIndex(NavigateTo.Previous, false);
        if (FolderIndex < 0)
        {
            FolderIndex = 0;
        }

        var preloadValue = PreLoader.Get(FolderIndex);
        PreLoader.Clear(); // Need to be cleared to avoid synchronization error
        await PreLoader.AddAsync(FolderIndex, preloadValue.FileInfo, preloadValue.BitmapSource).ConfigureAwait(false);
        await LoadPic.LoadPicAtIndexAsync(FolderIndex).ConfigureAwait(false);
    }
}