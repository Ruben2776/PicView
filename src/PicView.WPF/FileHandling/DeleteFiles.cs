using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.VisualBasic.FileIO;
using PicView.Core.FileHandling;
using PicView.Core.Localization;
using PicView.Core.Navigation;
using PicView.WPF.ChangeImage;
using PicView.WPF.UILogic;
using static PicView.WPF.ChangeImage.Navigation;
using static PicView.WPF.UILogic.Tooltip;

namespace PicView.WPF.FileHandling
{
    internal static class DeleteFiles
    {
        /// <summary>
        /// Deletes the temporary files when an archived file has been opened
        /// </summary>
        internal static void DeleteTempFiles()
        {
            if (!Directory.Exists(Core.FileHandling.ArchiveExtraction.TempFilePath))
            {
                return;
            }

            try
            {
                Array.ForEach(Directory.GetFiles(Core.FileHandling.ArchiveExtraction.TempFilePath), File.Delete);
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
                Directory.Delete(Core.FileHandling.ArchiveExtraction.TempFilePath);
#if DEBUG
                Trace.WriteLine("Temp zip folder " + Core.FileHandling.ArchiveExtraction.TempFilePath + " deleted");
#endif
            }
            catch (Exception)
            {
                return;
            }

            Core.FileHandling.ArchiveExtraction.TempZipFile = Core.FileHandling.ArchiveExtraction.TempFilePath = null;
        }

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
            var deleteFile = FileDeletionHelper.DeleteFile(fileName, recycle);
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

            ShowTooltipMessage(recycle
                ? TranslationHelper.GetTranslation("SentFileToRecycleBin")
                : TranslationHelper.GetTranslation("Deleted"));

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
        }

        /// Delete file or move it to recycle bin, navigate to next pic
        /// and display information
        /// </summary>
        /// <param name="recycle"></param>
        internal static async Task DeleteFileAsync(bool recycle, int index)
        {
            if (index == FolderIndex)
            {
                await DeleteCurrentFileAsync(recycle).ConfigureAwait(false);
                return;
            }

            var currentFile = Pics.IndexOf(Pics[FolderIndex]);
            var fileName = Pics[index];
            if (index < 0)
            {
                return;
            }

            var deleteFile = FileDeletionHelper.DeleteFile(fileName, recycle);
            if (!string.IsNullOrWhiteSpace(deleteFile))
            {
                // Show error message to user
                ShowTooltipMessage(deleteFile);
                return;
            }

            Pics.RemoveAt(index);
            if (Pics.Count <= 0)
            {
                ErrorHandling.UnexpectedError();
                return;
            }
            FolderIndex = Pics.IndexOf(Pics[currentFile]);

            await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
            {
                // Sync with gallery
                if (UC.GetPicGallery is not null && UC.GetPicGallery.Container.Children.Count > index)
                {
                    UC.GetPicGallery.Container.Children.RemoveAt(index);
                }
            });

            try
            {
                PreLoader.Clear();
            }
            catch (Exception)
            {
                //
            }
            await PreLoader.PreLoadAsync(FolderIndex, Pics.Count).ConfigureAwait(false);
        }
    }
}