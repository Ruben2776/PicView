using PicView.ImageHandling;
using PicView.UI.PicGallery;
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
using static PicView.Library.Fields;
using static PicView.UI.Tooltip;
using static PicView.UI.TransformImage.Rotation;

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
            Trace.WriteLine("Entered PicErrorFix"); // We don't want to go in here
#endif
            if (Pics == null)
            {
                Reload(true);
                return null;
            }

            if (x == -1)
            {
                GetValues(Pics[0]);
            }

            if (Pics.Count < 0)
            {
                ShowTooltipMessage("Unexpected error", true, TimeSpan.FromSeconds(3));
                Unload();
                return null;
            }
            else if (x >= Pics.Count)
            {
                if (Pics.Count > 0)
                {
                    if (x < Pics.Count)
                    {
                        pic = await RenderToBitmapSource(Pics[x]).ConfigureAwait(false);
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
                pic = await RenderToBitmapSource(Pics[FolderIndex]).ConfigureAwait(false);
                if (pic != null)
                {
                    return pic;
                }
                else
                {
                    Pics = FileList(Path.GetDirectoryName(Pics[FolderIndex]));
                    Pics.Remove(Pics[FolderIndex]);
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
                ShowTooltipMessage("Unexpected error", true, TimeSpan.FromSeconds(3));
                Unload();
                return null;
            }

            // Retry if exists, fixes rare error
            if (File.Exists(file))
            {
                pic = await RenderToBitmapSource(file).ConfigureAwait(false);
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
                ShowTooltipMessage("No images in folder", true, TimeSpan.FromSeconds(3));
                Unload();

                return null;
            }

            // Go to next image
            if (Properties.Settings.Default.Looping)
            {
                FolderIndex = FolderIndex == Pics.Count - 1 ? 0 : FolderIndex;
            }
            else
            {
                FolderIndex = FolderIndex == Pics.Count - 1 ? Pics.Count - 2 : FolderIndex;
            }

            if (File.Exists(file))
            {
                ShowTooltipMessage("File not found or unable to render, " + file, false, TimeSpan.FromSeconds(2.5));
            }

            // Repeat process if the next image was not found
            if (FolderIndex > 0 && FolderIndex < Pics.Count)
            {
                await PicErrorFix(FolderIndex).ConfigureAwait(false);
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
                    xPicPath = Pics[FolderIndex];
                }
            }

            Pics.Clear();
            GalleryFunctions.Clear();
            Preloader.Clear();
            PreloadCount = 0;
            FreshStartup = true;
            DeleteTempFiles();
        }

        /// <summary>
        /// Refresh the current list of pics and reload them if there is some missing or changes.
        /// </summary>
        internal static void Reload(bool fromBackup = false)
        {
            if (fromBackup && string.IsNullOrWhiteSpace(xPicPath))
            {
                Unload();
                return;
            }

            string s;
            if (Pics != null && Pics.Count > 0)
            {
                s = fromBackup ? xPicPath : Pics[FolderIndex];
            }
            else
            {
                // TODO extract url from path or get alternative method
                s = Path.GetFileName(TheMainWindow.Bar.Text);
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
                ShowTooltipMessage("Unknown error occured");
            }
        }

        /// <summary>
        /// Reset to default state
        /// </summary>
        internal static void Unload()
        {
            TheMainWindow.Bar.ToolTip = TheMainWindow.Bar.Text = NoImage;
            TheMainWindow.Title = NoImage + " - " + AppName;
            CanNavigate = false;
            TheMainWindow.MainImage.Source = null;
            FreshStartup = true;
            if (Pics != null)
            {
                Pics.Clear();
            }

            PreloadCount = 0;
            Preloader.Clear();
            GalleryFunctions.Clear();
            FolderIndex = 0;
            TheMainWindow.MainImage.Width = TheMainWindow.Scroller.Width = TheMainWindow.Scroller.Height =
            TheMainWindow.MainImage.Height = double.NaN;
            xWidth = xHeight = 0;
            prevPicResource = null;

            if (!string.IsNullOrWhiteSpace(TempZipPath))
            {
                DeleteTempFiles();
                TempZipPath = string.Empty;
            }

            SystemIntegration.Taskbar.NoProgress();
        }
    }
}