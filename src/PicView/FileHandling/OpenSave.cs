using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using PicView.ChangeImage;
using PicView.ChangeTitlebar;
using PicView.ImageHandling;
using PicView.UILogic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static PicView.ChangeImage.ErrorHandling;
using static PicView.ChangeImage.Navigation;
using static PicView.UILogic.Tooltip;
using static PicView.UILogic.TransformImage.Rotation;
using static PicView.UILogic.UC;

namespace PicView.FileHandling;

internal static class OpenSave
{
    internal static bool IsDialogOpen { get; set; }

    /// <summary>
    ///  Files filtering string used for file/save dialog
    ///  TODO update for and check file support
    /// </summary>
    internal const string FilterFiles =
        "*|*.bmp;*.jpg;*.png;*.tif;*.tiff;*.gif;*.ico;*.jpeg;*.webp;*.qoi;*.psd;*.psb;*.xcf;*.avif;*.jp2;*.hdr;*.heif;*.heif;*.pcd;*.wbmd;*.thm;*.pcd;*.pcx;*.flif;*.kdc;"
        + "|jpg|*.jpg;*.jpeg;"                                                                                                  // JPG
        + "|PNG|*.png;"                                                                                                         // PNG
        + "|gif|*.gif;"                                                                                                         // GIF
        + "|TIFF|*.tif;*.tiff;"                                                                                                 // TIF
        + "|ico|*.ico;"                                                                                                         // ICO
        + "|svg|*.svg;"                                                                                                         // SVG
        + "|webp|*.webp;"                                                                                                       // WEBP
        + "|tga|*.tga;"                                                                                                         // TGA
        + "|dds|*.dds;"                                                                                                         // DDS
        + "|ico|*.ico;"                                                                                                         // ICO
        + "|HEIC|*.heic;*.heif;"                                                                                                // HEIC
        + "|HDR|*.hdr;"                                                                                                         // HDR
        + "|svg|*.svg;"                                                                                                         // SVG
        + "|Photoshop|*.psd;*.psb;"                                                                                             // PSD
        + "|GIMP|*.xcf;"                                                                                                        // GIMP
        + "|QOI|*.qoi;"                                                                                                         // GQOI (Quite OK Image)
        + "|THM|*.thm;"                                                                                                         // THM (Video Thumbnail File)
        + "|Base64|*.b64;"                                                                                                      // Base64
        + "|Archives|*.zip;*.7zip;*.7z;*.rar;*.bzip2;*.tar;*.wim;*.iso;*.cab;"                                                  // Archives
        + "|Comics|*.cbr;*.cb7;*.cbt;*.cbz;*.xz;"                                                                               // Comics
        + "|Camera files|*.orf;*.cr2;*.crw;*.dng;*.raf;*.ppm;*.raw;*.mrw;*.nef;*.pef;*.3xf;*.arw;*.pcd;*.pcx;*.flif;*.kdc;";    // Camera files

    /// <summary>
    /// Opens image in File Explorer
    /// </summary>
    internal static void Open_In_Explorer()
    {
        string? directory = null, file = null;

        if (Pics?.Count <= 0)
        {
            // Check if from URL and locate it
            var url = FileFunctions.RetrieveFromURL();
            if (!string.IsNullOrEmpty(url))
            {
                file = ArchiveExtraction.TempFilePath;
                directory = Path.GetDirectoryName(file);
            }
        }
        else if (Pics?.Count > FolderIndex)
        {
            file = Pics[FolderIndex];
            directory = Path.GetDirectoryName(file);
        }

        if (file is null || directory is null)
        {
            return;
        }

        try
        {
            Close_UserControls();
            FileExplorer.OpenFolderAndSelectFile(directory, file); // https://stackoverflow.com/a/39427395
        }
        catch (Exception e)
        {
            ShowTooltipMessage("Open_In_Explorer exception \n" + e.Message);
        }
    }

    /// <summary>
    /// Open a file dialog where user can select a supported file
    /// </summary>
    internal static async Task OpenAsync()
    {
        IsDialogOpen = true;

        var dlg = new OpenFileDialog
        {
            Filter = FilterFiles,
            Title = $"{Application.Current.Resources["OpenFileDialog"]} - {SetTitle.AppName}"
        };
        if (dlg.ShowDialog() == true)
        {
            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
            {
                ToggleStartUpUC(true);
                Close_UserControls();
            });
            await LoadPic.LoadPicFromStringAsync(dlg.FileName).ConfigureAwait(false);
            IsDialogOpen = false;
        }
    }

    /// <summary>
    /// Start Windows "Open With" function
    /// </summary>
    internal static void OpenWith()
    {
        if (CheckOutOfRange())
        {
            return;
        }
        try
        {
            using var process = new Process
            {
                StartInfo =
                {
                    FileName = "openwith",
                    Arguments = $"\"{Pics[FolderIndex]}\"",
                    ErrorDialog = true
                }
            };

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

    /// <summary>
    /// Open a File Dialog, where the user can save a supported file type.
    /// </summary>
    internal static async Task SaveFilesAsync(bool showFileDialog)
    {
        if (ConfigureWindows.GetMainWindow.MainImage.Source == null)
        {
            return;
        }

        string fileName;
        var randomized = false;
        SaveFileDialog? saveDialog = null;

        if (Pics?.Count > FolderIndex)
        {
            fileName = showFileDialog ? Path.GetFileName(Pics[FolderIndex]) : Pics[FolderIndex];
        }
        else
        {
            fileName = Path.GetRandomFileName();
            randomized = true;
            showFileDialog = true;
        }

        if (showFileDialog)
        {
            saveDialog = new SaveFileDialog
            {
                Filter = FilterFiles,
                Title = Application.Current.Resources["Save"] + $" - {SetTitle.AppName}",
                FileName = fileName,
            };
            IsDialogOpen = true;

            if (randomized is false)
            {
                saveDialog.InitialDirectory = Path.GetDirectoryName(Pics[FolderIndex]);
            }

            if (!saveDialog.ShowDialog().HasValue)
            {
                IsDialogOpen = false;
                return;
            }
            fileName = saveDialog.FileName;
        }

        var success = false;
        var source = ConfigureWindows.GetMainWindow.MainImage.Source as BitmapSource;
        var effectApplied = ConfigureWindows.GetMainWindow.MainImage.Effect != null;

        if (Pics?.Count > FolderIndex)
        {
            success = await SaveImages.SaveImageAsync(RotationAngle, IsFlipped, null, Pics[FolderIndex], fileName, null, effectApplied).ConfigureAwait(false);
        }
        else if (source != null)
        {
            success = await SaveImages.SaveImageAsync(RotationAngle, IsFlipped, source, null, fileName, null, effectApplied).ConfigureAwait(false);
        }

        if (success == false)
        {
            ShowTooltipMessage(Application.Current.Resources["SavingFileFailed"]);
        }

        //Reload if same pic to show changes
        else if (fileName == Pics[FolderIndex])
        {
            await ReloadAsync().ConfigureAwait(false);
        }

        await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(Close_UserControls);
        IsDialogOpen = false;
    }

    /// <summary>
    /// Sends the file to Windows print system
    /// </summary>
    /// <param name="path">The file path</param>
    internal static void Print(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        if (!File.Exists(path))
        {
            return;
        }

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo(path)
        {
            Verb = "print",
            UseShellExecute = true,
        };
        process.Start();
    }

    internal static string? SelectAndReturnFolder()
    {
        IsDialogOpen = true;

        var dialog = new CommonOpenFileDialog
        {
            IsFolderPicker = true,
        };

        return dialog.ShowDialog() == CommonFileDialogResult.Ok ? dialog.FileName : null;
    }
}