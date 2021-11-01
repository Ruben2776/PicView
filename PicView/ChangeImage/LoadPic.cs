using PicView.FileHandling;
using PicView.ImageHandling;
using PicView.PicGallery;
using PicView.SystemIntegration;
using PicView.UILogic;
using System;
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
                LoadPicFromString(file, false);
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

        /// <summary>
        /// Quickly load image and then update values
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        internal static async Task QuickLoadAsync(FileInfo fileInfo)
        {
            bool archive = false;
            var pic = await ImageDecoder.ReturnBitmapSourceAsync(fileInfo).ConfigureAwait(false);
            if (pic is null)
            {
                archive = SupportedFiles.IsSupportedArchives(fileInfo.FullName);
                if (archive == false)
                {
                    ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Render, () =>
                    {
                        Unload(true);
                    });
                    return;
                }
            }
            else
            {
                ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Send, () =>
                {
                    if (fileInfo.Extension == ".gif")
                    {
                        XamlAnimatedGif.AnimationBehavior.SetSourceUri(ConfigureWindows.GetMainWindow.MainImage, new Uri(fileInfo.FullName));
                    }
                    else
                    {
                        ConfigureWindows.GetMainWindow.MainImage.Source = pic;
                    }

                    SetLoadingString();
                });
            }

            await RetrieveFilelistAsync(fileInfo).ConfigureAwait(false);

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

            ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Render, () =>
            {
                FitImage(pic.PixelWidth, pic.PixelHeight);

                if (Properties.Settings.Default.AutoFitWindow)
                {
                    UILogic.Sizing.WindowSizing.SetWindowBehavior();
                }

                SetTitleString(pic.PixelWidth, pic.PixelHeight, FolderIndex, fileInfo);
            });

            if (archive == false)
            {
                _ = Preloader.PreLoad(FolderIndex).ConfigureAwait(false);
                _ = Preloader.AddAsync(FolderIndex).ConfigureAwait(false);
            }

            if (FolderIndex > 0)
            {
                _ = Taskbar.Progress((double)FolderIndex / Pics.Count).ConfigureAwait(false);
            }

            if (GalleryFunctions.IsVerticalFullscreenOpen || GalleryFunctions.IsHorizontalFullscreenOpen)
            {
                _ = GalleryLoad.Load().ConfigureAwait(false);
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
        internal static void LoadPicFromString(string path, bool checkExists = true, FileInfo? fileInfo = null)
        {
            ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Render, () =>
            {
                UC.ToggleStartUpUC(true);
            });

            if (checkExists && File.Exists(path))
            {
                if (fileInfo is null)
                {
                    fileInfo = new FileInfo(path);
                }

                LoadingPreview(fileInfo);

                _ = LoadPiFromFileAsync(fileInfo).ConfigureAwait(false);
            }
            else
            {
                ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Render, () =>
                {
                    SetLoadingString();
                });

                string check = Error_Handling.CheckIfLoadableString(path);
                switch (check)
                {
                    default: _ = LoadPic.LoadPiFromFileAsync(check).ConfigureAwait(false); return;
                    case "web": _ = WebFunctions.PicWeb(path).ConfigureAwait(false); return;
                    case "base64": _ = LoadPic.LoadBase64PicAsync(path).ConfigureAwait(false); return;
                    case "directory": _ = LoadPic.LoadPicFromFolderAsync(path).ConfigureAwait(false); return;
                    case "": ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Render, () => { Unload(true); }); return;
                }
            }
        }

        /// <summary>
        /// Loads a picture from a given file path and does extra error checking
        /// </summary>
        /// <param name="path"></param>
        internal static async Task LoadPiFromFileAsync(string path)
        {
            var fileInfo = new FileInfo(path);
            await LoadPiFromFileAsync(fileInfo).ConfigureAwait(false);
        }

        /// <summary>
        /// Loads a picture from a given file path and does extra error checking
        /// </summary>
        /// <param name="path"></param>
        internal static async Task LoadPiFromFileAsync(FileInfo fileInfo)
        {
            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Render, () =>
            {
                UC.ToggleStartUpUC(true);
            });

            if (fileInfo.Exists == false)
            {
                LoadPicFromString(fileInfo.FullName, false, fileInfo);
                return;
            }

            if (Pics.Count > FolderIndex && fileInfo.DirectoryName == Path.GetDirectoryName(Pics[FolderIndex]))
            {
                if (Pics.Contains(fileInfo.FullName) == false)
                {
                    await RetrieveFilelistAsync(fileInfo).ConfigureAwait(false);
                }
                await LoadPicAtIndexAsync(Pics.IndexOf(fileInfo.FullName), false, fileInfo).ConfigureAwait(false);
                return;
            }

            LoadingPreview(fileInfo);

            bool folderChanged = await Error_Handling.CheckDirectoryChangeAndPicGallery(fileInfo).ConfigureAwait(false);
            await FileLists.RetrieveFilelistAsync(fileInfo).ConfigureAwait(false);

            if (Pics?.Count > 0)
            {
                FolderIndex = Pics.IndexOf(fileInfo.FullName);
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
                await LoadPicAtIndexAsync(FolderIndex, false, fileInfo).ConfigureAwait(false);
            }

            FreshStartup = false;

            if (GalleryFunctions.IsVerticalFullscreenOpen || GalleryFunctions.IsHorizontalFullscreenOpen)
            {
                await GalleryLoad.Load().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Handle logic if user wants to load from a folder
        /// </summary>
        /// <param name="folder"></param>
        internal static async Task LoadPicFromFolderAsync(string folder)
        {
            var fileInfo = new FileInfo(folder);
            if (fileInfo is null)
            {
                Error_Handling.UnexpectedError();
                return;
            }

            await LoadPicFromFolderAsync(fileInfo).ConfigureAwait(false);
        }

        /// <summary>
        /// Handle logic if user wants to load from a folder
        /// </summary>
        /// <param name="folder"></param>
        internal static async Task LoadPicFromFolderAsync(FileInfo fileInfo)
        {
            // TODO add new function that can go to next/prev folder
            await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
            {
                SetLoadingString();
                UC.ToggleStartUpUC(true);
            });

            bool folderChanged = await Error_Handling.CheckDirectoryChangeAndPicGallery(fileInfo).ConfigureAwait(false);

            if (FreshStartup is false || folderChanged)
            {
                Preloader.Clear();
            }

            await FileLists.RetrieveFilelistAsync(fileInfo).ConfigureAwait(false);

            if (Pics?.Count > 0)
            {
                await LoadPicAtIndexAsync(0).ConfigureAwait(false);
            }
            else
            {
                Error_Handling.UnexpectedError();
                return;
            }

            if (GalleryFunctions.IsVerticalFullscreenOpen || GalleryFunctions.IsHorizontalFullscreenOpen)
            {
                await GalleryLoad.Load().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Loads image at specified index
        /// </summary>
        /// <param name="index">The index of file to load from Pics</param>
        internal static async Task LoadPicAtIndexAsync(int index, bool showLoadingThumb = true, FileInfo? fileInfo = null)
        {
            if (Pics?.Count < index || Pics?.Count < 1)
            {
                return;
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
                        if (fileInfo is null)
                        {
                            fileInfo = new FileInfo(Pics[FolderIndex]);
                        }
                        if (fileInfo.Exists)
                        {
                            thumb = GetBitmapSourceThumb(fileInfo);
                        }
                        else
                        {
                            await FileLists.RetrieveFilelistAsync(fileInfo).ConfigureAwait(false);
                            await LoadPiFromFileAsync(fileInfo).ConfigureAwait(false);
                            return;
                        }
                    }

                    ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Render, () =>
                    {
                        if (GalleryFunctions.IsHorizontalFullscreenOpen || GalleryFunctions.IsVerticalFullscreenOpen)
                        {
                            thumb = GetThumb(index, fileInfo);
                            GalleryNavigation.FullscreenGalleryNavigation();
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

                    return;
                }

                if (preloadValue is not null)
                {
                    preloadValue = await CheckLoadingAsync(preloadValue, index).ConfigureAwait(false);
                    if (preloadValue is null)
                    {
                        if (FolderIndex == index)
                        {
                            Error_Handling.UnexpectedError();
                        }
                        return;
                    }
                }
                else if (FolderIndex == index && Pics?.Count > index && index > -1) // Error correctiom
                {
                    try
                    {
                        await Preloader.AddAsync(index).ConfigureAwait(false);
                        preloadValue = Preloader.Get(Navigation.Pics[index]);
                    }
                    catch (Exception)
                    {
                        if (FolderIndex == index)
                        {
                            Error_Handling.UnexpectedError();
                        }
                        return;
                    }

                    if (preloadValue == null)
                    {
                        if (index == FolderIndex)
                        {
                            // Trying again fixes error when recovering from divide by zero
                            await Preloader.AddAsync(index).ConfigureAwait(false);
                            preloadValue = Preloader.Get(Navigation.Pics[index]);
                        }
                        else { return; }

                        if (preloadValue == null)
                        {
                            if (FolderIndex == index)
                            {
                                Error_Handling.UnexpectedError();
                            }
                            return;
                        }
                    }

                    preloadValue = await CheckLoadingAsync(preloadValue, index).ConfigureAwait(false);
                    if (preloadValue == null)
                    {
                        if (FolderIndex == index)
                        {
                            Error_Handling.UnexpectedError();
                        }
                        return;
                    }
                }
            }

            // Make loading skippable
            if (FolderIndex != index)
            {
                // Start preloading when browsing very fast to catch up
                if (Preloader.IsRunning == false)
                {
                    _ = Preloader.PreLoad(index).ConfigureAwait(false);
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

            UpdatePic(index, preloadValue.bitmapSource, preloadValue.fileInfo);

            // Update PicGallery selected item, if needed
            if (GalleryFunctions.IsHorizontalFullscreenOpen || GalleryFunctions.IsVerticalFullscreenOpen)
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
                {
                    GalleryNavigation.FullscreenGalleryNavigation();
                });
            }
            else if (GetToolTipMessage is not null && GetToolTipMessage.IsVisible)
            {
                ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Render, () =>
                {
                    GetToolTipMessage.Visibility = Visibility.Hidden;
                });
            }

            _ = ImageInfo.UpdateValuesAsync(preloadValue.fileInfo).ConfigureAwait(false);

            if (Pics?.Count > 1)
            {
                if (Preloader.IsRunning == false)
                {
                    _ = Preloader.PreLoad(index).ConfigureAwait(false);
                }

                if (FolderIndex == index)
                {
                    _ = Taskbar.Progress((double)index / Pics.Count).ConfigureAwait(false);
                }
            }

            // Add recent files, except when browing archive
            if (string.IsNullOrWhiteSpace(TempZipFile) && Pics?.Count > index)
            {
                History.Add(Pics?[index]);
            }
        }

        #endregion

        #region UpdatePic

        /// <summary>
        /// Update picture, size it and set the title from index
        /// </summary>
        /// <param name="index"></param>
        /// <param name="bitmapSource"></param>
        internal static void UpdatePic(int index, BitmapSource bitmapSource, FileInfo? fileInfo = null)
        {
            ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Render, () =>
            {
                if (bitmapSource is null)
                {
                    bitmapSource = ImageFunctions.ImageErrorMessage();
                    if (bitmapSource is null)
                    {
                        Error_Handling.UnexpectedError();
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

                FitImage(bitmapSource.PixelWidth, bitmapSource.PixelHeight);
                SetTitleString(bitmapSource.PixelWidth, bitmapSource.PixelHeight, index, fileInfo);
            });
        }

        /// <summary>
        /// Update picture, size it and set the title from string
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="bitmapSource"></param>
        internal static void UpdatePic(string imageName, BitmapSource bitmapSource)
        {
            ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Render, () =>
            {
                Unload(false);

                if (Properties.Settings.Default.ScrollEnabled)
                {
                    ConfigureWindows.GetMainWindow.Scroller.ScrollToTop();
                }

                ConfigureWindows.GetMainWindow.MainImage.Source = bitmapSource;

                FitImage(bitmapSource.PixelWidth, bitmapSource.PixelHeight);
                SetTitleString(bitmapSource.PixelWidth, bitmapSource.PixelHeight, imageName);

                CloseToolTipMessage();

                UC.ToggleStartUpUC(true);
            });

            _ = Taskbar.NoProgress().ConfigureAwait(false);
            FolderIndex = 0;

            _ = ImageInfo.UpdateValuesAsync(null).ConfigureAwait(false);
        }

        /// <summary>
        /// Load a picture from a prepared bitmap
        /// </summary>
        /// <param name="pic"></param>
        /// <param name="imageName"></param>
        internal static void LoadPicFromBitmap(BitmapSource bitmap, string imageName)
        {
            ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Render, () =>
            {
                SetTitle.SetLoadingString();
            });

            UpdatePic(imageName, bitmap);

            ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Render, () =>
            {
                UC.ToggleStartUpUC(true);
            });
        }

        /// <summary>
        /// Load a picture from a prepared string
        /// </summary>
        /// <param name="pic"></param>
        /// <param name="imageName"></param>
        internal static async Task LoadPreparedPicAsync(string file, string imageName, bool isGif)
        {
            FileInfo fileInfo = new FileInfo(file);
            BitmapSource? bitmapSource = isGif ? null : await ImageDecoder.ReturnBitmapSourceAsync(fileInfo).ConfigureAwait(false);

            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, async () =>
            {
                if (Properties.Settings.Default.ScrollEnabled)
                {
                    ConfigureWindows.GetMainWindow.Scroller.ScrollToTop();
                }

                if (isGif)
                {
                    Size? imageSize = await ImageSizeFunctions.ImageSizeAsync(file).ConfigureAwait(true);
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
                    Error_Handling.UnexpectedError();
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
        internal static async Task LoadBase64PicAsync(string base64string)
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

            UpdatePic(b64, pic);
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
                Error_Handling.UnexpectedError();
                return;
            }

            UpdatePic(FolderIndex, preloadValue.bitmapSource);
        }

        #endregion

        static async Task<Preloader.PreloadValue?> CheckLoadingAsync(Preloader.PreloadValue preloadValue, int index)
        {
            while (preloadValue.isLoading)
            {
                // Wait for finnished result
                await Task.Delay(100).ConfigureAwait(false); // Using task delay makes it responsive and enables showing thumb whilst loading

                if (FolderIndex != index && Preloader.IsRunning == false)
                {
                    // Start preloading when browsing very fast to catch up
                    await Preloader.PreLoad(FolderIndex).ConfigureAwait(false);
                }
            }
            if (preloadValue.bitmapSource == null) // Show image error, unload if showing image error somehow fails
            {
                preloadValue = new Preloader.PreloadValue(ImageFunctions.ImageErrorMessage(), false, null);

                if (preloadValue == null || preloadValue.bitmapSource == null)
                {
                    Error_Handling.UnexpectedError();
                    return null;
                }
            }
            return preloadValue;
        }

        static void LoadingPreview(FileInfo fileInfo)
        {
            ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Render, () =>
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
