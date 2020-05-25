using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using static PicView.Error_Handling;
using static PicView.Fields;
using static PicView.ImageDecoder;
using static PicView.Navigation;
using static PicView.ToggleMenus;
using static PicView.Tooltip;

namespace PicView
{
    internal static class Open_Save
    {
        /// <summary>
        /// Opens image in File Explorer
        /// </summary>
        internal static void Open_In_Explorer()
        {
            if (!File.Exists(Pics[FolderIndex]) || mainWindow.img.Source == null)
            {
                ToolTipStyle("Error, File does not exist, or something went wrong...");
                return;
            }
            try
            {
                Close_UserControls();
                ToolTipStyle(ExpFind);
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
            dialogOpen = true;

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
                    ToolTipStyle(e.Message, true);
                }
                
            }
        }

        /// <summary>
        /// Open a File Dialog, where the user can save a supported file type.
        /// </summary>
        internal static void SaveFiles()
        {
            dialogOpen = true;

            var Savedlg = new SaveFileDialog()
            {
                Filter = FilterFiles,
                Title = "Save image - PicView",
                FileName = Path.GetFileName(Pics[FolderIndex])
            };

            if (!string.IsNullOrEmpty(Pics[FolderIndex]))
            {
                if (Savedlg.ShowDialog().Value)
                {
                    if (!TrySaveImage(Rotateint, Flipped, Pics[FolderIndex], Savedlg.FileName))
                    {
                        ToolTipStyle("Error, File didn't get saved");
                    }
                }
                else
                {
                    return;
                }

                //Refresh the list of pictures.
                Reload();

                Close_UserControls();
                dialogOpen = false;
            }
        }
    }
}
