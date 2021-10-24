using PicView.FileHandling;
using PicView.ImageHandling;
using PicView.PicGallery;
using PicView.UILogic;
using PicView.UILogic.Sizing;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using static PicView.ChangeImage.Navigation;
using static PicView.FileHandling.DeleteFiles;
using static PicView.UILogic.TransformImage.Rotation;

namespace PicView.ChangeImage
{
    internal static class Error_Handling
    {
        internal static bool CheckOutOfRange()
        {
            if (Pics?.Count < FolderIndex || Pics?.Count < 1 || UILogic.UC.GetCropppingTool != null && UILogic.UC.GetCropppingTool.IsVisible)
            {
                return true;
            }
            return false;
        }

        internal static async Task UnexpectedError()
        {
            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
            {
                Unload();
                Tooltip.ShowTooltipMessage(Application.Current.Resources["UnexpectedError"]);
                ConfigureWindows.GetMainWindow.Title = (string)Application.Current.Resources["UnexpectedError"] + " - PicView";
                ConfigureWindows.GetMainWindow.TitleText.Text = (string)Application.Current.Resources["UnexpectedError"];
                ConfigureWindows.GetMainWindow.TitleText.ToolTip = (string)Application.Current.Resources["UnexpectedError"];
            });
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
            await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
            {
                SetTitle.SetLoadingString();
            });

            string? path = null;
            if (fromBackup)
            {
                if (string.IsNullOrWhiteSpace(BackupPath))
                {
                    await UnexpectedError().ConfigureAwait(false);
                    return;
                }
                path = BackupPath;
            }
            else if (CheckOutOfRange() == false)
            {
                path = Pics[FolderIndex];
            }
            else
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(() =>
                {
                    path = Path.GetFileName(ConfigureWindows.GetMainWindow.TitleText.Text);
                });
            }
            if (path == null)
            {
                await UnexpectedError().ConfigureAwait(false);
                return;
            }

            if (File.Exists(path))
            {
                // Force reloading values by setting freshStartup to true
                FreshStartup = true;

                Preloader.Clear();

                FileInfo fileInfo = new FileInfo(path);
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
                    await LoadPic.LoadPicAtIndexAsync(FolderIndex, true, fileInfo).ConfigureAwait(false);
                }
                else
                {
                    await LoadPic.LoadPiFromFileAsync(fileInfo).ConfigureAwait(false);
                }

                // Reset
                await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
                {
                    if (Flipped)
                    {
                        Flip();
                    }

                    if (Rotateint != 0)
                    {
                        Rotate(0);
                    }
                });

            }
            else if (Base64.IsBase64String(path))
            {
                await LoadPic.LoadBase64PicAsync(path).ConfigureAwait(false);
            }
            else if (Clipboard.ContainsImage())
            {
                await LoadPic.LoadPicFromBitmap(Clipboard.GetImage(), (string)Application.Current.Resources["Base64Image"]).ConfigureAwait(false);
            }
            else if (Uri.IsWellFormedUriString(path, UriKind.Absolute)) // Check if from web
            {
                await WebFunctions.PicWeb(path).ConfigureAwait(false);
            }
            else
            {
                await UnexpectedError().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Reset to default state
        /// </summary>
        internal static void Unload()
        {
            ConfigureWindows.GetMainWindow.TitleText.ToolTip = ConfigureWindows.GetMainWindow.TitleText.Text = (string)Application.Current.Resources["NoImage"];
            ConfigureWindows.GetMainWindow.Title = Application.Current.Resources["NoImage"] + " - " + SetTitle.AppName;
            ConfigureWindows.GetMainWindow.MainImage.Source = null;

            UC.ToggleStartUpUC(false);

            FreshStartup = true;
            if (Pics != null)
            {
                Pics.Clear();
            }

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
                _ = SystemIntegration.Taskbar.NoProgress();
            }
            catch { }
        }
    }
}