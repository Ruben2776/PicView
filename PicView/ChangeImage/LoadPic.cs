using PicView.FileHandling;
using PicView.ImageHandling;
using PicView.PicGallery;
using PicView.Properties;
using PicView.SystemIntegration;
using PicView.UILogic;
using System.Diagnostics;
using System.IO;
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
        #region LoadPicAtValue

        /// <summary>
        /// Determine proper path from given string value
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        internal static async Task LoadPicFromStringAsync(string path, bool checkExists = true, FileInfo? fileInfo = null)
        {
            await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () => ToggleStartUpUC(true));

            if (checkExists && File.Exists(path))
            {
                if (fileInfo is null)
                {
                    fileInfo = new FileInfo(path);
                }
                await LoadPiFromFileAsync(fileInfo).ConfigureAwait(false);
            }
            else
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, () => SetLoadingString());

                string check = CheckIfLoadableString(path);
                switch (check)
                {
                    default: await LoadPiFromFileAsync(check).ConfigureAwait(false); return;
                    case "web": await HttpFunctions.LoadPicFromURL(path).ConfigureAwait(false); return;
                    case "base64": await UpdateImage.UpdateImageFromBase64PicAsync(path).ConfigureAwait(false); return;
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
            try
            {
                var fileInfo = new FileInfo(path);
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

        /// <summary>
        /// Loads a picture from a given file path and does extra error checking
        /// </summary>
        /// <param name="path"></param>
        internal static async Task LoadPiFromFileAsync(FileInfo fileInfo)
        {
            LoadingPreview(fileInfo);
            await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() => ToggleStartUpUC(true));
            bool folderChanged = false;

            if (fileInfo.Exists == false) // If file does not exist, try to load it if base64 or URL
            {
                await LoadPicFromStringAsync(fileInfo.FullName, false, fileInfo).ConfigureAwait(false);
                return;
            }

            if (FolderIndex < 0 || Pics.Count <= 0)
            {
                await RetrieveFilelistAsync(fileInfo).ConfigureAwait(false);
            }
            else if (Pics.Count > FolderIndex && fileInfo.DirectoryName == Path.GetDirectoryName(Pics[FolderIndex]))
            {
                if (Pics.Contains(fileInfo.FullName) == false)
                {
                    await RetrieveFilelistAsync(fileInfo).ConfigureAwait(false);
                }
                await LoadPicAtIndexAsync(Pics.IndexOf(fileInfo.FullName), fileInfo).ConfigureAwait(false);
                return;
            }
            else
            {
                folderChanged = await CheckDirectoryChangeAndPicGallery(fileInfo).ConfigureAwait(false);
                await RetrieveFilelistAsync(fileInfo).ConfigureAwait(false);
            }

            if (Pics?.Count > 0)
            {
                FolderIndex = Pics.IndexOf(fileInfo.FullName);
            }

            if (FolderIndex < 0)
            {
                FolderIndex = 0;
            }

            if (FreshStartup is false || folderChanged) // Clear preloaded cache if needed
            {
                Preloader.Clear();
            }

            if (FolderIndex >= 0 && Pics?.Count > 0) // check if being extracted and need to wait for it instead
            {
                // Navigate to picture using obtained index
                await LoadPicAtIndexAsync(FolderIndex, fileInfo).ConfigureAwait(false);
            }

            FreshStartup = false;

            if (GalleryFunctions.IsHorizontalFullscreenOpen)
            {
                await GalleryLoad.Load().ConfigureAwait(false);
                GalleryNavigation.SetSelected(FolderIndex, true);
            }

            if (string.IsNullOrWhiteSpace(InitialPath) || folderChanged)
            {
                InitialPath = fileInfo.FullName;
            }
        }

        internal static async Task LoadPicFromArchiveAsync(string? archive, FileInfo? fileInfo = null)
        {
            fileInfo ??= new FileInfo(archive);
            Preloader.Clear();
            GalleryFunctions.Clear();
            await RetrieveFilelistAsync(fileInfo).ConfigureAwait(false);
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
        /// Loads image at specified index
        /// </summary>
        /// <param name="index">The index of file to load from Pics</param>
        internal static async Task LoadPicAtIndexAsync(int index, FileInfo? fileInfo = null)
        {
            if (ErrorHandling.CheckOutOfRange())
            {
                await ReloadAsync().ConfigureAwait(false);
                return;
            }

            FolderIndex = index;
            var preloadValue = Preloader.Get(Pics[index]);
            fileInfo ??= new FileInfo(Pics[index]);

            if (!fileInfo.Exists)
            {
                if (fileInfo.Attributes.HasFlag(FileAttributes.Directory))
                {
                    fileInfo = new FileInfo(Pics[index]);
                }
                else
                {
                    try // Fix deleting files outside application
                    {
                        Preloader.Clear();
                        await RetrieveFilelistAsync(fileInfo).ConfigureAwait(false);
                        await LoadPicAtIndexAsync(GetImageIterateIndex(Reverse, false)).ConfigureAwait(false);
                        return;
                    }
                    catch (Exception)
                    {
                        await ReloadAsync().ConfigureAwait(false);
                        return;
                    }
                }
            }

            if (preloadValue is null || preloadValue.IsLoading)
            {
                LoadingPreview(fileInfo);

                if (preloadValue is null)
                {
                    preloadValue = await Preloader.AddAsync(index, fileInfo).ConfigureAwait(false);
                    if (preloadValue is null)
                    {
                        await ErrorHandling.ReloadAsync().ConfigureAwait(false);
                        return;
                    }
                    else if (index != FolderIndex) return;
                }
                while (preloadValue.IsLoading)
                {
                    await Task.Delay(20).ConfigureAwait(false);

                    if (index != FolderIndex) return; // Skip loading if user went to next value
                }
            }

            await UpdateImage.UpdateImageAsync(index, preloadValue.BitmapSource, preloadValue.FileInfo).ConfigureAwait(false);

            if (GalleryFunctions.IsHorizontalFullscreenOpen)
            {
                GalleryNavigation.FullscreenGalleryNavigation();
            }

            if (GetToolTipMessage is not null && GetToolTipMessage.IsVisible)
            {
                ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Render, () =>
                {
                    GetToolTipMessage.Visibility = Visibility.Hidden;
                });
            }

            if (Pics?.Count > 1)
            {
                await Preloader.PreLoadAsync(index).ConfigureAwait(false);
                await Preloader.AddAsync(FolderIndex, preloadValue.FileInfo, preloadValue.BitmapSource).ConfigureAwait(false);

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
                GetFileHistory ??= new FileHistory();
                GetFileHistory.Add(Pics?[index]);
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
                    {
                        bitmapSource.Freeze();
                    }
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