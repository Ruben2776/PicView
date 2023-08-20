using PicView.ChangeTitlebar;
using PicView.FileHandling;
using PicView.ImageHandling;
using PicView.PicGallery;
using PicView.Properties;
using PicView.SystemIntegration;
using PicView.UILogic;
using PicView.UILogic.Sizing;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using static PicView.ChangeImage.Navigation;
using static PicView.FileHandling.DeleteFiles;

namespace PicView.ChangeImage;

internal static class ErrorHandling
{
    /// <summary>
    /// Check if index is valid and user is intended to change image
    /// </summary>
    /// <returns>True if not intended to change image or index is not valid</returns>
    internal static bool CheckOutOfRange()
    {
        bool value = true;
        ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
        {
            value = Pics.Count < FolderIndex || Pics.Count <= 0 ||
                    UC.GetCroppingTool is { IsVisible: true } || (UC.GetQuickResize?.Opacity > 0);
        }));
        return value;
    }

    internal static void UnexpectedError()
    {
        ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Render, () =>
        {
            Unload(true);
            Tooltip.ShowTooltipMessage(Application.Current.Resources["UnexpectedError"]);
            ConfigureWindows.GetMainWindow.Title = (string)Application.Current.Resources["UnexpectedError"] + " - PicView";
            ConfigureWindows.GetMainWindow.TitleText.Text = (string)Application.Current.Resources["UnexpectedError"];
            ConfigureWindows.GetMainWindow.TitleText.ToolTip = (string)Application.Current.Resources["UnexpectedError"];
            ConfigureWindows.GetMainWindow.MainImage.Cursor = Cursors.Arrow;
        });
    }

    internal static bool CheckDirectoryChangeAndPicGallery(FileInfo fileInfo)
    {
        // If count not correct or just started, get values
        if (Pics?.Count <= FolderIndex || FolderIndex < 0)
        {
            return true;
        }
        // If the file is in the same folder, navigate to it. If not, start manual loading procedure.
        if (string.IsNullOrWhiteSpace(Pics?[FolderIndex]) || fileInfo.Directory.FullName == Path.GetDirectoryName(Pics[FolderIndex]))
        {
            return Pics.Contains(fileInfo.FullName) == false;
        }

        // Reset old values and get new
        ChangeFolder(true);
        return true;
    }

    /// <summary>
    /// If url returns "web", if base64 returns "base64" if file, returns file path, if directory returns "directory" else returns empty
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    internal static string CheckIfLoadableString(string s)
    {
        if (!string.IsNullOrWhiteSpace(s.GetURL()))
            return "web";

        if (Base64.IsBase64String(s))
            return "base64";

        if (File.Exists(s))
            return Path.GetExtension(s).IsArchive() ? "zip" : s;

        s = s.Trim().Replace("\"", "");
        if (File.Exists(s))
            return s;

        return Directory.Exists(s) ? "directory" : string.Empty;
    }

    /// <summary>
    /// Clears data, to free objects no longer necessary to store in memory and allow changing folder without error.
    /// </summary>
    internal static void ChangeFolder(bool backup = false)
    {
        if (Pics?.Count > 0 && Pics.Count > FolderIndex && backup)
        {
            // Make a backup of xPicPath and FolderIndex
            if (!string.IsNullOrWhiteSpace(Pics[FolderIndex]))
            {
                BackupPath = Pics[FolderIndex];
            }
        }

        Pics.Clear();
        if (ConfigureWindows.GetMainWindow.CheckAccess())
        {
            GalleryFunctions.Clear();
        }
        else
        {
            ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Background, new Action(GalleryFunctions.Clear));
        }

        PreLoader.Clear();
        DeleteTempFiles();
    }

    /// <summary>
    /// Refresh the current list of pics and reload them if there is some missing or changes.
    /// </summary>
    internal static async Task ReloadAsync(bool fromBackup = false)
    {
        await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(SetTitle.SetLoadingString);
        try
        {
            string path;
            if (Settings.Default.IncludeSubDirectories)
            {
                path = GetReloadPath() ?? BackupPath ?? string.Empty;
            }
            else
            {
                path = (fromBackup ? BackupPath ?? null : GetReloadPath()) ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                UnexpectedError();
            }
            else if (File.Exists(path))
            {
                if (Settings.Default.IncludeSubDirectories)
                {
                    var fileInfo = new FileInfo(Path.GetDirectoryName(path));
                    await ResetValues(fileInfo).ConfigureAwait(false);
                    await LoadPic.LoadPicAtIndexAsync(Pics.IndexOf(path), fileInfo).ConfigureAwait(false);
                }
                else
                {
                    var fileInfo = new FileInfo(path);
                    await ResetValues(fileInfo).ConfigureAwait(false);
                    await LoadPic.LoadPiFromFileAsync(null, fileInfo).ConfigureAwait(false);
                }
            }
            else if (Directory.Exists(path))
            {
                var fileInfo = new FileInfo(path);
                await ResetValues(fileInfo).ConfigureAwait(false);
                await LoadPic.LoadPicFromFolderAsync(fileInfo, FolderIndex).ConfigureAwait(false);
            }
            else if (Base64.IsBase64String(path))
            {
                await UpdateImage.UpdateImageFromBase64PicAsync(path).ConfigureAwait(false);
            }
            else if (Clipboard.ContainsImage())
            {
                await UpdateImage.UpdateImageAsync((string)Application.Current.Resources["ClipboardImage"], Clipboard.GetImage()).ConfigureAwait(false);
            }
            else if (Uri.IsWellFormedUriString(path, UriKind.Absolute)) // Check if from web
            {
                await HttpFunctions.LoadPicFromUrlAsync(path).ConfigureAwait(false);
            }
            else
            {
                UnexpectedError();
            }
        }
        catch (Exception ex)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(ReloadAsync)} exception:\n{ex.Message}");
#endif
            Tooltip.ShowTooltipMessage(ex.Message, true);
        }
    }

    internal static string? GetReloadPath()
    {
        if (CheckOutOfRange())
        {
            return ConfigureWindows.GetMainWindow.Dispatcher.Invoke(() =>
            {
                var fileName = Path.GetFileName(ConfigureWindows.GetMainWindow.TitleText.Text);
                return fileName == (string)Application.Current.Resources["Loading"] ? InitialPath : fileName;
            });
        }

        if (!string.IsNullOrWhiteSpace(InitialPath) && Settings.Default.IncludeSubDirectories
                                                    && Path.GetDirectoryName(InitialPath) != Path.GetDirectoryName(Pics[FolderIndex]))
        {
            return InitialPath;
        }

        return Pics[FolderIndex];
    }

    private static async Task ResetValues(FileInfo fileInfo)
    {
        PreLoader.Clear();
        Pics = await Task.FromResult(FileLists.FileList(fileInfo)).ConfigureAwait(false);

        bool containerCheck = false;

        await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
        {
            if (UC.GetPicGallery?.Container?.Children?.Count > 0)
            {
                containerCheck = true;
            }
        });

        if (ConfigureWindows.GetImageInfoWindow is not null)
        {
            await ImageInfo.UpdateValuesAsync(fileInfo).ConfigureAwait(false);
        }

        if (containerCheck)
        {
            GalleryFunctions.Clear();
            await GalleryLoad.LoadAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Reset to default state
    /// </summary>
    internal static void Unload(bool showStartup)
    {
        ConfigureWindows.GetMainWindow.Dispatcher.Invoke((Action)(() =>
        {
            ConfigureWindows.GetMainWindow.TitleText.ToolTip = ConfigureWindows.GetMainWindow.TitleText.Text =
                (string)Application.Current.Resources["NoImage"];
            ConfigureWindows.GetMainWindow.Title = Application.Current.Resources["NoImage"] + " - " + SetTitle.AppName;
            ConfigureWindows.GetMainWindow.MainImage.Source = null;
            ConfigureWindows.GetMainWindow.MainImage.Width = 0;
            ConfigureWindows.GetMainWindow.MainImage.Height = 0;

            WindowSizing.SetWindowBehavior();

            UC.ToggleStartUpUC(!showStartup);
            if (UC.GetSpinWaiter is not null)
            {
                UC.GetSpinWaiter.Visibility = Visibility.Collapsed;
            }

            if (UC.GetPicGallery is not null)
            {
                UC.GetPicGallery.Visibility = Visibility.Collapsed;
            }
        }));

        Pics?.Clear();

        PreLoader.Clear();
        GalleryFunctions.Clear();
        ScaleImage.XWidth = ScaleImage.XHeight = 0;

        if (!string.IsNullOrWhiteSpace(ArchiveExtraction.TempFilePath))
        {
            DeleteTempFiles();
            ArchiveExtraction.TempFilePath = string.Empty;
        }
        Taskbar.NoProgress();
    }
}