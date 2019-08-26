using PicView.PreLoading;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using static PicView.Error_Handling;
using static PicView.FileFunctions;
using static PicView.FileLists;
using static PicView.Fields;
using static PicView.Navigation;
using static PicView.Interface;

namespace PicView
{
    internal static class Copy_Paste
    {
        /// <summary>
        /// Copy image location to clipboard
        /// </summary>
        internal static void CopyText()
        {
            Clipboard.SetText(PicPath);
            ToolTipStyle(TxtCopy);
        }

        /// <summary>
        /// Add image to clipboard
        /// </summary>
        internal static void CopyPic()
        {
            // Copy pic if from web
            if (string.IsNullOrWhiteSpace(PicPath) || Uri.IsWellFormedUriString(PicPath, UriKind.Absolute))
            {
                CopyBitmap();
            }
            else
            {
                var paths = new System.Collections.Specialized.StringCollection { PicPath };
                Clipboard.SetFileDropList(paths);
                ToolTipStyle(FileCopy);
            }
        }

        internal static void CopyBitmap()
        {
            if (Preloader.Contains(PicPath))
                Clipboard.SetImage(Preloader.Load(PicPath));
            else if (mainWindow.img.Source != null)
                Clipboard.SetImage((BitmapSource)mainWindow.img.Source);
            else
                return;

            ToolTipStyle("Copied Image to clipboard");
        }

        /// <summary>
        /// Retrieves the data from the clipboard and attemps to load image, if possible
        /// </summary>
        internal static void Paste()
        {
            // file

            if (Clipboard.ContainsFileDropList()) // If Clipboard has one or more files
            {
                var files = Clipboard.GetFileDropList().Cast<string>().ToArray();

                if (!string.IsNullOrWhiteSpace(PicPath) &&
                    Path.GetDirectoryName(files[0]) == Path.GetDirectoryName(PicPath))
                    Pic(Pics.IndexOf(files[0]));
                else
                    Pic(files[0]);

                if (files.Length > 0)
                {
                    Parallel.For(1, files.Length, x =>
                    {
                        var myProcess = new Process
                        {
                            StartInfo = { FileName = Assembly.GetExecutingAssembly().Location, Arguments = files[x] }
                        };
                        myProcess.Start();
                    });
                }

                return;
            }

            // Clipboard Image
            if (Clipboard.ContainsImage())
            {
                Pic(Clipboard.GetImage(), "Clipboard Image");
                return;
            }

            // text/string/adddress

            var s = Clipboard.GetText(TextDataFormat.Text);

            if (string.IsNullOrEmpty(s))
                return;

            if (FilePathHasInvalidChars(s))
                MakeValidFileName(s);

            s = s.Replace("\"", "");
            s = s.Trim();

            if (File.Exists(s))
            {
                Pic(s);
            }
            else if (Directory.Exists(s))
            {
                ChangeFolder();
                Pics = FileList(s);
                if (Pics.Count > 0)
                    Pic(Pics[0]);
                else if (!string.IsNullOrWhiteSpace(PicPath))
                    Pic(PicPath);
                else
                    Unload();
            }
            else if (Uri.IsWellFormedUriString(s, UriKind.Absolute)) // Check if from web
                PicWeb(s);
            else
                ToolTipStyle("An error occured while trying to paste file");
        }

        /// <summary>
        /// Add file to move/paste clipboard
        /// </summary>
        /// <param name="path"></param>
        internal static void Cut(string path)
        {
            var x = new System.Collections.Specialized.StringCollection
            {
                path
            };

            byte[] moveEffect = new byte[] { 2, 0, 0, 0 };
            MemoryStream dropEffect = new MemoryStream();
            dropEffect.Write(moveEffect, 0, moveEffect.Length);

            DataObject data = new DataObject();
            data.SetFileDropList(x);
            data.SetData("Preferred DropEffect", dropEffect);

            Clipboard.Clear();
            Clipboard.SetDataObject(data, true);

            PreloadCount = 4;

            ToolTipStyle("Added Image to move clipboard");
        }
    }
}
