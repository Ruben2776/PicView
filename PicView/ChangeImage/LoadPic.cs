﻿using PicView.FileHandling;
using PicView.ImageHandling;
using PicView.PicGallery;
using PicView.UILogic;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using System.Windows.Threading;
using static PicView.ChangeImage.ErrorHandling;
using static PicView.ChangeImage.Navigation;
using static PicView.ChangeTitlebar.SetTitle;
using static PicView.FileHandling.ArchiveExtraction;
using static PicView.FileHandling.FileLists;
using static PicView.UILogic.Sizing.ScaleImage;
using static PicView.UILogic.UC;

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
        SetLoadingString();

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
            else if (path is not null && !string.IsNullOrWhiteSpace(path.GetURL()) || !string.IsNullOrWhiteSpace(fileInfo.LinkTarget.GetURL()))
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
                default: await LoadPiFromFileAsync(check).ConfigureAwait(false); return;
                case "web": await HttpFunctions.LoadPicFromUrlAsync(path).ConfigureAwait(false); return;
                case "base64": await UpdateImage.UpdateImageFromBase64PicAsync(path).ConfigureAwait(false); return;
                case "directory": await LoadPicFromFolderAsync(path).ConfigureAwait(false); return;
                case "zip": await LoadPicFromArchiveAsync(path).ConfigureAwait(false); return;
                case "": ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Render, () => Unload(true)); return;
            }
        }
        else
        {
            await ReloadAsync().ConfigureAwait(false);
        }
    }

    #endregion

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

        if (!fileInfo.Exists)  // If file does not exist, try to load it if base64 or URL
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

            Pics = FileList(fileInfo);
        }

        var folderChanged = CheckDirectoryChangeAndPicGallery(fileInfo);

        if (folderChanged)
        {
            PreLoader.Clear();

            Pics = FileList(fileInfo);

            if (GalleryFunctions.IsHorizontalFullscreenOpen)
            {
                await GalleryLoad.LoadAsync().ConfigureAwait(false);
            }

            if (string.IsNullOrWhiteSpace(InitialPath) || folderChanged)
                InitialPath = fileInfo.FullName;
        }
        else if (Pics.Count > PreLoader.MaxCount) PreLoader.Clear();

        FolderIndex = Pics.IndexOf(fileInfo.FullName);
        await LoadPicAtIndexAsync(FolderIndex, fileInfo).ConfigureAwait(false);
    }

    #endregion

    #region Load Pic from Archive

    /// <summary>
    /// Initiate loading behavior for archive extraction logic
    /// </summary>
    /// <param name="archive"></param>
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
            PreLoader.Clear();
            GalleryFunctions.Clear();
            var extraction = Extract(archive);
            if (!extraction)
            {
                // insert error message here?
            }
        }).ConfigureAwait(false);
    }

    #endregion

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

        var folderChanged = CheckDirectoryChangeAndPicGallery(fileInfo);

        if (folderChanged)
        {
            PreLoader.Clear();
        }

        Pics = FileList(fileInfo);

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

    #endregion

    #region Load Pic at Index 

    /// <summary>
    /// Loads the image at the specified index asynchronously and updates the UI.
    /// </summary>
    /// <param name="index">The index of the image to load.</param>
    /// <param name="fileInfo">The file information for the image. If not specified, the file information will be obtained from the image list using the specified index.</param>
    internal static async Task LoadPicAtIndexAsync(int index, FileInfo? fileInfo = null)
    {
        FolderIndex = index;
        var preLoadValue = PreLoader.Get(index);
        fileInfo ??= preLoadValue?.FileInfo ?? new FileInfo(Pics[index]);

        if (!fileInfo.Exists)
        {
            if (fileInfo.Attributes.HasFlag(FileAttributes.Directory))
            {
                // If the file is a directory, create a new FileInfo object using the file path from the image list.
                fileInfo = new FileInfo(Pics[index]);
            }
            else // Fix deleting files outside application
            {
                PreLoader.Clear();
                Pics = FileList(fileInfo);
                var navigateTo = Reverse ? NavigateTo.Previous : NavigateTo.Next;
                await GoToNextImage(navigateTo).ConfigureAwait(false);
                return;
            }
        }

        // If the preload value for the image is null or the image is still loading,
        // display the loading preview and wait until the image is loaded.
        if (preLoadValue is null or { IsLoading: true })
        {
            LoadingPreview(fileInfo);

            if (preLoadValue is null)
            {
                var bitmapSource = await ImageDecoder.ReturnBitmapSourceAsync(fileInfo).ConfigureAwait(false) ??
                                   ImageFunctions.ImageErrorMessage();
                preLoadValue = new PreLoader.PreLoadValue(bitmapSource, false, fileInfo);
                if (index != FolderIndex) return;
            }
            while (preLoadValue.IsLoading)
            {
                await Task.Delay(10).ConfigureAwait(false);

                if (index != FolderIndex) return;
            }
        }

        if (index != FolderIndex) return; // Skip loading if user went to next value

        await UpdateImage.UpdateImageAsync(index, preLoadValue).ConfigureAwait(false);
    }

    #endregion

    #region Loading Preview

    /// <summary>
    /// Loads a thumbnail preview of an image file and displays a loading message while it's being loaded.
    /// </summary>
    /// <param name="fileInfo">The file information of the image to be loaded.</param>
    /// <param name="showLoading"></param>
    internal static void LoadingPreview(FileInfo fileInfo, bool showLoading = true)
    {
        var isLogo = false;
        var thumb = Thumbnails.GetThumb(FolderIndex, fileInfo);
        if (thumb is null)
        {
            thumb = ImageFunctions.ShowLogo() ?? ImageFunctions.ImageErrorMessage();
            isLogo = true;
        }

        var bitmapSourceHolder = new Thumbnails.LogoOrThumbHolder(thumb, isLogo);
        ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Send, () =>
        {
            if (showLoading)
            {
                // Set Loading
                SetLoadingString();
            }

            ConfigureWindows.GetMainWindow.MainImage.Cursor = Cursors.Wait;

            ConfigureWindows.GetMainWindow.MainImage.Source = bitmapSourceHolder.Thumb;
            // Set to logo size or don't allow image size to stretch the whole screen, fixes when opening new image from unloaded status
            if (!bitmapSourceHolder.isLogo && !(XWidth < 1)) return;
            ConfigureWindows.GetMainWindow.MainImage.Width = Thumbnails.LogoOrThumbHolder.Size;
            ConfigureWindows.GetMainWindow.MainImage.Height = Thumbnails.LogoOrThumbHolder.Size;
        });
    }


    #endregion
}