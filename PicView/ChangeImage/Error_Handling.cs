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
            string? path = null;
            if (fromBackup)
            {
                if (string.IsNullOrWhiteSpace(BackupPath))
                {
                    await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(() =>
                    {
                        Unload();
                    });

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

            if (path is not null && File.Exists(path))
            {
                // Force reloading values by setting freshStartup to true
                FreshStartup = true;

                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(() =>
                {
                    // Clear Preloader, to avoid errors by FolderIndex changing location because of re-sorting
                    Preloader.Clear();
                });

                bool containerCheck = false;

                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(() =>
                {
                    if (UC.GetPicGallery?.Container?.Children?.Count > 0)
                    {
                        containerCheck = true;
                    }
                });


                if (containerCheck)
                {
                    await GalleryFunctions.SortGallery().ConfigureAwait(false);
                    await LoadPicAtIndexAsync(FolderIndex).ConfigureAwait(false);
                }
                else
                {
                    await LoadPiFromFileAsync(path).ConfigureAwait(false);
                }

                // Reset
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(() =>
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
            else if (Clipboard.ContainsImage() || Base64.IsBase64String(path))
            {
                return;
            }
            else if (Uri.IsWellFormedUriString(path, UriKind.Absolute)) // Check if from web
            {
                await WebFunctions.PicWeb(path).ConfigureAwait(false);
            }
            else
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(() => { Unload(); });
            }
        }

        /// <summary>
        /// Reset to default state
        /// </summary>
        internal static void Unload()
        {
            ConfigureWindows.GetMainWindow.TitleText.ToolTip = ConfigureWindows.GetMainWindow.TitleText.Text = (string)Application.Current.Resources["NoImage"];
            ConfigureWindows.GetMainWindow.Title = Application.Current.Resources["NoImage"] + " - " + SetTitle.AppName;

            Uri uri = new Uri($"pack://application:,,,/PicView;component/Themes/Resources/img/icon__Q6k_icon.ico", UriKind.Absolute);
            ConfigureWindows.GetMainWindow.MainImage.Source = new System.Windows.Media.Imaging.BitmapImage(uri);
            UILogic.Sizing.ScaleImage.FitImage(48,48);
            
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