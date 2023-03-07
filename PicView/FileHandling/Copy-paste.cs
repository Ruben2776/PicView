using PicView.ChangeImage;
using PicView.ImageHandling;
using PicView.ProcessHandling;
using PicView.UILogic;
using System.Collections.Specialized;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static PicView.ChangeImage.Navigation;
using static PicView.UILogic.Tooltip;

namespace PicView.FileHandling
{
    internal static class Copy_Paste
    {
        /// <summary>
        /// Copy image location to clipboard
        /// </summary>
        internal static void CopyFilePath()
        {
            Clipboard.SetText(Pics[FolderIndex]);
            ShowTooltipMessage(Application.Current.Resources["FileCopyPathMessage"] as string);
        }

        /// <summary>
        /// Add file to clipboard
        /// </summary>
        internal static void CopyFile()
        {
            if (Pics?.Count <= 0)
            {
                // Check if from URL and download it
                string url = FileFunctions.RetrieveFromURL();
                if (!string.IsNullOrEmpty(url)) 
                {
                    Copyfile(ArchiveExtraction.TempFilePath);
                }
                else
                {
                    CopyBitmap();
                }
            }
            else if (Pics?.Count > FolderIndex)
            {
                Copyfile(Pics[FolderIndex]);
            }
        }

        static void Copyfile(string path)
        {
            var paths = new StringCollection { path };
            Clipboard.SetFileDropList(paths);
            ShowTooltipMessage(Application.Current.Resources["FileCopy"]);
        }

        internal static void CopyBitmap()
        {
            BitmapSource? pic;
            if (ConfigureWindows.GetMainWindow.MainImage.Source != null)
            {
                if (ConfigureWindows.GetMainWindow.MainImage.Effect != null)
                {
                    pic = ImageDecoder.GetRenderedBitmapFrame();
                }
                else
                {
                    pic = (BitmapSource)ConfigureWindows.GetMainWindow.MainImage.Source;
                }

                if (pic == null)
                {
                    ShowTooltipMessage(Application.Current.Resources["UnknownError"]);
                    return;
                }

                Clipboard.SetImage(pic);
            }

            ShowTooltipMessage(Application.Current.Resources["CopiedImage"]);
        }

        /// <summary>
        /// Retrieves the data from the clipboard and attemps to load image, if possible
        /// </summary>
        internal static async Task PasteAsync()
        {
            if (Clipboard.ContainsFileDropList()) // file
            {
                var files = Clipboard.GetFileDropList().Cast<string>().ToArray();

                if (files == null) { return; }

                await LoadPic.LoadPicFromStringAsync(files[0]).ConfigureAwait(false);

                for (int i = 1; i < files.Length; i++) // If Clipboard has more files
                {
                    ProcessLogic.StartProcessWithFileArgument(files[i]);
                }
            }
            else if (Clipboard.ContainsImage())  // Clipboard Image
            {
                await UpdateImage.UpdateImageAsync((string)Application.Current.Resources["ClipboardImage"], Clipboard.GetImage()).ConfigureAwait(false);
                return;
            }
            else // text/string/adddress
            {
                var s = Clipboard.GetText(TextDataFormat.Text);

                if (string.IsNullOrEmpty(s)) { return; }

                string check = ErrorHandling.CheckIfLoadableString(s);
                switch (check)
                {
                    case "": return;
                    default: await LoadPic.LoadPiFromFileAsync(check).ConfigureAwait(false); return;
                    case "web": await HttpFunctions.LoadPicFromURL(s).ConfigureAwait(false); return;
                    case "base64": await UpdateImage.UpdateImageFromBase64PicAsync(s).ConfigureAwait(false); return;
                    case "directory": await LoadPic.LoadPicFromFolderAsync(s).ConfigureAwait(false); return;
                }
            }
        }

        /// <summary>
        /// Add file to move/paste clipboard
        /// </summary>
        internal static void Cut()
        {
            if (Pics.Count <= 0 || FolderIndex >= Pics.Count)
            {
                return;
            }

            var filePath = Pics[FolderIndex];
            var fileDropList = new StringCollection { filePath };

            var moveEffect = new byte[] { 2, 0, 0, 0 };
            var dropEffect = new MemoryStream();
            dropEffect.Write(moveEffect, 0, moveEffect.Length);

            var data = new DataObject();
            data.SetFileDropList(fileDropList);
            data.SetData("Preferred DropEffect", dropEffect);

            Clipboard.Clear();
            Clipboard.SetDataObject(data, true);
            ShowTooltipMessage(Application.Current.Resources["FileCutMessage"]);
        }
    }
}