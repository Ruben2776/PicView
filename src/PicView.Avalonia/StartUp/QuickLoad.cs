using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using ImageMagick;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.UI;
using PicView.Avalonia.Views.UC;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.DebugTools;
using PicView.Core.FileHistory;
using PicView.Core.FileSorting;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.StartUp;

/// <summary>
/// Opens the first source at startup in the order that makes opening feel fast: the window, then the image, then
/// navigation (see ADR 0005). Reached through <see cref="SourceOpener.OpenAsync"/>.
/// </summary>
public static class QuickLoad
{
    /// <summary>
    /// Opens a file, directory, archive or URL while the window is not shown yet, and shows the window.
    /// </summary>
    /// <param name="continueFromLeftOff">List files from the remembered start-up directory, and remember it again.</param>
    public static async ValueTask<LoadResult> StartUpAsync(MainWindow mainWindow, CoreViewModel core, string source,
        bool continueFromLeftOff)
    {
        var check = FileTypeResolver.CheckIfLoadableString(source);
        LoadResult result;
        switch (check?.Type)
        {
            case FileTypeResolver.LoadAbleFileType.File:
                result = await LoadFileAsync(mainWindow, core, new FileInfo(check.Value.Data), continueFromLeftOff)
                    .ConfigureAwait(false);
                break;
            case FileTypeResolver.LoadAbleFileType.Directory:
                var files = FileListRetriever.RetrieveFiles(new FileInfo(check.Value.Data), core.PlatformService.CompareStrings);
                result = files.Count is 0
                    ? LoadResult.Failed()
                    : await LoadFileAsync(mainWindow, core, files[0], continueFromLeftOff, files).ConfigureAwait(false);
                break;
            case FileTypeResolver.LoadAbleFileType.Zip:
            case FileTypeResolver.LoadAbleFileType.Web:
                result = await LoadThroughCoreAsync(mainWindow, core, check.Value.Data).ConfigureAwait(false);
                break;
            default:
                result = LoadResult.Failed();
                break;
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (!result.IsLoaded)
            {
                SourceOpener.ShowError(mainWindow.ViewModel!, result);
                ViewChangeHelper.SwitchToStartUpMenu(mainWindow.ViewModel!);
                mainWindow.Show();
            }
            if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = mainWindow;
            }
        });
        return result;
    }

    /// <summary>
    /// Archives and URLs: show the window, initialize navigation, and let Core load them.
    /// </summary>
    private static async ValueTask<LoadResult> LoadThroughCoreAsync(MainWindow mainWindow, CoreViewModel core, string source)
    {
        var vm = mainWindow.ViewModel!;
        var tab = vm.WindowTabs.ActiveTab.CurrentValue;
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            mainWindow.Show();
            tab.CurrentView.Value = new ImageViewer();
        }, DispatcherPriority.Send);
        if (vm.WindowModeState.CurrentValue.AutoFit)
        {
            WindowFunctions.CenterWindowOnScreen(mainWindow);
        }

        TabNavigationInitializer.PrepareDockedGallery(tab);
        TabNavigationInitializer.Initialize(core, [], vm);

        vm.IsLoadingIndicatorShown.Value = true;
        LoadResult result;
        try
        {
            result = await vm.WindowTabs.LoadFromStringAsync(source).ConfigureAwait(false);
        }
        finally
        {
            vm.IsLoadingIndicatorShown.Value = false;
        }

        if (!result.IsLoaded)
        {
            return result;
        }

        SourceOpener.ShowHoverBarIfNeeded(tab);
        if (vm.WindowModeState.CurrentValue.AutoFit)
        {
            await Dispatcher.UIThread.InvokeAsync(() => mainWindow.WindowFitter.Center(), DispatcherPriority.Render);
        }
        return result;
    }

    /// <summary>
    /// Files and directories: show the image before navigation is initialized.
    /// </summary>
    private static async ValueTask<LoadResult> LoadFileAsync(MainWindow mainWindow, CoreViewModel core,
        FileInfo fileInfo,
        bool continueFromLeftOff,
        List<FileInfo>? files = null)
    {
        var vm = mainWindow.ViewModel!;
        var tab = vm.WindowTabs.ActiveTab.CurrentValue;
        vm.IsLoadingIndicatorShown.Value = !vm.IsTopToolbarShown.Value;
        tab.SingleImageType = SingleImageType.None;

        var rootDirectory = continueFromLeftOff ? SourceOpener.GetStartUpDirectory(fileInfo.FullName) : null;
        var listFrom = rootDirectory is null ? fileInfo : new FileInfo(rootDirectory);
        var isGalleryEnabled = TabNavigationInitializer.PrepareDockedGallery(tab);

        if (Settings.ImageScaling.ShowImageSideBySide)
        {
            await LoadSideBySideAsync(mainWindow, core, vm, tab, fileInfo, files ?? core.PlatformService.GetFiles(listFrom), listFrom)
                .ConfigureAwait(false);
        }
        else
        {
            await LoadSingleAsync(mainWindow, core, vm, tab, fileInfo, files, listFrom).ConfigureAwait(false);
        }
        
        if (vm.WindowModeState.CurrentValue.AutoFit)
        {
            Dispatcher.UIThread.Post(() =>
            {
                mainWindow.WindowFitter.Center();
            }, DispatcherPriority.Send);
        }

        vm.IsLoadingIndicatorShown.Value = false;
        UpdateImage.ChangeTabTitleToNewImage(tab);



        SourceOpener.ShowHoverBarIfNeeded(tab);
        if (Settings.UIProperties.IsTaskbarProgressEnabled)
        {
            core.PlatformService.SetTaskbarProgress((ulong)tab.ImageIterator.CurrentIndex, (ulong)tab.ImageIterator.Files.Count);
        }

        FileHistoryManager.Add(fileInfo.FullName);

        if (rootDirectory is not null)
        {
            Settings.StartUp.StartUpDirectory = rootDirectory;
        }

        if (isGalleryEnabled && tab is { GalleryCoordinator: not null, ImageIterator: not null })
        {
            await Task.Run(() => tab.GalleryCoordinator.LoadGallery(tab.ImageIterator.Files, vm.Core)).ConfigureAwait(false);
        }
        return LoadResult.Loaded;
    }

    private static async ValueTask LoadSingleAsync(MainWindow mainWindow, CoreViewModel core, MainWindowViewModel vm,
        TabViewModel tab, FileInfo fileInfo, List<FileInfo>? files, FileInfo listFrom)
    {
        using var magickImage = new MagickImage();
        var isPinged = false;
        try
        {
#pragma warning disable MA0042
            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
            // ReSharper disable once MethodHasAsyncOverload
            magickImage.Ping(fileInfo);
#pragma warning restore MA0042
            isPinged = true;
            tab.Model.PixelWidth = magickImage.Width;
            tab.Model.PixelHeight = magickImage.Height;

            // Need to set size for the Maximize or fullscreen functions to work
            // Predict window size and center beforehand for pleasant opening when double-clicking a file
            if (vm.WindowModeState.CurrentValue.AutoFit)
            {
                // Using await and DispatcherPriority.Render fixes cases where the window is not staying in place
                // after resizing the next image
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ShowSized(mainWindow, new PixelSizes(tab.Model.PixelWidth, tab.Model.PixelHeight));
                }, DispatcherPriority.Render);
            }
            else
            {
                Dispatcher.UIThread.Post(() =>
                {
                    ShowSized(mainWindow, new PixelSizes(tab.Model.PixelWidth, tab.Model.PixelHeight));
                }, DispatcherPriority.Send);
            }

        }
        catch (Exception e)
        {
            // Pinging can lead to crashes when the file cannot be read.
            // Just catching the exception here means it will still load correctly regardless
            ShowNow(mainWindow);
            DebugHelper.LogDebug(nameof(QuickLoad), nameof(LoadSingleAsync), e);
        }

        var imageModel = await GetImageModel.GetImageModelAsync(fileInfo, isPinged ? magickImage : null).ConfigureAwait(false);
        tab.Display(imageModel);
        await ShowImageViewerAsync(tab).ConfigureAwait(false);

        UpdateImage.ChangeImage(mainWindow, tab, vm);
        if (files is null)
        {
            TabNavigationInitializer.Initialize(core, listFrom, vm);
        }
        else
        {
            TabNavigationInitializer.Initialize(core, files, vm);
        }
    }

    /// <summary>
    /// Side by side needs the file list first to know which file is beside the opened one. With AutoFit the window
    /// is sized for both images before it is shown; without it, the window shows at once and the images follow.
    /// </summary>
    private static async ValueTask LoadSideBySideAsync(MainWindow mainWindow, CoreViewModel core, MainWindowViewModel vm,
        TabViewModel tab, FileInfo fileInfo, List<FileInfo> files, FileInfo listFrom)
    {
        var index = SideBySidePair.IndexOf(files, fileInfo) ?? 0;

        // Without another image there is no pair to size or show, so the image loads as a single one
        if (SideBySidePair.PartnerOf(index, files.Count) is not { } nextIndex)
        {
            await LoadSingleAsync(mainWindow, core, vm, tab, fileInfo, files, listFrom).ConfigureAwait(false);
            return;
        }

        if (!vm.WindowModeState.CurrentValue.AutoFit)
        {
            ShowNow(mainWindow);
        }

        var nextFileInfo = files[nextIndex];

        using var firstMagick = new MagickImage();
        using var secondMagick = new MagickImage();
        var isPinged = false;
        if (vm.WindowModeState.CurrentValue.AutoFit)
        {
            try
            {
#pragma warning disable MA0042
                // ReSharper disable MethodHasAsyncOverloadWithCancellation
                // ReSharper disable MethodHasAsyncOverload
                firstMagick.Ping(fileInfo);
                secondMagick.Ping(nextFileInfo);
                // ReSharper restore MethodHasAsyncOverloadWithCancellation
                // ReSharper restore MethodHasAsyncOverload
#pragma warning restore MA0042
                isPinged = true;
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    var sizes = new PixelSizes(firstMagick.Width, firstMagick.Height, secondMagick.Width,
                        secondMagick.Height);
                    if (!ShowSized(mainWindow, sizes))
                    {
                        mainWindow.Show();
                    }
                }, DispatcherPriority.Send);
            }
            catch (Exception e)
            {
                ShowNow(mainWindow);
                DebugHelper.LogDebug(nameof(QuickLoad), nameof(LoadSideBySideAsync), e);
            }
        }

        var firstModel = await GetImageModel.GetImageModelAsync(fileInfo, isPinged ? firstMagick : null).ConfigureAwait(false);
        var secondModel = await GetImageModel.GetImageModelAsync(nextFileInfo, isPinged ? secondMagick : null).ConfigureAwait(false);
        tab.Display(firstModel, secondModel);
        await ShowImageViewerAsync(tab).ConfigureAwait(false);

        TabNavigationInitializer.Initialize(core, files, vm);
        UpdateImage.ChangeImage(mainWindow, tab, vm);
        UpdateImage.UpdateTabSideBySideTitles(tab, index, nextIndex, fileInfo, nextFileInfo, files);
        Dispatcher.UIThread.Post(() =>
        {
            mainWindow.Show();
            mainWindow.WindowFitter.Refit();
            mainWindow.SetLayoutSizeAndVisibility(mainWindow.Bounds.Width);
        }, DispatcherPriority.Background);
    }

    /// <summary>
    /// Sizes the window for the image, or side-by-side pair, of <paramref name="sizes"/> and shows it. Call on the UI
    /// thread.
    /// </summary>
    /// <returns>Whether the window could be sized; it is left hidden when it could not.</returns>
    private static bool ShowSized(MainWindow mainWindow, PixelSizes sizes)
    {
        // Before it is shown, an AutoFit window is given the Fit as its size, then follows its content again
        if (!mainWindow.WindowFitter.FitBeforeShowing(sizes))
        {
            return false;
        }

        mainWindow.Show();
        mainWindow.SetLayoutSizeAndVisibility(mainWindow.Bounds.Width);
        return true;
    }

    private static void ShowNow(MainWindow mainWindow) =>
        Dispatcher.UIThread.Post(() =>
        {
            mainWindow.Show();
            mainWindow.SetLayoutSizeAndVisibility(mainWindow.Bounds.Width);
        });

    private static async ValueTask ShowImageViewerAsync(TabViewModel tab) =>
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            tab.CurrentView.Value = new ImageViewer();
        }, DispatcherPriority.Send);
}
