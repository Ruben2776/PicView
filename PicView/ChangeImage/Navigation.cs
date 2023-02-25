using PicView.PicGallery;
using PicView.Properties;
using PicView.UILogic;
using static System.Net.Mime.MediaTypeNames;

namespace PicView.ChangeImage
{
    enum NavigateTo
    {
        Next,
        Previous,
        First,
        Last,
    }

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
        /// Navigates to the next or previous image.
        /// </summary>
        /// <param name="navigateTo">Specifies whether to navigate to the next or previous image, or to the first or last image.</param>
        /// <param name="fastPic">Whether to use fast picture loading.</param>
        internal static async Task GoToNextImage(NavigateTo navigateTo, bool fastPic = false)
        {
            if (ErrorHandling.CheckOutOfRange()) return;

            int prev = FolderIndex;
            int next = FolderIndex;

            int indexChange = navigateTo switch
            {
                NavigateTo.Next => 1,
                NavigateTo.Previous => -1,
                _ => 0
            };

            bool isSlideshowEnabled = Slideshow.SlideTimer?.Enabled == true;

            Reverse = navigateTo == NavigateTo.Previous;

            if (fastPic || Settings.Default.Looping || isSlideshowEnabled)
            {
                next = FolderIndex = (FolderIndex + indexChange + Pics.Count) % Pics.Count;
            }
            else
            {
                int newIndex = FolderIndex + indexChange;
                if (newIndex < 0 || newIndex >= Pics.Count) return;
                next = FolderIndex = newIndex;
            }

            if (navigateTo == NavigateTo.First || navigateTo == NavigateTo.Last)
            {
                if (Pics.Count() > Preloader.MaxCount) Preloader.Clear();
                next = FolderIndex = navigateTo == NavigateTo.First ? 0 : Pics.Count - 1;
            }

            // If the horizontal fullscreen view is open, set the selected index to the previous index
            if (GalleryFunctions.IsHorizontalFullscreenOpen) GalleryNavigation.SetSelected(prev, false);

            if (fastPic) await FastPic.Run(next).ConfigureAwait(false);
            else await LoadPic.LoadPicAtIndexAsync(next).ConfigureAwait(false);
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
                    RightbuttonClicked = true; // Update flag to move cursor when resized
                    await Navigation.GoToNextImage(NavigateTo.Next).ConfigureAwait(false);
                }
                else
                {
                    LeftbuttonClicked = true; // Update flag to move cursor when resized
                    await Navigation.GoToNextImage(NavigateTo.Previous).ConfigureAwait(false);
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
                    ClickArrowRightClicked = true; // Update flag to move cursor when resized
                    await Navigation.GoToNextImage(NavigateTo.Next).ConfigureAwait(false);
                }
                else
                {
                    ClickArrowLeftClicked = true; // Update flag to move cursor when resized
                    await Navigation.GoToNextImage(NavigateTo.Previous).ConfigureAwait(false);
                }
            }
        }

        #endregion Change navigation values
    }
}