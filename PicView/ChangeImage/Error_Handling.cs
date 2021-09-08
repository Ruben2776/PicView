using PicView.FileHandling;
using PicView.ImageHandling;
using PicView.PicGallery;
using PicView.UILogic;
using PicView.UILogic.Sizing;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using static PicView.ChangeImage.Navigation;
using static PicView.FileHandling.DeleteFiles;
using static PicView.FileHandling.FileLists;
using static PicView.ImageHandling.ImageDecoder;
using static PicView.UILogic.Tooltip;
using static PicView.UILogic.TransformImage.Rotation;

namespace PicView.ChangeImage
{
    internal static class Error_Handling
    {

        /// <summary>
        /// Clears data, to free objects no longer necessary to store in memory and allow changing folder without error.
        /// </summary>
        internal static void ChangeFolder(bool backup = false)
        {
            if (Pics.Count > 0 && backup)
            {
                // Make a backup of xPicPath and FolderIndex
                if (!string.IsNullOrWhiteSpace(Pics[FolderIndex]))
                {
                    BackupPath = Pics[FolderIndex];
                }
            }

            Pics.Clear();
            GalleryFunctions.Clear();
            Preloader.Clear();
            FreshStartup = true;
            DeleteTempFiles();
        }

        /// <summary>
        /// Refresh the current list of pics and reload them if there is some missing or changes.
        /// </summary>
        internal static async Task ReloadAsync(bool fromBackup = false)
        {
            if (fromBackup && string.IsNullOrWhiteSpace(BackupPath))
            {
                Unload();
                return;
            }

            string s;
            if (Pics != null && Pics.Count > 0)
            {
                s = fromBackup ? BackupPath : Pics[FolderIndex];
            }
            else
            {
                // TODO extract url from path or get alternative method
                s = Path.GetFileName(ConfigureWindows.GetMainWindow.TitleText.Text);
            }

            if (File.Exists(s))
            {
                // Force reloading values by setting freshStartup to true
                FreshStartup = true;

                // Clear Preloader, to avoid errors by FolderIndex changing location because of re-sorting
                Preloader.Clear();

                // Need a sort method instead
                GalleryFunctions.Clear();

                _ = LoadPiFrom(s);

                // Reset

                if (Flipped)
                {
                    Flip();
                }

                if (Rotateint != 0)
                {
                    Rotate(0);
                }
            }
            else if (Clipboard.ContainsImage() || Base64.IsBase64String(s))
            {
                return;
            }
            else if (Uri.IsWellFormedUriString(s, UriKind.Absolute)) // Check if from web
            {
                await WebFunctions.PicWeb(s).ConfigureAwait(false);
            }
            else
            {
                Unload();
            }
        }

        /// <summary>
        /// Reset to default state
        /// </summary>
        internal static void Unload()
        {
            ConfigureWindows.GetMainWindow.TitleText.ToolTip = ConfigureWindows.GetMainWindow.TitleText.Text = (string)Application.Current.Resources["NoImage"];
            ConfigureWindows.GetMainWindow.Title = Application.Current.Resources["NoImage"] + " - " + SetTitle.AppName;
            CanNavigate = false;
            ConfigureWindows.GetMainWindow.MainImage.Source = null;
            FreshStartup = true;
            if (Pics != null)
            {
                Pics.Clear();
            }

            Preloader.Clear();
            GalleryFunctions.Clear();
            ConfigureWindows.GetMainWindow.MainImage.Width = ConfigureWindows.GetMainWindow.Scroller.Width = ConfigureWindows.GetMainWindow.Scroller.Height =
            ConfigureWindows.GetMainWindow.MainImage.Height = double.NaN;
            ScaleImage.XWidth = ScaleImage.XHeight = 0;

            if (!string.IsNullOrWhiteSpace(ArchiveExtraction.TempFilePath))
            {
                DeleteTempFiles();
                ArchiveExtraction.TempFilePath = string.Empty;
            }

            try
            {
                SystemIntegration.Taskbar.NoProgress();
            }
            catch { }
        }
    }
}