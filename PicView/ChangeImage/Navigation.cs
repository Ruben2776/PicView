using System.Collections.Generic;
using System.Threading.Tasks;
using PicView.PicGallery;
using PicView.Properties;
using PicView.UILogic;

namespace PicView.ChangeImage
{
    internal static class Navigation
    {
        #region Static fields

        /// <summary>
        /// List of file paths to supported files
        /// </summary>
        internal static List<string>? Pics { get; set; }

        /// <summary>
        /// Counter used to get current index
        /// </summary>
        internal static int FolderIndex { get; set; }

        /// <summary>
        /// Backup of Previous file, if changed folder etc.
        /// </summary>
        internal static string? BackupPath { get; set; }

        /// <summary>
        /// First file when starting application, used for recursive directory, eroor checking etc.
        /// </summary>
        internal static string? InitialPath { get; set; }

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
        /// <param name="forward">Whether it's forward or not</param>
        /// <param name="end">Whether to go to last or first,
        /// depending on the next value</param>
        internal static async Task NavigateToPicAsync(bool forward = true, bool end = false, bool fastPic = false)
        {
            int prev = FolderIndex;
            int next = GetImageIterateIndex(forward, end);
            if (next == -1) { return; }

            if (fastPic)
            {
                await FastPic.Run(next).ConfigureAwait(false);
                return;
            }

            // Go to the image!
            await LoadPic.LoadPicAtIndexAsync(next).ConfigureAwait(false);

            if (GalleryFunctions.IsHorizontalFullscreenOpen || GalleryFunctions.IsVerticalFullscreenOpen)
            {
                GalleryNavigation.SetSelected(prev, false);
            }
        }

        internal static int GetImageIterateIndex(bool forward = true, bool end = false)
        {
            // Exit if not intended to change picture
            if (ErrorHandling.CheckOutOfRange())
            {
                return -1;
            }

            int next = FolderIndex;

            if (end) // Go to first or last
            {
                next = forward ? Pics.Count - 1 : 0;

                // Reset preloader values to prevent errors
                if (Pics?.Count > 10)
                {
                    Preloader.Clear();
                }
            }
            else // Go to next or previous
            {
                if (forward)
                {
                    // loop next
                    if (Settings.Default.Looping || Slideshow.SlideTimer != null && Slideshow.SlideTimer.Enabled)
                    {
                        next = FolderIndex == Pics?.Count - 1 ? 0 : FolderIndex + 1;
                    }
                    else
                    {
                        // Go to next if able
                        if (FolderIndex + 1 == Pics?.Count)
                        {
                            return -1;
                        }

                        next++;
                    }
                    Reverse = false;
                }
                else
                {
                    // Loop prev
                    if (Settings.Default.Looping || Slideshow.SlideTimer != null && Slideshow.SlideTimer.Enabled)
                    {
                        next = FolderIndex == 0 ? Pics.Count - 1 : FolderIndex - 1;
                    }
                    else
                    {
                        // Go to prev if able
                        if (next - 1 < 0)
                        {
                            return -1;
                        }

                        next--;
                    }
                    Reverse = true;
                }
            }

            return next;
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

                if (ErrorHandling.CheckOutOfRange())
                {
                    return;
                }

                if (right)
                {
                    RightbuttonClicked = true;
                    await NavigateToPicAsync().ConfigureAwait(false);
                }
                else
                {
                    LeftbuttonClicked = true;
                    await NavigateToPicAsync(false).ConfigureAwait(false);
                }
            }
            else // Alternative interface buttons
            {
                if (ErrorHandling.CheckOutOfRange())
                {
                    return;
                }

                if (right)
                {
                    ClickArrowRightClicked = true;
                    await NavigateToPicAsync().ConfigureAwait(false);
                }
                else
                {
                    ClickArrowLeftClicked = true;
                    await NavigateToPicAsync(false).ConfigureAwait(false);
                }
            }
        }

        #endregion Change navigation values
    }
}