using System.Collections.ObjectModel;
using System.Diagnostics;
using PicView.Core.DebugTools;
using PicView.Core.FileSorting;
using PicView.Core.IPlatform;
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
    
    // The services every Main Window shares; navigation, thumbnail loading and file watching exist once the first
    // tab has opened a source
    private CoreViewModel? Core => _parentVm?.Core;
    public INavigationService? SharedNavigation => Core?.SharedNavigationService;
    public SharedImageCache? SharedCache => Core?.SharedCache;
    public IThumbnailCache? SharedThumbnailCache => Core?.SharedThumbnailCache;
    public IThumbnailLoader? SharedThumbnailLoader => Core?.SharedThumbnailLoader;
    public IFileWatcherService? SharedFileWatcher => Core?.SharedFileWatcher;

    private readonly ReactiveProperty<bool> _isTabStripShown = new(false);

    /// <summary>
    /// Whether the window shows its tab strip, which it does with two or more tabs. The strip is part of the Reserved
    /// Space, so the window is fitted again when this changes.
    /// </summary>
    public ReadOnlyReactiveProperty<bool> IsTabStripShown => _isTabStripShown;

    // The Main Window these tabs belong to
    private readonly MainWindowViewModel _parentVm;
    private ITabHookups? _hookups;
    private bool _isTornDown;

    /// <param name="parentVm">The Main Window these tabs belong to.</param>
    /// <param name="firstTab">A tab moved out of another Main Window, for a window that opens with it; otherwise the
    /// window opens with a new tab.</param>
    public TabOverviewViewModel(MainWindowViewModel parentVm, TabViewModel? firstTab = null)
    {
        _parentVm = parentVm;
        if (firstTab is null)
        {
            ActiveTab = new BindableReactiveProperty<TabViewModel>(CreateInitialTab());
        }
        else
        {
            firstTab.ParentWindowContext = parentVm;
            Tabs.Value.Add(firstTab);
            ActiveTab = new BindableReactiveProperty<TabViewModel>(firstTab);
        }
        ActiveTab.Value.IsSelected = true;
        Tabs.Value.CollectionChanged += (_, _) => _isTabStripShown.Value = Tabs.Value.Count > 1;
    }

    /// <summary>
    /// Gives this window what it does when its tabs change, and attaches every tab already opened, such as a tab it
    /// opened with after a move. Called once, when the window is created.
    /// </summary>
    public void SetHookups(ITabHookups hookups)
    {
        _hookups = hookups;
        foreach (var tab in Tabs.Value)
        {
            Attach(tab);
        }
    }

    /// <summary>
    /// Marks one of this window's tabs as opened, the first time it opens a source, and attaches it to the window.
    /// </summary>
    public void HookUp(TabViewModel tab)
    {
        tab.IsInitialized = true;
        Attach(tab);
    }

    private void Attach(TabViewModel tab)
    {
        if (_hookups is null || !tab.IsInitialized || tab.Hookups is not null)
        {
            return;
        }
        tab.Hookups = _hookups.Attach(tab);
    }

    private static void Detach(TabViewModel tab)
    {
        tab.Hookups?.Dispose();
        tab.Hookups = null;
    }

    /// <summary>
    /// Gives the active tab, when it first opens a source, its file list from <paramref name="files"/> and starts
    /// loading its image. Needs the shared navigation set up first.
    /// </summary>
    public void InitializeActiveTab(IReadOnlyList<FileInfo> files)
    {
        if (SharedCache is not { } cache || SharedThumbnailCache is not { } thumbnails ||
            SharedThumbnailLoader is not { } thumbnailLoader)
        {
            return;
        }

        var tab = ActiveTab.Value;
        tab.InitializeImageIterator(files, cache, thumbnails, thumbnailLoader, SharedFileWatcher, thumbnails);
        var currentIndex = tab.ImageIterator.CurrentIndex;
        if (currentIndex >= 0 && currentIndex < files.Count)
        {
            cache.Preload(tab.Id, files[currentIndex], false, tab.GetTabCancellation().Token);
        }
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
        var tab = new TabViewModel(_parentVm, SharedFileWatcher);
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
        SelectTab(tab);
        return tab;
    }
    
    /// <summary>
    /// Moves one of this window's tabs to the Main Window of <paramref name="target"/>, where it is selected and
    /// attached. The last tab stays.
    /// </summary>
    /// <param name="index">Where the tab goes among the target's tabs, or after them when null.</param>
    /// <returns>Whether the tab moved.</returns>
    public bool MoveTab(TabViewModel tab, TabOverviewViewModel target, int? index = null)
    {
        if (ReferenceEquals(target, this) || !MoveOut(tab))
        {
            return false;
        }

        target.Adopt(tab, index);
        return true;
    }

    /// <summary>
    /// Moves one of this window's tabs to another place among them. The active tab stays active.
    /// </summary>
    public void Reorder(TabViewModel tab, int index)
    {
        var from = Tabs.Value.IndexOf(tab);
        if (from < 0)
        {
            return;
        }

        var to = Math.Clamp(index, 0, Tabs.Value.Count - 1);
        if (from == to)
        {
            return;
        }

        var active = ActiveTab.Value;
        Tabs.Value.RemoveAt(from);
        Tabs.Value.Insert(to, tab);
        SelectTab(active);
    }

    /// <summary>
    /// Takes one of this window's tabs out, for a new Main Window that opens with it. The last tab stays.
    /// </summary>
    /// <returns>Whether the tab was taken out.</returns>
    public bool MoveOut(TabViewModel tab)
    {
        var index = Tabs.Value.IndexOf(tab);
        if (index < 0 || Tabs.Value.Count <= 1)
        {
            return false;
        }

        Tabs.Value.RemoveAt(index);
        Detach(tab);
        if (ReferenceEquals(tab, ActiveTab.Value) && Tabs.Value.Count > 0)
        {
            // Target the previous tab
            var targetIndex = Math.Max(0, index - 1);
            SelectTab(Tabs.Value[Math.Clamp(targetIndex, 0, Tabs.Value.Count - 1)]);
        }

        return true;
    }

    /// <summary>
    /// Makes a tab moved from another Main Window one of this window's tabs at <paramref name="index"/>, or after them,
    /// owned by and attached to this window, and selects it.
    /// </summary>
    private void Adopt(TabViewModel tab, int? index)
    {
        Tabs.Value.Insert(Math.Clamp(index ?? Tabs.Value.Count, 0, Tabs.Value.Count), tab);
        tab.ParentWindowContext = _parentVm;
        Attach(tab);
        SelectTab(tab);
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


    /// <summary>
    /// Closes one of this window's tabs and disposes it. The last tab, or a tab of another window, stays open.
    /// </summary>
    public void CloseTab(TabViewModel tab)
    {
        // 1. Guard Clause: Prevent closing if this is the last tab.
        if (Tabs.Value.Count <= 1)
        {
            return;
        }

        // 2. Capture the index BEFORE removing the item
        var indexToRemove = Tabs.Value.IndexOf(tab);
        if (indexToRemove < 0)
        {
            return;
        }

        var wasActive = ReferenceEquals(tab, ActiveTab.Value);
        Tabs.Value.RemoveAt(indexToRemove);

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

        Release(tab);
    }

    /// <summary>
    /// Releases every tab when this window's Main Window closes: their cached images, thumbnails, watched folders and
    /// extracted archives. Runs once; the tabs stay in <see cref="Tabs"/>, since the closing window still reads its
    /// active tab.
    /// </summary>
    public void TearDown()
    {
        if (_isTornDown)
        {
            return;
        }
        _isTornDown = true;

        foreach (var tab in Tabs.Value)
        {
            Release(tab);
        }
    }

    private void Release(TabViewModel tab)
    {
        Detach(tab);
        SharedCache?.ReleaseOwner(tab.Id);
        // Also removes the tab as a thumbnail owner and stops watching its folder
        tab.Dispose();
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


    /// <inheritdoc cref="INavigationService.LoadFromStringAsync"/>
    public async ValueTask<LoadResult> LoadFromStringAsync(string source, TabViewModel? senderTab = null, string? rootDirectory = null)
    {
        if (SharedNavigation is null)
        {
            DebugHelper.LogDebug(nameof(TabOverviewViewModel), nameof(LoadFromStringAsync), 
                $"{nameof(SharedNavigation)} is null, make sure to initialize it before use");
            return LoadResult.Failed();
        }
        var tab = senderTab ?? ActiveTab.Value;
        var ct = tab.GetTabCancellation();
        
        var result = await SharedNavigation.LoadFromStringAsync(source, tab, ct, rootDirectory)
            .ConfigureAwait(false);
        if (result.IsLoaded)
        {
            ActiveTab.Value = tab;
        }
        return result;
    }
    
    public async ValueTask<LoadResult> LoadFromUrlAsync(string source, TabViewModel? senderTab = null)
    {
        if (SharedNavigation is null)
        {
            return LoadResult.Failed();
        }
        var tab = senderTab ?? ActiveTab.Value;
        var ct = tab.GetTabCancellation();

        var result = await SharedNavigation.LoadFromUrlAsync(source, tab, ct).ConfigureAwait(false);
        if (result.IsLoaded)
        {
            ActiveTab.Value = tab;
        }
        return result;
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
    }
    
    public async ValueTask LoadFromFileAsync(string file, TabViewModel? senderTab = null) => 
        await LoadFromFileAsync(new FileInfo(file), senderTab).ConfigureAwait(false);

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
        if (SharedCache is null || fileInfo is null)
        {
            return null;
        }

        if (SharedCache.TryGetReady(fileInfo, out var ready) && ready.Image is not null)
        {
            return ready.Image;
        }

        var tab = ActiveTab.CurrentValue;
        var request = SharedCache.GetAsync(tab.Id, fileInfo, tab.GetTabCancellation().Token);
        // A pending ValueTask cannot be blocked on directly
        return (request.IsCompleted ? request.Result : request.AsTask().Result)?.Image;
    }

    

    #endregion


}
