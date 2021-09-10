using PicView.ChangeImage;
using PicView.ImageHandling;
using PicView.UILogic;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using static PicView.ChangeImage.Error_Handling;
using static PicView.ChangeImage.Navigation;
using static PicView.FileHandling.FileFunctions;
using static PicView.FileHandling.FileLists;
using static PicView.UILogic.Tooltip;

namespace PicView.FileHandling
{
    internal static class Copy_Paste
    {
        /// <summary>
        /// Copy image location to clipboard
        /// </summary>
        internal static async Task CopyTextAsync()
        {
            Clipboard.SetText(Pics[FolderIndex]);
            await ShowTooltipMessage(Application.Current.Resources["FileCopyPath"] as string).ConfigureAwait(false);
        }

        /// <summary>
        /// Add file to clipboard
        /// </summary>
        internal static async Task CopyfileAsync()
        {
            if (Pics == null)
            {
                return;
            }

            if (Pics.Count == 0)
            {
                await CopyBitmapAsync().ConfigureAwait(false);
                return;
            }

            // Copy pic if from web
            if (string.IsNullOrWhiteSpace(Pics[FolderIndex]) || Uri.IsWellFormedUriString(Pics[FolderIndex], UriKind.Absolute))
            {
                await CopyBitmapAsync().ConfigureAwait(false);
            }
            else
            {
                await CopyfileAsync(Pics[FolderIndex]).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Add file to clipboard
        /// </summary>
        internal static async Task CopyfileAsync(string path)
        {
            var paths = new System.Collections.Specialized.StringCollection { path };
            Clipboard.SetFileDropList(paths);
            await ShowTooltipMessage(Application.Current.Resources["FileCopy"]).ConfigureAwait(false);
        }

        internal static async Task CopyBitmapAsync()
        {
            BitmapSource pic;
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

                Clipboard.SetImage(pic);
            }

            await ShowTooltipMessage(Application.Current.Resources["CopiedImage"]).ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieves the data from the clipboard and attemps to load image, if possible
        /// </summary>
        internal static async Task PasteAsync()
        {
            // file

            if (Clipboard.ContainsFileDropList()) // If Clipboard has one or more files
            {
                var files = Clipboard.GetFileDropList().Cast<string>().ToArray();

                if (files != null)
                {
                    var firstFile = files[0];

                    if (Pics.Count != 0)
                    {
                        // If from same folder
                        if (!string.IsNullOrWhiteSpace(Pics[FolderIndex]) && Path.GetDirectoryName(firstFile) == Path.GetDirectoryName(Pics[FolderIndex]))
                        {
                            await LoadPicAt(Pics.IndexOf(firstFile)).ConfigureAwait(false);
                        }
                        else
                        {
                            if (FileFunctions.CheckIfDirectoryOrFile(firstFile))
                            {
                                await LoadPicFromFolderAsync(firstFile).ConfigureAwait(false);
                            }
                            else
                            {
                                await LoadPiFromFileAsync(firstFile).ConfigureAwait(false);
                            }
                        }
                    }
                    else
                    {
                        await LoadPiFromFileAsync(firstFile).ConfigureAwait(false);
                    }

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
                Pic(Clipboard.GetImage(), (string)Application.Current.Resources["ClipboardImage"]);
                return;
            }

            // text/string/adddress

            var s = Clipboard.GetText(TextDataFormat.Text);

            if (string.IsNullOrEmpty(s))
            {
                return;
            }

            if (Base64.IsBase64String(s))
            {
                await Pic64(s).ConfigureAwait(false);
                return;
            }

            if (FilePathHasInvalidChars(s))
            {
                MakeValidFileName(s);
            }

            s = s.Replace("\"", "");
            s = s.Trim();

            if (File.Exists(s))
            {
                await LoadPiFromFileAsync(s).ConfigureAwait(false);
            }
            else if (Directory.Exists(s))
            {
                ChangeFolder();
                Pics = FileList(s);
                if (Pics.Count > 0)
                {
                    await LoadPiFromFileAsync(Pics[0]).ConfigureAwait(false);
                }
                else if (Pics.Count == 0)
                {
                    Unload();
                }
                else if (!string.IsNullOrWhiteSpace(Pics[FolderIndex]))
                {
                    await LoadPiFromFileAsync(Pics[FolderIndex]).ConfigureAwait(false);
                }
                else
                {
                    Unload();
                }
            }
            else if (Uri.IsWellFormedUriString(s, UriKind.Absolute)) // Check if from web
            {
                await WebFunctions.PicWeb(s).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Add file to move/paste clipboard
        /// </summary>
        /// <param name="path"></param>
        internal static async Task CutAsync(string path)
        {
            var x = new System.Collections.Specialized.StringCollection
            {
                path
            };

            byte[] moveEffect = new byte[] { 2, 0, 0, 0 };
            using (var dropEffect = new MemoryStream())
            {
                dropEffect.Write(moveEffect, 0, moveEffect.Length);

                DataObject data = new DataObject();
                data.SetFileDropList(x);
                data.SetData("Preferred DropEffect", dropEffect);

                Clipboard.Clear();
                Clipboard.SetDataObject(data, true);
            }

            await ShowTooltipMessage(Application.Current.Resources["FileCut"] as string).ConfigureAwait(false);
        }
    }
}