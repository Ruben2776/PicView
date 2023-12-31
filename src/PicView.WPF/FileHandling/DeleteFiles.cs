using PicView.Core.FileHandling;
using PicView.Core.Localization;
using PicView.Core.Navigation;
using PicView.WPF.ChangeImage;
using PicView.WPF.UILogic;
using System.Diagnostics;
using static PicView.WPF.ChangeImage.Navigation;
using static PicView.WPF.UILogic.Tooltip;

namespace PicView.WPF.FileHandling;

internal static class DeleteFiles
{
    /// <summary>
    /// Delete file or move it to recycle bin, navigate to next pic
    /// and display information
    /// </summary>
    /// <param name="recycle"></param>
    internal static async Task DeleteCurrentFileAsync(bool recycle)
    {
        if (ErrorHandling.CheckOutOfRange())
        {
            return;
        }

        var fileName = Pics[FolderIndex];
        var index = Pics.IndexOf(fileName);
        var deleteFile = FileDeletionHelper.DeleteFileWithErrorMsg(fileName, recycle);
        if (!string.IsNullOrWhiteSpace(deleteFile))
        {
            // Show error message to user
            ShowTooltipMessage(deleteFile);
            return;
        }

        await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
        {
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

        FolderIndex = ImageIteration.GetNextIndex(NavigateTo.Previous, Slideshow.SlideTimer != null, Pics, FolderIndex);
        if (FolderIndex < 0)
        {
            FolderIndex = 0;
        }

        var preloadValue = PreLoader.Get(FolderIndex);
        PreLoader.Clear(); // Need to be cleared to avoid synchronization error
        await PreLoader.AddAsync(FolderIndex, preloadValue?.FileInfo, preloadValue?.BitmapSource)
            .ConfigureAwait(false);
        await LoadPic.LoadPicAtIndexAsync(FolderIndex).ConfigureAwait(false);

        ShowTooltipMessage(recycle
            ? TranslationHelper.GetTranslation("SentFileToRecycleBin")
            : TranslationHelper.GetTranslation("Deleted"));
    }

    /// <summary>
    /// Delete file or move it to recycle bin, navigate to next pic
    /// and display information
    /// </summary>
    /// <param name="recycle"></param>
    /// <param name="file"></param>
    internal static async Task DeleteFileAsync(bool recycle, string file)
    {
        var index = 0;
        if (FolderIndex < Pics.Count && Pics.Count > 0)
        {
            index = Pics.IndexOf(file);
            if (index == FolderIndex)
            {
                await DeleteCurrentFileAsync(recycle).ConfigureAwait(false);
                return;
            }
        }

        var deleteFile = FileDeletionHelper.DeleteFileWithErrorMsg(file, recycle);
        if (!string.IsNullOrWhiteSpace(deleteFile))
        {
            // Show error message to user
            ShowTooltipMessage(deleteFile);
            return;
        }

        try
        {
            Pics.RemoveAt(index);
        }
        catch (Exception exception)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(DeleteFileAsync)} caught exception:\n{exception.Message}");
#endif
            return;
        }
        if (Pics.Count <= 0)
        {
            ErrorHandling.UnexpectedError();
            return;
        }

        await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
        {
            // Sync with gallery
            if (UC.GetPicGallery is not null && UC.GetPicGallery.Container.Children.Count > index)
            {
                UC.GetPicGallery.Container.Children.RemoveAt(index);
            }
        });

        if (PreLoader.Contains(index))
        {
            PreLoader.Clear();
            await PreLoader.PreLoadAsync(FolderIndex, Pics.Count).ConfigureAwait(false);
        }
    }
}