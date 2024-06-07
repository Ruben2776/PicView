using PicView.Core.FileHandling;
using PicView.Core.Localization;
using PicView.Core.Navigation;
using PicView.Core.ProcessHandling;
using PicView.WPF.ChangeImage;
using PicView.WPF.ChangeTitlebar;
using PicView.WPF.ImageHandling;
using PicView.WPF.UILogic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static PicView.WPF.ChangeImage.Navigation;
using static PicView.WPF.UILogic.Tooltip;

namespace PicView.WPF.FileHandling;

internal static class CopyPaste
{
    /// <summary>
    /// Duplicates the current file and handles naming collisions by appending a number inside parentheses.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task DuplicateFile()
    {
        if (ErrorHandling.CheckOutOfRange())
        {
            return;
        }

        await DuplicateFile(Pics[FolderIndex]).ConfigureAwait(false);
    }

    /// <summary>
    /// Duplicates the specified file and handles naming collisions by appending a number inside parentheses.
    /// </summary>
    /// <param name="currentFile">The path of the file to be duplicated.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private static async Task DuplicateFile(string currentFile)
    {
        // Display it's loading to the user
        await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(SetTitle.SetLoadingString);

        try
        {
            await Task.Run(() => FileHelper.DuplicateAndReturnFileName(currentFile)).ConfigureAwait(false);
            // File system watcher takes care of updating the UI
        }
        catch (Exception exception)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(DuplicateFile)} {currentFile} exception:\n{exception.Message}");
#endif
            ShowTooltipMessage(exception.Message);
        }
        finally
        {
            // Revert to the previous title since it's no longer loading
            await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(SetTitle.SetTitleString);
        }
    }

    /// <summary>
    /// Copy image location to clipboard
    /// </summary>
    internal static void CopyFilePath()
    {
        try
        {
            Clipboard.SetText(Pics[FolderIndex]);
            ShowTooltipMessage(TranslationHelper.GetTranslation("FileCopyPathMessage"));
        }
        catch (Exception exception)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(CopyFilePath)} exception:\n{exception.Message}");
#endif
            ShowTooltipMessage(exception.Message);
        }
    }

    /// <summary>
    /// Add file to clipboard
    /// </summary>
    internal static void CopyFile()
    {
        if (Pics?.Count <= 0)
        {
            // Check if from URL and download it
            var url = ConfigureWindows.GetMainWindow.TitleText.Text.GetURL();
            if (!string.IsNullOrEmpty(url))
            {
                CopyFile(ArchiveHelper.TempFilePath);
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
        ShowTooltipMessage(TranslationHelper.GetTranslation("FileCopy"), UC.FileMenuOpen);
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
                    pic = Image2BitmapSource.GetRenderedBitmapFrame();
                }
                else
                {
                    pic = (BitmapSource)ConfigureWindows.GetMainWindow.MainImage.Source;
                }

                if (pic == null)
                {
                    ShowTooltipMessage(TranslationHelper.GetTranslation("UnknownError"));
                    return;
                }
            }

            Set(pic);
            ShowTooltipMessage(TranslationHelper.GetTranslation("CopiedImage"));
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
                        bitmap = await Image2BitmapSource.ReturnBitmapSourceAsync(new FileInfo(Pics[id.Value]));
                    }
                    else
                    {
                        bitmap = preloadValue.BitmapSource ??
                                 await Image2BitmapSource.ReturnBitmapSourceAsync(new FileInfo(Pics[id.Value]));
                    }

                    try
                    {
                        Set(bitmap);
                    }
                    catch (Exception e)
                    {
                        ShowTooltipMessage(e.Message);
                    }

                    ShowTooltipMessage(TranslationHelper.GetTranslation("CopiedImage"));
                }
                catch (Exception e)
                {
                    ShowTooltipMessage(e.Message);
                }
            }).ConfigureAwait(false);
        }

        return;

        void Set(BitmapSource source)
        {
            ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
            {
                var bmp = ImageFunctions.BitmapSourceToBitmap(source);
                ClipboardHelper.SetClipboardImage(bmp, bmp, null);
                ShowTooltipMessage(TranslationHelper.GetTranslation("CopiedImage"));
            }));
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

            if (files == null)
            {
                return;
            }
            await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
            {
                CloseToolTipMessage();

                ConfigureWindows.GetMainWindow.Activate();
                if (UC.GetStartUpUC is not null)
                {
                    UC.GetStartUpUC.Visibility = Visibility.Collapsed;
                }
            });

            await LoadPic.LoadPicFromStringAsync(files[0]).ConfigureAwait(false);

            for (var i = 1; i < files.Length; i++) // If Clipboard has more files
            {
                ProcessHelper.StartNewProcess(files[i]);
            }
        }
        else if (Clipboard.ContainsImage()) // Clipboard Image
        {
            await UpdateImage
                .UpdateImageAsync(TranslationHelper.GetTranslation("ClipboardImage"), Clipboard.GetImage())
                .ConfigureAwait(false);
        }
        else // text/string/adddress
        {
            var s = Clipboard.GetText(TextDataFormat.Text);

            if (string.IsNullOrEmpty(s))
            {
                return;
            }
            
            await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
            {
                CloseToolTipMessage();

                ConfigureWindows.GetMainWindow.Activate();
                if (UC.GetStartUpUC is not null)
                {
                    UC.GetStartUpUC.Visibility = Visibility.Collapsed;
                }
            });


            var check = ErrorHelper.CheckIfLoadableString(s);
            switch (check)
            {
                case "":
                    await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
                    {
                        ErrorHandling.Unload(ConfigureWindows.GetMainWindow.MainImage.Source is null);
                    });
                   
                    return;
                default:
                    await LoadPic.LoadPiFromFileAsync(check).ConfigureAwait(false);
                    return;

                case "web":
                    await HttpFunctions.LoadPicFromUrlAsync(s).ConfigureAwait(false);
                    return;

                case "base64":
                    await UpdateImage.UpdateImageFromBase64PicAsync(s).ConfigureAwait(false);
                    return;

                case "directory":
                    await LoadPic.LoadPicFromFolderAsync(s).ConfigureAwait(false);
                    return;
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
        ShowTooltipMessage(TranslationHelper.GetTranslation("FileCutMessage"));
    }
}