using PicView.Core.Config;
using PicView.Core.FileHandling;
using PicView.Core.Navigation;
using PicView.WPF.FileHandling;
using PicView.WPF.ImageHandling;
using PicView.WPF.PicGallery;
using PicView.WPF.SystemIntegration;
using PicView.WPF.UILogic;
using PicView.WPF.UILogic.Sizing;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using static PicView.WPF.ChangeImage.ErrorHandling;
using static PicView.WPF.ChangeImage.Navigation;
using static PicView.WPF.ChangeTitlebar.SetTitle;
using static PicView.WPF.FileHandling.ArchiveExtraction;
using static PicView.WPF.FileHandling.FileLists;
using static PicView.WPF.UILogic.UC;
using ArchiveHelper = PicView.Core.FileHandling.ArchiveHelper;

namespace PicView.WPF.ChangeImage;

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
        await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
        {
            ToggleStartUpUC(true);
            SetLoadingString();
            GetSpinWaiter.Visibility = Visibility.Visible;
            ConfigureWindows.GetMainWindow.MainImage.Source = null;
        });

        await Task.Run(async () =>
        {
            if (fileInfo is not null)
            {
                if (fileInfo.Exists)
                {
                    if (fileInfo.IsSupported())
                    {
                        await LoadPiFromFileAsync(fileInfo).ConfigureAwait(false);
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
                var check = ErrorHelper.CheckIfLoadableString(path);
                switch (check)
                {
                    default:
                        await LoadPiFromFileAsync(check).ConfigureAwait(false);
                        return;

                    case "web":
                        await HttpFunctions.LoadPicFromUrlAsync(path).ConfigureAwait(false);
                        return;

                    case "base64":
                        await UpdateImage.UpdateImageFromBase64PicAsync(new FileInfo(path)).ConfigureAwait(false);
                        return;

                    case "directory":
                        await LoadPicFromFolderAsync(path).ConfigureAwait(false);
                        return;

                    case "zip":
                        await LoadPicFromArchiveAsync(path).ConfigureAwait(false);
                        return;

                    case "":
                        ConfigureWindows.GetMainWindow.Dispatcher.Invoke(DispatcherPriority.Render,
                            () => Unload(true));
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
        await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
        {
            ToggleStartUpUC(true);
            SetLoadingString();
            GetSpinWaiter.Visibility = Visibility.Visible;
            ConfigureWindows.GetMainWindow.MainImage.Source = null;
        });

        fileInfo ??= new FileInfo(path);
        try
        {
            await LoadPiFromFileAsync(fileInfo).ConfigureAwait(false);
        }
        catch (Exception e)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(LoadPiFromFileAsync)} exception:\n{e.Message}");

#endif
            Tooltip.ShowTooltipMessage(e, true, TimeSpan.FromSeconds(5));
            await ReloadAsync(true).ConfigureAwait(false);
        }
    }

    internal static async Task LoadPiFromFileAsync(FileInfo fileInfo)
    {
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
        if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
        {
            if (folderChanged)
            {
                if (GetPicGallery is null)
                {
                    await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
                    {
                        GalleryToggle.ShowBottomGallery();
                        ScaleImage.TryFitImage();
                    });

                    await GalleryLoad.LoadAsync().ConfigureAwait(false);
                }
                else
                {
                    await GalleryLoad.ReloadGalleryAsync().ConfigureAwait(false);
                }
            }
            else
            {
                if (GetPicGallery is null)
                {
                    await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
                    {
                        GalleryToggle.ShowBottomGallery();
                        ScaleImage.TryFitImage();
                    });

                    await GalleryLoad.LoadAsync().ConfigureAwait(false);
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

                await GetPicGallery?.Dispatcher?.InvokeAsync(() =>
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
            GetSpinWaiter.Visibility = Visibility.Visible;
            ConfigureWindows.GetMainWindow.MainImage.Source = null;
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
        await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
        {
            ToggleStartUpUC(true);
            SetLoadingString();
            GetSpinWaiter.Visibility = Visibility.Visible;
            ConfigureWindows.GetMainWindow.MainImage.Source = null;
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

        if (index >= 0)
        {
            await LoadPicAtIndexAsync(index, fileInfo).ConfigureAwait(false);
        }
        else
        {
            await LoadPicAtIndexAsync(0, fileInfo).ConfigureAwait(false);
        }

        if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
        {
            if (GetPicGallery is null)
            {
                await ConfigureWindows.GetMainWindow.Dispatcher.InvokeAsync(() =>
                {
                    GalleryToggle.ShowBottomGallery();
                    if (ConfigureWindows.GetMainWindow.MainImage.Source is not null)
                    {
                        ScaleImage.FitImage(ConfigureWindows.GetMainWindow.MainImage.Source.Width, ConfigureWindows.GetMainWindow.MainImage.Source.Height);
                    }
                });
            }
            else
            {
                await GetPicGallery?.Dispatcher.InvokeAsync(() =>
                {
                    GetPicGallery.Visibility = Visibility.Visible;
                    if (ConfigureWindows.GetMainWindow.MainImage.Source is not null)
                    {
                        ScaleImage.FitImage(ConfigureWindows.GetMainWindow.MainImage.Source.Width, ConfigureWindows.GetMainWindow.MainImage.Source.Height);
                    }
                });
            }

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

    internal static async Task LoadPicAtIndexAsync(int index, FileInfo? fileInfo = null)
    {
        using var cts = new CancellationTokenSource();
        var cancelToken = cts.Token;
        try
        {
            await LoadPicAtIndexAsync(index, cancelToken, fileInfo).ConfigureAwait(false);
        }
        catch (TaskCanceledException)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(LoadPicAtIndexAsync)} cancelled:\n");
#endif
        }
        catch (Exception ex)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(LoadPicAtIndexAsync)} exception:\n{ex.Message}");
#endif
            Tooltip.ShowTooltipMessage(ex.Message, true, TimeSpan.FromSeconds(5));
        }
    }

    /// <summary>
    /// Loads the image at the specified index asynchronously and updates the UI.
    /// </summary>
    /// <param name="index">The index of the image to load.</param>
    /// <param name="fileInfo">The file information for the image. If not specified, the file information will be obtained from the image list using the specified index.</param>
    private static async Task LoadPicAtIndexAsync(int index, CancellationToken cancellationToken, FileInfo? fileInfo = null) => await Task.Run(async () =>
    {
        if (index < 0 || index >= Pics.Count)
        {
            await ReloadAsync().ConfigureAwait(false);
            return;
        }

        FolderIndex = index;
        var preLoadValue = PreLoader.Get(index);
        fileInfo ??= preLoadValue?.FileInfo;
        if (fileInfo == null)
        {
            try
            {
                fileInfo = new FileInfo(Pics[index]);
            }
            catch (Exception ex)
            {
#if DEBUG
                Trace.WriteLine($"{nameof(LoadPicAtIndexAsync)} {index} exception:\n{ex.Message}");
#endif
                Tooltip.ShowTooltipMessage(ex.Message, true, TimeSpan.FromSeconds(5));
                return;
            }
        }

        if (!fileInfo.Exists)
        {
            var next = ImageIteration.GetNextIndex(Reverse ? NavigateTo.Previous : NavigateTo.Next,
                SettingsHelper.Settings.UIProperties.Looping, Pics, index);
            await LoadPicAtIndexAsync(next, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (preLoadValue != null)
        {
            var showThumb = true;
            while (preLoadValue.IsLoading)
            {
                if (showThumb)
                {
                    await SetThumb();
                    showThumb = false;
                }

                await Task.Delay(50, cancellationToken).ConfigureAwait(false);
                if (FolderIndex != index)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return; // Skip loading if user went to next value
                }
            }
        }
        else
        {
            fileInfo = new FileInfo(Pics[index]);
            await SetThumb();
            await PreLoader.AddAsync(index, fileInfo).ConfigureAwait(false);
            if (index != FolderIndex)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return; // Skip loading if user went to next value
            }
            preLoadValue = PreLoader.Get(index);
        }

        if (FolderIndex != index)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return; // Skip loading if user went to next value
        }

        await UpdateImage.UpdateImageValuesAsync(index, preLoadValue, cancellationToken).ConfigureAwait(false);

        if (GetPicGallery is not null)
        {
            await GetPicGallery?.Dispatcher?.InvokeAsync(() =>
            {
                if (index != FolderIndex)
                    return;
                // Select next item
                GalleryNavigation.SetSelected(FolderIndex, true);
                GalleryNavigation.SelectedGalleryItem = FolderIndex;
                GalleryNavigation.ScrollToGalleryCenter();
            }, DispatcherPriority.Render, cancellationToken);
        }

        if (ConfigureWindows.GetImageInfoWindow is { IsVisible: true })
            await ImageInfo.UpdateValuesAsync(preLoadValue?.FileInfo).ConfigureAwait(false);

        await PreLoader.AddAsync(index, preLoadValue?.FileInfo, preLoadValue?.BitmapSource).ConfigureAwait(false);
        if (Pics.Count > 1)
        {
            Taskbar.Progress((double)index / Pics.Count);
            await PreLoader.PreLoadAsync(index, Pics.Count).ConfigureAwait(false);
        }

        // Add recent files, except when browsing archive
        if (string.IsNullOrWhiteSpace(ArchiveHelper.TempFilePath) && Pics.Count > index)
        {
            FileHistoryNavigation.Add(Pics[index]);
        }
        return;

        async Task SetThumb()
        {
            var thumb = Thumbnails.GetThumb(index, fileInfo);
            if (thumb.HasValue)
            {
                await ConfigureWindows.GetMainWindow?.Dispatcher?.InvokeAsync(() =>
                {
                    if (index != FolderIndex)
                        return;
                    ConfigureWindows.GetMainWindow.MainImage.Source = thumb.Value.BitmapSource;
                    if (thumb.Value.Width.HasValue && thumb.Value.Height.HasValue)
                    {
                        ScaleImage.FitImage(thumb.Value.Width.Value, thumb.Value.Height.Value);
                    }
                }, DispatcherPriority.Normal, cancellationToken);
            }
        }
    }, cancellationToken);

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

            if (thumb.HasValue == false)
            {
                return;
            }

            ConfigureWindows.GetMainWindow.MainImage.Source = thumb.Value.BitmapSource;
            if (thumb.Value.Width.HasValue && thumb.Value.Height.HasValue)
            {
                ScaleImage.FitImage(thumb.Value.Width.Value, thumb.Value.Height.Value);
            }
        });
    }

    #endregion Loading Preview
}