using PicView.Core.ArchiveHandling;
using PicView.Core.DebugTools;
using PicView.Core.FileHandling;
using PicView.Core.FileSorting;
using PicView.Core.Http;
using PicView.Core.IPlatform;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.ViewModels;

namespace PicView.Core.Navigation;

public class NavigationService(
    IImageModelLoader imageModelLoader,
    SharedImageCache cache,
    IFileWatcherService fileWatcherService,
    IPlatformSpecificService platformService,
    Func<string, string, int> stringComparer)
    : INavigationService
{
    
    private FileAndDirectoryNavigator? _navigator;

    public async ValueTask RepopulateIterator(FileInfo fileInfo, TabViewModel tab, CancellationTokenSource ct, List<FileInfo>? files = null) =>
        await RepopulateIterator(fileInfo, tab, ct, files, tab.BeginLoad()).ConfigureAwait(false);

    /// <param name="load">The version of the load this is part of. Once a newer load begins, this one stops before
    /// changing the tab's image, files or gallery, so a slow load that finishes last cannot overwrite a newer one.</param>
    /// <returns>Whether the tab showed the file.</returns>
    private async ValueTask<bool> RepopulateIterator(FileInfo fileInfo, TabViewModel tab, CancellationTokenSource ct,
        IReadOnlyList<FileInfo>? files, int load)
    {
        var isShown = false;
        try
        {
            fileWatcherService.Unwatch(tab);

            // Show image quickly to make it feel fast
            ImageModel? secondaryModel = null;
            var secondaryIndex = 0;
            var model = await cache.GetAsync(tab.Id, fileInfo, ct.Token).ConfigureAwait(false);
            if (model is null || !tab.IsLatestLoad(load))
            {
                return false;
            }

            if (!Settings.ImageScaling.ShowImageSideBySide)
            {
                tab.Display(model);
                isShown = true;
            }

            await Task.Yield();
            var retrieved = files ?? FileListRetriever.RetrieveFiles(fileInfo, stringComparer);
            if (!tab.IsLatestLoad(load))
            {
                return isShown;
            }
            var index = SideBySidePair.IndexOf(retrieved, fileInfo) ?? 0;
            tab.ImageIterator.Initialize(retrieved, index);

            if (Settings.ImageScaling.ShowImageSideBySide)
            {
                if (SideBySidePair.PartnerOf(index, tab.ImageIterator.Files.Count) is { } nextIteration)
                {
                    var secondaryFileInfo = tab.ImageIterator.Files[nextIteration];
                    secondaryModel = await cache.GetAsync(tab.Id, secondaryFileInfo, ct.Token).ConfigureAwait(false);
                    if (secondaryModel is null || !tab.IsLatestLoad(load))
                    {
                        return false;
                    }
                    secondaryIndex = nextIteration;
                }
                tab.Display(model, secondaryModel);
                isShown = true;
            }

            // A newer load that begins after this sets its own files after these
            if (!tab.IsLatestLoad(load))
            {
                return true;
            }
            fileWatcherService.Watch(tab, fileInfo.DirectoryName);
            cache.ReleaseImages(tab.Id);
            if (tab.ImageIterator.Files.Count > 0)
            {
                var currentFile = tab.ImageIterator.Files[index];
                cache.Put(tab.Id, currentFile, model);
                if (secondaryModel is not null)
                {
                    cache.Put(tab.Id, tab.ImageIterator.Files[secondaryIndex], secondaryModel);
                }
                cache.Preload(tab.Id, currentFile, false, tab.GetTabCancellation().Token);
            }

            return true;
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(NavigationService), nameof(RepopulateIterator), e);
            return isShown;
        }
    }

    /// <inheritdoc cref="LoadFromFileAsync(FileInfo, TabViewModel, CancellationTokenSource)"/>
    public async ValueTask LoadFromFileAsync(string source, TabViewModel tab, CancellationTokenSource ct)
    {
        ArgumentNullException.ThrowIfNull(source);
        await LoadFromFileAsync(new FileInfo(source), tab, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Loads a file into <paramref name="tab"/>, and completes only once the file shows, a newer load supersedes this one,
    /// or the load fails, since a step Back or Forward ends when its load completes (ADR-0012).
    /// </summary>
    public async ValueTask LoadFromFileAsync(FileInfo fileInfo, TabViewModel tab, CancellationTokenSource ct)
    {
        if (!fileInfo.Exists)
        {
            DebugHelper.LogDebug(nameof(NavigationService), nameof(LoadFromFileAsync), $"Attempted to load a file that does not exist: {fileInfo}");
            return;
        }

        if (fileInfo.FullName.IsArchive())
        {
            await LoadFromArchiveAsync(fileInfo.FullName, tab, ct).ConfigureAwait(false);
            return;
        }
        var load = tab.BeginLoad();
        // The archives the tab opened so far, which it leaves once the file shows. One that opens after survives this load
        var mark = tab.Archives.Mark();
        var iterator = tab.ImageIterator;

        if (iterator?.Files is null || iterator.Files.Count is 0)
        {
            // TODO: Figure out way to share file list, if another tab is already in the same directory
            await Repopulate().ConfigureAwait(false);
            return;
        }

        var index = SideBySidePair.IndexOf(tab.ImageIterator.Files, fileInfo);
        if (index is null && IsInShownFolder(fileInfo, tab))
        {
            // A file just written next to the shown one, such as a duplicate, is added by the folder watcher,
            // which also adds it to the gallery, so wait for it rather than rebuilding the list and the gallery
            if (await iterator.WaitForFileAsync(fileInfo.FullName, FolderWatcherWait, ct.Token).ConfigureAwait(false))
            {
                index = SideBySidePair.IndexOf(tab.ImageIterator.Files, fileInfo);
            }
            if (ct.IsCancellationRequested || !tab.IsLatestLoad(load))
            {
                return;
            }
        }

        if (index is { } found)
        {
            await tab.ImageIterator.IterateToIndexAsync(found, ct).ConfigureAwait(false);
            tab.Archives.Shown(mark);
        }
        else
        {
            await Repopulate().ConfigureAwait(false);
        }

        return;

        // Only a file that shows leaves the archives, as a load that a newer one overtakes leaves them to it
        async Task Repopulate()
        {
            // AsTask, because Task.Run waits for a Task but returns a ValueTask without awaiting it
            if (await Task.Run(() => RepopulateIterator(fileInfo, tab, ct, null, load).AsTask(), ct.Token)
                    .ConfigureAwait(false))
            {
                tab.Archives.Shown(mark);
            }
        }
    }

    /// <summary>
    /// How long to wait for the folder watcher to add a new file before rebuilding the file list instead.
    /// </summary>
    private static readonly TimeSpan FolderWatcherWait = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Whether <paramref name="fileInfo"/> is an image in the folder of the file the tab shows, which the folder
    /// watcher reports when it is created.
    /// </summary>
    private static bool IsInShownFolder(FileInfo fileInfo, TabViewModel tab) =>
        fileInfo.FullName.IsSupported() &&
        string.Equals(fileInfo.DirectoryName, tab.Model?.FileInfo?.DirectoryName, StringComparison.OrdinalIgnoreCase);

    public async Task<bool> LoadFromDirectoryAsync(FileInfo source, TabViewModel tab, CancellationTokenSource ct)
    {
        var load = tab.BeginLoad();
        // The archives the tab opened so far, which it leaves once the file shows. One that opens after survives this load
        var mark = tab.Archives.Mark();
        var files = await Task.Run(() =>
        {
            var files = FileListRetriever.RetrieveFiles(source, stringComparer);
            return files.Count is 0 ? null : files;
        }, ct.Token).ConfigureAwait(false);
        if (files is null)
        {
            return false;
        }
        if (await RepopulateIterator(files[0], tab, ct, files, load).ConfigureAwait(false))
        {
            tab.Archives.Shown(mark);
        }
        return true;
    }

    public async ValueTask<LoadResult> LoadFromStringAsync(string source, TabViewModel tab, CancellationTokenSource ct,
        string? rootDirectory = null, string? archiveEntry = null)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return LoadResult.Failed();
        }

        var check = FileTypeResolver.CheckIfLoadableString(source);
        if (check is null)
        {
            return LoadResult.Failed();
        }

        switch (check.Value.Type)
        {
            case FileTypeResolver.LoadAbleFileType.File:
                if (rootDirectory is null)
                {
                    await LoadFromFileAsync(check.Value.Data, tab, ct).ConfigureAwait(false);
                }
                else
                {
                    var load = tab.BeginLoad();
                    // The archives the tab opened so far, which it leaves once the file shows. One that opens after
                    // survives this load
                    var mark = tab.Archives.Mark();
                    var files = await Task.Run(() => FileListRetriever.RetrieveFiles(new FileInfo(rootDirectory), stringComparer), ct.Token)
                        .ConfigureAwait(false);
                    if (await RepopulateIterator(new FileInfo(check.Value.Data), tab, ct, files.Count > 0 ? files : null,
                            load).ConfigureAwait(false))
                    {
                        tab.Archives.Shown(mark);
                    }
                }
                return LoadResult.Loaded;
            case FileTypeResolver.LoadAbleFileType.Directory:
                return await LoadFromDirectoryAsync(new FileInfo(check.Value.Data), tab, ct).ConfigureAwait(false)
                    ? LoadResult.Loaded
                    : LoadResult.Failed();
            case FileTypeResolver.LoadAbleFileType.Web:
                return await LoadFromUrlAsync(check.Value.Data, tab, ct).ConfigureAwait(false);
            case FileTypeResolver.LoadAbleFileType.Zip:
                return await LoadFromArchiveAsync(check.Value.Data, tab, ct, archiveEntry).ConfigureAwait(false)
                    ? LoadResult.Loaded
                    : LoadResult.Failed();
            default:
                return LoadResult.Failed();
        }
    }

    public async ValueTask<bool> LoadFromArchiveAsync(string archivePath, TabViewModel tab, CancellationTokenSource ct,
        string? startEntry = null)
    {
        if (string.IsNullOrEmpty(archivePath) || !File.Exists(archivePath))
        {
            return false;
        }

        // Taken first, so a load that a newer one overtakes while its archive opens shows nothing
        var load = tab.BeginLoad();
        tab.SetLoading();
        // Show progress spinner while extracting archive
        if (tab.ParentWindowContext is not null)
        {
            tab.ParentWindowContext.IsLoadingIndicatorShown.Value = true;
        }

        // The archives the tab opened so far, which it leaves once a page of this one shows (ADR-0013)
        var mark = tab.Archives.Mark();
        ArchiveOpening? opening = null;
        var isShown = false;

        try
        {
            opening = await tab.Archives.OpenAsync(archivePath, startEntry, platformService.ExtractWithLocalSoftwareAsync,
                stringComparer, ct.Token).ConfigureAwait(false);
            // A newer load of a file overtakes this one too, which the opening cannot see
            if (opening is null || !tab.IsLatestLoad(load))
            {
                return false;
            }

            isShown = await RepopulateIterator(opening.StartFile, tab, ct, opening.Files, load).ConfigureAwait(false);
            if (!isShown)
            {
                return false;
            }

            // The other pages come in the background while the start page shows
            opening.ExtractRest();
            tab.Archives.Shown(mark);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(NavigationService), nameof(LoadFromArchiveAsync), ex);
            return false;
        }
        finally
        {
            if (!isShown)
            {
                // Unless a page of it shows, the opening goes again, and the tab's archive stays the one it shows
                opening?.Abandon();
                // This load said Loading and showed nothing, so the titles say what the tab shows again
                tab.EndLoading();
            }

            // Hide progress spinner when initial extraction and display complete
            if (tab.ParentWindowContext is not null)
            {
                tab.ParentWindowContext.IsLoadingIndicatorShown.Value = false;
            }
        }
    }

    public async ValueTask<LoadResult> LoadFromUrlAsync(string url, TabViewModel tab, CancellationTokenSource ct)
    {
        // Showing the downloaded image supersedes loading files still running.
        // Drops the iterator, as for a clipboard image, so SourceOpener docks the gallery again when browsing files
        tab.BeginLoad();
        tab.DisposeImageIterator();

        platformService.StopTaskbarProgress();
        var safeFileName = HttpManager.GetSafeFileName(url);
        var destPath = TempFileManager.GetNewTempFilePath(safeFileName);
        
        using var client = new HttpClientDownloadWithProgress(url, destPath);
        client.ProgressChanged += (totalFileSize, totalBytesDownloaded, progressPercentage) =>
        {
            var displayProgress = HttpManager.GetProgressDisplay(totalFileSize, totalBytesDownloaded, progressPercentage);
            tab.SetDownloading($"{safeFileName} {TranslationManager.Translation?.Downloading} {displayProgress}");

            if (Settings.UIProperties.IsTaskbarProgressEnabled && totalBytesDownloaded.HasValue && totalFileSize.HasValue)
            {
                platformService.SetTaskbarProgress((ulong)totalBytesDownloaded.Value, (ulong)totalFileSize.Value);
            }
        };

        try
        {
            await client.StartDownloadAsync(ct.Token).ConfigureAwait(false);

            platformService.StopTaskbarProgress();

            if (ct.IsCancellationRequested)
            {
                // Nothing shows, so the progress text must not stay in the titles
                tab.EndLoading();
                return LoadResult.Failed();
            }

            var model = await imageModelLoader.GetImageModelAsync(new FileInfo(destPath), ct.Token)
                .ConfigureAwait(false);

            // Set before the image is shown, since the tab records the URL then
            tab.SourceURL = url;
            tab.DisplaySingleImage(model, SingleImageType.Url, url);

            tab.CanNavigateBackwards.Value = false;
            tab.CanNavigateForwards.Value = false;

            tab.Archives.Cleanup();
            return LoadResult.Loaded;
        }
        catch (OperationCanceledException)
        {
            platformService.StopTaskbarProgress();
            // Nothing shows, so the progress text must not stay in the titles
            tab.EndLoading();
            return LoadResult.Failed();
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(NavigationService), nameof(LoadFromUrlAsync), e);
            platformService.StopTaskbarProgress();
            // Held until the next load, so the user sees that this one failed
            tab.SetError(TranslationManager.Translation?.ErrorLoadingImage ?? "Error");
            return LoadResult.Failed(e.Message);
        }
    }

    public async ValueTask NavigateAsync(TabViewModel tab, NavigateTo to, CancellationTokenSource ct) =>
        await tab.ImageIterator.NavigateAsync(to, SkipAmount.One, ct).ConfigureAwait(false);

    public async ValueTask NavigateByIncrementsAsync(TabViewModel tab, SkipAmount skipAmount, bool forwards, CancellationTokenSource ct)
    {
        var iterator = tab.ImageIterator;
        if (iterator is null)
        {
            return;
        }
        await iterator.NavigateByIncrementsAsync(skipAmount,forwards, ct).ConfigureAwait(false);
    }
    
    public async ValueTask NavigateToNextFolderAsync(TabViewModel tab, CancellationTokenSource ct)
    {
        var currentDir = tab.Model?.FileInfo?.DirectoryName;
        if (currentDir == null)
        {
            return;
        }

        _navigator ??= new FileAndDirectoryNavigator(stringComparer);
        var nextDir = await Task.Run(() => _navigator.FindNextValidDirectory(currentDir), ct.Token).ConfigureAwait(false);
        if (nextDir != null)
        {
            await LoadFromDirectoryAsync(new FileInfo(nextDir), tab, ct).ConfigureAwait(false);
        }
    }

    public async ValueTask NavigateToPreviousFolderAsync(TabViewModel tab, CancellationTokenSource ct)
    {
        var currentDir = tab.Model?.FileInfo?.DirectoryName;
        if (currentDir == null)
        {
            return;
        }
        _navigator ??= new FileAndDirectoryNavigator(stringComparer);
        var prevDir = await Task.Run(() => _navigator.FindPreviousValidDirectory(currentDir), ct.Token).ConfigureAwait(false);
        if (prevDir != null)
        {
            await LoadFromDirectoryAsync(new FileInfo(prevDir), tab, ct).ConfigureAwait(false);
        }
    }

    public async ValueTask NavigateToNextArchiveAsync(TabViewModel tab, CancellationTokenSource ct) 
        => await NavigateArchiveCoreAsync(tab, true, ct).ConfigureAwait(false);

    public async ValueTask NavigateToPreviousArchiveAsync(TabViewModel tab, CancellationTokenSource ct)
        => await NavigateArchiveCoreAsync(tab, false, ct).ConfigureAwait(false);

    private async ValueTask NavigateArchiveCoreAsync(TabViewModel tab, bool next, CancellationTokenSource ct)
    {
        var currentFile = tab.Archives.OpenArchive is { } openArchive ?
            new FileInfo(openArchive) : tab.Model.FileInfo;
           
        var currentDir = currentFile?.DirectoryName;
        if (currentDir == null)
        {
            return;
        }
        _navigator ??= new FileAndDirectoryNavigator(stringComparer);
        var nextArchive = await Task.Run(() =>
            _navigator.FindNextArchive(currentDir, next, currentFile!.FullName), ct.Token).ConfigureAwait(false);
        if (nextArchive != null)
        {
            await LoadFromArchiveAsync(nextArchive, tab, ct).ConfigureAwait(false);
        }
    }

    public async ValueTask SortAsync(TabViewModel tab, SortFilesBy sortOrder, CancellationTokenSource ct)
    {
        Settings.Sorting.SortPreference = (int)sortOrder;
        await ApplySortAsync(tab, ct).ConfigureAwait(false);
    }

    public async ValueTask SortAsync(TabViewModel tab, bool ascending, CancellationTokenSource ct)
    {
        Settings.Sorting.Ascending = ascending;
        await ApplySortAsync(tab, ct).ConfigureAwait(false);
    }

    private async ValueTask ApplySortAsync(TabViewModel tab, CancellationTokenSource ct)
    {
        if (tab.ImageIterator.Files.Count <= 0)
        {
            return;
        }

        try
        {
            // Get current file to maintain position
            var currentFile = tab.Model?.FileInfo;
            if (currentFile is null)
            {
                return;
            }

            // Retrieve and sort files based on new settings
            var newFiles = await Task.Run(() => FileListRetriever.RetrieveFiles(currentFile, stringComparer), ct.Token).ConfigureAwait(false);

            if (newFiles.Count is 0)
            {
                return;
            }

            // Update files in iterator, keeping the position on the current file
            // The titles and the gallery follow the sorted list by themselves
            tab.ImageIterator.Initialize(newFiles, SideBySidePair.IndexOf(newFiles, currentFile) ?? -1);
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(NavigationService), nameof(ApplySortAsync), e);
        }
    }
}