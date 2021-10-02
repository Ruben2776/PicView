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

        #region Load Pic from value

        /// <summary>
        /// Quickly load image and then update values
        /// Only to be used from startup
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        internal static async Task QuickLoad(string file)
        {
            if (File.Exists(file) == false)
            {
                await LoadPicFromString(file, false).ConfigureAwait(false);
                return;
            }

            BitmapSource? pic = null, thumb;
            thumb = await Task.FromResult(GetBitmapSourceThumb(file)).ConfigureAwait(false);
            bool archive = false;
            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Send, () =>
            {
                // Set Loading
                SetLoadingString();

                if (thumb is  not null && ConfigureWindows.GetMainWindow.MainImage.Source is null)
                {
                    ConfigureWindows.GetMainWindow.MainImage.Source = thumb;

                    if (Properties.Settings.Default.Fullscreen)
                    {
                        ConfigureWindows.GetMainWindow.MainImage.Width = thumb.PixelWidth;
                        ConfigureWindows.GetMainWindow.MainImage.Height = thumb.PixelHeight;
                    }
                    else
                    {
                        ConfigureWindows.GetMainWindow.MainImage.Width = ConfigureWindows.GetMainWindow.ActualWidth;
                        ConfigureWindows.GetMainWindow.MainImage.Height = ConfigureWindows.GetMainWindow.ActualHeight;
                    }
                }
            });

            pic = await ImageDecoder.RenderToBitmapSource(file).ConfigureAwait(false);
            if (pic is null)
            {
                archive = SupportedFiles.IsSupportedArchives(file);
                if (archive == false)
                {
                    await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Send, () =>
                    {
                        Unload();
                    });
                    return;
                }
            }
            else
            {
                await TryFitImageAsync(file).ConfigureAwait(false);

                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Send, () =>
                {
                    ConfigureWindows.GetMainWindow.MainImage.Source = pic;
                });
            }

            await GetValues(file).ConfigureAwait(false);

            if (Pics.Count > 0)
            {
                FolderIndex = Pics.IndexOf(file);
            }
            else
            {
                FolderIndex = 0;
            }

            if (archive == false)
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Send, () =>
                {
                    UpdatePic(FolderIndex, pic, false);
                });

                await Preloader.PreLoad(FolderIndex).ConfigureAwait(false);
                await Preloader.AddAsync(FolderIndex).ConfigureAwait(false);
            }

            if (FolderIndex > 0)
            {
                await Taskbar.Progress((double)FolderIndex / Pics.Count).ConfigureAwait(false);
            }

            if (Properties.Settings.Default.FullscreenGalleryHorizontal || Properties.Settings.Default.FullscreenGalleryVertical)
            {
                await GalleryLoad.Load().ConfigureAwait(false);
            }

            FreshStartup = false;

            // Add recent files, except when browing archive
            if (string.IsNullOrWhiteSpace(TempZipFile) && Pics?.Count > FolderIndex)
            {
                RecentFiles.Add(Pics?[FolderIndex]);
            }
        }

        /// <summary>
        /// Determine proper path from given string value
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        internal static async Task LoadPicFromString(string path, bool checkExists = true)
        {
            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, () =>
            {
                // Set Loading
                SetLoadingString();

                if (ConfigureWindows.GetMainWindow.MainImage.Source == null)
                {
                    BitmapSource? bitmapSource = GetBitmapSourceThumb(path);
                    if (bitmapSource != null)
                    {
                        ConfigureWindows.GetMainWindow.MainImage.Source = bitmapSource;
                    }
                }

                // Don't allow image size to stretch the whole screen, fixes when opening new image from unloaded status
                if (XWidth < 1)
                {
                    ConfigureWindows.GetMainWindow.MainImage.Width = ConfigureWindows.GetMainWindow.ParentContainer.ActualWidth;
                    ConfigureWindows.GetMainWindow.MainImage.Height = ConfigureWindows.GetMainWindow.ParentContainer.ActualHeight;
                }
            });

            if (checkExists && File.Exists(path))
            {
                // set up size so it feels better when starting application
                await TryFitImageAsync(path).ConfigureAwait(false);

                await LoadPiFromFileAsync(path).ConfigureAwait(false);

                FreshStartup = false;
            }
            else
            {
                bool result = Uri.TryCreate(path, UriKind.Absolute, out Uri? uriResult)
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
                    await TryFitImageAsync(path).ConfigureAwait(false);

                    await LoadPiFromFileAsync(path).ConfigureAwait(false);

                    FreshStartup = false;
                }

                else if (Directory.Exists(path))
                {
                    await LoadPicFromFolderAsync(path).ConfigureAwait(false);

                    FreshStartup = false;
                }
                else
                {
                    await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, () =>
                    {
                        Unload();
                    });
                }
            }
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
            else if (Pics.Contains(path) == false)
            {
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

            if (Properties.Settings.Default.FullscreenGalleryHorizontal || Properties.Settings.Default.FullscreenGalleryVertical)
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
            FreshStartup = false;
        }

        /// <summary>
        /// Loads image at specified index
        /// </summary>
        /// <param name="index">The index of file to load from Pics</param>
        internal static async Task LoadPicAtIndexAsync(int index, bool resize = true)
        {
            if (Pics?.Count < index || Pics?.Count < 1)
            {
                return;
            }

            if (GetToolTipMessage is not null && GetToolTipMessage.IsVisible)
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, () =>
                {
                    GetToolTipMessage.Visibility = Visibility.Hidden;
                });
            }

            FolderIndex = index;
            var preloadValue = Preloader.Get(Navigation.Pics[index]);

            // Initate loading behavior, if needed
            if (preloadValue == null || preloadValue.isLoading)
            {
                // Show a thumbnail while loading
                BitmapSource? thumb = null;
                
                if (FreshStartup == false && GalleryFunctions.IsOpen == false)
                {
                    thumb = GetBitmapSourceThumb(Pics[FolderIndex]);
                }

                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, () =>
                {
                    if (GalleryFunctions.IsOpen)
                    {
                        thumb = GetThumb(index);
                    }

                    if (FreshStartup)
                    {
                        // Set loading from translation service
                        SetLoadingString();
                        FreshStartup = false;
                    }
                    else
                    {
                        var image = Application.Current.Resources["Image"] as string;

                        ConfigureWindows.GetMainWindow.TitleText.ToolTip =
                        ConfigureWindows.GetMainWindow.Title =
                        ConfigureWindows.GetMainWindow.TitleText.Text
                        = $"{image} {index + 1} / {Pics?.Count}";
                    }

                    if (thumb != null)
                    {
                        ConfigureWindows.GetMainWindow.MainImage.Source = thumb;
                    }
                });

                if (FastPicRunning) // Holding down button is too fast and will be laggy when not just loading thumbnails
                {
                    return;
                }

                while (preloadValue != null && preloadValue.isLoading)
                {
                    // Wait for finnished result
                    await Task.Delay(5).ConfigureAwait(false);

                    // Make loading skippable
                    if (FolderIndex != index)
                    {
                        // Start preloading when browsing very fast to catch up
                        await Preloader.PreLoad(FolderIndex).ConfigureAwait(false);
                        return;
                    }
                }
                if (preloadValue == null) // Error correctiom
                {
                    await Preloader.AddAsync(index).ConfigureAwait(false);
                    preloadValue = Preloader.Get(Navigation.Pics[index]);
                }
            }

            // Make loading skippable
            if (FolderIndex != index)
            {
                // Start preloading when browsing very fast to catch up
                if (Preloader.IsRunning == false)
                {
                    await Preloader.PreLoad(FolderIndex).ConfigureAwait(false);
                }
                return;
            }

            // Check if works, if not show error message
            if (preloadValue == null || preloadValue.bitmapSource == null)
            {
                preloadValue = new Preloader.PreloadValue(ImageFunctions.ImageErrorMessage(), false);

                if (preloadValue == null || preloadValue.bitmapSource == null)
                {
                    Error_Handling.Unload();
                    ShowTooltipMessage(Application.Current.Resources["UnexpectedError"]);
                    return;
                }
            }

            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Send, () =>
            {
                UpdatePic(index, preloadValue.bitmapSource, resize);
            });

            await ImageInfo.UpdateValuesAsync(Pics?[FolderIndex]).ConfigureAwait(false);

            if (Pics?.Count > 1)
            {
                if (Preloader.IsRunning == false)
                {
                    await Preloader.PreLoad(index).ConfigureAwait(false);
                }
                await Taskbar.Progress((double)index / Pics.Count).ConfigureAwait(false);
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
        internal static void UpdatePic(int index, BitmapSource bitmapSource, bool resise = true)
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
                if (GetQuickSettingsMenu is not null && GetQuickSettingsMenu.FlipButton is not null)
                {
                    GetQuickSettingsMenu.FlipButton.TheButton.IsChecked = false;
                }

                ConfigureWindows.GetMainWindow.MainImage.LayoutTransform = null;
            }

            // Loads gif from XamlAnimatedGif if neccesary
            string? ext = Path.GetExtension(Pics?[index]);
            if (ext is not null && ext.Equals(".gif", StringComparison.OrdinalIgnoreCase))
            {
                XamlAnimatedGif.AnimationBehavior.SetSourceUri(ConfigureWindows.GetMainWindow.MainImage, new Uri(Pics?[index]));
            }
            else
            {
                ConfigureWindows.GetMainWindow.MainImage.Source = bitmapSource;
            }

            if (resise)
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
        internal static async Task UpdatePic(string imageName, BitmapSource bitmapSource)
        {
            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Send, () =>
            {
                Unload();

                if (Properties.Settings.Default.ScrollEnabled)
                {
                    ConfigureWindows.GetMainWindow.Scroller.ScrollToTop();
                }

                ConfigureWindows.GetMainWindow.MainImage.Source = bitmapSource;

                FitImage(bitmapSource.PixelWidth, bitmapSource.PixelHeight);
                SetTitleString(bitmapSource.PixelWidth, bitmapSource.PixelHeight, imageName);
            });

            CloseToolTipMessage();

            await Taskbar.NoProgress().ConfigureAwait(false);
            FolderIndex = 0;

            await ImageInfo.UpdateValuesAsync(imageName).ConfigureAwait(false);
        }

        /// <summary>
        /// Load a picture from a prepared bitmap
        /// </summary>
        /// <param name="pic"></param>
        /// <param name="imageName"></param>
        internal static void Pic(BitmapSource bitmap, string imageName)
        {
            _ = UpdatePic(imageName, bitmap);

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
            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, async () =>
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
            });

            await Taskbar.NoProgress().ConfigureAwait(false);
            FolderIndex = 0;

            await ImageInfo.UpdateValuesAsync(file).ConfigureAwait(false);

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
            if (Application.Current.Resources["Base64Image"] is not string b64)
            {
                return;
            }
            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Send, async () =>
            {
                await UpdatePic(b64, pic).ConfigureAwait(false);
            });
        }

        /// <summary>
        /// Handle logic if user wants to load from a folder
        /// </summary>
        /// <param name="folder"></param>
        internal static async Task LoadPicFromFolderAsync(string folder)
        {
            // TODO add new function that can go to next/prev folder
            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, () =>
            {
                ChangeFolder(true);
            });


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
            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, async () =>
            {
                if (GetImageSettingsMenu is not null)
                {
                    GetImageSettingsMenu.GoToPic.GoToPicBox.Text = (FolderIndex + 1).ToString(CultureInfo.CurrentCulture);
                }

                // Load new gallery values, if changing folder
                if (GetPicGallery != null && Properties.Settings.Default.FullscreenGalleryHorizontal || GetPicGallery != null && Properties.Settings.Default.FullscreenGalleryVertical)
                {
                    if (GetPicGallery.Container.Children.Count == 0)
                    {
                        await GalleryLoad.Load().ConfigureAwait(false);
                    }
                }
            });
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

            // Go to the image!
            await LoadPicAtIndexAsync(startingpoint).ConfigureAwait(false);

            // Update PicGallery selected item, if needed
            if (GalleryFunctions.IsOpen)
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, () =>
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
                });
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
                preloadValue = Preloader.Get(Navigation.Pics[FolderIndex]);
            }
            else
            {
                preloadValue = Preloader.Get(Navigation.Pics[FolderIndex]);

                if (preloadValue == null) // Error correctiom
                {
                    await Preloader.AddAsync(FolderIndex).ConfigureAwait(false);
                    preloadValue = Preloader.Get(Navigation.Pics[FolderIndex]);
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

            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Send, () =>
            {
                UpdatePic(FolderIndex, preloadValue.bitmapSource);
            });
        }

        #endregion Change navigation values
    }
}