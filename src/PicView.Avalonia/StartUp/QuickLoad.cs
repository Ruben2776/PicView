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
                var folder = check.Value.Data;
                var files = FileListRetriever.RetrieveFiles(new FileInfo(folder), core.PlatformService.CompareStrings);
                result = files.Count is 0
                    ? LoadResult.Failed()
                    : await LoadFileAsync(mainWindow, core, files[0], continueFromLeftOff, folder).ConfigureAwait(false);
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
            // A load a newer one overtook leaves the view to it
            if (result.Outcome is LoadOutcome.Failed)
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
    /// Archives and URLs: show the window, open the tab, and let its Load load them.
    /// </summary>
    private static async ValueTask<LoadResult> LoadThroughCoreAsync(MainWindow mainWindow, CoreViewModel core, string source)
    {
        var vm = mainWindow.ViewModel!;
        var tab = vm.WindowTabs.ActiveTab.CurrentValue;
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            mainWindow.Show();
            tab.CurrentView.Value = new ImageViewer(tab);
        }, DispatcherPriority.Send);
        if (vm.WindowModeState.CurrentValue.AutoFit)
        {
            WindowFunctions.CenterWindowOnScreen(mainWindow);
        }

        TabNavigationInitializer.Initialize(core, vm, tab);

        var result = await tab.Load.LoadAsync(source).ConfigureAwait(false);

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
    /// Files and directories: show the image before the tab's Load lists the files around it.
    /// </summary>
    /// <param name="folder">The folder opened at startup, which <paramref name="fileInfo"/> is the first file of, to
    /// list files from.</param>
    private static async ValueTask<LoadResult> LoadFileAsync(MainWindow mainWindow, CoreViewModel core,
        FileInfo fileInfo,
        bool continueFromLeftOff,
        string? folder = null)
    {
        var vm = mainWindow.ViewModel!;
        var tab = vm.WindowTabs.ActiveTab.CurrentValue;

        var rootDirectory = continueFromLeftOff ? SourceOpener.GetStartUpDirectory(fileInfo.FullName) : null;
        var listRoot = folder ?? rootDirectory;
        tab.Gallery.Switcher.Start(Settings.Gallery.IsGalleryDocked);

        LoadResult result;
        if (Settings.ImageScaling.ShowImageSideBySide)
        {
            var listFrom = listRoot is null ? fileInfo : new FileInfo(listRoot);
            result = await LoadSideBySideAsync(mainWindow, core, vm, tab, fileInfo, core.PlatformService.GetFiles(listFrom),
                listRoot).ConfigureAwait(false);
        }
        else
        {
            result = await LoadSingleAsync(mainWindow, core, vm, tab, fileInfo, listRoot).ConfigureAwait(false);
        }

        if (vm.WindowModeState.CurrentValue.AutoFit)
        {
            Dispatcher.UIThread.Post(() =>
            {
                mainWindow.WindowFitter.Center();
            }, DispatcherPriority.Send);
        }

        SourceOpener.ShowHoverBarIfNeeded(tab);
        if (Settings.UIProperties.IsTaskbarProgressEnabled && tab.ImageIterator is { } iterator)
        {
            core.PlatformService.SetTaskbarProgress((ulong)iterator.CurrentIndex, (ulong)iterator.Files.Count);
        }

        if (rootDirectory is not null)
        {
            Settings.StartUp.StartUpDirectory = rootDirectory;
        }
        return result;
    }

    private static async ValueTask<LoadResult> LoadSingleAsync(MainWindow mainWindow, CoreViewModel core,
        MainWindowViewModel vm, TabViewModel tab, FileInfo fileInfo, string? rootDirectory)
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
            var sizes = new PixelSizes(magickImage.Width, magickImage.Height);

            // Need to set size for the Maximize or fullscreen functions to work
            // Predict window size and center beforehand for pleasant opening when double-clicking a file
            if (vm.WindowModeState.CurrentValue.AutoFit)
            {
                // Using await and DispatcherPriority.Render fixes cases where the window is not staying in place
                // after resizing the next image
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ShowSized(mainWindow, sizes);
                }, DispatcherPriority.Render);
            }
            else
            {
                Dispatcher.UIThread.Post(() =>
                {
                    ShowSized(mainWindow, sizes);
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

        // Decoded while the viewer is set up and the tab hooked up, so the first image's reaction comes from the same
        // subscription as every later one's. The pinged size rides on the decoded model
        var decoding = GetImageModel.GetImageModelAsync(fileInfo, isPinged ? magickImage : null);
        await ShowImageViewerAndHookUpAsync(tab).ConfigureAwait(false);
        return await ShowThroughTheLoadAsync(core, tab, fileInfo, await decoding.ConfigureAwait(false), rootDirectory)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Side by side needs the file list first to know which file is beside the opened one. With AutoFit the window
    /// is sized for both images before it is shown; without it, the window shows at once and the images follow.
    /// </summary>
    private static async ValueTask<LoadResult> LoadSideBySideAsync(MainWindow mainWindow, CoreViewModel core,
        MainWindowViewModel vm, TabViewModel tab, FileInfo fileInfo, List<FileInfo> files, string? rootDirectory)
    {
        var index = SideBySidePair.IndexOf(files, fileInfo) ?? 0;

        // Without another image there is no pair to size or show, so the image loads as a single one
        if (SideBySidePair.PartnerOf(index, files.Count) is not { } nextIndex)
        {
            return await LoadSingleAsync(mainWindow, core, vm, tab, fileInfo, rootDirectory).ConfigureAwait(false);
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

        // The Load fetches the image beside it through the cache and shows the pair; the pinged sizes sized the window
        var decodingFirst = GetImageModel.GetImageModelAsync(fileInfo, isPinged ? firstMagick : null);
        await ShowImageViewerAndHookUpAsync(tab).ConfigureAwait(false);
        var result = await ShowThroughTheLoadAsync(core, tab, fileInfo, await decodingFirst.ConfigureAwait(false),
            rootDirectory).ConfigureAwait(false);

        Dispatcher.UIThread.Post(() =>
        {
            mainWindow.Show();
            mainWindow.WindowFitter.Refit();
            mainWindow.SetLayoutSizeAndVisibility(mainWindow.Bounds.Width);
        }, DispatcherPriority.Background);
        return result;
    }

    /// <summary>
    /// Shows the decoded first <paramref name="model"/> through the tab's Load, which then lists the files around it,
    /// from <paramref name="rootDirectory"/> when given, and gives the tab's gallery the shared settings after.
    /// </summary>
    private static async ValueTask<LoadResult> ShowThroughTheLoadAsync(CoreViewModel core, TabViewModel tab,
        FileInfo fileInfo, ImageModel model, string? rootDirectory)
    {
        var result = await tab.Load.LoadAsync(fileInfo.FullName, new LoadOptions(Model: model, RootDirectory: rootDirectory))
            .ConfigureAwait(false);
        TabNavigationInitializer.InitializeGallery(core, tab);
        return result;
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

    /// <summary>
    /// Gives the tab its image viewer and hooks it up to its window before its first image shows, so the window reacts
    /// to that image as it does to every later one. The window's hookups exist since the lifecycle created it.
    /// </summary>
    private static async ValueTask ShowImageViewerAndHookUpAsync(TabViewModel tab) =>
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            tab.CurrentView.Value = new ImageViewer(tab);
            tab.ParentWindowContext.WindowTabs.HookUp(tab);
        }, DispatcherPriority.Send);
}
