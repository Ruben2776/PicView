using PicView.FileHandling;
using PicView.ImageHandling;
using PicView.PicGallery;
using PicView.UILogic;
using PicView.UILogic.PicGallery;
using PicView.UILogic.Sizing;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
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
        /// Attemps to fix erros and prevent crashes
        /// </summary>
        /// <param name="x">The index to start from</param>
        internal static async Task<BitmapSource> PicErrorFix(int x)
        {
            BitmapSource pic = null;
#if DEBUG
            Trace.WriteLine("Entered PicErrorFix");
#endif
            if (Pics == null)
            {
                Reload(true);
                return null;
            }

            if (x == -1)
            {
                await GetValues(Pics[0]).ConfigureAwait(true);
            }

            if (Pics.Count < 0)
            {
                ShowTooltipMessage(Application.Current.Resources["UnexpectedError"], true, TimeSpan.FromSeconds(3));
                Unload();
                return null;
            }
            else if (x >= Pics.Count)
            {
                if (Pics.Count > 0)
                {
                    if (x < Pics.Count)
                    {
                        pic = await RenderToBitmapSource(Pics[x]).ConfigureAwait(true);
                        if (pic != null)
                        {
                            return pic;
                        }
                    }
                }
                else
                {
                    Unload();
                    return null;
                }
            }
            else if (x < 0)
            {
                pic = await RenderToBitmapSource(Pics[x]).ConfigureAwait(true);
                if (pic != null)
                {
                    return pic;
                }
                else
                {
                    Pics = FileList(Path.GetDirectoryName(Pics[x]));
                    Pics.Remove(Pics[x]);
                    x--;

                    if (x < 0)
                    {
                        Unload();
                        return null;
                    }
                }
            }

            var file = Pics[x];

            if (file == null)
            {
                ShowTooltipMessage(Application.Current.Resources["UnexpectedError"], true, TimeSpan.FromSeconds(3));
                Unload();
                return null;
            }

            // Retry if exists, fixes rare error
            if (File.Exists(file))
            {
                pic = await RenderToBitmapSource(file).ConfigureAwait(true);
                if (pic != null)
                {
                    return pic;
                }

                return null;
            }

            // Continue to remove file if can't be rendered
            Pics.Remove(file);

            // Check if there's still images in folder
            if (Pics.Count < 0)
            {
                ShowTooltipMessage(Application.Current.Resources["No images"], true, TimeSpan.FromSeconds(3));
                Unload();

                return null;
            }

            // Go to next image
            if (Properties.Settings.Default.Looping)
            {
                x = x == Pics.Count - 1 ? 0 : FolderIndex;
            }
            else
            {
                x = x == Pics.Count - 1 ? Pics.Count - 2 : FolderIndex;
            }

            // Repeat process if the next image was not found
            if (x > 0 && x < Pics.Count)
            {
                await PicErrorFix(x).ConfigureAwait(false);
            }

            return null;
        }

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
        internal static void Reload(bool fromBackup = false)
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

                Pic(s);

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
                LoadFromWeb.PicWeb(s);
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
            ConfigureWindows.GetMainWindow.TitleText.ToolTip = ConfigureWindows.GetMainWindow.TitleText.Text = Application.Current.Resources["NoImage"] as string;
            ConfigureWindows.GetMainWindow.Title = Application.Current.Resources["NoImage"] as string + " - " + SetTitle.AppName;
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
            ScaleImage.xWidth = ScaleImage.xHeight = 0;

            if (!string.IsNullOrWhiteSpace(ArchiveExtraction.TempZipPath))
            {
                DeleteTempFiles();
                ArchiveExtraction.TempZipPath = string.Empty;
            }

            try
            {
                SystemIntegration.Taskbar.NoProgress();
            }
            catch { }
        }
    }
}