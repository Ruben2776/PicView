using System.Collections.ObjectModel;
using System.Diagnostics;
using PicView.Core.FileSorting;
using PicView.Core.IPlatform;
using PicView.Core.Navigation;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.Thumbnailing;
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
    
    // The services every Main Window shares, composed once at App Start
    private CoreViewModel? Core => _parentVm?.Core;
    public INavigationService? SharedNavigation => Core?.SharedNavigationService;
    public SharedImageCache? SharedCache => Core?.Services?.Cache;
    public Thumbnails? SharedThumbnails => Core?.Services?.Thumbnails;

    private readonly ReactiveProperty<bool> _isTabStripShown = new(false);

    /// <summary>
    /// Whether the window shows its tab strip, which it does with two or more tabs. The strip is part of the Reserved
    /// Space, so the window is fitted again when this changes.
    /// </summary>
    public ReadOnlyReactiveProperty<bool> IsTabStripShown => _isTabStripShown;

    // The Main Window these tabs belong to
    private readonly MainWindowViewModel _parentVm;
    private ITabHookups? _hookups;
    private ITabViewFactory? _viewFactory;
    private bool _isTornDown;

    /// <param name="parentVm">The Main Window these tabs belong to.</param>
    /// <param name="firstTab">A tab moved out of another Main Window, for a window that opens with it; otherwise the
    /// window opens with a new tab.</param>
    public TabOverviewViewModel(MainWindowViewModel parentVm, TabViewModel? firstTab = null)
    {
        _parentVm = parentVm;
        // Made before the window's tabs, which are made with it
        _fileOrder = parentVm?.WindowSettings.FileOrder ??
                     new BindableReactiveProperty<FileOrder>(FileOrder.FromAppSettings(Settings));
        IsSortedByName = IsSortedBy(SortFilesBy.Name);
        IsSortedByFileSize = IsSortedBy(SortFilesBy.FileSize);
        IsSortedByCreationTime = IsSortedBy(SortFilesBy.CreationTime);
        IsSortedByExtension = IsSortedBy(SortFilesBy.Extension);
        IsSortedByLastAccessTime = IsSortedBy(SortFilesBy.LastAccessTime);
        IsSortedByLastWriteTime = IsSortedBy(SortFilesBy.LastWriteTime);
        IsSortedByRandomization = IsSortedBy(SortFilesBy.Random);
        IsAscending = _fileOrder.Select(static order => order.Ascending).ToReadOnlyBindableReactiveProperty();

        if (firstTab is null)
        {
            ActiveTab = new BindableReactiveProperty<TabViewModel>(CreateInitialTab());
        }
        else
        {
            Debug.Assert(parentVm != null);
            firstTab.ParentWindowContext = parentVm;
            Tabs.Value.Add(firstTab);
            // Published once the tab is in this window's tabs, so what follows its window finds it there. Its views and
            // hookups attach later, when the window gives them
            firstTab.FollowWindow();
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

    /// <summary>
    /// Gives this window how it makes each tab's views, and attaches every tab already in it, such as a tab it opened
    /// with after a move. Called once, when the window is created.
    /// </summary>
    public void SetViewFactory(ITabViewFactory factory)
    {
        _viewFactory = factory;
        foreach (var tab in Tabs.Value)
        {
            AttachViews(tab);
        }
    }

    private void AttachViews(TabViewModel tab)
    {
        if (_viewFactory is null || tab.Views is not null)
        {
            return;
        }
        tab.SetViews(_viewFactory.Attach(tab));
    }

    private static void Detach(TabViewModel tab)
    {
        tab.Hookups?.Dispose();
        tab.Hookups = null;
        tab.Views?.Dispose();
        tab.SetViews(null);
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
    
    private TabViewModel CreateTabInternal() => InsertTab(Tabs.Value.Count);

    /// <summary>
    /// Creates a tab at <paramref name="index"/>, or after the others when it is past them, without selecting it. Its
    /// Load makes its iterator when it first opens a source.
    /// </summary>
    public TabViewModel InsertTab(int index)
    {
        var tab = new TabViewModel(_parentVm, Core?.Services);
        Tabs.Value.Insert(Math.Clamp(index, 0, Tabs.Value.Count), tab);
        AttachViews(tab);
        return tab;
    }

    /// <summary>
    /// Creates a tab and selects it. Its Load makes its iterator when it first opens a source.
    /// </summary>
    public TabViewModel CreateTab()
    {
        var tab = CreateTabInternal();
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
        if (!ReferenceEquals(tab, ActiveTab.Value) || Tabs.Value.Count <= 0)
        {
            return true;
        }

        // Target the previous tab
        var targetIndex = Math.Max(0, index - 1);
        SelectTab(Tabs.Value[Math.Clamp(targetIndex, 0, Tabs.Value.Count - 1)]);

        return true;
    }

    /// <summary>
    /// Makes a tab moved from another Main Window one of this window's tabs at <paramref name="index"/>, or after them,
    /// owned by and attached to this window, and selects it.
    /// </summary>
    private void Adopt(TabViewModel tab, int? index)
    {
        tab.ParentWindowContext = _parentVm;
        Tabs.Value.Insert(Math.Clamp(index ?? Tabs.Value.Count, 0, Tabs.Value.Count), tab);
        AttachViews(tab);
        Attach(tab);
        tab.FollowWindow();
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
    /// Closes every tab of this window but <paramref name="tab"/>, which becomes the active tab if the active tab closes.
    /// </summary>
    public void CloseOtherTabs(TabViewModel tab) =>
        CloseTabsAround(tab, static (index, keep) => index != keep);

    /// <summary>
    /// Closes the tabs to the left of <paramref name="tab"/>, which becomes the active tab if the active tab closes.
    /// </summary>
    public void CloseTabsToTheLeft(TabViewModel tab) =>
        CloseTabsAround(tab, static (index, keep) => index < keep);

    /// <summary>
    /// Closes the tabs to the right of <paramref name="tab"/>, which becomes the active tab if the active tab closes.
    /// </summary>
    public void CloseTabsToTheRight(TabViewModel tab) =>
        CloseTabsAround(tab, static (index, keep) => index > keep);

    private void CloseTabsAround(TabViewModel keep, Func<int, int, bool> closes)
    {
        var keepIndex = Tabs.Value.IndexOf(keep);
        if (keepIndex < 0)
        {
            return;
        }

        var closing = new List<TabViewModel>(Tabs.Value.Count);
        for (var i = 0; i < Tabs.Value.Count; i++)
        {
            if (closes(i, keepIndex))
            {
                closing.Add(Tabs.Value[i]);
            }
        }

        if (closing.Count == 0)
        {
            return;
        }

        // Select the kept tab first, so closing the active tab does not select tabs that close next
        if (closing.Contains(ActiveTab.Value))
        {
            SelectTab(keep);
        }

        foreach (var tab in closing)
        {
            tab.CloseTab();
        }
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
        await tab.ImageIterator.NavigateAsync(navigateTo, SkipAmount.One, ct).ConfigureAwait(false);
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

    public async ValueTask ReloadAsync(TabViewModel? senderTab = null)
    {
        var tab = senderTab ?? ActiveTab.Value;
        // A tab that never browsed files, such as a New Tab, has nothing to reload
        if (tab.ImageIterator is not { } iterator)
        {
            return;
        }

        await iterator.ReloadAsync().ConfigureAwait(false);
    }
    #endregion

    #region Sort

    // The window's File Order, or this overview's own, seeded from the record, for an overview without a window
    private readonly BindableReactiveProperty<FileOrder> _fileOrder;

    // Using converters fails on macOS NativeMenu, so each check mark is a bool of its own, derived from the order
    public IReadOnlyBindableReactiveProperty<bool> IsSortedByName { get; }
    public IReadOnlyBindableReactiveProperty<bool> IsSortedByFileSize { get; }
    public IReadOnlyBindableReactiveProperty<bool> IsSortedByCreationTime { get; }
    public IReadOnlyBindableReactiveProperty<bool> IsSortedByExtension { get; }
    public IReadOnlyBindableReactiveProperty<bool> IsSortedByLastAccessTime { get; }
    public IReadOnlyBindableReactiveProperty<bool> IsSortedByLastWriteTime { get; }
    public IReadOnlyBindableReactiveProperty<bool> IsSortedByRandomization { get; }
    public IReadOnlyBindableReactiveProperty<bool> IsAscending { get; }

    private IReadOnlyBindableReactiveProperty<bool> IsSortedBy(SortFilesBy by) =>
        _fileOrder.Select(order => order.By == by).ToReadOnlyBindableReactiveProperty();

    /// <summary>
    /// Sorts this window's files by <paramref name="sortOrder"/>: sets the window's File Order, which every tab of this
    /// window follows, and no tab of another. Asked for Random again, the files are shuffled again.
    /// </summary>
    /// <returns>A task that ends once this window's tabs have sorted their files again.</returns>
    public ValueTask SortAsync(SortFilesBy sortOrder)
    {
        var order = _fileOrder.Value with { By = sortOrder };
        return SortAsync(order, reshuffle: order == _fileOrder.Value && sortOrder is SortFilesBy.Random);
    }

    /// <summary>
    /// Sorts this window's files ascending or descending: sets the window's File Order, as
    /// <see cref="SortAsync(SortFilesBy)"/> does.
    /// </summary>
    /// <returns>A task that ends once this window's tabs have sorted their files again.</returns>
    public ValueTask SortAsync(bool ascending) =>
        SortAsync(_fileOrder.Value with { Ascending = ascending }, reshuffle: false);

    private async ValueTask SortAsync(FileOrder order, bool reshuffle)
    {
        // Each tab that follows the order starts sorting as it is set, so asking it again joins that run
        _fileOrder.Value = order;
        foreach (var tab in Tabs.Value.ToArray())
        {
            await tab.ResortAsync(reshuffle).ConfigureAwait(false);
        }
    }

    #endregion

    #region Retrieval

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
