using PicView.ChangeImage;
using PicView.ImageHandling;
using PicView.ProcessHandling;
using PicView.UILogic;
using System.Collections.Specialized;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using static PicView.ChangeImage.Navigation;
using static PicView.UILogic.Tooltip;

namespace PicView.FileHandling;

internal static class CopyPaste
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
            var url = FileFunctions.RetrieveFromURL();
            if (!string.IsNullOrEmpty(url))
            {
                CopyFile(ArchiveExtraction.TempFilePath);
            }
            else
            {
                CopyBitmap();
            }
        }
        else if (Pics?.Count > FolderIndex)
        {
            CopyFile(Pics[FolderIndex]);
        }
    }

    internal static void CopyFile(string path)
    {
        var paths = new StringCollection { path };
        Clipboard.SetFileDropList(paths);
        ShowTooltipMessage(Application.Current.Resources["FileCopy"], UC.FileMenuOpen);
    }

    internal static void CopyBitmap(int? id = null)
    {
        if (id is null)
        {
            BitmapSource? pic = null;
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
            }
            Clipboard.SetImage(pic);
            ShowTooltipMessage(Application.Current.Resources["CopiedImage"]);
        }
        else
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var preloadValue = PreLoader.Get(id.Value);
                    BitmapSource bitmap;
                    if (preloadValue is null)
                    {
                        bitmap = await ImageDecoder.ReturnBitmapSourceAsync(new FileInfo(Pics[id.Value]));
                    }
                    else
                    {
                        bitmap = preloadValue.BitmapSource ?? await ImageDecoder.ReturnBitmapSourceAsync(new FileInfo(Pics[id.Value]));
                    }
                    await UC.GetPicGallery.Dispatcher.InvokeAsync(() =>
                    {
                        Clipboard.SetImage(bitmap);
                        ShowTooltipMessage(Application.Current.Resources["CopiedImage"]);
                    });
                }
                catch (Exception e)
                {
                    ShowTooltipMessage(e.Message);
                }
            }).ConfigureAwait(false);
        }
    }

    internal static void Copy()
    {
        if (ConfigureWindows.GetMainWindow.MainImage.Source == null) return;

        if (ErrorHandling.CheckOutOfRange())
        {
            CopyBitmap();
        }
        else
        {
            if (ConfigureWindows.GetMainWindow.MainImage.Effect is not null)
            {
                CopyBitmap();
            }
            else
            {
                CopyFile(Pics[FolderIndex]);
            }
        }
    }

    /// <summary>
    /// Retrieves the data from the clipboard and attempts to load image, if possible
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
        }
        else // text/string/adddress
        {
            var s = Clipboard.GetText(TextDataFormat.Text);

            if (string.IsNullOrEmpty(s)) { return; }

            var check = ErrorHandling.CheckIfLoadableString(s);
            switch (check)
            {
                case "": return;
                default: await LoadPic.LoadPiFromFileAsync(check).ConfigureAwait(false); return;
                case "web": await HttpFunctions.LoadPicFromUrlAsync(s).ConfigureAwait(false); return;
                case "base64": await UpdateImage.UpdateImageFromBase64PicAsync(s).ConfigureAwait(false); return;
                case "directory": await LoadPic.LoadPicFromFolderAsync(s).ConfigureAwait(false); return;
            }
        }
    }

    /// <summary>
    /// Add file to move/paste clipboard
    /// </summary>
    internal static void Cut(string? path = null)
    {
        string filePath;
        if (path is null)
        {
            if (Pics.Count <= 0 || FolderIndex >= Pics.Count)
            {
                return;
            }

            filePath = Pics[FolderIndex];
        }
        else
        {
            filePath = path;
        }

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