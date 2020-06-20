using Microsoft.Win32;
using PicView.ImageHandling;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Media.Imaging;
using static PicView.ChangeImage.Error_Handling;
using static PicView.ChangeImage.Navigation;
using static PicView.Library.Fields;
using static PicView.UI.Tooltip;
using static PicView.UI.UserControls.UC;

namespace PicView.FileHandling
{
    internal static class Open_Save
    {
        /// <summary>
        /// Opens image in File Explorer
        /// </summary>
        internal static void Open_In_Explorer()
        {

            if (Pics.Count > 0)
            {
                if (Pics.Count < FolderIndex)
                {
                    return;
                }
            }
            else return;

            if (!File.Exists(Pics[FolderIndex]) || mainWindow.img.Source == null)
            {
                return;
            }
            try
            {
                Close_UserControls();
                ShowTooltipMessage(ExpFind);
                Process.Start("explorer.exe", "/select,\"" + Pics[FolderIndex] + "\"");
            }
#if DEBUG
            catch (InvalidCastException e)
            {
                Trace.WriteLine("Open_In_Explorer exception \n" + e.Message);
            }
#else
            catch (InvalidCastException) { }
#endif
        }

        /// <summary>
        /// Open a file dialog where user can select a supported file
        /// </summary>
        internal static void Open()
        {
            IsDialogOpen = true;

            var dlg = new OpenFileDialog()
            {
                Filter = FilterFiles,
                Title = "Open image - PicView"
            };
            if (dlg.ShowDialog().Value)
            {
                Pic(dlg.FileName);
            }
            else
            {
                return;
            }

            Close_UserControls();
        }

        /// <summary>
        /// Start Windows "Open With" function
        /// </summary>
        /// <param name="file">The absolute path to the file</param>
        internal static void OpenWith(string file)
        {
            using (var process = new Process())
            {
                process.StartInfo.FileName = "openwith";
                process.StartInfo.Arguments = $"\"{file}\"";
                process.StartInfo.ErrorDialog = true;
                try
                {
                    process.Start();
                }
                catch (Exception e)
                {
#if DEBUG
                    Trace.WriteLine("OpenWith exception \n" + e.Message);

#endif
                    ShowTooltipMessage(e.Message, true);
                }
            }
        }

        /// <summary>
        /// Open a File Dialog, where the user can save a supported file type.
        /// </summary>
        internal static void SaveFiles()
        {
            string fileName;

            if (Pics.Count > 0)
            {
                if (string.IsNullOrEmpty(Pics[FolderIndex])) return;
                fileName = Path.GetFileName(Pics[FolderIndex]);
            }
            else
            {
                fileName = Path.GetRandomFileName();
            }

            var Savedlg = new SaveFileDialog()
            {
                Filter = FilterFiles,
                Title = "Save image - PicView",
                FileName = fileName
            };

            if (!Savedlg.ShowDialog().Value) return;

            IsDialogOpen = true;

            if (Pics.Count > 0)
            {
                if (!SaveImages.TrySaveImage(Rotateint, Flipped, Pics[FolderIndex], Savedlg.FileName))
                {
                    ShowTooltipMessage("Saving file failed");
                }
            }
            else
            {
                if (!SaveImages.TrySaveImage(Rotateint, Flipped, mainWindow.img.Source as BitmapSource, Savedlg.FileName))
                {
                    ShowTooltipMessage("Saving file failed");
                }
            }

            if (Savedlg.FileName == fileName)
            {
                //Refresh the list of pictures.
                Reload();
            }

            Close_UserControls();
            IsDialogOpen = false;
        }

        /// <summary>
        /// Sends the file to Windows print system
        /// </summary>
        /// <param name="path">The file path</param>
        internal static bool Print(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            if (!File.Exists(path))
            {
                return false;
            }

            using (var p = new Process())
            {
                p.StartInfo.FileName = path;
                p.StartInfo.Verb = "print";
                p.StartInfo.UseShellExecute = true;
                p.Start();
            }
            return true;
        }
    }
}