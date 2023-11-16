using PicView.FileHandling;
using PicView.ImageHandling;
using PicView.PicGallery;
using PicView.Properties;
using PicView.SystemIntegration;
using PicView.UILogic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using PicView.UILogic.Sizing;
using static PicView.ChangeImage.ErrorHandling;
using static PicView.ChangeImage.Navigation;
using static PicView.ChangeTitlebar.SetTitle;
using static PicView.FileHandling.ArchiveExtraction;
using static PicView.FileHandling.FileLists;
using static PicView.UILogic.UC;
using PicView.Views.UserControls.Gallery;
using System.Windows.Media.Imaging;
using ImageMagick;

namespace PicView.ChangeImage;

internal static class LoadPic
{
    #region Load Pic from String

    /// <summary>
    /// Determine proper path from given string value
    /// </summary>
    /// <param name="path"></param>
    /// <param name="fileInfo"></param>
    /// <returns></returns>
    internal static async Task LoadPicFromStringAsync(string? path, FileInfo? fileInfo = null)
    {
        await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(SetLoadingString);

        await Task.Run(async () =>
        {
            if (fileInfo is not null)
            {
                if (fileInfo.Exists)
                {
                    if (fileInfo.IsSupported())
                    {
                        await LoadPiFromFileAsync(null, fileInfo).ConfigureAwait(false);
                    }
                    else if (fileInfo.IsArchive())
                    {
                        await LoadPicFromArchiveAsync(path).ConfigureAwait(false);
                    }
                }
                else if (path is not null && !string.IsNullOrWhiteSpace(path.GetURL()) ||
                         !string.IsNullOrWhiteSpace(fileInfo.LinkTarget.GetURL()))
                {
                    await HttpFunctions.LoadPicFromUrlAsync(path).ConfigureAwait(false);
                }
                else if (fileInfo.Attributes.HasFlag(FileAttributes.Directory))
                {
                    await LoadPicFromFolderAsync(fileInfo, 0).ConfigureAwait(false);
                }
                else
                {
                    await ReloadAsync().ConfigureAwait(false);
                }
            }
            else if (!string.IsNullOrWhiteSpace(path))
            {
                var check = CheckIfLoadableString(path);
                switch (check)
                {
                    default:
                        await LoadPiFromFileAsync(check).ConfigureAwait(false);
                        return;

                    case "web":
                        await HttpFunctions.LoadPicFromUrlAsync(path).ConfigureAwait(false);
                        return;

                    case "base64":
                        await UpdateImage.UpdateImageFromBase64PicAsync(path).ConfigureAwait(false);
                        return;

                    case "directory":
                        await LoadPicFromFolderAsync(path).ConfigureAwait(false);
                        return;

                    case "zip":
                        await LoadPicFromArchiveAsync(path).ConfigureAwait(false);
                        return;

                    case "":
                        ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Render, () => Unload(true));
                        return;
                }
            }
            else
            {
                await ReloadAsync().ConfigureAwait(false);
            }
        });
    }

    #endregion Load Pic from String

    #region Load Pic from File

    /// <summary>
    /// Loads a picture from a given file path and does extra error checking
    /// </summary>
    /// <param name="path"></param>
    /// <param name="fileInfo"></param>
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
            await ReloadAsync(true).ConfigureAwait(false);
        }
    }

    private static async Task LoadPiFromFileAsync(FileInfo fileInfo)
    {
        LoadingPreview(fileInfo);

        await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() => ToggleStartUpUC(true));

        if (!fileInfo.Exists) // If file does not exist, try to load it if base64 or URL
        {
            await LoadPicFromStringAsync(fileInfo.FullName, fileInfo).ConfigureAwait(false);
            return;
        }

        if (Pics.Count == 0)
        {
            if (fileInfo.IsArchive())
            {
                await LoadPicFromArchiveAsync(fileInfo.FullName).ConfigureAwait(false);
                return;
            }

            Pics = await Task.FromResult(FileList(fileInfo)).ConfigureAwait(false);
        }

        var folderChanged = CheckDirectoryChangeAndPicGallery(fileInfo);

        if (folderChanged)
        {
            PreLoader.Clear();

            Pics = await Task.FromResult(FileList(fileInfo)).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(InitialPath) || folderChanged)
                InitialPath = fileInfo.FullName;
        }
        else
        {
            if (GetPicGallery is not null)
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
                {
                    GetPicGallery.Visibility = Visibility.Visible;
                    // Select next item
                    GalleryNavigation.SetSelected(FolderIndex, false);
                });
            }
            if (Pics.Count > PreLoader.MaxCount)
                PreLoader.Clear();
        }

        FolderIndex = Pics.IndexOf(fileInfo.FullName);
        await LoadPicAtIndexAsync(FolderIndex, fileInfo).ConfigureAwait(false);
        if (Settings.Default.IsBottomGalleryShown)
        {
            if (folderChanged)
            {
                await GalleryLoad.ReloadGalleryAsync().ConfigureAwait(false);
            }
            else
            {
                var checkIfEmpty = false;
                await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
                {
                    checkIfEmpty = GetPicGallery?.Container.Children.Count <= 0;
                });
                if (checkIfEmpty)
                {
                    await GalleryLoad.ReloadGalleryAsync().ConfigureAwait(true);
                }
                await GetPicGallery.Dispatcher.InvokeAsync(() =>
                {
                    GalleryNavigation.SetSelected(FolderIndex, true);
                    GalleryNavigation.SelectedGalleryItem = FolderIndex;
                    GalleryNavigation.ScrollToGalleryCenter();
                });
            }
        }
    }

    #endregion Load Pic from File

    #region Load Pic from Archive

    /// <summary>
    /// Initiate loading behavior for archive extraction logic
    /// </summary>
    /// <param name="archive"></param>
    internal static async Task LoadPicFromArchiveAsync(string? archive)
    {
        await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
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
            PreLoader.Clear();
            GalleryFunctions.Clear();
            var extraction = Extract(archive);
            if (!extraction)
            {
                _ = ReloadAsync(true).ConfigureAwait(false);
            }
        }).ConfigureAwait(false);
    }

    #endregion Load Pic from Archive

    #region Load Pic from Folder

    /// <summary>
    /// Handle logic if user wants to load from a folder
    /// </summary>
    /// <param name="folder"></param>
    internal static async Task LoadPicFromFolderAsync(string folder)
    {
        var fileInfo = new FileInfo(folder);

        await LoadPicFromFolderAsync(fileInfo).ConfigureAwait(false);
    }

    /// <summary>
    /// Handle logic if user wants to load from a folder
    /// </summary>
    /// <param name="fileInfo"></param>
    /// <param name="index"></param>
    internal static async Task LoadPicFromFolderAsync(FileInfo fileInfo, int index = -1)
    {
        if (CheckOutOfRange() == false)
        {
            BackupPath = Pics[FolderIndex];
        }

        var folderChanged = CheckDirectoryChangeAndPicGallery(fileInfo);

        if (folderChanged)
        {
            PreLoader.Clear();
        }

        Pics = await Task.FromResult(FileList(fileInfo)).ConfigureAwait(false);

        if (Pics.Count <= 0) // If no files, reload if possible or unload if not
        {
            if (!string.IsNullOrWhiteSpace(BackupPath))
            {
                await ReloadAsync(true).ConfigureAwait(false);
            }
            else
            {
                Unload(true);
            }
            return;
        }

        if (Settings.Default.IsBottomGalleryShown)
        {
            await GetPicGallery?.Dispatcher.InvokeAsync(() =>
            {
                GetPicGallery.Visibility = Visibility.Visible;
            });
        }

        if (index >= 0)
        {
            await LoadPicAtIndexAsync(index, fileInfo).ConfigureAwait(false);
        }
        else
        {
            await LoadPicAtIndexAsync(0, fileInfo).ConfigureAwait(false);
        }

        if (Settings.Default.IsBottomGalleryShown)
        {
            if (folderChanged)
            {
                await GalleryLoad.ReloadGalleryAsync().ConfigureAwait(false);
            }
            else
            {
                var checkIfEmpty = false;
                await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
                {
                    checkIfEmpty = GetPicGallery?.Container.Children.Count <= 0;
                });
                if (checkIfEmpty)
                {
                    await GalleryLoad.ReloadGalleryAsync().ConfigureAwait(false);
                }
            }
        }

        if (folderChanged || string.IsNullOrWhiteSpace(InitialPath))
        {
            InitialPath = fileInfo.FullName;
        }
    }

    #endregion Load Pic from Folder

    #region Load Pic at Index

    /// <summary>
    /// Loads the image at the specified index asynchronously and updates the UI.
    /// </summary>
    /// <param name="index">The index of the image to load.</param>
    /// <param name="fileInfo">The file information for the image. If not specified, the file information will be obtained from the image list using the specified index.</param>
    internal static async Task LoadPicAtIndexAsync(int index, FileInfo? fileInfo = null)
    {
        if (index < 0 || index >= Pics.Count)
        {
            await ReloadAsync().ConfigureAwait(false);
            return;
        }

        FolderIndex = index;
        var preLoadValue = PreLoader.Get(index);
        fileInfo ??= preLoadValue?.FileInfo ?? new FileInfo(Pics[index]);

        if (!fileInfo.Exists)
        {
            try
            {
                fileInfo = new FileInfo(Path.GetInvalidFileNameChars().Aggregate(fileInfo.FullName, (current, c) => current.Replace(c.ToString(), string.Empty)));

                if (fileInfo.Attributes.HasFlag(FileAttributes.Directory))
                {
                    // If the file is a directory, create a new FileInfo object using the file path from the image list.
                    fileInfo = new FileInfo(Pics[index]);
                }
                else if (!fileInfo.Exists) // Fix deleting files outside application
                {
                    PreLoader.Clear();
                    Pics = await Task.FromResult(FileList(fileInfo)).ConfigureAwait(false);
                    if (Pics.Count is 0)
                    {
                        Unload(true);
                        return;
                    }
                    var navigateTo = Reverse ? NavigateTo.Previous : NavigateTo.Next;
                    await GoToNextImage(navigateTo).ConfigureAwait(false);
                    return;
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Trace.WriteLine($"{nameof(LoadPicAtIndexAsync)} {fileInfo.Name} exception:\n{ex.Message}");
#endif
                Tooltip.ShowTooltipMessage(ex.Message, true);
                await ReloadAsync().ConfigureAwait(false);
                return;
            }
        }

        // If the preload value for the image is null or the image is still loading,
        // display the loading preview and wait until the image is loaded.
        if (preLoadValue is null or { BitmapSource: null })
        {
            using var image = new MagickImage();
            image.Ping(fileInfo);
            BitmapSource? thumb = null;
            if (GetPicGallery != null)
            {
                var fromGallery = false;
                await UC.GetPicGallery.Dispatcher.InvokeAsync(() =>
                {
                    if (GetPicGallery.Container.Children.Count > 0 && index < GetPicGallery.Container.Children.Count)
                    {
                        var y = GetPicGallery.Container.Children[index] as PicGalleryItem;
                        thumb = (BitmapSource)y.ThumbImage.Source;
                        fromGallery = true;
                    }
                });
                if (!fromGallery)
                {
                    var exifThumbnail = image.GetExifProfile()?.CreateThumbnail();
                    thumb = exifThumbnail?.ToBitmapSource();
                }
            }
            else
            {
                var exifThumbnail = image.GetExifProfile()?.CreateThumbnail();
                thumb = exifThumbnail?.ToBitmapSource();
            }

            if (index != FolderIndex)
            {
                await PreLoader.PreLoadAsync(index, Pics.Count).ConfigureAwait(false);
                return; // Skip loading if user went to next value
            }

            await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
            {
                SetLoadingString();
                GetSpinWaiter.Visibility = Visibility.Visible;

                ConfigureWindows.GetMainWindow.MainImage.Source = thumb;
                if (image is { Height: > 0, Width: > 0 })
                    ScaleImage.FitImage(image.Width, image.Height);
            }, DispatcherPriority.Send);

            // Update gallery selections
            if (GetPicGallery is not null && index == FolderIndex)
            {
                await UC.GetPicGallery.Dispatcher.InvokeAsync(() =>
                {
                    if (index != FolderIndex)
                        return;
                    // Select next item
                    GalleryNavigation.SetSelected(FolderIndex, true);
                    GalleryNavigation.SelectedGalleryItem = FolderIndex;
                    GalleryNavigation.ScrollToGalleryCenter();
                });
            }

            if (preLoadValue is null)
            {
                var bitmapSource = await ImageDecoder.ReturnBitmapSourceAsync(fileInfo).ConfigureAwait(false);
                preLoadValue = new PreLoader.PreLoadValue(bitmapSource, fileInfo);
            }
            while (preLoadValue.BitmapSource is null)
            {
                await Task.Delay(10).ConfigureAwait(false);

                if (index != FolderIndex)
                {
                    await PreLoader.PreLoadAsync(index, Pics.Count).ConfigureAwait(false);
                    return; // Skip loading if user went to next value
                }
            }
        }
        else if (GetPicGallery is not null)
        {
            await UC.GetPicGallery.Dispatcher.InvokeAsync(() =>
            {
                if (index != FolderIndex)
                    return;
                // Select next item
                GalleryNavigation.SetSelected(FolderIndex, true);
                GalleryNavigation.SelectedGalleryItem = FolderIndex;
                GalleryNavigation.ScrollToGalleryCenter();
            });
        }

        if (index != FolderIndex)
        {
            await PreLoader.PreLoadAsync(index, Pics.Count).ConfigureAwait(false);
            return; // Skip loading if user went to next value
        }

        await UpdateImage.UpdateImageValuesAsync(index, preLoadValue).ConfigureAwait(false);

        if (ConfigureWindows.GetImageInfoWindow is { IsVisible: true })
            _ = ImageInfo.UpdateValuesAsync(preLoadValue.FileInfo).ConfigureAwait(false);

        _ = PreLoader.AddAsync(index, preLoadValue.FileInfo, preLoadValue.BitmapSource).ConfigureAwait(false);
        if (Pics.Count > 1)
        {
            Taskbar.Progress((double)index / Pics.Count);
            _ = PreLoader.PreLoadAsync(index, Pics.Count).ConfigureAwait(false);
        }

        // Add recent files, except when browsing archive
        if (string.IsNullOrWhiteSpace(TempZipFile) && Pics.Count > index)
        {
            GetFileHistory ??= new FileHistory();
            GetFileHistory.Add(Pics[index]);
        }
    }

    #endregion Load Pic at Index

    #region Loading Preview

    /// <summary>
    /// Loads a thumbnail preview of an image file and displays a loading message while it's being loaded.
    /// </summary>
    /// <param name="fileInfo">The file information of the image to be loaded.</param>
    /// <param name="showLoading"></param>
    internal static void LoadingPreview(FileInfo fileInfo, bool showLoading = true)
    {
        var thumb = Thumbnails.GetThumb(FolderIndex, fileInfo);

        ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Send, () =>
        {
            if (showLoading)
            {
                // Set Loading
                SetLoadingString();
            }
            GetSpinWaiter.Visibility = Visibility.Visible;

            ConfigureWindows.GetMainWindow.MainImage.Source = thumb;
        });
    }

    #endregion Loading Preview
}