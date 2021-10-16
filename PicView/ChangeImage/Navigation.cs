using PicView.FileHandling;
using PicView.ImageHandling;
using PicView.PicGallery;
using PicView.SystemIntegration;
using PicView.UILogic;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static PicView.ChangeImage.Error_Handling;
using static PicView.FileHandling.ArchiveExtraction;
using static PicView.FileHandling.FileLists;
using static PicView.ImageHandling.Thumbnails;
using static PicView.UILogic.SetTitle;
using static PicView.UILogic.Sizing.ScaleImage;
using static PicView.UILogic.Tooltip;
using static PicView.UILogic.UC;

namespace PicView.ChangeImage
{
    internal static class Navigation
    {
        #region Static fields

        /// <summary>
        /// List of file paths to supported files
        /// </summary>
        internal static System.Collections.Generic.List<string>? Pics { get; set; }

        /// <summary>
        /// Counter used to get current index
        /// </summary>
        internal static int FolderIndex { get; set; }

        /// <summary>
        /// Backup of Previous file, if changed folder etc.
        /// </summary>
        internal static string? BackupPath { get; set; }

        /// <summary>
        /// Used to determine if values need to get retrieved (again)
        /// </summary>
        internal static bool FreshStartup { get; set; }

        /// <summary>
        /// Determine direction user is going
        /// </summary>
        internal static bool Reverse { get; private set; }

        /// <summary>
        /// Used to move cursor when clicked
        /// </summary>
        internal static bool LeftbuttonClicked { get; set; }

        /// <summary>
        /// Used to move cursor when clicked
        /// </summary>
        internal static bool RightbuttonClicked { get; set; }

        /// <summary>
        /// Used to move cursor when clicked
        /// </summary>
        internal static bool ClickArrowRightClicked { get; set; }

        /// <summary>
        /// Used to move cursor when clicked
        /// </summary>
        internal static bool ClickArrowLeftClicked { get; set; }

        internal static bool FastPicRunning { get; set; }

        #endregion Static fields

        #region Change navigation values

        /// <summary>
        /// Goes to next, previous, first or last file in folder
        /// </summary>
        /// <param name="next">Whether it's forward or not</param>
        /// <param name="end">Whether to go to last or first,
        /// depending on the next value</param>
        internal static async Task PicAsync(bool next = true, bool end = false)
        {
            // Exit if not intended to change picture
            if (Error_Handling.CheckOutOfRange())
            {
                return;
            }

            // Make backup
            int indexBackup = FolderIndex;
            int startingpoint = FolderIndex;

            if (end) // Go to first or last
            {
                startingpoint = next ? Pics.Count - 1 : 0;
                indexBackup = FolderIndex;

                // Reset preloader values to prevent errors
                if (Pics?.Count > Preloader.LoadBehind + Preloader.LoadInfront + 2)
                {
                    Preloader.Clear();
                }
            }
            else // Go to next or previous
            {
                if (next)
                {
                    // loop next
                    if (Properties.Settings.Default.Looping || Slideshow.SlideTimer != null && Slideshow.SlideTimer.Enabled)
                    {
                        startingpoint = FolderIndex == Pics?.Count - 1 ? 0 : FolderIndex + 1;
                    }
                    else
                    {
                        // Go to next if able
                        if (FolderIndex + 1 == Pics?.Count)
                        {
                            return;
                        }

                        startingpoint++;
                    }
                    Reverse = false;
                }
                else
                {
                    // Loop prev
                    if (Properties.Settings.Default.Looping || Slideshow.SlideTimer != null && Slideshow.SlideTimer.Enabled)
                    {
                        startingpoint = FolderIndex == 0 ? Pics.Count - 1 : FolderIndex - 1;
                    }
                    else
                    {
                        // Go to prev if able
                        if (startingpoint - 1 < 0)
                        {
                            return;
                        }

                        startingpoint--;
                    }
                    Reverse = true;
                }
            }

            if (GalleryFunctions.IsHorizontalFullscreenOpen || GalleryFunctions.IsVerticalFullscreenOpen)
            {
                GalleryNavigation.SetSelected(indexBackup, false);
            }

            // Go to the image!
            await LoadPic.LoadPicAtIndexAsync(startingpoint).ConfigureAwait(false);
        }

        /// <summary>
        /// Extra functionality and error checking when clicking
        /// on the navigation buttons
        /// </summary>
        /// <param name="arrow"></param>
        /// <param name="right"></param>
        internal static async Task PicButtonAsync(bool arrow, bool right)
        {
            if (!arrow) // Normal buttons
            {
                if (GalleryFunctions.IsHorizontalOpen)
                {
                    GalleryNavigation.ScrollTo(!right);
                    return;
                }

                if (Error_Handling.CheckOutOfRange())
                {
                    return;
                }

                if (right)
                {
                    RightbuttonClicked = true;
                    await PicAsync().ConfigureAwait(false);
                }
                else
                {
                    LeftbuttonClicked = true;
                    await PicAsync(false, false).ConfigureAwait(false);
                }
            }
            else // Alternative interface buttons
            {
                if (Error_Handling.CheckOutOfRange())
                {
                    return;
                }

                if (right)
                {
                    ClickArrowRightClicked = true;
                    await PicAsync().ConfigureAwait(false);
                }
                else
                {
                    ClickArrowLeftClicked = true;
                    await PicAsync(false, false).ConfigureAwait(false);
                }
            }
        }

        #endregion Change navigation values
    }
}