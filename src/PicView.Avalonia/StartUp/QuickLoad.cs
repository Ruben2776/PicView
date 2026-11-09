using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using ImageMagick;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.Navigation;
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
        // Loading from the start, while the window is sized and the image decoded, before the tab's Load says it. The
        // shown image ends it; nothing shown ends this ticket, unless a newer phase began since
        var tab = mainWindow.ViewModel!.WindowTabs.ActiveTab.CurrentValue;
        var ticket = tab.SetLoading();

        var check = FileTypeResolver.CheckIfLoadableString(source);
        LoadResult result;
        // Ended in the finally, so a load that throws leaves no Loading behind either
        var loaded = false;
        try
        {
            switch (check?.Type)
            {
                case FileTypeResolver.LoadAbleFileType.File:
                    result = await LoadFileAsync(mainWindow, core, new FileInfo(check.Value.Data), continueFromLeftOff)
                        .ConfigureAwait(false);
                    break;
                case FileTypeResolver.LoadAbleFileType.Directory:
                    // Listed once: the tab's Load browses these files instead of listing the folder again
                    var files = FileListRetriever.RetrieveFiles(new FileInfo(check.Value.Data),
                        core.PlatformService.CompareStrings);
                    result = files.Count is 0
                        ? LoadResult.Failed()
                        : await LoadFileAsync(mainWindow, core, files[0], continueFromLeftOff, files)
                            .ConfigureAwait(false);
                    break;
                case FileTypeResolver.LoadAbleFileType.Zip:
                case FileTypeResolver.LoadAbleFileType.Web:
                    result = await LoadThroughCoreAsync(mainWindow, core, check.Value.Data).ConfigureAwait(false);
                    break;
                default:
                    result = LoadResult.Failed();
                    break;
            }

            loaded = result.Outcome is LoadOutcome.Loaded;
        }
        finally
        {
            if (!loaded)
            {
                tab.EndLoading(ticket);
            }
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            // A load a newer one overtook leaves the view to it
            if (result.Outcome is LoadOutcome.Failed)
            {
                SourceOpener.ShowError(mainWindow.ViewModel!, result);
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
        }, DispatcherPriority.Send);

        TabNavigationInitializer.Initialize(core, vm, tab);

        return await tab.Load.LoadAsync(source).ConfigureAwait(false);
    }

    /// <summary>
    /// Files and directories: show the image before the tab's Load lists the files around it.
    /// </summary>
    /// <param name="files">The files of the folder opened at startup, which <paramref name="fileInfo"/> is the first of,
    /// for the tab to browse without listing them again.</param>
    private static async ValueTask<LoadResult> LoadFileAsync(MainWindow mainWindow, CoreViewModel core,
        FileInfo fileInfo,
        bool continueFromLeftOff,
        List<FileInfo>? files = null)
    {
        var vm = mainWindow.ViewModel!;
        var tab = vm.WindowTabs.ActiveTab.CurrentValue;

        var rootDirectory = continueFromLeftOff ? SourceOpener.GetStartUpDirectory(fileInfo.FullName) : null;
        tab.Gallery.Switcher.Start(Settings.Gallery.IsGalleryDocked);

        LoadResult result;
        if (tab.PairSettings().SideBySide)
        {
            // Listed once, to find the image beside the opened one; the tab's Load browses the same files
            var listFrom = rootDirectory is null ? fileInfo : new FileInfo(rootDirectory);
            result = await LoadSideBySideAsync(mainWindow, core, vm, tab, fileInfo,
                files ?? core.PlatformService.GetFiles(listFrom), rootDirectory).ConfigureAwait(false);
        }
        else
        {
            result = await LoadSingleAsync(mainWindow, core, vm, tab, fileInfo, files, rootDirectory)
                .ConfigureAwait(false);
        }

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

    /// <param name="files">The files around <paramref name="fileInfo"/> when they are listed already; the tab's Load
    /// lists them otherwise, from <paramref name="rootDirectory"/> when given.</param>
    private static async ValueTask<LoadResult> LoadSingleAsync(MainWindow mainWindow, CoreViewModel core,
        MainWindowViewModel vm, TabViewModel tab, FileInfo fileInfo, IReadOnlyList<FileInfo>? files,
        string? rootDirectory)
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
            // Predict window size beforehand, so it opens centered at its size when double-clicking a file
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

        // Decoded while the tab is hooked up, so the first image's reaction comes from the same subscription as every
        // later one's. The pinged size rides on the decoded model
        var decoding = GetImageModel.GetImageModelAsync(fileInfo, isPinged ? magickImage : null);
        await HookUpAsync(tab).ConfigureAwait(false);
        var model = await decoding.ConfigureAwait(false);
        return await ShowThroughTheLoadAsync(core, tab, fileInfo,
            new LoadOptions(Model: model, Files: files, RootDirectory: rootDirectory)).ConfigureAwait(false);
    }

    /// <summary>
    /// Side by side needs the file list first to know which file is beside the opened one. With AutoFit the window
    /// is sized for both images before it is shown; without it, the window shows at once and the images follow.
    /// </summary>
    private static async ValueTask<LoadResult> LoadSideBySideAsync(MainWindow mainWindow, CoreViewModel core,
        MainWindowViewModel vm, TabViewModel tab, FileInfo fileInfo, List<FileInfo> files, string? rootDirectory)
    {
        var index = SideBySidePair.IndexOf(files, fileInfo) ?? 0;
        var (sideBySide, looping) = tab.PairSettings();
        var (primary, partner) = SideBySidePair.PairAt(index, files.Count, sideBySide, looping);

        // Without another image there is no pair to size or show, so the image loads as a single one
        if (partner is not { } partnerIndex)
        {
            return await LoadSingleAsync(mainWindow, core, vm, tab, fileInfo, files, rootDirectory)
                .ConfigureAwait(false);
        }

        if (!vm.WindowModeState.CurrentValue.AutoFit)
        {
            ShowNow(mainWindow);
        }

        // The other image of the pair: the one after the opened file, or before it when that is the last (ADR-0024)
        var isOpenedFirst = primary == index;
        var nextFileInfo = files[isOpenedFirst ? partnerIndex : primary];

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
                    // Sized in the order the pair shows, left to right
                    var sizes = isOpenedFirst
                        ? new PixelSizes(firstMagick.Width, firstMagick.Height, secondMagick.Width, secondMagick.Height)
                        : new PixelSizes(secondMagick.Width, secondMagick.Height, firstMagick.Width, firstMagick.Height);
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

        // Both decode while the tab is hooked up, and the tab's Load shows them as the pair with the files listed above
        var decodingFirst = GetImageModel.GetImageModelAsync(fileInfo, isPinged ? firstMagick : null);
        var decodingSecond = GetImageModel.GetImageModelAsync(nextFileInfo, isPinged ? secondMagick : null);
        await HookUpAsync(tab).ConfigureAwait(false);
        var first = await decodingFirst.ConfigureAwait(false);
        var second = await decodingSecond.ConfigureAwait(false);
        var result = await ShowThroughTheLoadAsync(core, tab, fileInfo,
            new LoadOptions(Model: first, SecondaryModel: second, Files: files, RootDirectory: rootDirectory))
            .ConfigureAwait(false);

        Dispatcher.UIThread.Post(() =>
        {
            mainWindow.Show();
            mainWindow.SetLayoutSizeAndVisibility(mainWindow.Bounds.Width);
        }, DispatcherPriority.Background);
        return result;
    }

    /// <summary>
    /// Shows the decoded models of <paramref name="options"/> through the tab's Load, which then browses the files
    /// around them, and gives the tab's gallery the shared settings after.
    /// </summary>
    private static async ValueTask<LoadResult> ShowThroughTheLoadAsync(CoreViewModel core, TabViewModel tab,
        FileInfo fileInfo, LoadOptions options)
    {
        var result = await tab.Load.LoadAsync(fileInfo.FullName, options).ConfigureAwait(false);
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
    /// Hooks the tab up to its window before its first image shows, so the window reacts to that image as it does to
    /// every later one. The window's hookups exist since the lifecycle created it.
    /// </summary>
    private static async ValueTask HookUpAsync(TabViewModel tab) =>
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            tab.ParentWindowContext.WindowTabs.HookUp(tab);
        }, DispatcherPriority.Send);
}
