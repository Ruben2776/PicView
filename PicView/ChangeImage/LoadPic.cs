using PicView.FileHandling;
using PicView.ImageHandling;
using PicView.PicGallery;
using PicView.Properties;
using PicView.SystemIntegration;
using PicView.UILogic;
using PicView.UILogic.Sizing;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using XamlAnimatedGif;
using static PicView.ChangeImage.ErrorHandling;
using static PicView.ChangeImage.Navigation;
using static PicView.ChangeTitlebar.SetTitle;
using static PicView.FileHandling.ArchiveExtraction;
using static PicView.FileHandling.FileLists;
using static PicView.ImageHandling.Thumbnails;
using static PicView.UILogic.Sizing.ScaleImage;
using static PicView.UILogic.Tooltip;
using static PicView.UILogic.UC;
using Rotation = PicView.UILogic.TransformImage.Rotation;

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
            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
            {
                WindowSizing.SetWindowBehavior();
            });

            if (File.Exists(file) == false)
            {
                await LoadPicFromStringAsync(file, false).ConfigureAwait(false);
                return;
            }

            await QuickLoadAsync(new FileInfo(file)).ConfigureAwait(false);
            InitialPath = file;
        }

        /// <summary>
        /// Quickly load image and then update values
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        internal static async Task QuickLoadAsync(FileInfo fileInfo)
        {
            ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Render, () =>
            {
                SetLoadingString();
            });

            bool archive = fileInfo.Extension.Equals(fileInfo);
            BitmapSource? pic = null;

            if (archive is false)
            {
                pic = await ImageDecoder.ReturnBitmapSourceAsync(fileInfo).ConfigureAwait(false);
                if (pic is null)
                {
                    pic = ImageFunctions.ImageErrorMessage();
                }
                else
                {
                    ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Render, () =>
                    {
                        if (fileInfo.Extension == ".gif")
                        {
                            AnimationBehavior.SetSourceUri(ConfigureWindows.GetMainWindow.MainImage, new Uri(fileInfo.FullName));
                        }
                        else
                        {
                            ConfigureWindows.GetMainWindow.MainImage.Source = pic;
                        }

                        FitImage(pic.PixelWidth, pic.PixelHeight);
                    });
                }
            }

            await RetrieveFilelistAsync(fileInfo).ConfigureAwait(false);

            FolderIndex = Pics.Count > 0 ? Pics.IndexOf(fileInfo.FullName) : 0;

            if (pic is not null)
            {
                ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Render, () =>
                {
                    SetTitleString(pic.PixelWidth, pic.PixelHeight, FolderIndex, fileInfo);
                });
            }

            if (archive == false)
            {
                await Preloader.PreLoadAsync(FolderIndex).ConfigureAwait(false);
                await Preloader.AddAsync(FolderIndex, fileInfo, pic).ConfigureAwait(false);
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
                if (GetFileHistory is null)
                {
                    GetFileHistory = new FileHistory();
                }

                GetFileHistory.Add(Pics?[FolderIndex]);
            }
        }

        #endregion

        #region LoadPicAtValue

        /// <summary>
        /// Determine proper path from given string value
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        internal static async Task LoadPicFromStringAsync(string path, bool checkExists = true, FileInfo? fileInfo = null)
        {
            ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Render, () =>
            {
                ToggleStartUpUC(true);
            });

            if (checkExists && File.Exists(path))
            {
                if (fileInfo is null)
                {
                    fileInfo = new FileInfo(path);
                }

                LoadingPreview(fileInfo);

                await LoadPiFromFileAsync(fileInfo).ConfigureAwait(false);
            }
            else
            {
                ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Render, () =>
                {
                    SetLoadingString();
                });

                string check = CheckIfLoadableString(path);
                switch (check)
                {
                    default: await LoadPiFromFileAsync(check).ConfigureAwait(false); return;
                    case "web": await WebFunctions.PicWeb(path).ConfigureAwait(false); return;
                    case "base64": await LoadBase64PicAsync(path).ConfigureAwait(false); return;
                    case "directory": await LoadPicFromFolderAsync(path).ConfigureAwait(false); return;
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
                ToggleStartUpUC(true);
            });

            if (fileInfo.Exists == false)
            {
                await LoadPicFromStringAsync(fileInfo.FullName, false, fileInfo).ConfigureAwait(false);
                return;
            }

            if (Pics.Count > FolderIndex && fileInfo.DirectoryName == Path.GetDirectoryName(Pics[FolderIndex]))
            {
                if (Pics.Contains(fileInfo.FullName) == false)
                {
                    await RetrieveFilelistAsync(fileInfo).ConfigureAwait(false);
                }
                await LoadPicAtIndexAsync(Pics.IndexOf(fileInfo.FullName), fileInfo).ConfigureAwait(false);
                return;
            }

            LoadingPreview(fileInfo);

            bool folderChanged = await CheckDirectoryChangeAndPicGallery(fileInfo).ConfigureAwait(false);
            await RetrieveFilelistAsync(fileInfo).ConfigureAwait(false);

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
                await LoadPicAtIndexAsync(FolderIndex, fileInfo).ConfigureAwait(false);
            }

            FreshStartup = false;

            if (GalleryFunctions.IsVerticalFullscreenOpen || GalleryFunctions.IsHorizontalFullscreenOpen)
            {
                await GalleryLoad.Load().ConfigureAwait(false);
                GalleryNavigation.SetSelected(FolderIndex, true);
            }

            if (string.IsNullOrWhiteSpace(InitialPath) || folderChanged)
            {
                InitialPath = fileInfo.FullName;
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
                UnexpectedError();
                return;
            }

            await LoadPicFromFolderAsync(fileInfo).ConfigureAwait(false);
        }

        /// <summary>
        /// Handle logic if user wants to load from a folder
        /// </summary>
        /// <param name="folder"></param>
        internal static async Task LoadPicFromFolderAsync(FileInfo fileInfo, int index = -1)
        {
            // TODO add new function that can go to next/prev folder
            await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
            {
                SetLoadingString();
                ToggleStartUpUC(true);
            });

            if (CheckOutOfRange() == false)
            {
                BackupPath = Pics[FolderIndex];
            }

            bool folderChanged = await CheckDirectoryChangeAndPicGallery(fileInfo).ConfigureAwait(false);

            if (FreshStartup is false || folderChanged)
            {
                Preloader.Clear();
            }

            await RetrieveFilelistAsync(fileInfo).ConfigureAwait(false);

            if (Pics.Count < 0) // TODO make function to find first folder with pics, when not browsing recursively
            {
                await ReloadAsync(true).ConfigureAwait(false);
                return;
            }

            if (index >= 0)
            {
                await LoadPicAtIndexAsync(index).ConfigureAwait(false);
            }
            else
            {
                await LoadPicAtIndexAsync(0).ConfigureAwait(false);
            }

            if (GalleryFunctions.IsVerticalFullscreenOpen || GalleryFunctions.IsHorizontalFullscreenOpen)
            {
                await GalleryLoad.Load().ConfigureAwait(false);
            }

            if (folderChanged || string.IsNullOrWhiteSpace(InitialPath))
            {
                InitialPath = fileInfo.FullName;
            }
        }

        /// <summary>
        /// Loads image at specified index
        /// </summary>
        /// <param name="index">The index of file to load from Pics</param>
        internal static async Task LoadPicAtIndexAsync(int index, FileInfo? fileInfo = null)
        {
            if (Pics?.Count < index || Pics?.Count < 1)
            {
                //  Prevent infinite loading when dropping folder and can't find file
                try
                {
                    await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, async () =>
                    {
                        if (ConfigureWindows.GetMainWindow.TitleText.Text == (string)Application.Current.Resources["Loading"])
                        {
                            await ReloadAsync(true).ConfigureAwait(false);
                        }
                    });
                }
                catch (Exception e)
                {
#if DEBUG
                    System.Diagnostics.Trace.WriteLine(e.Message);
#endif
                    await ReloadAsync(true).ConfigureAwait(false);
                }

                return;
            }

            FolderIndex = index;
            var preloadValue = Preloader.Get(Pics[index]);

            // Initate loading behavior, if needed
            if (preloadValue == null || preloadValue.IsLoading)
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
                        try
                        {
                            thumb = GetBitmapSourceThumb(fileInfo);
                        }
                        catch (Exception)
                        {
                            thumb = null;
                        }
                    }
                    else
                    {
                        try // Fix deleting files outside application
                        {
                            var x = index - 1 >= 0 ? index - 1 : 0;
                            fileInfo = new FileInfo(Pics[x]);
                            await RetrieveFilelistAsync(fileInfo).ConfigureAwait(false);
                            await LoadPiFromFileAsync(fileInfo).ConfigureAwait(false);
                            return;
                        }
                        catch (Exception)
                        {
                            thumb = null;
                        }
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

                    // Don't allow image size to stretch the whole screen
                    if (XWidth == 0)
                    {
                        ConfigureWindows.GetMainWindow.MainImage.Width = ConfigureWindows.GetMainWindow.ParentContainer.ActualWidth;
                        ConfigureWindows.GetMainWindow.MainImage.Height = ConfigureWindows.GetMainWindow.ParentContainer.ActualHeight;
                    }
                });

                if (preloadValue is null)
                {
                    bool added = await Preloader.AddAsync(index, fileInfo).ConfigureAwait(false);
                    if (added)
                    {
                        preloadValue = Preloader.Get(Pics[index]);
                    }

                    if (preloadValue is null)
                    {
                        Preloader.Remove(index);
                        return;
                    }

                    if (preloadValue.BitmapSource is null)
                    {
                        preloadValue.BitmapSource = ImageFunctions.ImageErrorMessage();
                    }
                }
                else
                {
                    while (preloadValue.IsLoading)
                    {
                        // Make loading skippable
                        if (FolderIndex != index)
                        {
                            await Preloader.PreLoadAsync(index).ConfigureAwait(false);

                            if (GalleryFunctions.IsHorizontalFullscreenOpen || GalleryFunctions.IsVerticalFullscreenOpen)
                            {
                                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
                                {
                                    GalleryNavigation.FullscreenGalleryNavigation();
                                });
                            }
                            return;
                        }

                        await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
                        {
                            SetLoadingString();
                            if (thumb is not null)
                            {
                                ConfigureWindows.GetMainWindow.MainImage.Source = thumb;
                            }
                        });

                        // Wait for finnished result
                        await Task.Delay(20).ConfigureAwait(false); // Using task delay makes it responsive and enables showing thumb whilst loading
                    }
                    if (preloadValue.BitmapSource == null) // Show image error, unload if showing image error somehow fails
                    {
                        preloadValue = new Preloader.PreloadValue(ImageFunctions.ImageErrorMessage(), false, null);

                        if (preloadValue == null || preloadValue.BitmapSource == null)
                        {
                            await Preloader.PreLoadAsync(index).ConfigureAwait(false);
                            return;
                        }
                    }
                }
            }

            // Make loading skippable
            if (FolderIndex != index)
            {
                if (GalleryFunctions.IsHorizontalFullscreenOpen || GalleryFunctions.IsVerticalFullscreenOpen)
                {
                    await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
                    {
                        GalleryNavigation.FullscreenGalleryNavigation();
                    });
                }
                //await Preloader.PreLoad(index).ConfigureAwait(false);
                return;
            }

            UpdatePic(index, preloadValue.BitmapSource, preloadValue.FileInfo);

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

            if (Pics?.Count > 1)
            {
                await Preloader.PreLoadAsync(index).ConfigureAwait(false);

                if (FolderIndex == index)
                {
                    await Taskbar.Progress((double)index / Pics.Count).ConfigureAwait(false);
                }
            }

            if (ConfigureWindows.GetImageInfoWindow is not null)
            {
                await ImageInfo.UpdateValuesAsync(preloadValue.FileInfo).ConfigureAwait(false);
            }

            // Add recent files, except when browing archive
            if (string.IsNullOrWhiteSpace(TempZipFile) && Pics?.Count > index)
            {
                if (GetFileHistory is null)
                {
                    GetFileHistory = new FileHistory();
                }
                GetFileHistory.Add(Pics?[index]);
            }
        }

        #endregion

        #region UpdatePic

        /// <summary>
        /// Update picture, size it and set the title from index
        /// </summary>
        /// <param name="index"></param>
        /// <param name="bitmapSource"></param>
        internal static void UpdatePic(int index, BitmapSource? bitmapSource, FileInfo? fileInfo = null)
        {
            ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Render, () =>
            {
                if (bitmapSource is null)
                {
                    bitmapSource = ImageFunctions.ImageErrorMessage();
                    if (bitmapSource is null)
                    {
                        UnexpectedError();
                        return;
                    }
                }

                // Scroll to top if scroll enabled
                if (Settings.Default.ScrollEnabled)
                {
                    ConfigureWindows.GetMainWindow.Scroller.ScrollToTop();
                }

                // Reset transforms if needed
                if (Rotation.Flipped || Rotation.RotationAngle != 0)
                {
                    Rotation.Flipped = false;
                    Rotation.RotationAngle = 0;
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
                    AnimationBehavior.SetSourceUri(ConfigureWindows.GetMainWindow.MainImage, new Uri(Pics?[index]));
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
        internal static async Task UpdatePicAsync(string imageName, BitmapSource bitmapSource)
        {
            ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Render, () =>
            {
                Unload(false);

                if (Settings.Default.ScrollEnabled)
                {
                    ConfigureWindows.GetMainWindow.Scroller.ScrollToTop();
                }

                ConfigureWindows.GetMainWindow.MainImage.Source = bitmapSource;

                FitImage(bitmapSource.PixelWidth, bitmapSource.PixelHeight);
                SetTitleString(bitmapSource.PixelWidth, bitmapSource.PixelHeight, imageName);

                CloseToolTipMessage();

                ToggleStartUpUC(true);
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
        internal static async Task LoadPicFromBitmapAsync(BitmapSource bitmap, string imageName)
        {
            ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Render, () =>
            {
                SetLoadingString();
            });

            await UpdatePicAsync(imageName, bitmap);

            ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Render, () =>
            {
                ToggleStartUpUC(true);
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
                ToggleStartUpUC(true);

                if (Settings.Default.ScrollEnabled)
                {
                    ConfigureWindows.GetMainWindow.Scroller.ScrollToTop();
                }

                if (isGif)
                {
                    Size? imageSize = await ImageSizeFunctions.GetImageSizeAsync(file).ConfigureAwait(true);
                    if (imageSize.HasValue)
                    {
                        FitImage(imageSize.Value.Width, imageSize.Value.Height);
                        SetTitleString((int)imageSize.Value.Width, (int)imageSize.Value.Height, imageName);
                    }
                    AnimationBehavior.SetSourceUri(ConfigureWindows.GetMainWindow.MainImage, new Uri(file));
                }
                else if (bitmapSource != null)
                {
                    ConfigureWindows.GetMainWindow.MainImage.Source = bitmapSource;
                    SetTitleString(bitmapSource.PixelWidth, bitmapSource.PixelHeight, imageName);
                    FitImage(bitmapSource.PixelWidth, bitmapSource.PixelHeight);
                }
                else
                {
                    UnexpectedError();
                    return;
                }

                CloseToolTipMessage();
            });

            await Taskbar.NoProgress().ConfigureAwait(false);
            FolderIndex = 0;

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

            await UpdatePicAsync(b64, pic).ConfigureAwait(false);
        }

        #endregion

        internal static void LoadingPreview(FileInfo fileInfo)
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
