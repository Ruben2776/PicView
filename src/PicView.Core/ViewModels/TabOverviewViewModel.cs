using System.Collections.ObjectModel;
using System.Diagnostics;
using PicView.Core.DebugTools;
using PicView.Core.FileSorting;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Navigation.Interfaces;
using R3;

namespace PicView.Core.ViewModels;

/// <summary>
/// The central hub that orchestrates the lifecycle and interaction of all application tabs.
/// <para>
/// This ViewModel manages the collection of <see cref="TabViewModel"/>s, handles the creation and 
/// closing of tabs, and initializes the shared services (Gallery, Navigation, Cache) that 
/// are injected into individual tabs.
/// </para>
/// </summary>
public class TabOverviewViewModel
{
    public BindableReactiveProperty<ObservableCollection<TabViewModel>> Tabs { get; } = new([]);
    public BindableReactiveProperty<TabViewModel> ActiveTab { get; }
    
    public INavigationService? SharedNavigation { get; private set; }
    public IImageCache? SharedCache { get; private set; }
    public IThumbnailCache? SharedThumbnailCache { get; private set; }
    public IThumbnailLoader? SharedThumbnailLoader { get; private set; }
    public IFileWatcherService? SharedFileWatcher { get; private set; }

    // Needed for correct context in multi-window scenarios
    private MainWindowViewModel? _parentVm;

    public TabOverviewViewModel(MainWindowViewModel parentVm)
    {
        _parentVm = parentVm;
        ActiveTab = new BindableReactiveProperty<TabViewModel>(CreateInitialTab());
        ActiveTab.Value.IsSelected = true;
    }

    /// <summary>
    /// Initialize the navigation service. Must be called before any other methods.
    /// </summary>
    /// <remarks>This is separated from constructor to improve initial startup time</remarks>
    /// <param name="navigationService">The navigation service responsible for navigating within the tabs</param>
    /// <param name="cache">The bitmap cache shared between tabs to reduce application memory usage</param>
    /// <param name="thumbnailLoader">The thumbnail loader to use for preloading images</param>
    /// <param name="fileWatcherService">The service that watches for file changes in the directory</param>
    /// <param name="thumbnailCache">The shared thumbnail cache</param>
    private void Initialize(INavigationService navigationService, IImageCache cache, IThumbnailLoader thumbnailLoader, IFileWatcherService fileWatcherService, IThumbnailCache thumbnailCache)
    {
        SharedCache = cache;
        SharedNavigation = navigationService;
        SharedThumbnailLoader = thumbnailLoader;
        SharedFileWatcher = fileWatcherService;
        SharedThumbnailCache = thumbnailCache;
        SharedCache.RegisterOwner(ActiveTab.Value.Id);
    }
    
    /// <inheritdoc cref="Initialize(INavigationService, IImageCache, IThumbnailLoader, IFileWatcherService, IThumbnailCache)"/>>
    public void LoadAndInitializeFromPath(IReadOnlyList<FileInfo> files, INavigationService navigationService, IImageCache cache, IThumbnailCache thumbCache,  IThumbnailLoader thumbnailLoader, IFileWatcherService fileWatcherService)
    {
        Initialize(navigationService, cache, thumbnailLoader, fileWatcherService, thumbCache);
        ActiveTab.Value.InitializeImageIterator(files, cache, thumbCache, thumbnailLoader, fileWatcherService, thumbCache);
        SharedCache.Preload(ActiveTab.Value.Id, ActiveTab.Value.ImageIterator.CurrentIndex, false, files, ActiveTab.CurrentValue.GetTabCancellation().Token);
    }
    /// <inheritdoc cref="Initialize(INavigationService, IImageCache, IThumbnailLoader, IFileWatcherService, IThumbnailCache)"/>>
    public void LoadAndInitialize(INavigationService navigationService, IImageCache cache, IThumbnailCache thumbCache, IThumbnailLoader thumbnailLoader, IFileWatcherService fileWatcherService)
    {
        Initialize(navigationService, cache, thumbnailLoader, fileWatcherService, thumbCache);
        ActiveTab.Value.InitializeImageIterator([], cache, thumbCache, thumbnailLoader, fileWatcherService, thumbCache);
        ActiveTab.Value.Initialize(cache, thumbCache, thumbnailLoader, fileWatcherService, thumbCache);
    }
    
    /// <summary>
    /// Creates the initial tab and adds it to the tabs collection. Used to show the initial image in the application,
    /// before the Initialize has been called.
    /// </summary>
    /// <returns></returns>
    public TabViewModel CreateInitialTab()
    {
        var tab = CreateTabInternal();
        tab.IsSelected = true;
        return tab;
    }
    
    private TabViewModel CreateTabInternal(FileInfo? file = null)
    {
        var tab = new TabViewModel(CloseTab, _parentVm, SharedFileWatcher);
        if (file is not null)
        {
            tab.Model.FileInfo = file;
        }
        Tabs.Value.Add(tab);
        return tab;
    }

    public TabViewModel CreateTab(FileInfo? file = null)
    {
        var tab = CreateTabInternal(file);
        if (SharedCache != null && SharedThumbnailLoader != null && SharedThumbnailCache != null)
        {
            tab.Initialize(SharedCache, SharedThumbnailCache, SharedThumbnailLoader, SharedFileWatcher, SharedThumbnailCache);
        }
        SharedCache!.RegisterOwner(tab.Id);
        SelectTab(tab);
        return tab;
    }
    
    public void SetParentContext(MainWindowViewModel parent)
    {
        _parentVm = parent;
        // Update existing tabs if any
        foreach(var tab in Tabs.Value)
        {
            tab.ParentWindowContext = parent;
        }
    }
    
    public void SelectTab(TabViewModel tab)
    {
        ActiveTab.Value = tab;
        ActiveTab.Value.IsSelected = true;
    }
    
    public void CloseTab()
    {
        var tab = ActiveTab.Value;
        CloseTab(tab);
    }
    
    public void CloseTab(uint tabId)
    {
        var tab = Tabs.Value.FirstOrDefault(t => t.Id == tabId);
        if (tab is null)
        {
            return;
        }
        CloseTab(tab);
    }


    public void CloseTab(TabViewModel tab)
    {
        // 1. Guard Clause: Prevent closing if this is the last tab.
        if (Tabs.Value.Count <= 1)
        {
            return;
        }

        var wasActive = ReferenceEquals(tab, ActiveTab.Value);
    
        // 2. Capture the index BEFORE removing the item
        var indexToRemove = Tabs.Value.IndexOf(tab);
        Tabs.Value.Remove(tab);

        if (wasActive && Tabs.Value.Count > 0)
        {
            // 3. Calculate the new index. 
            // We subtract 1 to go "behind" (left). 
            // We use Math.Max(0, ...) to ensure we don't go below zero if the first tab was closed.
            var targetIndex = Math.Max(0, indexToRemove - 1);
        
            // 4. Clamp ensures we don't exceed the new count
            var newIndex = Math.Clamp(targetIndex, 0, Tabs.Value.Count - 1);
            SelectTab(Tabs.Value[newIndex]);
        }

        SharedCache?.RemoveOwner(tab.Id);
        SharedThumbnailCache?.RemoveOwner(tab.Id);
        tab.Dispose();
    }

    // Used when detaching the tab
    public void RemoveTab(TabViewModel tab)
    {
        var wasActive = ReferenceEquals(tab, ActiveTab.Value);
    
        // Capture index
        var indexToRemove = Tabs.Value.IndexOf(tab);
    
        Tabs.Value.Remove(tab);

        if (!wasActive || Tabs.Value.Count <= 0)
        {
            return;
        }

        // Target the previous tab
        var targetIndex = Math.Max(0, indexToRemove - 1);
        var newIndex = Math.Clamp(targetIndex, 0, Tabs.Value.Count - 1);
        
        SelectTab(Tabs.Value[newIndex]);
    }

    #region Navigation

    public async ValueTask NextFile() =>
        await NavigateDirectionalAsync(false, NavigateTo.Next).ConfigureAwait(false);

    public async ValueTask PrevFile() =>
        await NavigateDirectionalAsync(false, NavigateTo.Previous).ConfigureAwait(false);

    public async ValueTask NextFolder()
    {
        var tab = ActiveTab.Value;
        if (SharedNavigation is null)
        {
            return;
        }
        var ct = tab.GetTabCancellation();
        await SharedNavigation.NavigateToNextFolderAsync(tab, ct).ConfigureAwait(false);
    }

    public async ValueTask PrevFolder()
    {
        var tab = ActiveTab.Value;
        if (SharedNavigation is null)
        {
            return;
        }
        var ct = tab.GetTabCancellation();
        await SharedNavigation.NavigateToPreviousFolderAsync(tab, ct).ConfigureAwait(false);
    }

    public async ValueTask NextArchive()
    {
        var tab = ActiveTab.Value;
        if (SharedNavigation is null)
        {
            return;
        }
        var ct = tab.GetTabCancellation();
        await SharedNavigation.NavigateToNextArchiveAsync(tab, ct).ConfigureAwait(false);
    }

    public async ValueTask PrevArchive()
    {
        var tab = ActiveTab.Value;
        if (SharedNavigation is null)
        {
            return;
        }
        var ct = tab.GetTabCancellation();
        await SharedNavigation.NavigateToPreviousArchiveAsync(tab, ct).ConfigureAwait(false);
    }

    public async ValueTask NavigateDirectionalAsync(bool isKeyHeldDown, NavigateTo direction)
    {
        var tab = ActiveTab.Value;

        if (tab.Gallery.IsGalleryExpanded.Value)
        {
            tab.Gallery.Navigate(direction);
            return;
        }
        
        if (SharedNavigation is null || tab.ImageIterator is null)
        {
            return;
        }

        if (isKeyHeldDown)
        {
            await tab.ImageIterator.RepeatNavigateAsync(direction,
                TimeSpan.FromSeconds(Settings.UIProperties.NavSpeed), tab.GetTabCancellation()).ConfigureAwait(false);
        }
        else
        {
            await NextFileCore(direction).ConfigureAwait(false);
        }
    }

    public void StopRepeatedNavigation()
    {
        var tab = ActiveTab.Value;
        tab.ImageIterator?.StopRepeatedNavigation();
    }
    
    public async ValueTask FirstFile() =>
        await NextFileCore(NavigateTo.First).ConfigureAwait(false);

    public async ValueTask LastFile() =>
        await NextFileCore(NavigateTo.Last).ConfigureAwait(false);
    
    public async ValueTask Next10() =>
        await IncrementsCore(SkipAmount.Ten, true).ConfigureAwait(false);
    
    public async ValueTask Next100() =>
        await IncrementsCore(SkipAmount.Hundred, true).ConfigureAwait(false);
    
    public async ValueTask Prev10() =>
        await IncrementsCore(SkipAmount.Ten, false).ConfigureAwait(false);
    
    public async ValueTask Prev100() =>
        await IncrementsCore(SkipAmount.Hundred, false).ConfigureAwait(false);

    private async ValueTask NextFileCore(NavigateTo navigateTo)
    {
        var tab = ActiveTab.Value;
        var ct = tab.GetTabCancellation();
        Debug.Assert(SharedNavigation != null, nameof(SharedNavigation) + " != null");
        await SharedNavigation.NavigateAsync(tab, navigateTo, ct).ConfigureAwait(false);
    }
    
    private async ValueTask IncrementsCore(SkipAmount skipAmount, bool forwards)
    {
        var tab = ActiveTab.Value;
        
        if (SharedNavigation is null || tab.ImageIterator is null)
        {
            return;
        }
        var ct = tab.GetTabCancellation();
        await SharedNavigation.NavigateByIncrementsAsync(tab, skipAmount, forwards, ct).ConfigureAwait(false);
    }


    public async ValueTask<bool> LoadFromStringAsync(string source, TabViewModel? senderTab = null)
    {
        if (SharedNavigation is null)
        {
            DebugHelper.LogDebug(nameof(TabOverviewViewModel), nameof(LoadFromStringAsync), 
                $"{nameof(SharedNavigation)} is null, make sure to initialize it before use");
            return false;
        }
        var tab = senderTab ?? ActiveTab.Value;
        var ct = tab.GetTabCancellation();
        
        var success = await SharedNavigation.LoadFromStringAsync(source, tab, ct)
            .ConfigureAwait(false);
        if (!success)
        {
            return false;
        }

        ActiveTab.Value = tab;
        return true;
    }
    
    public async ValueTask<bool> LoadFromUrlAsync(string source, TabViewModel? senderTab = null)
    {
        if (SharedNavigation is null)
        {
            return false;
        }
        var tab = senderTab ?? ActiveTab.Value;
        var ct = tab.GetTabCancellation();

        await SharedNavigation.LoadFromUrlAsync(source, tab, ct).ConfigureAwait(false);

        ActiveTab.Value = tab;
        return true;
    }
    
    public async ValueTask LoadFromFileAsync(FileInfo file, TabViewModel? senderTab = null)
    {
        if (SharedNavigation is null)
        {
            return;
        }
        var tab = senderTab ?? ActiveTab.Value;
        var ct = tab.GetTabCancellation();
        await SharedNavigation.LoadFromFileAsync(file, tab, ct).ConfigureAwait(false);
        tab.SingleImageType = SingleImageType.None;
    }
    
    public async ValueTask LoadFromFileAsync(string file, TabViewModel? senderTab = null) => 
        await LoadFromFileAsync(new FileInfo(file), senderTab).ConfigureAwait(false);

    public async ValueTask LoadFromDirectoryAsync(string file, TabViewModel? senderTab = null)
    {
        if (SharedNavigation is null)
        {
            return;
        }
        var tab = senderTab ?? ActiveTab.Value;
        var ct = tab.GetTabCancellation();
        await SharedNavigation.LoadFromDirectoryAsync(new FileInfo(file), tab, ct).ConfigureAwait(false);
        tab.SingleImageType = SingleImageType.None;
    }
    
    public async ValueTask LoadFromIndexAsync(int index, TabViewModel? senderTab = null)
    {
        var tab = senderTab ?? ActiveTab.Value;
        var ct = tab.GetTabCancellation();
        await tab.ImageIterator.IterateToIndexAsync(index, ct).ConfigureAwait(false);
        tab.SingleImageType = SingleImageType.None;
    }
    
    public async ValueTask<bool> LoadLastFileAsync()
    {
        if (SharedNavigation is null)
        {
            return false;
        }
        var tab = ActiveTab.Value;
        var ct = tab.GetTabCancellation();
        var lastFileExists = await SharedNavigation.LoadLastFileAsync(tab, ct).ConfigureAwait(false);
        if (lastFileExists)
        {
            return true;
        }
        tab.SingleImageType = SingleImageType.None;
        return false;
    }

    public async ValueTask<bool> LoadFromArchiveAsync(string source, TabViewModel? senderTab = null)
    {
        if (SharedNavigation is null)
        {
            return false;
        }
        var tab = senderTab ?? ActiveTab.Value;
        var ct = tab.GetTabCancellation();
        return await SharedNavigation.LoadFromArchiveAsync(source, tab, ct).ConfigureAwait(false);
    }

    public async ValueTask ReloadAsync(TabViewModel? senderTab = null)
    {
        var tab = senderTab ?? ActiveTab.Value;
        await tab.ImageIterator.ReloadAsync().ConfigureAwait(false);
    }
    #endregion

    #region Sort
    
    public BindableReactiveProperty<bool> IsSortedByName { get; } = new();
    public BindableReactiveProperty<bool> IsSortedByFileSize { get; } = new();
    public BindableReactiveProperty<bool> IsSortedByCreationTime { get; } = new();
    public BindableReactiveProperty<bool> IsSortedByExtension { get; } = new();
    public BindableReactiveProperty<bool> IsSortedByLastAccessTime { get; } = new();
    public BindableReactiveProperty<bool> IsSortedByLastWriteTime { get; } = new();
    public BindableReactiveProperty<bool> IsSortedByRandomization{ get; } = new();
    public BindableReactiveProperty<bool> IsAscending { get; } = new(Settings.Sorting.Ascending);

    public async ValueTask SortAsync(SortFilesBy sortOrder)
    {
        if (SharedNavigation is null)
        {
            return;
        }
        var tab = ActiveTab.Value;
        var ct = tab.GetTabCancellation();
        await SharedNavigation.SortAsync(tab, sortOrder, ct).ConfigureAwait(false);
        SetSortOrder(sortOrder);
    }
    
    public void SetSortOrder(SortFilesBy sortOrder)
    {
        // Using converters fails on macOS NativeMenu, so we have to do it manually
        switch (sortOrder)
        {
            case SortFilesBy.Name:
                IsSortedByName.Value = true;
                IsSortedByFileSize.Value = false;
                IsSortedByCreationTime.Value = false;
                IsSortedByExtension.Value = false;
                IsSortedByLastAccessTime.Value = false;
                IsSortedByLastWriteTime.Value = false;
                IsSortedByRandomization.Value = false;
                break;
            case SortFilesBy.FileSize:
                IsSortedByName.Value = false;
                IsSortedByFileSize.Value = true;
                IsSortedByCreationTime.Value = false;
                IsSortedByExtension.Value = false;
                IsSortedByLastAccessTime.Value = false;
                IsSortedByLastWriteTime.Value = false;
                IsSortedByRandomization.Value = false;
                break;
            case SortFilesBy.CreationTime:
                IsSortedByName.Value = false;
                IsSortedByFileSize.Value = false;
                IsSortedByCreationTime.Value = true;
                IsSortedByExtension.Value = false;
                IsSortedByLastAccessTime.Value = false;
                IsSortedByLastWriteTime.Value = false;
                IsSortedByRandomization.Value = false;
                break;
            case SortFilesBy.Extension:
                IsSortedByName.Value = false;
                IsSortedByFileSize.Value = false;
                IsSortedByCreationTime.Value = false;
                IsSortedByExtension.Value = true;
                IsSortedByLastAccessTime.Value = false;
                IsSortedByLastWriteTime.Value = false;
                IsSortedByRandomization.Value = false;
                break;
            case SortFilesBy.LastAccessTime:
                IsSortedByName.Value = false;
                IsSortedByFileSize.Value = false;
                IsSortedByCreationTime.Value = false;
                IsSortedByExtension.Value = false;
                IsSortedByLastAccessTime.Value = true;
                IsSortedByLastWriteTime.Value = false;
                IsSortedByRandomization.Value = false;
                break;
            case SortFilesBy.LastWriteTime:
                IsSortedByName.Value = false;
                IsSortedByFileSize.Value = false;
                IsSortedByCreationTime.Value = false;
                IsSortedByExtension.Value = false;
                IsSortedByLastAccessTime.Value = false;
                IsSortedByLastWriteTime.Value = true;
                IsSortedByRandomization.Value = false;
                break;
            case SortFilesBy.Random:
                IsSortedByName.Value = false;
                IsSortedByFileSize.Value = false;
                IsSortedByCreationTime.Value = false;
                IsSortedByExtension.Value = false;
                IsSortedByLastAccessTime.Value = false;
                IsSortedByLastWriteTime.Value = false;
                IsSortedByRandomization.Value = true;
                break;
        }
    }
    
    public async ValueTask SortAsync(bool ascending)
    {
        if (SharedNavigation is null)
        {
            return;
        }
        var tab = ActiveTab.Value;
        var ct = tab.GetTabCancellation();
        await SharedNavigation.SortAsync(tab, ascending, ct).ConfigureAwait(false);
        IsAscending.Value = ascending;
    }
    

    #endregion

    #region Retrieval

    public object? GetCurrentSource() => GetSourceFromFile(ActiveTab.CurrentValue.Model?.FileInfo);

    public object? GetSourceFromFile(FileInfo fileInfo)
    {
        if (SharedCache.TryGet(fileInfo, out var preLoadValue))
        {
            if (!preLoadValue.IsLoading && preLoadValue.ImageModel?.Image is not null)
            {
                return preLoadValue.ImageModel.Image;
            }
        }

        if (ActiveTab.CurrentValue.ImageIterator is null || ActiveTab.CurrentValue.ImageIterator.CurrentIndex < 0 
            || ActiveTab.CurrentValue.ImageIterator.CurrentIndex >= ActiveTab.CurrentValue.ImageIterator.Files.Count)
        {
            return null;
        }

        return SharedCache.LoadAsync(ActiveTab.CurrentValue.Id, ActiveTab.CurrentValue.ImageIterator.CurrentIndex,
            ActiveTab.CurrentValue.ImageIterator.Files, ActiveTab.CurrentValue.GetTabCancellation().Token)?.Result?.Image;
    }

    

    #endregion


}
