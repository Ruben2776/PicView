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
using static PicView.ChangeImage.Navigation;
using static PicView.FileHandling.ArchiveExtraction;
using static PicView.FileHandling.FileLists;
using static PicView.ImageHandling.Thumbnails;
using static PicView.UILogic.SetTitle;
using static PicView.UILogic.Sizing.ScaleImage;
using static PicView.UILogic.Tooltip;
using static PicView.UILogic.UC;

namespace PicView.ChangeImage
{
    internal static class LoadPic
    {
        #region QuickLoad
        /// <summary>
        /// Quickly load image and then update values
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        internal static async Task QuickLoadAsync(string file)
        {
            if (File.Exists(file) == false)
            {
                await LoadPicFromString(file, false).ConfigureAwait(false);
                if (Properties.Settings.Default.AutoFitWindow)
                {
                    await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
                    {
                        UILogic.Sizing.WindowSizing.SetWindowBehavior();
                    });
                }
                return;
            }

            await QuickLoadAsync(new FileInfo(file)).ConfigureAwait(false);
        }

        internal static async Task QuickLoadAsync(FileInfo fileInfo)
        {
            bool archive = false;
            var pic = await ImageDecoder.RenderToBitmapSource(fileInfo).ConfigureAwait(false);
            if (pic is null)
            {
                archive = SupportedFiles.IsSupportedArchives(fileInfo.FullName);
                if (archive == false)
                {
                    await Dispatcher.CurrentDispatcher.InvokeAsync(() =>
                    {
                        Unload();
                    });
                    return;
                }
            }
            else
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Send, () =>
                {
                    if (fileInfo.Extension == ".gif")
                    {
                        XamlAnimatedGif.AnimationBehavior.SetSourceUri(ConfigureWindows.GetMainWindow.MainImage, new Uri(fileInfo.FullName));
                    }
                    else
                    {
                        ConfigureWindows.GetMainWindow.MainImage.Source = pic;
                    }

                    if (Properties.Settings.Default.AutoFitWindow)
                    {
                        UILogic.Sizing.WindowSizing.SetWindowBehavior();
                    }

                    FitImage(pic.PixelWidth, pic.PixelHeight);
                    SetLoadingString();
                });
            }

            await GetValuesAsync(fileInfo).ConfigureAwait(false);

            switch (Pics.Count)
            {
                case > 0:
                    FolderIndex = Pics.IndexOf(fileInfo.FullName);
                    break;
                default:
                    FolderIndex = 0;
                    break;
            }
            FolderIndex = FolderIndex == -1 ? 0 : FolderIndex; // Fixes weird error if you load example.jpg where the actual name is example.JPG

            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
            {
                SetTitleString(pic.PixelWidth, pic.PixelHeight, FolderIndex, fileInfo);
            });

            if (archive == false)
            {
                await Task.Run(() => Preloader.PreLoad(FolderIndex)).ConfigureAwait(false);
                await Preloader.AddAsync(FolderIndex).ConfigureAwait(false);
            }

            if (FolderIndex > 0)
            {
                await Taskbar.Progress((double)FolderIndex / Pics.Count).ConfigureAwait(false);
            }

            if (GalleryFunctions.IsVerticalFullscreenOpen || GalleryFunctions.IsHorizontalFullscreenOpen)
            {
                await GalleryLoad.Load().ConfigureAwait(false);
            }

            FreshStartup = false;

            // Add recent files, except when browing archive
            if (string.IsNullOrWhiteSpace(TempZipFile) && Pics?.Count > FolderIndex)
            {
                History.Add(Pics?[FolderIndex]);
            }
        }

        #endregion

        #region LoadPicAtValue

        /// <summary>
        /// Determine proper path from given string value
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        internal static async Task LoadPicFromString(string path, bool checkExists = true)
        {
            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Render, () =>
            {
                UC.ToggleStartUpUC(true);
            });

            if (checkExists && File.Exists(path))
            {
                await LoadingPreview(new FileInfo(path)).ConfigureAwait(false);

                // set up size so it feels better when starting application
                await TryFitImageAsync(path).ConfigureAwait(false);

                await LoadPiFromFileAsync(path).ConfigureAwait(false);

                FreshStartup = false;
            }
            else
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
                {
                    // Set Loading
                    SetLoadingString();
                });

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
                    await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
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
            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Render, () =>
            {
                UC.ToggleStartUpUC(true);
            });

            FileInfo? fileInfo = new FileInfo(path);
            if (fileInfo.Exists == false)
            {
                await LoadPicFromString(path, false).ConfigureAwait(false);
                return;
            }

            if (Pics.Count > FolderIndex && fileInfo.DirectoryName == Path.GetDirectoryName(Pics[FolderIndex]))
            {
                if (Pics.Contains(fileInfo.FullName) == false)
                {
                    await GetValuesAsync(fileInfo).ConfigureAwait(false);
                }
                await LoadPicAtIndexAsync(Pics.IndexOf(path), true, false).ConfigureAwait(false);
                return;
            }

            if (fileInfo.Length < 5e+7)
            {
                await QuickLoadAsync(fileInfo).ConfigureAwait(false);
                return;
            }

            await LoadingPreview(fileInfo).ConfigureAwait(false);

            bool folderChanged = false;

            // If count not correct or just started, get values
            if (Pics?.Count <= FolderIndex || FolderIndex < 0 || FreshStartup)
            {
                await GetValuesAsync(fileInfo).ConfigureAwait(false);
                folderChanged = true;
            }
            // If the file is in the same folder, navigate to it. If not, start manual loading procedure.
            else if (!string.IsNullOrWhiteSpace(Pics?[FolderIndex]) && Path.GetDirectoryName(path) != Path.GetDirectoryName(Pics[FolderIndex]))
            {
                // Reset old values and get new
                ChangeFolder(true);
                await GetValuesAsync(fileInfo).ConfigureAwait(false);
                folderChanged = true;
            }
            else if (Pics.Contains(path) == false)
            {
                await GetValuesAsync(fileInfo).ConfigureAwait(false);
            }

            if (Pics?.Count > 0)
            {
                FolderIndex = Pics.IndexOf(path);
            }

            if (FolderIndex < 0)
            {
                FolderIndex = 0;
            }

            if (FreshStartup is false || folderChanged)
            {
                Preloader.Clear();
            }

            if (FolderIndex >= 0 && Pics?.Count > 0) // check if being extracted and need to wait for it instead
            {
                // Navigate to picture using obtained index
                await LoadPicAtIndexAsync(FolderIndex, true, false).ConfigureAwait(false);
            }

            if (GetPicGallery is not null && folderChanged)
            {
                if (Properties.Settings.Default.FullscreenGalleryHorizontal || Properties.Settings.Default.FullscreenGalleryVertical)
                {
                    await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)(() =>
                    {
                        // Remove children before loading new
                        if (GetPicGallery.Container.Children.Count > 0)
                        {
                            GetPicGallery.Container.Children.Clear();
                        }
                    }));

                    // Load new gallery values, if changing folder
                    await GalleryLoad.Load().ConfigureAwait(false);
                }
            }

            FreshStartup = false;
        }

        /// <summary>
        /// Loads image at specified index
        /// </summary>
        /// <param name="index">The index of file to load from Pics</param>
        internal static async Task LoadPicAtIndexAsync(int index, bool resize = true, bool showLoadingThumb = true)
        {
            if (Pics?.Count < index || Pics?.Count < 1)
            {
                return;
            }

            if (GetToolTipMessage is not null && GetToolTipMessage.IsVisible)
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
                {
                    GetToolTipMessage.Visibility = Visibility.Hidden;
                });
            }

            FolderIndex = index;
            var preloadValue = Preloader.Get(Navigation.Pics[index]);

            // Initate loading behavior, if needed
            if (preloadValue == null || preloadValue.isLoading)
            {
                if (showLoadingThumb)
                {
                    // Show a thumbnail while loading
                    BitmapSource? thumb = null;

                    if (GalleryFunctions.IsHorizontalFullscreenOpen == false || GalleryFunctions.IsVerticalFullscreenOpen == false)
                    {
                        thumb = GetBitmapSourceThumb(new FileInfo(Pics[FolderIndex]));
                    }

                    await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
                    {
                        if (GalleryFunctions.IsHorizontalFullscreenOpen || GalleryFunctions.IsVerticalFullscreenOpen)
                        {
                            thumb = GetThumb(index);
                        }

                        if (FreshStartup)
                        {
                            // Set loading from translation service
                            SetLoadingString();
                            FreshStartup = false;
                        }

                        if (thumb != null)
                        {
                            ConfigureWindows.GetMainWindow.MainImage.Source = thumb;
                        }
                    });
                }

                if (FastPicRunning) // Holding down button is too fast and will be laggy when not just loading thumbnails
                {
                    var image = Application.Current.Resources["Image"] as string;

                    ConfigureWindows.GetMainWindow.TitleText.ToolTip =
                    ConfigureWindows.GetMainWindow.Title =
                    ConfigureWindows.GetMainWindow.TitleText.Text
                    = $"{image} {index + 1} / {Pics?.Count}";

                    // Add recent files, except when browing archive
                    if (string.IsNullOrWhiteSpace(TempZipFile) && Pics?.Count > FolderIndex)
                    {
                        History.Add(Pics?[FolderIndex]);
                    }

                    return;
                }

                if (preloadValue is not null)
                {
                    preloadValue = await CheckLoadingAsync(preloadValue, index).ConfigureAwait(false);
                    if (preloadValue is null) { return; }
                }
                else // Error correctiom
                {
                    await Preloader.AddAsync(index).ConfigureAwait(false);
                    preloadValue = Preloader.Get(Navigation.Pics[index]);

                    if (preloadValue == null)
                    {
                        // Trying again fixes error when recovering from divide by zero
                        await Preloader.AddAsync(index).ConfigureAwait(false);
                        preloadValue = Preloader.Get(Navigation.Pics[index]);

                        if (preloadValue == null)
                        {
                            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
                            {
                                Error_Handling.Unload();
                                ShowTooltipMessage(Application.Current.Resources["UnexpectedError"]);
                            });
                            return;
                        }
                    }

                    preloadValue = await CheckLoadingAsync(preloadValue, index).ConfigureAwait(false);
                    if (preloadValue is null) { return; }
                }
            }

            // Make loading skippable
            if (FolderIndex != index)
            {
                // Start preloading when browsing very fast to catch up
                if (Preloader.IsRunning == false)
                {
                    await Task.Run(() => Preloader.PreLoad(FolderIndex)).ConfigureAwait(false);
                }

                if (GalleryFunctions.IsHorizontalFullscreenOpen || GalleryFunctions.IsVerticalFullscreenOpen)
                {
                    await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
                    {
                        GalleryNavigation.FullscreenGalleryNavigation();
                    });
                }
                return;
            }

            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Send, () =>
            {
                UpdatePic(index, preloadValue.bitmapSource, resize, preloadValue.fileInfo);
            });

            // Update PicGallery selected item, if needed
            if (GalleryFunctions.IsHorizontalFullscreenOpen || GalleryFunctions.IsVerticalFullscreenOpen)
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
                {
                    GalleryNavigation.FullscreenGalleryNavigation();
                });
            }

            await ImageInfo.UpdateValuesAsync(preloadValue.fileInfo).ConfigureAwait(false);

            if (Pics?.Count > 1)
            {
                if (Preloader.IsRunning == false)
                {
                    await Task.Run(() => Preloader.PreLoad(index)).ConfigureAwait(false);
                }

                if (FolderIndex == index)
                {
                    await Taskbar.Progress((double)index / Pics.Count).ConfigureAwait(false);
                }
            }

            // Add recent files, except when browing archive
            if (string.IsNullOrWhiteSpace(TempZipFile) && Pics?.Count > index)
            {
                History.Add(Pics?[index]);
            }
        }

        /// <summary>
        /// Handle logic if user wants to load from a folder
        /// </summary>
        /// <param name="folder"></param>
        internal static async Task LoadPicFromFolderAsync(string folder)
        {
            // TODO add new function that can go to next/prev folder
            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
            {
                SetLoadingString();
                ChangeFolder(true);
            });

            // If searching subdirectories, it might freeze UI, so wrap it in task
            await Task.Run(() =>
            {
                Pics = FileList(new FileInfo(folder));
            }).ConfigureAwait(false);

            if (Pics?.Count > 0)
            {
                await LoadPicAtIndexAsync(0).ConfigureAwait(false);
            }
            else
            {
                await ReloadAsync(true).ConfigureAwait(false);
            }
            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Background, async () =>
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

        #endregion

        #region UpdatePic

        /// <summary>
        /// Update picture, size it and set the title from index
        /// </summary>
        /// <param name="index"></param>
        /// <param name="bitmapSource"></param>
        internal static void UpdatePic(int index, BitmapSource bitmapSource, bool resise = true, FileInfo? fileInfo = null)
        {
            if (bitmapSource is null)
            {
                bitmapSource = ImageFunctions.ImageErrorMessage();
                if (bitmapSource is null)
                {
                    Error_Handling.Unload();
                    ShowTooltipMessage(Application.Current.Resources["UnexpectedError"]);
                    return;
                }
            }

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
            string? ext = fileInfo is null ? Path.GetExtension(Pics?[index]) : fileInfo.Extension;
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

            SetTitleString(bitmapSource.PixelWidth, bitmapSource.PixelHeight, index, fileInfo);
        }

        /// <summary>
        /// Update picture, size it and set the title from string
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="bitmapSource"></param>
        internal static async Task UpdatePic(string imageName, BitmapSource bitmapSource)
        {
            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Send, () =>
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
            });

            await Taskbar.NoProgress().ConfigureAwait(false);
            FolderIndex = 0;

            await ImageInfo.UpdateValuesAsync(null).ConfigureAwait(false);
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
            FileInfo fileInfo = new FileInfo(file);
            BitmapSource? bitmapSource = isGif ? null : await ImageDecoder.RenderToBitmapSource(fileInfo).ConfigureAwait(false);
            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, async () =>
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
                    await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
                    {
                        Error_Handling.Unload();
                        ShowTooltipMessage(Application.Current.Resources["UnexpectedError"]);
                    });
                    return;
                }

                CloseToolTipMessage();
            });

            await Taskbar.NoProgress().ConfigureAwait(false);
            FolderIndex = 0;

            await ImageInfo.UpdateValuesAsync(fileInfo).ConfigureAwait(false);

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
            await UpdatePic(b64, pic).ConfigureAwait(false);
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
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () => { Error_Handling.Unload(); ShowTooltipMessage(Application.Current.Resources["UnexpectedError"]); });
                return;
            }

            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Send, () =>
            {
                UpdatePic(FolderIndex, preloadValue.bitmapSource);
            });
        }

        #endregion

        static async Task<Preloader.PreloadValue?> CheckLoadingAsync(Preloader.PreloadValue preloadValue, int index)
        {
            while (preloadValue.isLoading)
            {
                // Wait for finnished result
                await Task.Delay(5).ConfigureAwait(false);

                // Make loading skippable
                if (FolderIndex != index)
                {
                    // Start preloading when browsing very fast to catch up
                    await Task.Run(() => Preloader.PreLoad(FolderIndex)).ConfigureAwait(false);
                }
            }
            if (preloadValue.bitmapSource == null) // Show image error, unload if showing image error somehow fails
            {
                preloadValue = new Preloader.PreloadValue(ImageFunctions.ImageErrorMessage(), false, null);

                if (preloadValue == null || preloadValue.bitmapSource == null)
                {
                    await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
                    {
                        Error_Handling.Unload();
                        ShowTooltipMessage(Application.Current.Resources["UnexpectedError"]);
                    });
                    return null;
                }
            }
            return preloadValue;
        }

        static async Task LoadingPreview(FileInfo fileInfo)
        {
            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
            {
                // Set Loading
                SetLoadingString();

                if (ConfigureWindows.GetMainWindow.MainImage.Source == null)
                {
                    BitmapSource? bitmapSource = GetBitmapSourceThumb(fileInfo);
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
        }
    }
}
