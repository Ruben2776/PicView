using PicView.ChangeImage;
using PicView.ImageHandling;
using PicView.UILogic;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
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
        internal static void Copyfile()
        {
            if (Pics == null)
            {
                return;
            }

            if (Pics.Count == 0)
            {
                CopyBitmap();
                return;
            }

            // Copy pic if from web
            if (string.IsNullOrWhiteSpace(Pics[FolderIndex]) || Uri.IsWellFormedUriString(Pics[FolderIndex], UriKind.Absolute))
            {
                CopyBitmap();
            }
            else
            {
                Copyfile(Pics[FolderIndex]);
            }
        }

        /// <summary>
        /// Add file to clipboard
        /// </summary>
        internal static void Copyfile(string path)
        {
            var paths = new System.Collections.Specialized.StringCollection { path };
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
        internal static void Paste()
        {
            // file
            if (Clipboard.ContainsFileDropList()) // If Clipboard has one or more files
            {
                var files = Clipboard.GetFileDropList().Cast<string>().ToArray();

                if (files != null)
                {
                    _ = LoadPic.LoadPicFromStringAsync(files[0]).ConfigureAwait(false);

                    if (files.Length > 1)
                    {
                        for (int i = 1; i < files.Length; i++)
                        {
                            ProcessHandling.ProcessLogic.StartProcessWithFileArgument(files[i]);
                        }
                    }
                    return;
                }
            }

            // Clipboard Image
            if (Clipboard.ContainsImage())
            {
                LoadPic.LoadPicFromBitmap(Clipboard.GetImage(), (string)Application.Current.Resources["ClipboardImage"]);
                return;
            }

            // text/string/adddress

            var s = Clipboard.GetText(TextDataFormat.Text);

            if (string.IsNullOrEmpty(s))
            {
                return;
            }

            string check = ErrorHandling.CheckIfLoadableString(s);
            switch (check)
            {
                default: _ = LoadPic.LoadPiFromFileAsync(check).ConfigureAwait(false); return;
                case "web": _ = WebFunctions.PicWeb(s).ConfigureAwait(false); return;
                case "base64": _ = LoadPic.LoadBase64PicAsync(s).ConfigureAwait(false); return;
                case "directory": _ = LoadPic.LoadPicFromFolderAsync(s).ConfigureAwait(false); return;
                case "": return;
            }
        }

        /// <summary>
        /// Add file to move/paste clipboard
        /// </summary>
        /// <param name="path"></param>
        internal static void Cut()
        {
            if (Pics.Count <= 0 || FolderIndex >= Pics.Count)
            {
                return;
            }

            var x = new System.Collections.Specialized.StringCollection
            {
                Pics[FolderIndex]
            };

            byte[] moveEffect = new byte[] { 2, 0, 0, 0 };
            using (var dropEffect = new MemoryStream())
            {
                dropEffect.Write(moveEffect, 0, moveEffect.Length);

                DataObject data = new();
                data.SetFileDropList(x);
                data.SetData("Preferred DropEffect", dropEffect);

                Clipboard.Clear();
                Clipboard.SetDataObject(data, true);
            }
        }
    }
}