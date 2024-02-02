using Microsoft.Win32;
using PicView.Core.Config;
using PicView.Core.Gallery;
using PicView.Core.Localization;
using PicView.WPF.ChangeImage;
using PicView.WPF.ImageHandling;
using PicView.WPF.PicGallery;
using PicView.WPF.UILogic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using PicView.Core.FileHandling;
using PicView.Core.ProcessHandling;
using static PicView.WPF.ChangeImage.ErrorHandling;
using static PicView.WPF.ChangeImage.Navigation;
using static PicView.WPF.PicGallery.GalleryLoad;
using static PicView.WPF.UILogic.Tooltip;
using static PicView.WPF.UILogic.TransformImage.Rotation;
using static PicView.WPF.UILogic.UC;

namespace PicView.WPF.FileHandling;

internal static class OpenSave
{
    internal static bool IsDialogOpen { get; set; }

    /// <summary>
    ///  Files filtering string used for file/save dialog
    ///  TODO update for and check file support
    /// </summary>
    internal const string FilterFiles =
        "*|*.bmp;*.jpg;*.png;*.tif;*.tiff;*.gif;*.ico;*.jpeg;*.webp;*.qoi;*.psd;*.psb;*.xcf;*.avif;*.jp2;*.hdr;*.heif;*.heif;*.pcd;*.wbmd;*.thm;*.pcd;*.pcx;*.flif;*.kdc;*.cr2;*.cr3;"
        + "|jpg|*.jpg;*.jpeg;" // JPG
        + "|PNG|*.png;" // PNG
        + "|gif|*.gif;" // GIF
        + "|TIFF|*.tif;*.tiff;" // TIF
        + "|ico|*.ico;" // ICO
        + "|svg|*.svg;" // SVG
        + "|webp|*.webp;" // WEBP
        + "|tga|*.tga;" // TGA
        + "|dds|*.dds;" // DDS
        + "|ico|*.ico;" // ICO
        + "|HEIC|*.heic;*.heif;" // HEIC
        + "|HDR|*.hdr;" // HDR
        + "|svg|*.svg;" // SVG
        + "|Photoshop|*.psd;*.psb;" // PSD
        + "|GIMP|*.xcf;" // GIMP
        + "|QOI|*.qoi;" // GQOI (Quite OK Image)
        + "|THM|*.thm;" // THM (Video Thumbnail File)
        + "|Base64|*.b64;" // Base64
        + "|Archives|*.zip;*.7zip;*.7z;*.rar;*.bzip2;*.tar;*.wim;*.iso;*.cab;" // Archives
        + "|Comics|*.cbr;*.cb7;*.cbt;*.cbz;*.xz;" // Comics
        + "|Camera files|*.orf;*.cr2;*.cr3;*.crw;*.dng;*.raf;*.ppm;*.raw;*.mrw;*.nef;*.pef;*.3xf;*.arw;*.pcd;*.pcx;*.flif;*.kdc;"; // Camera files

    /// <summary>
    /// Opens image in File Explorer
    /// </summary>
    internal static void OpenInExplorer(string? file = null)
    {
        try
        {
            string? directory = null;

            if (file == null)
            {
                switch (string.IsNullOrEmpty(file))
                {
                    case false when Pics?.Count <= 0:
                        // Check if from URL and locate it
                        var url = ConfigureWindows.GetMainWindow.TitleText.Text.GetURL();
                        if (!string.IsNullOrEmpty(url))
                        {
                            file = ArchiveHelper.TempFilePath;
                            directory = Path.GetDirectoryName(file);
                        }

                        break;

                    case false when Pics?.Count > FolderIndex:
                        file = Pics[FolderIndex];
                        directory = Path.GetDirectoryName(file);
                        break;

                    default:
                        return;
                }
            }
            else
            {
                directory = Path.GetDirectoryName(file);
            }

            if (file is null || directory is null)
            {
                return;
            }

            Close_UserControls();
            Windows.FileHandling.FileExplorer.OpenFolderAndSelectFile(directory, file);
        }
        catch (Exception e)
        {
            ShowTooltipMessage("OpenInExplorer exception \n" + e.Message);
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
            Title = $"{TranslationHelper.GetTranslation("OpenFileDialog")} - PicView"
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
    internal static void OpenWith(string? path = null)
    {
        if (path == null)
        {
            if (CheckOutOfRange())
            {
                return;
            }

            path = Pics[FolderIndex];
        }

        try
        {
            ProcessHelper.OpenWith(path);
        }
        catch (Exception e)
        {
#if DEBUG
            Trace.WriteLine("OpenWith exception \n" + e.Message);
#endif
            ShowTooltipMessage(e.Message, true);
        }
    }

    internal static async Task SaveFilesAsync()
    {
        await SaveFilesAsync(SettingsHelper.Settings.UIProperties.ShowFileSavingDialog).ConfigureAwait(false);
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
                Title = TranslationHelper.GetTranslation("Save") + " - PicView",
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

        if (Pics.Count > 0 && FolderIndex < Pics.Count)
        {
            success = await SaveImages
                .SaveImageAsync(RotationAngle, IsFlipped, null, Pics[FolderIndex], fileName, null, effectApplied)
                .ConfigureAwait(false);
        }
        else if (source != null)
        {
            success = await SaveImages
                .SaveImageAsync(RotationAngle, IsFlipped, source, null, fileName, null, effectApplied)
                .ConfigureAwait(false);
        }

        if (success == false)
        {
            ShowTooltipMessage(TranslationHelper.GetTranslation("SavingFileFailed"));
        }

        //Reload if same pic to show changes
        if (Pics.Count > 0 && FolderIndex < Pics.Count)
        {
            if (fileName == Pics[FolderIndex])
            {
                PreLoader.Remove(FolderIndex);
                await LoadPic.LoadPicAtIndexAsync(FolderIndex).ConfigureAwait(false);
                if (GetPicGallery is not null)
                {
                    var fileInfo = new FileInfo(Pics[FolderIndex]);
                    var bitmapSource = await Thumbnails.GetBitmapSourceThumbAsync(Pics[FolderIndex],
                        (int)GalleryNavigation.PicGalleryItemSize, fileInfo).ConfigureAwait(false);
                    var thumbData = GalleryThumbInfo.GalleryThumbHolder.GetThumbData(FolderIndex, bitmapSource, fileInfo);
                    await GetPicGallery.Dispatcher.InvokeAsync(() =>
                    {
                        UpdatePic(FolderIndex, bitmapSource,
                            thumbData.FileLocation,
                            thumbData.FileName, thumbData.FileSize,
                            thumbData.FileDate);
                    }, DispatcherPriority.Render);
                }
            }
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

        var folderDialog = new OpenFolderDialog();

        return folderDialog.ShowDialog() == true ? folderDialog.FolderName : null;
    }
}