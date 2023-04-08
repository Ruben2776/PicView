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

            int next;          
            switch (navigateTo)
            {
                case NavigateTo.Next:
                case NavigateTo.Previous:
                    int indexChange = navigateTo == NavigateTo.Next ? 1 : -1;
                    Reverse = navigateTo == NavigateTo.Previous;
                    if (Settings.Default.Looping || fastPic || Slideshow.SlideTimer != null)
                    {
                        next = (FolderIndex + indexChange + Pics.Count) % Pics.Count;
                    }
                    else
                    {
                        int newIndex = FolderIndex + indexChange;
                        if (newIndex <= 0 || newIndex >= Pics.Count) return; // Don't load same image because that causes the UI to blink
                        next = newIndex;
                    }
                    break;
                case NavigateTo.First:
                    if (Pics.Count > Preloader.MaxCount) Preloader.Clear();
                    next = 0;
                    break;
                case NavigateTo.Last:
                    if (Pics.Count > Preloader.MaxCount) Preloader.Clear();
                    next = Pics.Count - 1;
                    break;
                default: return;
            }

            // If the horizontal fullscreen gallery is open, deselect current index
            if (GalleryFunctions.IsHorizontalFullscreenOpen) GalleryNavigation.SetSelected(FolderIndex, false);

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

                if (right)
                {
                    if (Properties.Settings.Default.Fullscreen == false) 
                        RightbuttonClicked = true; // Update flag to move cursor when resized
                    else
                        RightbuttonClicked = false;
                    await Navigation.GoToNextImage(NavigateTo.Next).ConfigureAwait(false);
                }
                else
                {
                    if (Properties.Settings.Default.Fullscreen == false)
                        LeftbuttonClicked = true; // Update flag to move cursor when resized
                    else
                        LeftbuttonClicked = false;
                    await Navigation.GoToNextImage(NavigateTo.Previous).ConfigureAwait(false);
                }
            }
            else // Alternative interface buttons
            {
                if (right)
                {
                    if (Properties.Settings.Default.Fullscreen == false)
                        ClickArrowRightClicked = true; // Update flag to move cursor when resized
                    else
                        ClickArrowRightClicked = false;
                    await Navigation.GoToNextImage(NavigateTo.Next).ConfigureAwait(false);
                }
                else
                {
                    if (Properties.Settings.Default.Fullscreen == false)
                        ClickArrowLeftClicked = true; // Update flag to move cursor when resized
                    else
                        ClickArrowLeftClicked = false;
                    await Navigation.GoToNextImage(NavigateTo.Previous).ConfigureAwait(false);
                }
            }
        }

        #endregion Change navigation values
    }
}