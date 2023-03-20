using PicView.FileHandling;
using PicView.ImageHandling;
using PicView.PicGallery;
using PicView.SystemIntegration;
using PicView.UILogic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using static PicView.ChangeImage.ErrorHandling;
using static PicView.ChangeImage.Navigation;
using static PicView.ChangeTitlebar.SetTitle;
using static PicView.FileHandling.ArchiveExtraction;
using static PicView.FileHandling.FileLists;
using static PicView.UILogic.Sizing.ScaleImage;
using static PicView.UILogic.UC;

namespace PicView.ChangeImage
{
    internal static class LoadPic
    {
        #region LoadPicAtValue

        /// <summary>
        /// Determine proper path from given string value
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        internal static async Task LoadPicFromStringAsync(string? path, FileInfo? fileInfo = null)
        {
            if (fileInfo is not null)
            {
                if (fileInfo.Exists)
                {
                    if (SupportedFiles.IsSupported(fileInfo))
                    {
                        await LoadPiFromFileAsync(null, fileInfo).ConfigureAwait(false);
                    }
                    else if (SupportedFiles.IsArchive(fileInfo))
                    {
                        await LoadPicFromArchiveAsync(path, fileInfo).ConfigureAwait(false);
                    }
                }
                else if (fileInfo.Attributes.HasFlag(FileAttributes.Directory))
                {
                    await LoadPicFromFolderAsync(fileInfo, 0).ConfigureAwait(false);
                }
                else
                {
                    await ErrorHandling.ReloadAsync().ConfigureAwait(false);
                }
            }
            else if (!string.IsNullOrWhiteSpace(path))
            {
                string check = CheckIfLoadableString(path);
                switch (check)
                {
                    default: await LoadPiFromFileAsync(check).ConfigureAwait(false); return;
                    case "web": await HttpFunctions.LoadPicFromURL(path).ConfigureAwait(false); return;
                    case "base64": await UpdateImage.UpdateImageFromBase64PicAsync(path).ConfigureAwait(false); return;
                    case "directory": await LoadPicFromFolderAsync(path).ConfigureAwait(false); return;
                    case "zip": await LoadPicFromArchiveAsync(path, fileInfo).ConfigureAwait(!false); return;
                    case "": ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Render, () => Unload(true)); return;
                }
            }
            else
            {
                await ErrorHandling.ReloadAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Loads a picture from a given file path and does extra error checking
        /// </summary>
        /// <param name="path"></param>
        internal static async Task LoadPiFromFileAsync(string? path, FileInfo? fileInfo = null)
        {
            fileInfo ??= new FileInfo(path);
            try
            {
                await LoadPiFromFileAsync(fileInfo).ConfigureAwait(false);
            }
            catch (Exception e)
            {
#if DEBUG
                Trace.WriteLine("OpenWith exception \n" + e.Message);

#endif
                Tooltip.ShowTooltipMessage(e);
                await ErrorHandling.ReloadAsync(true).ConfigureAwait(false);
            }
        }

        private static async Task LoadPiFromFileAsync(FileInfo fileInfo)
        {
            LoadingPreview(fileInfo);
            await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() => ToggleStartUpUC(true));

            if (!fileInfo.Exists)  // If file does not exist, try to load it if base64 or URL
            {
                await LoadPicFromStringAsync(fileInfo.FullName, fileInfo).ConfigureAwait(false);
                return;
            }

            if (Pics.Count == 0)
            {
                if (SupportedFiles.IsArchive(fileInfo))
                {
                    await LoadPicFromArchiveAsync(null, fileInfo).ConfigureAwait(false);
                    return;
                }
                else
                {
                    await RetrieveFilelistAsync(fileInfo).ConfigureAwait(false);
                }
            }

            var folderChanged = await CheckDirectoryChangeAndPicGallery(fileInfo).ConfigureAwait(false);

            if (folderChanged)
            {
                Preloader.Clear();

                await RetrieveFilelistAsync(fileInfo).ConfigureAwait(false);

                if (GalleryFunctions.IsHorizontalFullscreenOpen)
                {
                    await GalleryLoad.Load().ConfigureAwait(false);
                }

                if (string.IsNullOrWhiteSpace(InitialPath) || folderChanged)
                    InitialPath = fileInfo.FullName;
            }
            else if (Pics.Count > Preloader.MaxCount) Preloader.Clear();

            FolderIndex = Pics.IndexOf(fileInfo.FullName);
            await LoadPicAtIndexAsync(FolderIndex, fileInfo).ConfigureAwait(false);

            FreshStartup = false;
        }

        internal static async Task LoadPicFromArchiveAsync(string? archive, FileInfo? fileInfo = null)
        {
            ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Normal, () =>
            {
                ToggleStartUpUC(true);
                SetLoadingString();
            });
            fileInfo ??= new FileInfo(archive);
            Preloader.Clear();
            GalleryFunctions.Clear();
            if (!ArchiveExtraction.IsBeingExtraced)
            {
                await RetrieveFilelistAsync(fileInfo).ConfigureAwait(false);
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

            if (Pics.Count <= 0)
            {
                await ReloadAsync().ConfigureAwait(false);
                return;
            }

            if (index >= 0)
            {
                await LoadPicAtIndexAsync(index, fileInfo).ConfigureAwait(false);
            }
            else
            {
                await LoadPicAtIndexAsync(0, fileInfo).ConfigureAwait(false);
            }

            if (GalleryFunctions.IsHorizontalFullscreenOpen)
            {
                await GalleryLoad.Load().ConfigureAwait(false);
            }

            if (folderChanged || string.IsNullOrWhiteSpace(InitialPath))
            {
                InitialPath = fileInfo.FullName;
            }
        }

        /// <summary>
        /// Loads the image at the specified index asynchronously and updates the UI.
        /// </summary>
        /// <param name="index">The index of the image to load.</param>
        /// <param name="fileInfo">The file information for the image. If not specified, the file information will be obtained from the image list using the specified index.</param>
        internal static async Task LoadPicAtIndexAsync(int index, FileInfo? fileInfo = null)
        {
            if (GetQuickResize is not null && GetQuickResize.Opacity > 0) return;

            FolderIndex = index;
            var preloadValue = Preloader.Get(Pics[index]);
            fileInfo ??= new FileInfo(Pics[index]);

            if (!fileInfo.Exists)
            {
                if (fileInfo.Attributes.HasFlag(FileAttributes.Directory))
                {
                    // If the file is a directory, create a new FileInfo object using the file path from the image list.
                    fileInfo = new FileInfo(Pics[index]);
                }
                else
                {
                    try // Fix deleting files outside application
                    {
                        Preloader.Clear();
                        await RetrieveFilelistAsync(fileInfo).ConfigureAwait(false);
                        var reverse = Reverse ? NavigateTo.Previous : NavigateTo.Next;
                        await GoToNextImage(reverse, false).ConfigureAwait(false);
                        return;
                    }
                    catch (Exception)
                    {
                        await ReloadAsync().ConfigureAwait(false);
                        return;
                    }
                }
            }

            // If the preload value for the image is null or the image is still loading,
            // display the loading preview and wait until the image is loaded.
            if (preloadValue is null || preloadValue.IsLoading)
            {
                LoadingPreview(fileInfo);

                if (preloadValue is null)
                {
                    await Preloader.AddAsync(index, fileInfo).ConfigureAwait(false);
                    preloadValue = Preloader.Get(Pics[index]);
                    if (preloadValue is null)
                    {
                        await ErrorHandling.ReloadAsync().ConfigureAwait(false);
                        return;
                    }
                    else if (index != FolderIndex)
                        return;
                }
                while (preloadValue.IsLoading)
                {
                    await Task.Delay(20).ConfigureAwait(false);

                    if (index != FolderIndex)
                        return; // Skip loading if user went to next value
                }
            }

            if (index != FolderIndex) return;

            await UpdateImage.UpdateImageAsync(index, preloadValue.BitmapSource, preloadValue.FileInfo).ConfigureAwait(false);

            if (GalleryFunctions.IsHorizontalFullscreenOpen)
                GalleryNavigation.FullscreenGalleryNavigation();

            if (GetToolTipMessage is not null && GetToolTipMessage.IsVisible)
                ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Normal, () =>
                    GetToolTipMessage.Visibility = Visibility.Hidden);

            if (Pics.Count > 1)
            {
                if (FolderIndex == index)
                    Taskbar.Progress((double)index / Pics.Count);

                await Preloader.AddAsync(FolderIndex, preloadValue.FileInfo, preloadValue.BitmapSource).ConfigureAwait(false);
                await Preloader.PreLoadAsync(index).ConfigureAwait(false);
            }

            if (ConfigureWindows.GetImageInfoWindow is not null)
                await ImageInfo.UpdateValuesAsync(preloadValue.FileInfo).ConfigureAwait(false);

            // Add recent files, except when browing archive
            if (string.IsNullOrWhiteSpace(TempZipFile) && Pics.Count > index)
            {
                GetFileHistory ??= new FileHistory();
                GetFileHistory.Add(Pics[index]);
            }
            FreshStartup = false;
        }

        #endregion LoadPicAtValue

        internal static void LoadingPreview(FileInfo fileInfo)
        {
            var bitmapSource = Thumbnails.GetBitmapSourceThumb(fileInfo);
            ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Send, () =>
            {
                // Set Loading
                SetLoadingString();

                if (bitmapSource != null)
                {
                    if (!bitmapSource.IsFrozen)
                        bitmapSource.Freeze();
                    ConfigureWindows.GetMainWindow.MainImage.Source = bitmapSource;
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