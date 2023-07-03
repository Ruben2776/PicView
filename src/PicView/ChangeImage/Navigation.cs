using PicView.ChangeTitlebar;
using PicView.FileHandling;
using PicView.PicGallery;
using PicView.Properties;
using PicView.UILogic;

namespace PicView.ChangeImage;

internal enum NavigateTo
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
    /// Keep track for recursive directory, error checking etc.
    /// </summary>
    internal static string? InitialPath { get; set; }

    /// <summary>
    /// Determine direction user is going
    /// </summary>
    internal static bool Reverse { get; private set; }

    /// <summary>
    /// Used to move cursor when clicked
    /// </summary>
    internal static bool LeftButtonClicked { get; set; }

    /// <summary>
    /// Used to move cursor when clicked
    /// </summary>
    internal static bool RightButtonClicked { get; set; }

    /// <summary>
    /// Used to move cursor when clicked
    /// </summary>
    internal static bool ClickArrowRightClicked { get; set; }

    /// <summary>
    /// Used to move cursor when clicked
    /// </summary>
    internal static bool ClickArrowLeftClicked { get; set; }

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
                var indexChange = navigateTo == NavigateTo.Next ? 1 : -1;
                Reverse = navigateTo == NavigateTo.Previous;
                if (Settings.Default.Looping || fastPic || Slideshow.SlideTimer != null)
                {
                    next = (FolderIndex + indexChange + Pics.Count) % Pics.Count;
                }
                else
                {
                    var newIndex = FolderIndex + indexChange;
                    if (newIndex < 0 || newIndex >= Pics.Count) return;
                    next = newIndex;
                }
                break;

            case NavigateTo.First:
                if (Pics.Count > PreLoader.MaxCount) PreLoader.Clear();
                next = 0;
                break;

            case NavigateTo.Last:
                if (Pics.Count > PreLoader.MaxCount) PreLoader.Clear();
                next = Pics.Count - 1;
                break;

            default: return;
        }

        // If the horizontal fullscreen gallery is open, deselect current index
        if (UC.GetPicGallery is not null)
        {
            await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
            {
                UC.GetPicGallery.Scroller.CanContentScroll = true; // Disable animations
                // Deselect current item
                GalleryNavigation.SetSelected(GalleryNavigation.SelectedGalleryItem, false);
                GalleryNavigation.SetSelected(FolderIndex, false);
            });
        }

        if (fastPic) await FastPic.Run(next).ConfigureAwait(false);
        else await LoadPic.LoadPicAtIndexAsync(next).ConfigureAwait(false);

        // Update gallery selections
        if (UC.GetPicGallery is not null)
        {
            await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
            {
                // Select next item
                GalleryNavigation.SetSelected(FolderIndex, true);
                GalleryNavigation.SelectedGalleryItem = FolderIndex;
                GalleryNavigation.ScrollToGalleryCenter();
            });
        }
    }

    /// <summary>
    /// Navigates to the next or previous folder based on the current directory.
    /// </summary>
    /// <param name="next">True to navigate to the next folder, false to navigate to the previous folder.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    internal static async Task GoToNextFolder(bool next)
    {
        SetTitle.SetLoadingString();
        var fileList = await Task.FromResult(FileLists.NextFileList(next)).ConfigureAwait(false);
        if (fileList is null or { Count: <= 0 })
        {
            SetTitle.SetTitleString();
            return;
        }
        ErrorHandling.ChangeFolder();
        Pics = fileList;
        if (GalleryFunctions.IsGalleryOpen)
        {
            if (Settings.Default.IsBottomGalleryShown)
            {
                _ = GalleryLoad.ReloadGallery().ConfigureAwait(false);
            }
            else
            {
                GalleryToggle.CloseCurrentGallery();
            }
        }
        await LoadPic.LoadPicAtIndexAsync(0).ConfigureAwait(false);
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
            if (GalleryFunctions.IsGalleryOpen)
            {
                GalleryNavigation.ScrollGallery(!right, false, false, true);
                return;
            }

            if (right)
            {
                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (Settings.Default.Fullscreen == false)
                    RightButtonClicked = true; // Update flag to move cursor when resized
                else
                    RightButtonClicked = false;
                await GoToNextImage(NavigateTo.Next).ConfigureAwait(false);
            }
            else
            {
                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (Settings.Default.Fullscreen == false)
                    LeftButtonClicked = true; // Update flag to move cursor when resized
                else
                    LeftButtonClicked = false;
                await GoToNextImage(NavigateTo.Previous).ConfigureAwait(false);
            }
        }
        else // Alternative interface buttons
        {
            if (right)
            {
                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (Settings.Default.Fullscreen == false)
                    ClickArrowRightClicked = true; // Update flag to move cursor when resized
                else
                    ClickArrowRightClicked = false;
                await GoToNextImage(NavigateTo.Next).ConfigureAwait(false);
            }
            else
            {
                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (Settings.Default.Fullscreen == false)
                    ClickArrowLeftClicked = true; // Update flag to move cursor when resized
                else
                    ClickArrowLeftClicked = false;
                await GoToNextImage(NavigateTo.Previous).ConfigureAwait(false);
            }
        }
    }

    #endregion Change navigation values
}