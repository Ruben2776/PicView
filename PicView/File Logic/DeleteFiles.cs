using Microsoft.VisualBasic.FileIO;
using System;
using System.IO;
using static PicView.Error_Handling;
using static PicView.Helper;
using static PicView.Interface;
using static PicView.Navigation;
using static PicView.Fields;


namespace PicView
{
    internal static class DeleteFiles
    {
        /// <summary>
        /// Deletes the temporary files when an archived file has been opened
        /// </summary>
        internal static void DeleteTempFiles()
        {
            if (!Directory.Exists(TempZipPath))
                return;
            try
            {
                Array.ForEach(Directory.GetFiles(TempZipPath), File.Delete);
            }
            catch (Exception)
            {
                return;
            }

            try
            {
                Directory.Delete(TempZipPath);
            }
            catch (Exception)
            {
                return;
            }
        }

        /// <summary>
        /// Deletes file or send it to recycle bin
        /// </summary>
        /// <param name="file"></param>
        /// <param name="Recycle"></param>
        /// <returns></returns>
        internal static bool TryDeleteFile(string file, bool Recycle)
        {
            /// Need to add function to remove from PicGallery
            if (!File.Exists(file))
                return false;

            try
            {
                var recycle = Recycle ? RecycleOption.SendToRecycleBin : RecycleOption.DeletePermanently;
                FileSystem.DeleteFile(file, UIOption.OnlyErrorDialogs, recycle);
                Pics.Remove(file);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Delete file or move it to recycle bin, navigate to next pic
        /// and display information
        /// </summary>
        /// <param name="Recyclebin"></param>
        internal static void DeleteFile(string file, bool Recyclebin)
        {
            if (TryDeleteFile(file, Recyclebin))
            {
                var filename = Path.GetFileName(file);
                Pics.Remove(filename);

                filename = filename.Length >= 25 ? Shorten(filename, 21) : filename;
                ToolTipStyle(Recyclebin ? "Sent " + filename + " to the recyle bin" : "Deleted " + filename);

                if (reverse)
                    FolderIndex = FolderIndex-- < 0 ? 0 : FolderIndex--;

                if (FolderIndex > Pics.Count || Pics.Count < 0)
                    Unload();
                else
                    // Go to next image
                    Pic(FolderIndex);
            }
            else
            {
                ToolTipStyle("An error occured when deleting " + file);
            }
        }
    }
}
