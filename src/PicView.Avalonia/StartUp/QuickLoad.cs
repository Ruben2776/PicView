using Avalonia.Controls;
using Avalonia.Threading;
using ImageMagick;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.Navigation.Services;
using PicView.Avalonia.UI;
using PicView.Avalonia.Views.UC;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.ArchiveHandling;
using PicView.Core.DebugTools;
using PicView.Core.FileHandling;
using PicView.Core.FileHistory;
using PicView.Core.FileSorting;
using PicView.Core.Gallery;
using PicView.Core.Http;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.StartUp;

/// <summary>
/// Provides methods for quickly loading the image first, and then initializing the rest of the navigation.
/// </summary>
public static class QuickLoad
{
    /// <summary>
    /// Asynchronously loads an image, archive, URL, base64 string, or directory into the application view,
    /// updating the UI state and loading indicative properties as necessary.
    /// </summary>
    /// <param name="mainWindow">The active main window.</param>
    /// <param name="core">The main view model.</param>
    /// <param name="source">The file, URL, or directory path to be loaded.</param>
    /// <param name="continueFromLeftOff">A boolean indicating whether to continue loading from the last session folder structure.</param>
    /// <param name="isStartup">A boolean used to determine start-up behavior for the window.</param>
    public static async ValueTask QuickLoadAsync(MainWindow mainWindow, CoreViewModel core, string source,
        bool continueFromLeftOff, bool isStartup = false)
    {        
        var fileInfo = new FileInfo(source);
        if (!fileInfo.Exists) // If not file, try to load if URL or directory
        {
            var check = FileTypeResolver.CheckIfLoadableString(source);
            if (check is null)
            {
                ViewChangeHelper.SwitchToStartUpMenu(core.MainWindows.ActiveWindow.CurrentValue);
                return;
            }

            switch (check.Value.Type)
            {
                case FileTypeResolver.LoadAbleFileType.Directory:
                {
                    var files = FileListRetriever.RetrieveFiles(new FileInfo(check.Value.Data),core.PlatformService.CompareStrings);
                    if (files.Count == 0)
                    {
                        ViewChangeHelper.SwitchToStartUpMenu(core.MainWindows.ActiveWindow.CurrentValue);
                        return;
                    }
                    await LoadSingleFileAsync(mainWindow, core, files[0], continueFromLeftOff, isStartup, files).ConfigureAwait(false);
                    return;
                }
                case FileTypeResolver.LoadAbleFileType.Web:
                {
                    await LoadUrlImageAsync(mainWindow, core, check.Value.Data).ConfigureAwait(false);
                    return;
                }
                default:
                    ViewChangeHelper.SwitchToStartUpMenu(core.MainWindows.ActiveWindow.CurrentValue);
                    return;
            }
        }
        
        if (source.IsArchive())
        {
            await LoadArchiveFileAsync(mainWindow, core, fileInfo).ConfigureAwait(false);
        }
        else
        {
            await LoadSingleFileAsync(mainWindow, core, fileInfo, continueFromLeftOff, isStartup).ConfigureAwait(false);
        }
        core.MainWindows.ActiveWindow.CurrentValue.TopTitlebarViewModel.DropDownMenu.CloseMenus();
        core.MainWindows.ActiveWindow.CurrentValue.TopTitlebarViewModel.DropDownMenu.IsDropDownMenuVisible.Value = false;
    }

    private static async ValueTask LoadUrlImageAsync(MainWindow mainWindow, CoreViewModel core, string url)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            core.MainWindows.ActiveWindow.Value.WindowTabs.ActiveTab.Value.CurrentView.Value = new ImageViewer();
        }, DispatcherPriority.Send);
        
        var safeFileName = HttpManager.GetSafeFileName(url);
        var destPath = TempFileManager.GetNewTempFilePath(safeFileName);
        using var client = new HttpClientDownloadWithProgress(url, destPath);
        var tab = core.MainWindows.ActiveWindow.CurrentValue.WindowTabs.ActiveTab.CurrentValue;
        
        TabNavigationInitializer.Initialize(core, mainWindow);
        ShowHoverBarIfNeeded(core);
        
        client.ProgressChanged += (totalFileSize, totalBytesDownloaded, progressPercentage) =>
        {
            var displayProgress = HttpManager.GetProgressDisplay(totalFileSize, totalBytesDownloaded, progressPercentage);
            var title = $"{safeFileName} {TranslationManager.Translation?.Downloading} {displayProgress}";

            tab.TabTitle.Value = 
            tab.Title.Value = 
            tab.WindowTitle.Value = 
            tab.TitleTooltip.Value = title;

            if (!Settings.UIProperties.IsTaskbarProgressEnabled || !totalBytesDownloaded.HasValue || !totalFileSize.HasValue)
            {
                return;
            }

            var downloadedBytes = (ulong)totalBytesDownloaded.Value;
            var totalSize = (ulong)totalFileSize.Value;
            core.PlatformService.SetTaskbarProgress(downloadedBytes, totalSize);

        };
        await client.StartDownloadAsync(CancellationToken.None).ConfigureAwait(false);
        var model = await GetImageModel.GetImageModelAsync(new FileInfo(destPath)).ConfigureAwait(false);
        tab.Model = model;
        tab.SourceURL = url;
        tab.SingleImageType = SingleImageType.Url;
        tab.UpdateTabTitle();
        
        FileHistoryManager.Add(url);

        if (Settings.UIProperties.IsTaskbarProgressEnabled)
        {
            core.PlatformService.StopTaskbarProgress();
        }
    }

    private static async ValueTask LoadSingleFileAsync(MainWindow mainWindow, CoreViewModel core,
        FileInfo fileInfo,
        bool continueFromLeftOff,
        bool isStartUp,
        List<FileInfo>? files = null)
    {
        core.MainWindows.ActiveWindow.Value.IsLoadingIndicatorShown.Value = !core.MainWindows.ActiveWindow.CurrentValue.IsTopToolbarShown.Value;
        Dispatcher.UIThread.Invoke(() =>
        {
           core.MainWindows.ActiveWindow.Value.WindowTabs.ActiveTab.Value.CurrentView.Value = new ImageViewer();
        }, DispatcherPriority.Send);
    
        var vm = core.MainWindows.ActiveWindow.CurrentValue;
        var tab = vm.WindowTabs.ActiveTab.CurrentValue;
        tab.SetLoading();
        
        var magickImage = new MagickImage();
        try
        {
            await magickImage.PingAsync(fileInfo);
            tab.Model.PixelWidth = magickImage.Width;
            tab.Model.PixelHeight = magickImage.Height;

            if (isStartUp)
            {
                // Need to set size for the Maximize or fullscreen functions to work
                if (Settings.WindowProperties.Maximized && !Settings.WindowProperties.Fullscreen)
                {
                    tab.Model.PixelWidth = magickImage.Width;
                    tab.Model.PixelHeight = magickImage.Height;
                }
                else if (Settings.WindowProperties.Fullscreen)
                {
                    tab.Model.PixelWidth = magickImage.Width;
                    tab.Model.PixelHeight = magickImage.Height;
                }
                else if (!Settings.ImageScaling.ShowImageSideBySide)
                {
                    // Predict window size and center beforehand for pleasant opening when double-clicking a file
                    Dispatcher.UIThread.Post(() =>
                    {
                        WindowResizing.SetSize(tab.Model.PixelWidth, tab.Model.PixelHeight,
                            0, 0, WindowResizeReason.Application, mainWindow, vm);
                        WindowResizing.FastCenterWindow(mainWindow);
                    }, DispatcherPriority.Render);
                }
            }
        }
        catch (Exception e)
        {
            // Pinging can lead to crashes when the file cannot be read. 
            // Just catching the exception here means it will still load correctly regardless
            DebugHelper.LogDebug(nameof(QuickLoad), nameof(QuickLoadAsync), e);
        }
        
        var imageModel = await GetImageModel.GetImageModelAsync(fileInfo, magickImage).ConfigureAwait(false);
        tab.Image.Value = imageModel.Image;
        tab.FileInfo.Value = fileInfo;
        tab.Model = imageModel;
        var initialDirectory = GetInitialDirectory(continueFromLeftOff, fileInfo);

        var isGalleryEnabled = CheckIfGalleryIsNeeded(core);
        
        if (Settings.ImageScaling.ShowImageSideBySide)
        {
            files ??= core.PlatformService.GetFiles(initialDirectory);
            var index = files.FindIndex(x =>
                x.FullName.AsSpan().Equals(fileInfo.FullName.AsSpan(), StringComparison.OrdinalIgnoreCase));
            var (_, nextIndex, _) = IterationHelper.GetIterations(index, files.Count, NavigateTo.Next, SkipAmount.None);
            var nextFileInfo = files[nextIndex];
            var secondImageModel = await GetImageModel.GetImageModelAsync(nextFileInfo).ConfigureAwait(false);
            tab.SecondaryModel = secondImageModel;
            TabNavigationInitializer.Initialize(core, files, mainWindow);
            UpdateImage.ChangeImage(mainWindow, tab, core.MainWindows.ActiveWindow.CurrentValue);
            UpdateImage.UpdateTabSideBySideTitles(tab, index, nextIndex, fileInfo, nextFileInfo, files);
        }
        else
        {
            UpdateImage.ChangeImage(mainWindow, tab, core.MainWindows.ActiveWindow.CurrentValue);
            if (files is null)
            {
                TabNavigationInitializer.Initialize(core, initialDirectory, mainWindow);
            }
            else
            {
                TabNavigationInitializer.Initialize(core, files, mainWindow);
            }
        }

        if (!Settings.WindowProperties.Fullscreen && !Settings.WindowProperties.Maximized && isStartUp && Settings.WindowProperties.AutoFit)
        {
            Dispatcher.UIThread.Post(() => WindowFunctions.CenterWindowOnScreen(mainWindow));
        }
        else if (Settings.ImageScaling.ShowImageSideBySide || isStartUp && !Settings.WindowProperties.AutoFit)
        {
            // Fixes certain instances where the image is not sized properly
            Dispatcher.UIThread.Post(() =>
            {
                WindowResizing.SetSize(mainWindow, WindowResizeReason.Application);
            }, DispatcherPriority.Background);
        }

        vm.IsLoadingIndicatorShown.Value = false;
        tab.UpdateTabTitle();
        ShowHoverBarIfNeeded(core);
        if (Settings.UIProperties.IsTaskbarProgressEnabled)
        {
            core.PlatformService.SetTaskbarProgress((ulong)tab.ImageIterator.CurrentIndex, (ulong)tab.ImageIterator.Files.Count);
        }
        
        FileHistoryManager.Add(fileInfo.FullName);

        if (isGalleryEnabled)
        {
            await LoadGallery(core).ConfigureAwait(false);
        }
        
        if (continueFromLeftOff)
        {
            Settings.StartUp.StartUpDirectory = initialDirectory.FullName;
        }
        
        magickImage.Dispose();
    }
    
    private static async ValueTask LoadArchiveFileAsync(MainWindow mainWindow, CoreViewModel core, FileInfo source)
    {
        var tab = core.MainWindows.ActiveWindow.CurrentValue.WindowTabs.ActiveTab.CurrentValue;
        Dispatcher.UIThread.Invoke(() =>
        {
            core.MainWindows.ActiveWindow.Value.WindowTabs.ActiveTab.Value.CurrentView.Value = new ImageViewer();
        }, DispatcherPriority.Send);
        TabNavigationInitializer.Initialize(core, source, mainWindow);
        core.MainWindows.ActiveWindow.Value.IsLoadingIndicatorShown.Value = true;
        tab.SetLoading();

        var isGalleryEnabled = CheckIfGalleryIsNeeded(core);
        var isArchiveLoaded = await core.MainWindows.ActiveWindow.CurrentValue.WindowTabs.LoadFromArchiveAsync(source.FullName).ConfigureAwait(false);
        if (!isArchiveLoaded)
        {
            ViewChangeHelper.SwitchToStartUpMenu(core.MainWindows.ActiveWindow.CurrentValue);
            return;
        }
        ShowHoverBarIfNeeded(core);
        core.MainWindows.ActiveWindow.Value.IsLoadingIndicatorShown.Value = false;
        if (isGalleryEnabled)
        {
            await LoadGallery(core).ConfigureAwait(false);
        }
    }

    private static bool CheckIfGalleryIsNeeded(CoreViewModel core)
    {
        if (Settings.Gallery.IsGalleryDocked)
        {
            if (!Settings.UIProperties.ShowInterface && !Settings.Gallery.ShowDockedGalleryInHiddenUI)
            {
                core.MainWindows.ActiveWindow.CurrentValue.WindowTabs.ActiveTab.CurrentValue.Gallery.IsGalleryDocked
                    .Value = false;
                return false;
            }
            if (Settings.Gallery.DockPosition is GalleryDockPosition.Closed)
            {
                Settings.Gallery.DockPosition = GalleryDockPosition.Bottom;
            }

            return true;
        }
        Settings.Gallery.DockPosition = GalleryDockPosition.Closed;
        return false;
    }
    
    private static async ValueTask LoadGallery(CoreViewModel core)
    {
        await GalleryLoader.LoadGalleryAsync(core.MainWindows.ActiveWindow.Value.WindowTabs.ActiveTab.Value,
                core.MainWindows.ActiveWindow.Value.WindowTabs.ActiveTab.Value.ImageIterator.Files,
                ServiceHelper.ThumbLoader,
                core.SharedThumbnailCache,
                core.MainWindows.ActiveWindow.Value.WindowTabs.ActiveTab.Value.GetTabCancellation().Token)
            .ConfigureAwait(false);
    }

    private static void ShowHoverBarIfNeeded(CoreViewModel core)
    {
        var tab = core.MainWindows.ActiveWindow.Value.WindowTabs.ActiveTab.Value;
        if (!Settings.UIProperties.ShowInterface && Settings.UIProperties.ShowAltInterfaceButtons
            || Settings.UIProperties.ShowAltInterfaceButtons && Settings.WindowProperties.Fullscreen)
        {
            tab.Hoverbar.IsHoverbarVisible.Value = Settings.UIProperties.ShowHoverNavigationBar;
        }
        else
        {
            tab.Hoverbar.IsHoverbarVisible.Value = false;
        }
    }

    private static FileInfo GetInitialDirectory(bool continueFromLeftOff, FileInfo fileInfo)
    {
        if (!continueFromLeftOff || !Settings.Sorting.IncludeSubDirectories)
        {
            return fileInfo;
        }

        if (!string.IsNullOrWhiteSpace(Settings.StartUp.StartUpDirectory) && !ArchiveExtraction.IsArchived)
        {
            return fileInfo.FullName.Contains(Settings.StartUp.StartUpDirectory) ?
                new FileInfo(Settings.StartUp.StartUpDirectory) : new FileInfo(fileInfo.DirectoryName);
        }
        return fileInfo;
    }
}
