using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using PicView.ChangeTitlebar;
using PicView.FileHandling;
using PicView.ImageHandling;
using PicView.PicGallery;
using PicView.Properties;
using PicView.SystemIntegration;
using PicView.UILogic;
using PicView.UILogic.Sizing;
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
            return Pics?.Count < FolderIndex || Pics?.Count < 1 || UC.GetCropppingTool is { IsVisible: true };
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
            });
        }

        internal static async Task<bool> CheckDirectoryChangeAndPicGallery(FileInfo fileInfo)
        {
            bool folderChanged = false;

            // If count not correct or just started, get values
            if (Pics?.Count <= FolderIndex || FolderIndex < 0 || FreshStartup)
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
            
            if (Settings.Default.FullscreenGalleryHorizontal || Settings.Default.FullscreenGalleryVertical)
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Render, (Action)(() =>
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
            bool result = Uri.TryCreate(s, UriKind.Absolute, out Uri? uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

            if (result)
            {
                return "web";
            }

            if (Base64.IsBase64String(s))
            {
                return "base64";
            }

            s = s.Replace("\"", "");
            s = s.Trim();

            if (File.Exists(s))
            {
                return s;
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
            FreshStartup = true;
            DeleteTempFiles();
        }

        /// <summary>
        /// Refresh the current list of pics and reload them if there is some missing or changes.
        /// </summary>
        internal static async Task ReloadAsync(bool fromBackup = false)
        {
            await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(SetTitle.SetLoadingString);

            string? path = null;
            if (fromBackup)
            {
                if (string.IsNullOrWhiteSpace(BackupPath))
                {
                    UnexpectedError();
                    return;
                }
                path = BackupPath;
            }
            else if (CheckOutOfRange() == false)
            {
                // Determine if browsing directories recursively or only update from single directory
                if (string.IsNullOrWhiteSpace(InitialPath) == false
                    && Settings.Default.IncludeSubDirectories
                    && Path.GetDirectoryName(InitialPath) != Path.GetDirectoryName(Pics[FolderIndex]))
                {
                    path = InitialPath;
                }
                else
                {
                    path = Pics[FolderIndex];
                }
            }
            else
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(() =>
                {
                    path = Path.GetFileName(ConfigureWindows.GetMainWindow.TitleText.Text);
                });

                if (path == (string)Application.Current.Resources["Loading"])
                {
                    path = InitialPath;
                }
            }

            if (path == null)
            {
                UnexpectedError();
                return;
            }

            if (File.Exists(path))
            {
                var fileInfo = new FileInfo(path);
                await ResetValues(fileInfo).ConfigureAwait(false);
                await LoadPic.LoadPiFromFileAsync(fileInfo).ConfigureAwait(false);
            }
            else if (Directory.Exists(path))
            {
                int index = 0;
                var fileInfo = new FileInfo(path);
                if (CheckOutOfRange() == false)
                {
                    index = FolderIndex;
                }

                await ResetValues(fileInfo).ConfigureAwait(false);
                await LoadPic.LoadPicFromFolderAsync(fileInfo, index).ConfigureAwait(false);
            }
            else if (Base64.IsBase64String(path))
            {
                await LoadPic.LoadBase64PicAsync(path).ConfigureAwait(false);
            }
            else if (Clipboard.ContainsImage())
            {
                LoadPic.LoadPicFromBitmap(Clipboard.GetImage(), (string)Application.Current.Resources["ClipboardImage"]);
            }
            else if (Uri.IsWellFormedUriString(path, UriKind.Absolute)) // Check if from web
            {
                await WebFunctions.PicWeb(path).ConfigureAwait(false);
            }
            else
            {
                UnexpectedError();
            }
        }

        private static async Task ResetValues(FileInfo fileInfo)
        {
            if (fileInfo is null)
            {
                UnexpectedError();
                return;
            }

            // Force reloading values by setting freshStartup to true
            FreshStartup = true;

            Preloader.Clear();

            await FileLists.RetrieveFilelistAsync(fileInfo).ConfigureAwait(false);

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
                await GalleryFunctions.SortGallery().ConfigureAwait(false);
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

            UC.ToggleStartUpUC(!showStartup);

            FreshStartup = true;
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
                _ = Taskbar.NoProgress();
            }
            catch
            {
                // ignored
            }
        }
    }
}