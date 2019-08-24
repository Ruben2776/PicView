using PicView.PreLoading;
using System;
using System.IO;
using System.Windows.Media.Imaging;
using static PicView.DeleteFiles;
using static PicView.FileLists;
using static PicView.Helper;
using static PicView.Fields;
using static PicView.ImageManager;
using static PicView.Navigation;
using static PicView.Resize_and_Zoom;
using static PicView.Rotate_and_Flip;
using static PicView.Interface;

namespace PicView
{
    internal static class Error_Handling
    {
        /// <summary>
        /// Attemps to fix Pics list by removing invalid files
        /// </summary>
        /// <param name="x">The index to start from</param>
        internal static bool PicErrorFix(int x)
        {
            if (Pics == null)
            {
                Reload(true);
                return false;
            }

            if (Pics.Count < 0)
            {
                ToolTipStyle("Unexpected error", true, TimeSpan.FromSeconds(3));
                Unload();
                return false;
            }
            else if (x >= Pics.Count)
            {
                if (Pics.Count > 0)
                {
                    Pic(Pics[0]);
                    return true;
                }
                else
                {
                    Unload();
                    return false;
                }
            }
            else if (x < 0)
            {
                var img = RenderToBitmapSource(PicPath);
                if (img != null)
                {
                    Pic(PicPath);
                    return true;
                }

                else
                {
                    Pics = FileList(Path.GetDirectoryName(PicPath));
                    Pics.Remove(PicPath);
                    x--;

                    if (x < 0)
                    {
                        Unload();
                        return false;
                    }
                }
            }

            var file = Pics[x];

            if (file == null)
            {
                ToolTipStyle("Unexpected error", true, TimeSpan.FromSeconds(3));
                Unload();
                return false;
            }

            // Retry if exists, fixes rare error
            if (File.Exists(file))
            {
                //Preloader.Add(file);
                BitmapSource pic = Preloader.Load(file);
                if (pic != null)
                {
                    Pic(file);
                    return true;
                }
                return false;
            }

            // Continue to remove file if can't be rendered
            Pics.Remove(file);

            // Check if there's still images in folder
            if (Pics.Count < 0)
            {
                ToolTipStyle("No images in folder", true, TimeSpan.FromSeconds(3));
                Unload();
                return false;
            }

            // Go to next image
            if (Properties.Settings.Default.Looping)
                FolderIndex = FolderIndex == Pics.Count - 1 ? 0 : FolderIndex;
            else
                FolderIndex = FolderIndex == Pics.Count - 1 ? Pics.Count - 2 : FolderIndex;

            if (File.Exists(file))
                ToolTipStyle("File not found or unable to render, " + file, false, TimeSpan.FromSeconds(2.5));

            AjaxLoadingEnd();

            // Repeat process if the next image was not found
            PicErrorFix(FolderIndex);
            return false;
        }



        /// <summary>
        /// Clears data, to free objects no longer necessary to store in memory and allow changing folder without error.
        /// </summary>
        internal static void ChangeFolder()
        {
            Pics.Clear();
            Preloader.Clear();
            DeleteTempFiles();
            PreloadCount = 0;
            freshStartup = true;

            if (Properties.Settings.Default.PicGallery > 0)
                PicGalleryLogic.Clear();
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

            var x = fromBackup ? xPicPath : PicPath;

            if (File.Exists(x))
            {
                // Force reloading values by setting freshStartup to true
                freshStartup = true;
                Pic(x);

                // Reset
                if (isZoomed)
                    ResetZoom();
                if (Flipped)
                    Flip();
                if (Rotateint != 0)
                    Rotate(0);
            }
            else
            {
                Unload();
                ToolTipStyle("Unknown error occured");
            }
        }

        /// <summary>
        /// Reset to default state
        /// </summary>
        internal static void Unload()
        {
            mainWindow.Bar.ToolTip = mainWindow.Bar.Text = NoImage;
            mainWindow.Title = NoImage + " - " + AppName;
            canNavigate = false;
            mainWindow.img.Source = null;
            freshStartup = true;
            if (Pics != null)
                Pics.Clear();
            PreloadCount = 0;
            Preloader.Clear();
            PicPath = string.Empty;
            FolderIndex = 0;
            mainWindow.img.Width = mainWindow.Scroller.Width = mainWindow.Scroller.Height =
            mainWindow.img.Height = double.NaN;

            if (!string.IsNullOrWhiteSpace(TempZipPath))
            {
                DeleteTempFiles();
                TempZipPath = string.Empty;
            }

            NoProgress();
            AnimationHelper.Fade(ajaxLoading, 0, TimeSpan.FromSeconds(.2));
        }
    }
}
