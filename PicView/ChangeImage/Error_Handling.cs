using PicView.ChangeTitlebar;
using PicView.FileHandling;
using PicView.ImageHandling;
using PicView.PicGallery;
using PicView.Properties;
using PicView.SystemIntegration;
using PicView.UILogic;
using PicView.UILogic.Sizing;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using static PicView.ChangeImage.Navigation;
using static PicView.FileHandling.DeleteFiles;

namespace PicView.ChangeImage
{
    internal static class ErrorHandling
    {
        /// <summary>
        /// Returns true, if navigating will result in out of rang exception
        /// </summary>
        /// <returns></returns>
        internal static bool CheckOutOfRange()
        {
            bool value = true;
            ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Render, (Action)(() =>
            {
                value = Pics.Count < FolderIndex || Pics.Count < 1 || UC.GetCropppingTool is not null and { IsVisible: true }
                || (UC.GetQuickResize?.Opacity > 0);
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
                ConfigureWindows.GetMainWindow.MainImage.Cursor = System.Windows.Input.Cursors.Arrow;
            });
        }

        internal static bool CheckDirectoryChangeAndPicGallery(FileInfo fileInfo)
        {
            bool folderChanged = false;

            // If count not correct or just started, get values
            if (Pics?.Count <= FolderIndex || FolderIndex < 0)
            {
                folderChanged = true;
            }
            // If the file is in the same folder, navigate to it. If not, start manual loading procedure.
            else if (!string.IsNullOrWhiteSpace(Pics?[FolderIndex]) && fileInfo.Directory.FullName != Path.GetDirectoryName(Pics[FolderIndex]))
            {
                // Reset old values and get new
                ChangeFolder(true);
                folderChanged = true;
            }

            if (Pics.Contains(fileInfo.FullName) == false)
            {
                folderChanged = true;
            }

            if (UC.GetPicGallery is null || folderChanged is false) { return folderChanged; }

            if (Settings.Default.FullscreenGalleryHorizontal)
            {
                ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Render, (Action)(() =>
                {
                    // Remove children before loading new
                    if (UC.GetPicGallery.Container.Children.Count > 0)
                    {
                        UC.GetPicGallery.Container.Children.Clear();
                    }
                }));
            }
            return folderChanged;
        }

        /// <summary>
        /// If url returns "web", if base64 returns "base64" if file, returns file path, if directory returns "directory" else returns empty
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        internal static string CheckIfLoadableString(string s)
        {
            if (!string.IsNullOrWhiteSpace(FileHandling.FileFunctions.GetURL(s)))
            {
                return "web";
            }

            if (Base64.IsBase64String(s))
            {
                return "base64";
            }

            if (File.Exists(s))
            {
                if (SupportedFiles.IsArchive(Path.GetExtension(s)))
                {
                    return "zip";
                }
                return s;
            }
            else
            {
                s = s.Trim().Replace("\"", "");
                if (File.Exists(s))
                {
                    return s;
                }
            }

            if (Directory.Exists(s))
            {
                return "directory";
            }

            return string.Empty;
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
                ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                {
                    GalleryFunctions.Clear();
                }));
            }

            Preloader.Clear();
            DeleteTempFiles();
        }

        /// <summary>
        /// Refresh the current list of pics and reload them if there is some missing or changes.
        /// </summary>
        internal static async Task ReloadAsync(bool fromBackup = false)
        {
            string? path = fromBackup
                ? BackupPath ?? null
                : GetReloadPath();
            if (path == null)
            {
                UnexpectedError();
            }
            if (File.Exists(path))
            {
                var fileInfo = new FileInfo(path);
                await ResetValues(fileInfo).ConfigureAwait(false);
                await LoadPic.LoadPiFromFileAsync(null, fileInfo).ConfigureAwait(false);
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
                await HttpFunctions.LoadPicFromURL(path).ConfigureAwait(false);
            }
            else
            {
                UnexpectedError();
            }

            string? GetReloadPath()
            {
                if (CheckOutOfRange() == false)
                {
                    if (string.IsNullOrWhiteSpace(InitialPath) == false
                        && Settings.Default.IncludeSubDirectories
                        && Path.GetDirectoryName(InitialPath) != Path.GetDirectoryName(Pics[FolderIndex]))
                    {
                        return InitialPath;
                    }
                    else
                    {
                        return Pics[FolderIndex];
                    }
                }
                else
                {
                    return ConfigureWindows.GetMainWindow?.Dispatcher.Invoke(() =>
                    {
                        var path = Path.GetFileName(ConfigureWindows.GetMainWindow.TitleText.Text);
                        if (path == (string)Application.Current.Resources["Loading"])
                        {
                            return InitialPath;
                        }
                        return path;
                    });
                }
            }
        }

        private static async Task ResetValues(FileInfo fileInfo)
        {
            if (fileInfo is null)
            {
                UnexpectedError();
                return;
            }

            Preloader.Clear();
            Pics = FileLists.FileList(fileInfo);

            bool containerCheck = false;

            await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
            {
                if (UC.GetPicGallery?.Container?.Children?.Count > 0)
                {
                    containerCheck = true;
                }
            });

            if (containerCheck)
            {
                await GalleryLoad.LoadAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Reset to default state
        /// </summary>
        internal static void Unload(bool showStartup)
        {
            ConfigureWindows.GetMainWindow.TitleText.ToolTip = ConfigureWindows.GetMainWindow.TitleText.Text = (string)Application.Current.Resources["NoImage"];
            ConfigureWindows.GetMainWindow.Title = Application.Current.Resources["NoImage"] + " - " + SetTitle.AppName;
            ConfigureWindows.GetMainWindow.MainImage.Source = null;
            ConfigureWindows.GetMainWindow.MainImage.Width = 0;
            ConfigureWindows.GetMainWindow.MainImage.Height = 0;

            WindowSizing.SetWindowBehavior();

            UC.ToggleStartUpUC(!showStartup);
            Pics?.Clear();

            Preloader.Clear();
            GalleryFunctions.Clear();
            ScaleImage.XWidth = ScaleImage.XHeight = 0;

            if (!string.IsNullOrWhiteSpace(ArchiveExtraction.TempFilePath))
            {
                DeleteTempFiles();
                ArchiveExtraction.TempFilePath = string.Empty;
            }

            try
            {
               Taskbar.NoProgress();
            }
            catch
            {
                // ignored
            }
        }
    }
}