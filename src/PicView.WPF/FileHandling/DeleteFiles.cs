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
    internal static void DeleteCurrentFile(bool recycle)
    {
        if (ErrorHandling.CheckOutOfRange())
        {
            return;
        }

        var fileName = Pics[FolderIndex];
        var deleteFile = FileDeletionHelper.DeleteFileWithErrorMsg(fileName, recycle);
        if (!string.IsNullOrWhiteSpace(deleteFile))
        {
            // Show error message to user
            ShowTooltipMessage(deleteFile);
            return;
        }

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
    internal static void DeleteFile(bool recycle, string file)
    {
        if (FolderIndex < Pics.Count && Pics.Count > 0)
        {
            var index = Pics.IndexOf(file);
            if (index == FolderIndex)
            {
                DeleteCurrentFile(recycle);
                return;
            }
        }

        var deleteFile = FileDeletionHelper.DeleteFileWithErrorMsg(file, recycle);
        if (string.IsNullOrWhiteSpace(deleteFile))
        {
            return;
        }

        // Show error message to user
        ShowTooltipMessage(deleteFile);
    }
}