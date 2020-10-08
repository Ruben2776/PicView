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
using static PicView.ChangeImage.Error_Handling;
using static PicView.FileHandling.ArchiveExtraction;
using static PicView.FileHandling.FileLists;
using static PicView.ImageHandling.Thumbnails;
using static PicView.UILogic.SetTitle;
using static PicView.UILogic.Sizing.ScaleImage;
using static PicView.UILogic.Tooltip;
using static PicView.UILogic.TransformImage.Scroll;
using static PicView.UILogic.UC;

namespace PicView.ChangeImage
{
    internal static class Navigation
    {
        #region Static fields

        /// <summary>
        /// List of file paths to supported files
        /// </summary>
        internal static System.Collections.Generic.List<string> Pics { get; set; }

        /// <summary>
        /// Counter used to get current index
        /// </summary>
        internal static int FolderIndex { get; private set; }

        /// <summary>
        /// Backup of Previous file, if changed folder etc.
        /// </summary>
        internal static string BackupPath { get; set; }

        /// <summary>
        /// Used for error handling to prevent navigating
        /// when not possibe
        /// </summary>
        internal static bool CanNavigate { get; set; }

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

        internal static bool FastPicRunning { get; private set; }

        #endregion Static fields

        #region Update Image values

        /// <summary>
        /// Loads a picture from a given file path and does extra error checking
        /// </summary>
        /// <param name="path"></param>
        internal static async Task LoadPiFrom(string path)
        {
            // Set Loading
            SetLoadingString();

            // Handle if from web
            if (!File.Exists(path))
            {
                if (Uri.IsWellFormedUriString(path, UriKind.Absolute))
                {
                    LoadFromWeb.PicWeb(path);
                    return;
                }
                else if (Directory.Exists(path))
                {
                    ChangeFolder(true);
                    await GetValues(path).ConfigureAwait(true);
                }
                else
                {
                    Unload();
                    return;
                }
            }

            // If count not correct or just started, get values
            if (Pics.Count <= FolderIndex || FolderIndex < 0 || FreshStartup)
            {
                await GetValues(path).ConfigureAwait(true);
            }
            // If the file is in the same folder, navigate to it. If not, start manual loading procedure.
            else if (!string.IsNullOrWhiteSpace(Pics[FolderIndex]) && Path.GetDirectoryName(path) != Path.GetDirectoryName(Pics[FolderIndex]))
            {
                // Reset old values and get new
                ChangeFolder(true);
                await GetValues(path).ConfigureAwait(true);
            }

            FolderIndex = Pics.IndexOf(path);

            if (!FreshStartup)
            {
                Preloader.Clear();
            }

            if (FolderIndex != -1) // if it is -1, it means it being extracted and need to wait for it instead
            {
                // Navigate to picture using obtained index
                await LoadPicAt(FolderIndex).ConfigureAwait(false);
            }

            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, (Action)(async() =>
            {
                // Load new gallery values, if changing folder
                if (GetPicGallery != null && Properties.Settings.Default.FullscreenGallery)
                {
                    if (GetPicGallery.Container.Children.Count == 0)
                    {
                        await GalleryLoad.Load().ConfigureAwait(false);
                    }
                }
            }));

        }

        /// <summary>
        /// Loads image at specified index
        /// </summary>
        /// <param name="index">The index of file to load from Pics</param>
        internal static async Task LoadPicAt(int index)
        {
            Preloader.PreloadValue preloadValue;
            // Error checking to fix rare cases of crashing
            if (Pics.Count < index)
            {
                preloadValue = await PicErrorFix(index).ConfigureAwait(true);
                if (preloadValue == null)
                {
                    /// Try to recover
                    /// TODO needs testing
                    Reload(true);
                    return;
                }
            }

            FolderIndex = index;
            preloadValue = Preloader.Get(Pics[index]);

            // Initate loading behavior, if needed
            if (preloadValue == null || preloadValue.isLoading)
            {
                CanNavigate = false; // Dissallow changing image while loading

                if (!GalleryFunctions.IsOpen)
                {
                    // Show a thumbnail while loading
                    var thumb = GetThumb(index);
                    await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, (Action)(() =>
                    {
                        // Set loading from translation service
                        SetLoadingString();

                        // Don't allow image size to stretch the whole screen
                        if (xWidth == 0)
                        {
                            ConfigureWindows.GetMainWindow.MainImage.Width = ConfigureWindows.GetMainWindow.ParentContainer.ActualWidth;
                            ConfigureWindows.GetMainWindow.MainImage.Height = ConfigureWindows.GetMainWindow.ParentContainer.ActualHeight;
                        }

                        if (thumb != null)
                        {
                            ConfigureWindows.GetMainWindow.MainImage.Source = thumb;
                        }
                    }));
                }

                if (preloadValue == null) // Error correctiom
                {
                    await Preloader.Add(Pics[index]).ConfigureAwait(true);
                    preloadValue = Preloader.Get(Pics[index]);
                }
                while (preloadValue.isLoading)
                {
                    // Wait for finnished result
                    await Task.Delay(5).ConfigureAwait(true);
                }
            }

            // Check if works, if not show error message
            if (preloadValue == null || preloadValue.bitmapSource == null)
            {
                preloadValue = new Preloader.PreloadValue(ImageDecoder.ImageErrorMessage(), false);
            }

            // Need to put UI change in dispatcher to fix slideshow bug
            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Send, (Action)(() =>
            {
                // Scroll to top if scroll enabled
                if (IsScrollEnabled)
                {
                    ConfigureWindows.GetMainWindow.Scroller.ScrollToTop();
                }

                // Reset transforms if needed
                if (UILogic.TransformImage.Rotation.Flipped || UILogic.TransformImage.Rotation.Rotateint != 0)
                {
                    UILogic.TransformImage.Rotation.Flipped = false;
                    UILogic.TransformImage.Rotation.Rotateint = 0;
                    GetImageSettingsMenu.FlipButton.TheButton.IsChecked = false;

                    ConfigureWindows.GetMainWindow.MainImage.LayoutTransform = null;
                }

                ConfigureWindows.GetMainWindow.MainImage.Source = preloadValue.bitmapSource;
                FitImage(preloadValue.bitmapSource.PixelWidth, preloadValue.bitmapSource.PixelHeight);
                SetTitleString(preloadValue.bitmapSource.PixelWidth, preloadValue.bitmapSource.PixelHeight, index);
            }));

            // Update values
            CanNavigate = true;
            FreshStartup = false;

            if (ConfigureWindows.GetImageInfoWindow != null)
            {
                if (ConfigureWindows.GetImageInfoWindow.IsVisible)
                {
                    ConfigureWindows.GetImageInfoWindow.UpdateValues();
                }
            }

            if (Pics.Count > 1)
            {
                Taskbar.Progress(index, Pics.Count);

                // Preload images \\
                await Preloader.PreLoad(index).ConfigureAwait(false);
            }

            // Add recent files, except when browing archive
            if (string.IsNullOrWhiteSpace(TempZipFile))
            {
                RecentFiles.Add(Pics[index]);
            }
        }

        /// <summary>
        /// Load a picture from a prepared bitmap
        /// </summary>
        /// <param name="pic"></param>
        /// <param name="imageName"></param>
        internal static void Pic(BitmapSource bitmap, string imageName)
        {
            /// Old method, might need updates?

            Unload();

            if (IsScrollEnabled)
            {
                ConfigureWindows.GetMainWindow.Scroller.ScrollToTop();
            }

            ConfigureWindows.GetMainWindow.MainImage.Source = bitmap;

            FitImage(bitmap.PixelWidth, bitmap.PixelHeight);
            CloseToolTipMessage();

            SetTitleString(bitmap.PixelWidth, bitmap.PixelHeight, imageName);

            Taskbar.NoProgress();

            CanNavigate = false;
            FolderIndex = 0;

            if (ConfigureWindows.GetImageInfoWindow != null)
            {
                if (ConfigureWindows.GetImageInfoWindow.IsVisible)
                {
                    ConfigureWindows.GetImageInfoWindow.UpdateValues();
                }
            }
        }

        /// <summary>
        /// Load a picture from a base64
        /// </summary>
        /// <param name="pic"></param>
        /// <param name="imageName"></param>
        internal static async void Pic64(string base64string)
        {
            var pic = await Base64.Base64StringToBitmap(base64string).ConfigureAwait(false);

            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, (Action)(() =>
            {
                Unload();

                if (IsScrollEnabled)
                {
                    ConfigureWindows.GetMainWindow.Scroller.ScrollToTop();
                }

                ConfigureWindows.GetMainWindow.MainImage.Source = pic;

                FitImage(pic.PixelWidth, pic.PixelHeight);
                CloseToolTipMessage();

                SetTitleString(pic.PixelWidth, pic.PixelHeight, Application.Current.Resources["Base64Image"] as string);

                if (ConfigureWindows.GetImageInfoWindow != null)
                {
                    if (ConfigureWindows.GetImageInfoWindow.IsVisible)
                    {
                        ConfigureWindows.GetImageInfoWindow.UpdateValues();
                    }
                }

            }));

            Taskbar.NoProgress();

            CanNavigate = false;
        }

        /// <summary>
        /// Handle logic if user wants to load from a folder
        /// </summary>
        /// <param name="folder"></param>
        internal static async Task PicFolder(string folder)
        {
            // TODO add new function that can go to next/prev folder

            ChangeFolder(true);

            // If searching subdirectories, it might freeze UI, so wrap it in task
            await Task.Run(() =>
            {
                Pics = FileList(folder);
            }).ConfigureAwait(true);

            if (Pics.Count > 0)
            {
                await LoadPicAt(0).ConfigureAwait(true);
            }
            else
            {
                Reload(true);
            }

            GetQuickSettingsMenu.GoToPic.GoToPicBox.Text = (FolderIndex + 1).ToString(CultureInfo.CurrentCulture);

            // Load new gallery values, if changing folder
            if (GetPicGallery != null && Properties.Settings.Default.FullscreenGallery)
            {
                if (GetPicGallery.Container.Children.Count == 0)
                {
                    await GalleryLoad.Load().ConfigureAwait(false);
                }
            }
        }

        #endregion Update Image values

        #region Change navigation values

        /// <summary>
        /// Goes to next, previous, first or last file in folder
        /// </summary>
        /// <param name="next">Whether it's forward or not</param>
        /// <param name="end">Whether to go to last or first,
        /// depending on the next value</param>
        internal static async void Pic(bool next = true, bool end = false)
        {
            // Exit if not intended to change picture
            if (!CanNavigate)
            {
                return;
            }

            // exit if browsing PicGallery
            if (GetPicGallery != null)
            {
                if (Properties.Settings.Default.FullscreenGallery == false)
                {
                    if (GalleryFunctions.IsOpen)
                    {
                        return;
                    }
                }
            }

            // Make backup
            var indexBackup = FolderIndex;

            if (!end) // Go to next or previous
            {
                if (next)
                {
                    // loop next
                    if (Properties.Settings.Default.Looping || Slideshow.SlideTimer != null && Slideshow.SlideTimer.Enabled)
                    {
                        FolderIndex = FolderIndex == Pics.Count - 1 ? 0 : FolderIndex + 1;
                    }
                    else
                    {
                        // Go to next if able
                        if (FolderIndex + 1 == Pics.Count)
                        {
                            return;
                        }

                        FolderIndex++;
                    }
                    Reverse = false;
                }
                else
                {
                    // Loop prev
                    if (Properties.Settings.Default.Looping || Slideshow.SlideTimer != null && Slideshow.SlideTimer.Enabled)
                    {
                        FolderIndex = FolderIndex == 0 ? Pics.Count - 1 : FolderIndex - 1;
                    }
                    else
                    {
                        // Go to prev if able
                        if (FolderIndex - 1 < 0)
                        {
                            return;
                        }

                        FolderIndex--;
                    }
                    Reverse = true;
                }
            }
            else // Go to first or last
            {
                FolderIndex = next ? Pics.Count - 1 : 0;
                indexBackup = FolderIndex;

                // Reset preloader values to prevent errors
                if (Pics.Count > 20)
                {
                    Preloader.Clear();
                }
            }

            // Go to the image!
            await LoadPicAt(FolderIndex).ConfigureAwait(false);

            // Update PicGallery selected item, if needed
            if (GalleryFunctions.IsOpen)
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, (Action)(() =>
                {
                    if (GetPicGallery.Container.Children.Count > FolderIndex && GetPicGallery.Container.Children.Count > indexBackup)
                    {
                        if (indexBackup != FolderIndex)
                        {
                            GalleryNavigation.SetSelected(indexBackup, false);
                        }

                        GalleryNavigation.SetSelected(FolderIndex, true);
                        GalleryNavigation.ScrollTo();
                    }
                    else
                    {
                        // TODO Find way to get PicGalleryItem an alternative way...
                    }
                }));
            }

            CloseToolTipMessage();
        }

        /// <summary>
        /// Extra functionality and error checking when clicking
        /// on the navigation buttons
        /// </summary>
        /// <param name="arrow"></param>
        /// <param name="right"></param>
        internal static void PicButton(bool arrow, bool right)
        {
            if (!arrow) // Normal buttons
            {
                if (GalleryFunctions.IsOpen)
                {
                    GalleryNavigation.ScrollTo(!right);
                    return;
                }

                if (!CanNavigate)
                {
                    return;
                }

                if (right)
                {
                    RightbuttonClicked = true;
                    Pic();
                }
                else
                {
                    LeftbuttonClicked = true;
                    Pic(false, false);
                }
            }
            else // Alternative interface buttons
            {
                if (!CanNavigate)
                {
                    return;
                }

                if (right)
                {
                    ClickArrowRightClicked = true;
                    Pic();
                }
                else
                {
                    ClickArrowLeftClicked = true;
                    Pic(false, false);
                }
            }
        }

        /// <summary>
        /// Only load thumb without resizing
        /// </summary>
        /// <param name="forwards">The direction</param>
        internal static void FastPic(bool forwards)
        {
            FastPicRunning = true;
            /// TODO FastPic Changes...
            /// Need solution for slowing down this thing to something useful
            /// await task delay only works once, it seems
            /// Timers doesn't deliver a proper result in my experience
            ///

            if (forwards)
            {
                if (FolderIndex == Pics.Count - 1)
                {
                    FolderIndex = 0;
                }
                else
                {
                    FolderIndex++;
                }
            }
            else
            {
                if (FolderIndex == 0)
                {
                    FolderIndex = Pics.Count - 1;
                }
                else
                {
                    FolderIndex--;
                }
            }

            var image = Application.Current.Resources["Image"] as string;

            ConfigureWindows.GetMainWindow.TitleText.ToolTip =
            ConfigureWindows.GetMainWindow.Title =
            ConfigureWindows.GetMainWindow.TitleText.Text
            = $"{image} {(FolderIndex + 1)} / {Pics.Count}";

            var thumb = GetThumb(FolderIndex);

            if (thumb != null)
            {
                ConfigureWindows.GetMainWindow.MainImage.Source = thumb;
            }

            Taskbar.Progress(FolderIndex, Pics.Count);
        }

        /// <summary>
        /// Update after FastPic() was used
        /// </summary>
        internal static async void FastPicUpdate()
        {
            /// TODO optimize preloader usage here, to not cause delays
            /// when very quickly browsing images
            /// Need a solution that will make sure no unused values
            /// are in the preloader collection

            // Make sure it's only updated when the key is actually held down
            if (!FastPicRunning)
            {
                return;
            }

            Preloader.Clear();
            await LoadPicAt(FolderIndex).ConfigureAwait(false);
            FastPicRunning = false;
        }

        #endregion Change navigation values
    }
}