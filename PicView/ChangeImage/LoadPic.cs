using PicView.FileHandling;
using PicView.ImageHandling;
using PicView.PicGallery;
using PicView.SystemIntegration;
using PicView.UILogic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
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
                        await LoadPicFromArchiveAsync(path).ConfigureAwait(false);
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
                    case "zip": await LoadPicFromArchiveAsync(path).ConfigureAwait(!false); return;
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

        static async Task LoadPiFromFileAsync(FileInfo fileInfo)
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
                    await LoadPicFromArchiveAsync(fileInfo.FullName).ConfigureAwait(false);
                    return;
                }
                else
                {
                    Pics = FileList(fileInfo);
                }
            }

            var folderChanged = CheckDirectoryChangeAndPicGallery(fileInfo);

            if (folderChanged)
            {
                Preloader.Clear();

                Pics = FileList(fileInfo);

                if (GalleryFunctions.IsHorizontalFullscreenOpen)
                {
                    await GalleryLoad.LoadAsync().ConfigureAwait(false);
                }

                if (string.IsNullOrWhiteSpace(InitialPath) || folderChanged)
                    InitialPath = fileInfo.FullName;
            }
            else if (Pics.Count > Preloader.MaxCount) Preloader.Clear();

            FolderIndex = Pics.IndexOf(fileInfo.FullName);
            await LoadPicAtIndexAsync(FolderIndex, fileInfo).ConfigureAwait(false);
        }

        internal static async Task LoadPicFromArchiveAsync(string? archive)
        {
            ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Normal, () =>
            {
                ToggleStartUpUC(true);
                SetLoadingString();
            });

            await Task.Run(() =>
            {
                if (Pics.Count > 0 && FolderIndex < Pics.Count)
                {
                    BackupPath = Pics[FolderIndex];
                }
                Preloader.Clear();
                GalleryFunctions.Clear();
                var extraction = ArchiveExtraction.Extract(archive);
                if (!extraction)
                {
                    // insert error message here?
                }
            }).ConfigureAwait(false);
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

            bool folderChanged = CheckDirectoryChangeAndPicGallery(fileInfo);

            if (folderChanged)
            {
                Preloader.Clear();
            }

            Pics = FileList(fileInfo);

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
                await GalleryLoad.LoadAsync().ConfigureAwait(false);
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
            FolderIndex = index;
            var preloadValue = Preloader.Get(index);
            fileInfo ??= preloadValue?.FileInfo ?? new FileInfo(Pics[index]);

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
                        Pics = FileList(fileInfo);
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
            if (preloadValue is null or { IsLoading : true })
            {
                LoadingPreview(fileInfo);

                if (preloadValue is null)
                {
                    await Preloader.AddAsync(index, fileInfo).ConfigureAwait(false);
                    preloadValue = Preloader.Get(index);
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

            if (GetToolTipMessage is not null and { IsVisible : true })
                ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Normal, () =>
                    GetToolTipMessage.Visibility = Visibility.Hidden);

            if (ConfigureWindows.GetImageInfoWindow is not null and { IsVisible : true})
                await ImageInfo.UpdateValuesAsync(preloadValue.FileInfo).ConfigureAwait(false);

            if (Pics.Count > 1)
            {
                if (FolderIndex == index)
                    Taskbar.Progress((double)index / Pics.Count);

                await Preloader.AddAsync(FolderIndex, preloadValue.FileInfo, preloadValue.BitmapSource).ConfigureAwait(false);
                await Preloader.PreLoadAsync(index).ConfigureAwait(false);
            }

            // Add recent files, except when browing archive
            if (string.IsNullOrWhiteSpace(TempZipFile) && Pics.Count > index)
            {
                GetFileHistory ??= new FileHistory();
                GetFileHistory.Add(Pics[index]);
            }
        }

        #endregion LoadPicAtValue


        /// <summary>
        /// Loads a thumbnail preview of an image file and displays a loading message while it's being loaded.
        /// </summary>
        /// <param name="fileInfo">The file information of the image to be loaded.</param>
        internal static void LoadingPreview(FileInfo fileInfo)
        {
            var bitmapSourceHolder = Thumbnails.GetBitmapSourceThumb(fileInfo);
            ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Send, () =>
            {
                // Set Loading
                SetLoadingString();

                ConfigureWindows.GetMainWindow.MainImage.Cursor = Cursors.Wait;

                ConfigureWindows.GetMainWindow.MainImage.Source = bitmapSourceHolder.Thumb;
                // Set to logo size or don't allow image size to stretch the whole screen, fixes when opening new image from unloaded status
                if (bitmapSourceHolder.isLogo || XWidth < 1)
                {
                    ConfigureWindows.GetMainWindow.MainImage.Width = bitmapSourceHolder.Size;
                    ConfigureWindows.GetMainWindow.MainImage.Height = bitmapSourceHolder.Size;
                }
            });
        }
    }
}