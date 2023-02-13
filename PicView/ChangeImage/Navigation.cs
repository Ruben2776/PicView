using PicView.PicGallery;
using PicView.Properties;
using PicView.UILogic;
using System.Collections.Generic;
using System.Threading.Tasks;

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

        internal static FileHistory? GetFileHistory;

        #endregion Static fields

        #region Change navigation values

        /// <summary>
        /// Asynchronous method that navigates to the next/previous picture
        /// </summary>
        /// <param name="forward">Indicates whether to navigate to the next (true) or previous (false) picture. Defaults to true.</param>
        /// <param name="end">Indicates whether to navigate to the first (false) or last (true) picture. Defaults to false.</param>
        /// <param name="fastPic">Indicates whether to use FastPic navigation or not. Defaults to false.</param>
        /// <returns></returns>
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

            if (GalleryFunctions.IsHorizontalFullscreenOpen || GalleryFunctions.IsVerticalFullscreenOpen)
            {
                GalleryNavigation.SetSelected(prev, false);
            }

            // Go to the image!
            await LoadPic.LoadPicAtIndexAsync(next).ConfigureAwait(false);
        }

        /// <summary>
        /// Calculates the index of the next/previous picture.
        /// </summary>
        /// <param name="forward">Indicates whether to navigate to the next (true) or previous (false) picture. Defaults to true.</param>
        /// <param name="end">ndicates whether to navigate to the first (false) or last (true) picture. Defaults to false.</param>
        /// <returns></returns>
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
                int indexChange = forward ? 1 : -1;
                bool isSlideshowEnabled = Slideshow.SlideTimer != null && Slideshow.SlideTimer.Enabled;

                if (Settings.Default.Looping || isSlideshowEnabled)
                {
                    next = (FolderIndex + indexChange + Pics.Count) % Pics.Count;
                }
                else
                {
                    int newIndex = FolderIndex + indexChange;
                    if (newIndex >= 0 && newIndex < Pics.Count)
                    {
                        next = newIndex;
                    }
                    else
                    {
                        return -1;
                    }
                }
                Reverse = !forward;
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