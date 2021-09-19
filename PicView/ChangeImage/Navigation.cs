using PicView.FileHandling;
using PicView.ImageHandling;
using PicView.PicGallery;
using PicView.SystemIntegration;
using PicView.UILogic;
using PicView.UILogic.Sizing;
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
        internal static int FolderIndex { get; private set; }

        /// <summary>
        /// Backup of Previous file, if changed folder etc.
        /// </summary>
        internal static string? BackupPath { get; set; }

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

        internal static bool FastPicRunning { get; set; }

        #endregion Static fields

        #region Update Image values

        /// <summary>
        /// Determine proper path from given string value
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        internal static async Task LoadPicFromString(string path)
        {
            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, (Action)(() =>
            {
                // Set Loading
                SetLoadingString();

                // Don't allow image size to stretch the whole screen, fixes when opening new image from unloaded status
                if (XWidth < 1)
                {
                    ConfigureWindows.GetMainWindow.MainImage.Width = ConfigureWindows.GetMainWindow.ParentContainer.ActualWidth;
                    ConfigureWindows.GetMainWindow.MainImage.Height = ConfigureWindows.GetMainWindow.ParentContainer.ActualHeight;
                }
            }));
            if (!File.Exists(path))
            {
                Uri? uriResult;
                bool result = Uri.TryCreate(path, UriKind.Absolute, out uriResult)
                    && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

                if (result)
                {
                    await WebFunctions.PicWeb(path).ConfigureAwait(false);
                    return;
                }
                else if (Base64.IsBase64String(path))
                {
                    await Pic64(path).ConfigureAwait(false);
                    return;
                }

                if (FileFunctions.FilePathHasInvalidChars(path))
                {
                    FileFunctions.MakeValidFileName(path);
                }

                path = path.Replace("\"", "");
                path = path.Trim();

                if (File.Exists(path))
                {
                    await ScaleImage.TryFitImageAsync(path).ConfigureAwait(false);

                    await LoadPiFromFileAsync(path).ConfigureAwait(false);
                }

                else if (Directory.Exists(path))
                {
                    await LoadPicFromFolderAsync(path).ConfigureAwait(false);
                }
                else
                {
                    await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, (Action)(() =>
                    {
                        Unload();
                    }));
                }

                return;
            }

            // set up size so it feels better when starting application
            await ScaleImage.TryFitImageAsync(path).ConfigureAwait(false);

            await LoadPiFromFileAsync(path).ConfigureAwait(false);
        }

        /// <summary>
        /// Loads a picture from a given file path and does extra error checking
        /// </summary>
        /// <param name="path"></param>
        internal static async Task LoadPiFromFileAsync(string path)
        {
            // If count not correct or just started, get values
            if (Pics?.Count <= FolderIndex || FolderIndex < 0 || FreshStartup)
            {
                await GetValues(path).ConfigureAwait(false);
            }
            // If the file is in the same folder, navigate to it. If not, start manual loading procedure.
            else if (!string.IsNullOrWhiteSpace(Pics?[FolderIndex]) && Path.GetDirectoryName(path) != Path.GetDirectoryName(Pics[FolderIndex]))
            {
                // Reset old values and get new
                ChangeFolder(true);
                await GetValues(path).ConfigureAwait(false);
            }

            if (Pics?.Count > 0)
            {
                FolderIndex = Pics.IndexOf(path);
            }

            if (!FreshStartup)
            {
                Preloader.Clear();
            }

            if (FolderIndex >= 0 && Pics?.Count > 0) // check if being extracted and need to wait for it instead
            {
                // Navigate to picture using obtained index
                await LoadPicAtIndexAsync(FolderIndex).ConfigureAwait(false);
            }

            if (Properties.Settings.Default.FullscreenGallery)
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, (Action)(() =>
                {
                    if (GetPicGallery == null)
                    {
                        return;
                    }

                    // Remove children before loading new
                    if (GetPicGallery.Container.Children.Count > 0)
                    {
                        GetPicGallery.Container.Children.Clear();
                    }
                }));

                // Load new gallery values, if changing folder
                await GalleryLoad.Load().ConfigureAwait(false);
                Timers.PicGalleryTimerHack();
            }
        }

        /// <summary>
        /// Loads image at specified index
        /// </summary>
        /// <param name="index">The index of file to load from Pics</param>
        internal static async Task LoadPicAtIndexAsync(int index)
        {
            FolderIndex = index;
            var preloadValue = Preloader.Get(index);

            // Initate loading behavior, if needed
            if (preloadValue == null || preloadValue.isLoading)
            {
                if (GalleryFunctions.IsOpen == false)
                {
                    await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, (Action)(() =>
                    {
                        // Show a thumbnail while loading
                        var thumb = GetThumb(index); // Need to be in dispatcher to prevent crashing when changing folder while picgallery is loading items

                        if (FreshStartup)
                        {
                            // Set loading from translation service
                            SetLoadingString();
                        }
                        else
                        {
                            var image = Application.Current.Resources["Image"] as string;

                            ConfigureWindows.GetMainWindow.TitleText.ToolTip =
                            ConfigureWindows.GetMainWindow.Title =
                            ConfigureWindows.GetMainWindow.TitleText.Text
                            = $"{image} {(index + 1)} / {Pics?.Count}";
                        }

                        if (thumb != null)
                        {
                            ConfigureWindows.GetMainWindow.MainImage.Source = thumb;
                        }
                    }));
                }
                if (FastPicRunning)
                {
                    return;
                }

                if (preloadValue == null) // Error correctiom
                {
                    await Preloader.AddAsync(index).ConfigureAwait(false);
                    preloadValue = Preloader.Get(index);
                }
                while (preloadValue != null && preloadValue.isLoading)
                {
                    // Wait for finnished result
                    await Task.Delay(5).ConfigureAwait(false);
                }
            }
            // Make loading skippable
            if (FastPicRunning)
            {
                return;
            }

            // Make loading skippable
            if (FolderIndex != index)
            {
                await Preloader.PreLoad(FolderIndex).ConfigureAwait(false);
                return;
            }

            // Check if works, if not show error message
            if (preloadValue == null || preloadValue.bitmapSource == null)
            {
                preloadValue = new Preloader.PreloadValue(ImageFunctions.ImageErrorMessage(), false);
            }

            if (preloadValue == null || preloadValue.bitmapSource == null)
            {
                Error_Handling.Unload();
                ShowTooltipMessage(Application.Current.Resources["UnexpectedError"]);
                return;
            }


            // Need to put UI change in dispatcher to fix slideshow bug
            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Send, (Action)(() =>
            {
                UpdatePic(index, preloadValue.bitmapSource);
            }));

            // Update values
            CanNavigate = true;
            FreshStartup = false;

            if (ConfigureWindows.GetImageInfoWindow != null)
            {
                if (ConfigureWindows.GetImageInfoWindow.IsVisible)
                {
                    await ConfigureWindows.GetImageInfoWindow.UpdateValuesAsync(Pics?[FolderIndex]).ConfigureAwait(false);
                }
            }

            if (Pics?.Count > 1)
            {
                Taskbar.Progress(index, Pics.Count);

                await Preloader.PreLoad(index).ConfigureAwait(false);
            }

            // Add recent files, except when browing archive
            if (string.IsNullOrWhiteSpace(TempZipFile) && Pics?.Count > index)
            {
                RecentFiles.Add(Pics?[index]);
            }
        }

        /// <summary>
        /// Update picture, size it and set the title from index
        /// </summary>
        /// <param name="index"></param>
        /// <param name="bitmapSource"></param>
        internal static void UpdatePic(int index, BitmapSource bitmapSource)
        {
            // Scroll to top if scroll enabled
            if (Properties.Settings.Default.ScrollEnabled)
            {
                ConfigureWindows.GetMainWindow.Scroller.ScrollToTop();
            }

            // Reset transforms if needed
            if (UILogic.TransformImage.Rotation.Flipped || UILogic.TransformImage.Rotation.Rotateint != 0)
            {
                UILogic.TransformImage.Rotation.Flipped = false;
                UILogic.TransformImage.Rotation.Rotateint = 0;
                GetQuickSettingsMenu.FlipButton.TheButton.IsChecked = false;

                ConfigureWindows.GetMainWindow.MainImage.LayoutTransform = null;
            }

            // Loads gif from XamlAnimatedGif if neccesary
            if (Path.GetExtension(Pics?[index]).Equals(".gif", StringComparison.OrdinalIgnoreCase))
            {
                XamlAnimatedGif.AnimationBehavior.SetSourceUri(ConfigureWindows.GetMainWindow.MainImage, new Uri(Pics?[index]));
            }
            else
            {
                ConfigureWindows.GetMainWindow.MainImage.Source = bitmapSource;
            }

            if (FastPicRunning == false) // Update size only when key is not held down
            {
                FitImage(bitmapSource.PixelWidth, bitmapSource.PixelHeight);
            }

            SetTitleString(bitmapSource.PixelWidth, bitmapSource.PixelHeight, index);
        }

        /// <summary>
        /// Update picture, size it and set the title from string
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="bitmapSource"></param>
        internal static void UpdatePic(string imageName, BitmapSource bitmapSource)
        {
            Unload();

            if (Properties.Settings.Default.ScrollEnabled)
            {
                ConfigureWindows.GetMainWindow.Scroller.ScrollToTop();
            }

            ConfigureWindows.GetMainWindow.MainImage.Source = bitmapSource;

            FitImage(bitmapSource.PixelWidth, bitmapSource.PixelHeight);
            SetTitleString(bitmapSource.PixelWidth, bitmapSource.PixelHeight, imageName);

            CloseToolTipMessage();

            Taskbar.NoProgress();

            CanNavigate = false;
            FolderIndex = 0;

            if (ConfigureWindows.GetImageInfoWindow != null)
            {
                if (ConfigureWindows.GetImageInfoWindow.IsVisible)
                {
                    _ = ConfigureWindows.GetImageInfoWindow.UpdateValuesAsync(imageName);
                }
            }

            CanNavigate = false;
        }

        /// <summary>
        /// Load a picture from a prepared bitmap
        /// </summary>
        /// <param name="pic"></param>
        /// <param name="imageName"></param>
        internal static void Pic(BitmapSource bitmap, string imageName)
        {
            UpdatePic(imageName, bitmap);

            DeleteFiles.DeleteTempFiles();
        }

        /// <summary>
        /// Load a picture from a prepared string
        /// </summary>
        /// <param name="pic"></param>
        /// <param name="imageName"></param>
        internal static async Task PicAsync(string file, string imageName, bool isGif)
        {
            BitmapSource? bitmapSource = isGif ? null : await ImageDecoder.RenderToBitmapSource(file).ConfigureAwait(false);
            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, (Action)(async () =>
            {
                if (Properties.Settings.Default.ScrollEnabled)
                {
                    ConfigureWindows.GetMainWindow.Scroller.ScrollToTop();
                }

                if (isGif)
                {
                    Size? imageSize = await ImageFunctions.ImageSizeAsync(file).ConfigureAwait(true);
                    if (imageSize.HasValue)
                    {
                        FitImage(imageSize.Value.Width, imageSize.Value.Height);
                        SetTitleString((int)imageSize.Value.Width, (int)imageSize.Value.Height, imageName);
                    }
                    XamlAnimatedGif.AnimationBehavior.SetSourceUri(ConfigureWindows.GetMainWindow.MainImage, new Uri(file));
                }
                else if (bitmapSource != null)
                {
                    ConfigureWindows.GetMainWindow.MainImage.Source = bitmapSource;
                    SetTitleString(bitmapSource.PixelWidth, bitmapSource.PixelHeight, imageName);
                    FitImage(bitmapSource.PixelWidth, bitmapSource.PixelHeight);
                }
                else
                {
                    Error_Handling.Unload();
                    ShowTooltipMessage(Application.Current.Resources["UnexpectedError"]);
                    return;
                }

                CloseToolTipMessage();
            }));

            Taskbar.NoProgress();
            CanNavigate = false;
            FolderIndex = 0;

            if (ConfigureWindows.GetImageInfoWindow != null)
            {
                if (ConfigureWindows.GetImageInfoWindow.IsVisible)
                {
                    await ConfigureWindows.GetImageInfoWindow.UpdateValuesAsync(file).ConfigureAwait(false);
                }
            }

            DeleteFiles.DeleteTempFiles();
        }

        /// <summary>
        /// Load a picture from a base64
        /// </summary>
        /// <param name="pic"></param>
        /// <param name="imageName"></param>
        internal static async Task Pic64(string base64string)
        {
            if (string.IsNullOrEmpty(base64string))
            {
                return;
            }
            var pic = await Base64.Base64StringToBitmap(base64string).ConfigureAwait(false);
            if (pic == null)
            {
                return;
            }
            var b64 = Application.Current.Resources["Base64Image"] as string;
            if (b64 == null)
            {
                return;
            }
            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, (Action)(() =>
            {
                UpdatePic(b64, pic);
            }));
        }

        /// <summary>
        /// Handle logic if user wants to load from a folder
        /// </summary>
        /// <param name="folder"></param>
        internal static async Task LoadPicFromFolderAsync(string folder)
        {
            // TODO add new function that can go to next/prev folder
            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, (Action)(() =>
            {
                ChangeFolder(true);
            }));
            

            // If searching subdirectories, it might freeze UI, so wrap it in task
            await Task.Run(() =>
            {
                Pics = FileList(folder);
            }).ConfigureAwait(false);

            if (Pics?.Count > 0)
            {
                await LoadPicAtIndexAsync(0).ConfigureAwait(false);
            }
            else
            {
                await ReloadAsync(true).ConfigureAwait(false);
            }
            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, (Action)(async () =>
            {
                GetImageSettingsMenu.GoToPic.GoToPicBox.Text = (FolderIndex + 1).ToString(CultureInfo.CurrentCulture);

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

        #endregion Update Image values

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
                        FolderIndex = FolderIndex == Pics?.Count - 1 ? 0 : FolderIndex + 1;
                    }
                    else
                    {
                        // Go to next if able
                        if (FolderIndex + 1 == Pics?.Count)
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
                if (Pics?.Count > Preloader.LoadBehind + Preloader.LoadInfront + 2)
                {
                    Preloader.Clear();
                }
            }

            // Go to the image!
            await LoadPicAtIndexAsync(FolderIndex).ConfigureAwait(false);

            // Update PicGallery selected item, if needed
            if (GalleryFunctions.IsOpen)
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, (Action)(() =>
                {
                    if (GetPicGallery?.Container.Children.Count > FolderIndex && GetPicGallery.Container.Children.Count > indexBackup)
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

                    CloseToolTipMessage();
                }));
            }
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
                if (!CanNavigate)
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

        /// <summary>
        /// Update after FastPic() was used
        /// </summary>
        internal static async Task FastPicUpdateAsync()
        {
            // Make sure it's only updated when the key is actually held down
            if (FastPicRunning == false)
            {
                return;
            }

            FastPicRunning = false;

            Preloader.PreloadValue? preloadValue;

            // Reset preloader values to prevent errors
            if (Pics?.Count > Preloader.LoadBehind + Preloader.LoadInfront + 2)
            {
                Preloader.Clear();
                await Preloader.AddAsync(FolderIndex).ConfigureAwait(false);
                preloadValue = Preloader.Get(FolderIndex);
            }
            else
            {
                preloadValue = Preloader.Get(FolderIndex);

                if (preloadValue == null) // Error correctiom
                {
                    await Preloader.AddAsync(FolderIndex).ConfigureAwait(false);
                    preloadValue = Preloader.Get(FolderIndex);
                }
                while (preloadValue != null && preloadValue.isLoading)
                {
                    // Wait for finnished result
                    await Task.Delay(5).ConfigureAwait(false);
                }
            }

            if (preloadValue == null || preloadValue.bitmapSource == null)
            {
                Error_Handling.Unload();
                ShowTooltipMessage(Application.Current.Resources["UnexpectedError"]);
                return;
            }

            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Send, (Action)(() =>
            {
                UpdatePic(FolderIndex, preloadValue.bitmapSource);
            }));
        }

        #endregion Change navigation values
    }
}