using System.Collections.ObjectModel;
using PicView.Core.ArchiveHandling;
using PicView.Core.DebugTools;
using PicView.Core.Extensions;
using PicView.Core.FileHandling;
using PicView.Core.FileHistory;
using PicView.Core.FileSearch;
using PicView.Core.FileSorting;
using PicView.Core.Gallery;
using PicView.Core.Http;
using PicView.Core.ImageDecoding;
using PicView.Core.IPlatform;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.Preloading;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Core.Navigation;

public class NavigationService(
    IImageLoader imageLoader,
    IImageCache cache,
    IFileWatcherService fileWatcherService,
    IPlatformSpecificService platformService,
    IThumbnailLoader thumbnailLoader,
    Func<string, string, int> stringComparer)
    : INavigationService
{
    
    public BindableReactiveProperty<ObservableCollection<FileSearchResult>?>? FilteredFileInfos { get; set; }
    public ReactiveCommand<string>? LoadFromStringCommand { get; set; }
    private FileAndDirectoryNavigator? _navigator;

    public async ValueTask RepopulateIterator(FileInfo fileInfo, TabViewModel tab, CancellationTokenSource ct, List<FileInfo>? files = null)
    {
        try
        {
            fileWatcherService.Unwatch(tab);

            // Show image quickly to make it feel fast
            ImageModel model;
            ImageModel? secondaryModel = null;
            int index;
            var secondaryIndex = 0;
            if (cache.TryGet(fileInfo, out var preLoadValue))
            {
                model = preLoadValue.ImageModel;
            }
            else
            {
                model = await imageLoader.GetImageModelAsync(fileInfo, ct.Token).ConfigureAwait(false);
            }
            if (Settings.ImageScaling.ShowImageSideBySide)
            {
                tab.ImageIterator.Files = files ?? FileListRetriever.RetrieveFiles(fileInfo, stringComparer);
                index = FindIndex(fileInfo, tab);
                var (_, nextIteration, _) = IterationHelper.GetIterations(index, tab.ImageIterator.Files.Count, NavigateTo.Next, SkipAmount.None);
                var secondaryFileInfo = tab.ImageIterator.Files[nextIteration];
                if (cache.TryGet(secondaryFileInfo, out var secondaryPreLoadValue))
                {
                    secondaryModel = secondaryPreLoadValue.ImageModel;
                }
                else
                {
                    secondaryModel = await imageLoader.GetImageModelAsync(secondaryFileInfo, ct.Token).ConfigureAwait(false);
                }
                tab.SecondaryModel = secondaryModel;
                tab.SecondaryImage.Value = secondaryModel.Image;
                tab.SecondaryImageType.Value = secondaryModel.ImageType;
                tab.SecondaryFileInfo.Value = secondaryFileInfo;
                ShowModel(model);
                secondaryIndex = nextIteration;
            }
            else
            {
                ShowModel(model);
                tab.ImageIterator.Files = files ?? FileListRetriever.RetrieveFiles(fileInfo, stringComparer);
                index = FindIndex(fileInfo, tab);
                tab.ImageIterator.SetCurrentIndex(index);
            }
            
            tab.UpdateTabTitle();
            fileWatcherService.Watch(tab, fileInfo.DirectoryName);
            cache.Clear(tab.Id);
            cache.Add(tab.Id, index, new PreLoadValue(model), tab.ImageIterator.Files.Count, false);
            if (secondaryModel is not null)
            {
                cache.Add(tab.Id, secondaryIndex, new PreLoadValue(model), tab.ImageIterator.Files.Count, false);
            }
            cache.Preload(tab.Id, index, false, tab.ImageIterator.Files, tab.GetTabCancellation().Token);
            FileHistoryManager.Add(fileInfo.FullName);

            if ((tab.Gallery.IsDockedGalleryVisible.CurrentValue || tab.Gallery.IsGalleryExpanded.CurrentValue) && tab.ThumbnailCache != null)
            {
                if (tab.Gallery.LoadingState is GalleryLoadingState.Loading or GalleryLoadingState.Loaded)
                {
                    await ct.CancelAsync();
                    tab.ResetNavigationCts();
                    await GalleryLoader.ReloadGallery(tab, tab.ImageIterator.Files, thumbnailLoader, tab.ThumbnailCache, tab.GetTabCancellation().Token).ConfigureAwait(false);
                    return;
                }
                tab.Gallery.LoadingState = GalleryLoadingState.NotLoaded;
                await GalleryLoader.LoadGalleryAsync(tab, tab.ImageIterator.Files, thumbnailLoader, tab.ThumbnailCache, ct.Token).ConfigureAwait(false);
            }
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(NavigationService), nameof(RepopulateIterator), e);
        }
        
        return;

        void ShowModel(ImageModel model)
        {
            tab.Model = model; // Image updated via reactive subscription
            tab.FileInfo.Value = model.FileInfo;
            tab.Image.Value = model.Image;
            tab.ImageType.Value = model.ImageType;
        }
    }

    public async ValueTask LoadFromFileAsync(string source, TabViewModel tab, CancellationTokenSource ct)
    {
        ArgumentNullException.ThrowIfNull(source);
        await LoadFromFileAsync(new FileInfo(source), tab, ct).ConfigureAwait(false);
    }

    public async ValueTask LoadFromFileAsync(FileInfo fileInfo, TabViewModel tab, CancellationTokenSource ct)
    {
        if (!fileInfo.Exists)
        {
            DebugHelper.LogDebug(nameof(NavigationService), nameof(LoadFromFileAsync), $"Attempted to load a file that does not exist: {fileInfo}");
            return;
        }
        var iterator = tab.ImageIterator;

        if (iterator.Files is null || iterator.Files.Count is 0)
        {
            // TODO: Figure out way to share file list, if another tab is already in the same directory
            await Repopulate();
            return;
        }

        var index = FindIndex(fileInfo, tab);
        if (index is not -1)
        {
            if (Settings.ImageScaling.ShowImageSideBySide)
            {
                var (_, nextIteration, _) = IterationHelper.GetIterations(index, tab.ImageIterator.Files.Count, NavigateTo.Next, SkipAmount.None);
                await tab.ImageIterator.IterateToIndicesAsync(index, nextIteration, ct).ConfigureAwait(false);
            }
            else
            {
                await tab.ImageIterator.IterateToIndexAsync(index, ct).ConfigureAwait(false);
            }
        }
        else
        {
            await Repopulate();
        }
        
        tab.ArchiveExtractionService.Cleanup();

        return;

        async ValueTask Repopulate()
        {
            await RepopulateIterator(fileInfo, tab, ct).ConfigureAwait(false);
        }
    }

    public async ValueTask LoadFromDirectoryAsync(FileInfo source, TabViewModel tab, CancellationTokenSource ct)
    {
        var files = await Task.Run(() => FileListRetriever.RetrieveFiles(source, stringComparer), ct.Token).ConfigureAwait(false);
        if (files.Count is 0)
        {
            return;
        }

        var first = files[0];
        await RepopulateIterator(first, tab, ct, files).ConfigureAwait(false);
        tab.ArchiveExtractionService.Cleanup();
    }

    public async ValueTask<bool> LoadFromStringAsync(string source, TabViewModel tab, CancellationTokenSource ct)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return false;
        }

        var check = FileTypeResolver.CheckIfLoadableString(source);
        if (check is null)
        {
            return false;
        }

        switch (check.Value.Type)
        {
            case FileTypeResolver.LoadAbleFileType.File:
                await LoadFromFileAsync(check.Value.Data, tab, ct).ConfigureAwait(false);
                return true;
            case FileTypeResolver.LoadAbleFileType.Directory:
            {
                await LoadFromDirectoryAsync(new FileInfo(check.Value.Data), tab, ct).ConfigureAwait(false);
                return true;
            }
            case FileTypeResolver.LoadAbleFileType.Web:
                await LoadFromUrlAsync(check.Value.Data, tab, ct).ConfigureAwait(false);
                return true;
            case FileTypeResolver.LoadAbleFileType.Zip:
                return await LoadFromArchiveAsync(check.Value.Data, tab, ct).ConfigureAwait(false);
            case FileTypeResolver.LoadAbleFileType.Base64:
                await LoadFromBase64Async(check.Value.Data, tab, ct).ConfigureAwait(false);
                return true;
            default:
                return false;
        }
    }

    public async ValueTask<bool> LoadFromArchiveAsync(string archivePath, TabViewModel tab, CancellationTokenSource ct)
    {
        if (string.IsNullOrEmpty(archivePath) || !File.Exists(archivePath))
        {
            return false;
        }
        
        // Retrieve the temporary directory for the possible previous archive extraction
        var tempZipDir = tab.ArchiveExtractionService.TempZipDirectory;

        var preparation = await tab.ArchiveExtractionService.PrepareArchiveAsync(
            archivePath,
            platformService.ExtractWithLocalSoftwareAsync,
            stringComparer).ConfigureAwait(false);

        if (preparation is null || string.IsNullOrEmpty(tab.ArchiveExtractionService.TempZipDirectory))
        {
            return false;
        }

        if (ct.IsCancellationRequested)
        {
            return false;
        }

        var prep = preparation.Value;

        if (prep.IsFullyExtracted)
        {
            // Local-software extractor already wrote every file to disk; build the file list
            // from the already-extracted paths so we don't depend on FileListRetriever's
            // recursion settings.
            var allFiles = prep.EntryKeys.Select(p => new FileInfo(p)).ToList();
            if (allFiles.Count is 0)
            {
                return false;
            }

            await RepopulateIterator(allFiles[0], tab, ct, allFiles).ConfigureAwait(false);

            FileHistoryManager.Add(archivePath);
            return true;
        }

        // Staged extraction: extract the first entry, navigate to it, then extract the rest in
        // the background while FileWatcherService inserts each new file into the iterator.
        var firstKey = prep.EntryKeys[0];
        var firstPath = await tab.ArchiveExtractionService.ExtractEntryAsync(archivePath, firstKey, ct.Token).ConfigureAwait(false);

        if (string.IsNullOrEmpty(firstPath) || ct.IsCancellationRequested)
        {
            return false;
        }

        if (Settings.ImageScaling.ShowImageSideBySide && prep.EntryKeys.Length > 1)
        {
            var secondKey = prep.EntryKeys[1];
            var secondPath = await tab.ArchiveExtractionService.ExtractEntryAsync(archivePath, secondKey, ct.Token).ConfigureAwait(false);
            var seedFiles = new List<FileInfo>
            {
                new(firstPath),
                new(secondPath)
            };
            await RepopulateIterator(seedFiles[0], tab, ct, seedFiles).ConfigureAwait(false);
        }
        else
        {
            // Seed the iterator with just the first extracted file. Watching the temp directory
            // first ensures every subsequent file creation event is captured by FileWatcherService
            // and inserted in sorted order into the iterator/gallery.
            var seedFiles = new List<FileInfo> { new(firstPath) };
            await RepopulateIterator(seedFiles[0], tab, ct, seedFiles).ConfigureAwait(false);
        }


        // Kick off background extraction of remaining entries. FileWatcherService picks them up.
        if (prep.EntryKeys.Length > 1)
        {
            var remainingKeys = prep.EntryKeys.Skip(1).ToArray();
            var backgroundToken = tab.GetTabCancellation().Token;
            _ = Task.Run(() => tab.ArchiveExtractionService.ExtractRemainingAsync(archivePath, remainingKeys, backgroundToken), backgroundToken);
        }

        FileHistoryManager.Add(archivePath);
        tab.ArchiveExtractionService.Cleanup(tempZipDir);
        return true;
    }

    public async ValueTask LoadFromBase64Async(string source, TabViewModel tab, CancellationTokenSource ct)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return;
        }

        try
        {
            var base64Data = Base64Decoder.IsBase64String(source);
            if (string.IsNullOrEmpty(base64Data))
            {
                return;
            }

            var bytes = Convert.FromBase64String(base64Data);

            // Write to a temp file so we can reuse the regular image-loading pipeline.
            var destPath = TempFileManager.GetNewTempFilePath($"base64_{Guid.NewGuid():N}");
            await File.WriteAllBytesAsync(destPath, bytes, ct.Token).ConfigureAwait(false);

            if (ct.IsCancellationRequested)
            {
                return;
            }

            tab.ImageIterator?.Dispose();

            var model = await imageLoader.GetImageModelAsync(new FileInfo(destPath), ct.Token).ConfigureAwait(false);
            tab.Model = model;
            tab.SecondaryModel = null;

            tab.SingleImageType = SingleImageType.Base64;
            tab.UpdateTabTitle();

            tab.CanNavigateBackwards.Value = false;
            tab.CanNavigateForwards.Value = false;

            tab.ArchiveExtractionService.Cleanup();
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(NavigationService), nameof(LoadFromBase64Async), e);
            tab.TabTitle.Value = TranslationManager.Translation?.ErrorLoadingImage ?? "Error";
        }
    }

    public async ValueTask LoadFromUrlAsync(string url, TabViewModel tab, CancellationTokenSource ct)
    {
        tab.ImageIterator?.Dispose();

        platformService.StopTaskbarProgress();
        var safeFileName = HttpManager.GetSafeFileName(url);
        var destPath = TempFileManager.GetNewTempFilePath(safeFileName);
        
        using var client = new HttpClientDownloadWithProgress(url, destPath);
        client.ProgressChanged += (totalFileSize, totalBytesDownloaded, progressPercentage) =>
        {
            var displayProgress = HttpManager.GetProgressDisplay(totalFileSize, totalBytesDownloaded, progressPercentage);
            var title = $"{safeFileName} {TranslationManager.Translation?.Downloading} {displayProgress}";

            // Update UI properties
            if (tab.TabTitle.Value != title) tab.TabTitle.Value = title;
            if (tab.Title.Value != title) tab.Title.Value = title;
            if (tab.WindowTitle.Value != title) tab.WindowTitle.Value = title;
            if (tab.TitleTooltip.Value != title) tab.TitleTooltip.Value = title;

            if (totalBytesDownloaded.HasValue && totalFileSize.HasValue)
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
                return;
            }
            
            var model = await imageLoader.GetImageModelAsync(new FileInfo(destPath), ct.Token).ConfigureAwait(false);
            tab.Model = model;
            tab.SecondaryModel = null;
            
            // Set titles to filename after successful load
            tab.SourceURL = url;
            tab.SingleImageType = SingleImageType.Url;
            tab.UpdateTabTitle();
            
            tab.CanNavigateBackwards.Value = false;
            tab.CanNavigateForwards.Value = false;

            FileHistoryManager.Add(url);
            tab.ArchiveExtractionService.Cleanup();
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(NavigationService), nameof(LoadFromUrlAsync), e);
            platformService.StopTaskbarProgress();
            // Revert or show error state if needed
            tab.TabTitle.Value = TranslationManager.Translation?.ErrorLoadingImage ?? "Error";
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
    
    public async ValueTask<bool> LoadLastFileAsync(TabViewModel tab, CancellationTokenSource ct)
    {
        var lastFile = Settings.StartUp.LastFile;
        var lastEntry = FileHistoryManager.GetLastEntry();

        // determine which file source to use (prioritize LastFile, fallback to History)
        var fileToLoad = !string.IsNullOrEmpty(lastFile) ? lastFile : lastEntry;
        if (string.IsNullOrEmpty(lastEntry))
        {
            return false;
        }

        await LoadFromStringAsync(fileToLoad, tab, ct).ConfigureAwait(false);
        return true;
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
        var currentFile = tab.ArchiveExtractionService.IsArchived ?
            new FileInfo(tab.ArchiveExtractionService.LastOpenedArchive) : tab.Model.FileInfo;
           
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

            if (newFiles.Count == 0)
            {
                return;
            }

            // Update files in iterator
            tab.ImageIterator.Files = newFiles;

            // Find new index of current file
            var newIndex = FindIndex(currentFile, tab);
            tab.ImageIterator.SetCurrentIndex(newIndex);
            
            // Update cache mapping
            cache.Resynchronize(tab.Id, newFiles);
            
            // Update title
            tab.UpdateTabTitle();
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(NavigationService), nameof(ApplySortAsync), e);
        }
    }

    private static int FindIndex(FileInfo fileInfo, TabViewModel tab) =>
        tab.ImageIterator.Files.FindIndex(x =>
            x.FullName.AsSpan().Equals(fileInfo.FullName.AsSpan(), StringComparison.OrdinalIgnoreCase));
}