using System.Collections.Concurrent;
using System.Collections.Specialized;
using ObservableCollections;
using PicView.Avalonia.StartUp;
using PicView.Core.FileSorting;
using PicView.Core.Gallery;
using PicView.Core.IPlatform;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.Titles;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.Navigation;

/// <summary>
/// Pins which Main Window a tab belongs to, observed through each window's <see cref="TabOverviewViewModel"/>.
/// </summary>
[Collection("Sequential")]
public class TabOwnershipTests : IDisposable
{
    private readonly CoreViewModel _core;

    // The files the shared cache decoded, shown or ahead of the shown one
    private readonly ConcurrentQueue<string> _decoded = new();

    // Decodes a test holds, by file name, until it gives their models
    private readonly ConcurrentDictionary<string, TaskCompletionSource<ImageModel>> _heldDecodes = new();
    private readonly ConcurrentDictionary<string, TaskCompletionSource> _decodesRequested = new();

    // A folder of the test's own, for the tests whose tabs browse files
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "PicViewTabOwnershipTests_" + Guid.NewGuid());

    // The files CreateFiles writes, by name and by size
    private static readonly string[] ByName = ["a.jpg", "b.jpg", "c.jpg", "d.jpg"];
    private static readonly string[] BySize = ["b.jpg", "d.jpg", "c.jpg", "a.jpg"];

    public TabOwnershipTests()
    {
        SetDefaults();
        TranslationManager.Init();
        _core = new CoreViewModel(null, file =>
        {
            _decoded.Enqueue(file.Name);
            if (_heldDecodes.TryGetValue(file.Name, out var held))
            {
                DecodeRequestedOf(file.Name).TrySetResult();
                return held.Task.GetAwaiter().GetResult();
            }

            return new ImageModel { FileInfo = file };
        });
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, true);
        }
    }

    [Fact]
    public void MoveTab_ToAnotherWindow_IsOwnedAndSelectedThereAndTheNeighbourHere()
    {
        var source = CreateWindow();
        var target = CreateWindow();
        var remaining = source.WindowTabs.ActiveTab.Value;
        var tab = source.WindowTabs.CreateTab();

        var moved = source.WindowTabs.MoveTab(tab, target.WindowTabs);

        Assert.True(moved);
        Assert.Equal([remaining], source.WindowTabs.Tabs.Value);
        Assert.Same(remaining, source.WindowTabs.ActiveTab.Value);
        Assert.Contains(tab, target.WindowTabs.Tabs.Value);
        Assert.Same(tab, target.WindowTabs.ActiveTab.Value);
        Assert.Same(target, tab.ParentWindowContext);
    }

    [Fact]
    public void MoveTab_ToAnIndex_InsertsItThere()
    {
        var source = CreateWindow();
        var target = CreateWindow();
        var first = target.WindowTabs.ActiveTab.Value;
        var second = target.WindowTabs.CreateTab();
        var tab = source.WindowTabs.CreateTab();

        source.WindowTabs.MoveTab(tab, target.WindowTabs, index: 1);

        Assert.Equal([first, tab, second], target.WindowTabs.Tabs.Value);
        Assert.Same(tab, target.WindowTabs.ActiveTab.Value);
    }

    [Fact]
    public void MoveOut_TabOfAnotherWindow_LeavesItThere()
    {
        var source = CreateWindow();
        var other = CreateWindow();
        var tab = source.WindowTabs.CreateTab();
        other.WindowTabs.CreateTab();

        var movedOut = other.WindowTabs.MoveOut(tab);

        Assert.False(movedOut);
        Assert.Contains(tab, source.WindowTabs.Tabs.Value);
        Assert.Equal(2, other.WindowTabs.Tabs.Value.Count);
    }

    [Fact]
    public void Reorder_TabToAnotherPlace_MovesItAndKeepsTheActiveTab()
    {
        var window = CreateWindow();
        var first = window.WindowTabs.ActiveTab.Value;
        var second = window.WindowTabs.CreateTab();
        var third = window.WindowTabs.CreateTab();
        window.WindowTabs.SelectTab(second);

        window.WindowTabs.Reorder(first, 2);

        Assert.Equal([second, third, first], window.WindowTabs.Tabs.Value);
        Assert.Same(second, window.WindowTabs.ActiveTab.Value);
    }

    [Fact]
    public void Reorder_PastTheLastTab_MovesItToTheEnd()
    {
        var window = CreateWindow();
        var first = window.WindowTabs.ActiveTab.Value;
        var second = window.WindowTabs.CreateTab();

        window.WindowTabs.Reorder(first, 5);

        Assert.Equal([second, first], window.WindowTabs.Tabs.Value);
    }

    [Fact]
    public void MoveTab_LastTab_StaysWhereItIs()
    {
        var source = CreateWindow();
        var target = CreateWindow();
        var tab = source.WindowTabs.ActiveTab.Value;

        var moved = source.WindowTabs.MoveTab(tab, target.WindowTabs);

        Assert.False(moved);
        Assert.Equal([tab], source.WindowTabs.Tabs.Value);
        Assert.Same(source, tab.ParentWindowContext);
    }

    [Fact]
    public void MoveTab_OpenedTab_IsAttachedThereAndDetachedHere()
    {
        var sourceHookups = new RecordingHookups();
        var targetHookups = new RecordingHookups();
        var source = CreateWindow(sourceHookups);
        var target = CreateWindow(targetHookups);
        var tab = source.WindowTabs.CreateTab();
        source.WindowTabs.HookUp(tab);

        source.WindowTabs.MoveTab(tab, target.WindowTabs);

        Assert.Equal([tab], sourceHookups.Detached);
        Assert.Equal([tab], targetHookups.Attached);
    }

    [Fact]
    public void MoveTab_FreshTab_StaysUnattached()
    {
        var targetHookups = new RecordingHookups();
        var source = CreateWindow(new RecordingHookups());
        var target = CreateWindow(targetHookups);
        var tab = source.WindowTabs.CreateTab();

        source.WindowTabs.MoveTab(tab, target.WindowTabs);

        Assert.Empty(targetHookups.Attached);
    }

    [Fact]
    public void CurrentWindowSettings_NewTab_AreItsWindows()
    {
        var window = CreateWindow();
        var tab = window.WindowTabs.CreateTab();
        Assert.Same(window.WindowSettings, tab.CurrentWindowSettings.CurrentValue);
    }

    [Fact]
    public void CurrentWindowSettings_TabMovedToAnotherWindow_BecomeThatWindows()
    {
        var source = CreateWindow();
        var target = CreateWindow();
        var tab = source.WindowTabs.CreateTab();
        var seen = new List<WindowSettings?>();
        using var _ = tab.CurrentWindowSettings.Subscribe(seen.Add);

        source.WindowTabs.MoveTab(tab, target.WindowTabs);

        Assert.Equal([source.WindowSettings, target.WindowSettings], seen);
    }

    [Fact]
    public void CurrentWindowSettings_TabMovedOutIntoANewWindow_BecomeThatWindows()
    {
        var source = CreateWindow();
        var tab = source.WindowTabs.CreateTab();
        source.WindowTabs.MoveOut(tab);
        Assert.Same(source.WindowSettings, tab.CurrentWindowSettings.CurrentValue);

        var window = CreateWindow(firstTab: tab);

        Assert.Same(window.WindowSettings, tab.CurrentWindowSettings.CurrentValue);
    }

    [Fact]
    public void MoveTab_PublishesTheNewWindowsSettingsAfterTheTabIsAttachedThere()
    {
        var targetHookups = new RecordingHookups();
        var source = CreateWindow();
        var target = CreateWindow(targetHookups);
        var tab = source.WindowTabs.CreateTab();
        source.WindowTabs.HookUp(tab);
        var inTabs = false;
        var attached = false;
        using var _ = tab.CurrentWindowSettings.Subscribe(settings =>
        {
            if (!ReferenceEquals(settings, target.WindowSettings))
            {
                return;
            }
            inTabs = target.WindowTabs.Tabs.Value.Contains(tab);
            attached = targetHookups.Attached.Contains(tab);
        });

        source.WindowTabs.MoveTab(tab, target.WindowTabs);

        Assert.True(inTabs);
        Assert.True(attached);
    }

    [Fact]
    public void CurrentWindowSettings_TabWithoutAWindow_AreNull()
    {
        Assert.Null(new TabViewModel(null!).CurrentWindowSettings.CurrentValue);
    }

    [Fact]
    public void NewWindow_OpenedWithAMovedTab_HasOnlyThatTabAndAttachesIt()
    {
        var source = CreateWindow(new RecordingHookups());
        var tab = source.WindowTabs.CreateTab();
        source.WindowTabs.HookUp(tab);
        var hookups = new RecordingHookups();

        source.WindowTabs.MoveOut(tab);
        var window = CreateWindow(hookups, firstTab: tab);

        Assert.Equal([tab], window.WindowTabs.Tabs.Value);
        Assert.Same(tab, window.WindowTabs.ActiveTab.Value);
        Assert.Same(window, tab.ParentWindowContext);
        Assert.Equal([tab], hookups.Attached);
    }

    [Fact]
    public void IsTabStripShown_FollowsTheTabCount()
    {
        var source = CreateWindow();
        var target = CreateWindow();
        var withOne = source.WindowTabs.IsTabStripShown.CurrentValue;
        var tab = source.WindowTabs.CreateTab();
        var withTwo = source.WindowTabs.IsTabStripShown.CurrentValue;

        source.WindowTabs.MoveTab(tab, target.WindowTabs);

        Assert.False(withOne);
        Assert.True(withTwo);
        Assert.False(source.WindowTabs.IsTabStripShown.CurrentValue);
        Assert.True(target.WindowTabs.IsTabStripShown.CurrentValue);

        tab.CloseTab();
        Assert.False(target.WindowTabs.IsTabStripShown.CurrentValue);
    }

    [Fact]
    public void HookUp_CalledTwice_AttachesTheTabOnce()
    {
        var hookups = new RecordingHookups();
        var window = CreateWindow(hookups);
        var tab = window.WindowTabs.ActiveTab.Value;

        window.WindowTabs.HookUp(tab);
        window.WindowTabs.HookUp(tab);

        Assert.True(tab.IsInitialized);
        Assert.Equal([tab], hookups.Attached);
    }

    [Fact]
    public void CloseTab_AfterMovingToAnotherWindow_ClosesItInThatWindow()
    {
        var source = CreateWindow();
        var target = CreateWindow();
        var sourceTab = source.WindowTabs.ActiveTab.Value;
        var targetTab = target.WindowTabs.ActiveTab.Value;
        var tab = source.WindowTabs.CreateTab();

        source.WindowTabs.MoveTab(tab, target.WindowTabs);
        tab.CloseTab();

        Assert.Equal([targetTab], target.WindowTabs.Tabs.Value);
        Assert.Same(targetTab, target.WindowTabs.ActiveTab.Value);
        Assert.Equal([sourceTab], source.WindowTabs.Tabs.Value);
    }

    [Fact]
    public void CloseTab_OpenedTab_DetachesIt()
    {
        var hookups = new RecordingHookups();
        var window = CreateWindow(hookups);
        var tab = window.WindowTabs.CreateTab();
        window.WindowTabs.HookUp(tab);

        window.WindowTabs.CloseTab(tab);

        Assert.Equal([tab], hookups.Detached);
    }

    [Fact]
    public void CloseTab_TabOfAnotherWindow_LeavesItOpen()
    {
        var source = CreateWindow();
        var other = CreateWindow();
        var tab = source.WindowTabs.CreateTab();
        other.WindowTabs.CreateTab();

        other.WindowTabs.CloseTab(tab);

        Assert.Contains(tab, source.WindowTabs.Tabs.Value);
        Assert.Equal(2, other.WindowTabs.Tabs.Value.Count);
    }

    [Fact]
    public void CloseTab_LastTab_StaysOpen()
    {
        var window = CreateWindow();
        var tab = window.WindowTabs.ActiveTab.Value;

        window.WindowTabs.CloseTab(tab);

        Assert.Equal([tab], window.WindowTabs.Tabs.Value);
    }

    [Fact]
    public async Task TearDown_CalledTwice_ReleasesEachTabOnce()
    {
        var watcher = new RecordingFileWatcher();
        // Gives no thumbnails of its own, so a thumbnail it returns is one a tab keeps
        var thumbnails = TestTabServices.NoThumbnails(_core.SharedCache);
        _core.InitializeNavigation(
            TestTabServices.For(_core.SharedCache, thumbnails, watcher),
            navigation: null!);
        var tabs = CreateWindow().WindowTabs;
        tabs.ActiveTab.Value.Initialize();
        var first = tabs.ActiveTab.Value;
        var second = tabs.CreateTab();
        thumbnails.Keep(first.Id, new FileInfo("first.jpg").FullName, new object(), 100);
        thumbnails.Keep(second.Id, new FileInfo("second.jpg").FullName, new object(), 100);
        Assert.NotNull(await thumbnails.GetQuickAsync(new FileInfo("first.jpg"), 100));

        tabs.TearDown();
        tabs.TearDown();

        Assert.Equal([first, second], watcher.Unwatched);
        Assert.Null(await thumbnails.GetQuickAsync(new FileInfo("first.jpg"), 100));
        Assert.Null(await thumbnails.GetQuickAsync(new FileInfo("second.jpg"), 100));
    }

    [Fact]
    public void SharedServices_TwoMainWindows_AreTheSameInstances()
    {
        var watcher = new RecordingFileWatcher();
        _core.InitializeNavigation(
            TestTabServices.For(_core.SharedCache, null, watcher),
            navigation: null!);

        var first = CreateWindow().WindowTabs;
        var second = CreateWindow().WindowTabs;

        Assert.Same(watcher, first.ActiveTab.Value.Services?.Watcher);
        Assert.Same(first.ActiveTab.Value.Services, second.ActiveTab.Value.Services);
        Assert.Same(first.SharedCache, second.SharedCache);
        Assert.Same(first.SharedThumbnails, second.SharedThumbnails);
    }

    [Fact]
    public void SetViewFactory_AttachesTheInitialTab()
    {
        var factory = new RecordingViewFactory();
        var window = CreateWindow(viewFactory: factory);

        Assert.Equal([window.WindowTabs.ActiveTab.Value], factory.Attached);
        Assert.NotNull(window.WindowTabs.ActiveTab.Value.Views);
    }

    [Fact]
    public void CreateTab_AttachesTheNewTab()
    {
        var factory = new RecordingViewFactory();
        var window = CreateWindow(viewFactory: factory);

        var tab = window.WindowTabs.CreateTab();

        Assert.Contains(tab, factory.Attached);
        Assert.NotNull(tab.Views);
    }

    [Fact]
    public void MoveTab_ReleasesItHereAndAttachesItThere()
    {
        var here = new RecordingViewFactory();
        var there = new RecordingViewFactory();
        var source = CreateWindow(viewFactory: here);
        var target = CreateWindow(viewFactory: there);
        var tab = source.WindowTabs.CreateTab();
        var views = (FakeViews)tab.Views!;

        source.WindowTabs.MoveTab(tab, target.WindowTabs);

        Assert.True(views.IsDisposed);
        Assert.Contains(tab, there.Attached);
        Assert.NotSame(views, tab.Views);
    }

    [Fact]
    public void CloseTab_ReleasesIt()
    {
        var factory = new RecordingViewFactory();
        var window = CreateWindow(viewFactory: factory);
        var tab = window.WindowTabs.CreateTab();
        var views = (FakeViews)tab.Views!;

        window.WindowTabs.CloseTab(tab);

        Assert.True(views.IsDisposed);
        Assert.Null(tab.Views);
    }

    [Fact]
    public void CurrentView_FollowsTheAttachedViewsAndIsNullAfterRelease()
    {
        var factory = new RecordingViewFactory();
        var window = CreateWindow(viewFactory: factory);
        var tab = window.WindowTabs.CreateTab();
        var views = (FakeViews)tab.Views!;

        views.Control.Value = "a control";
        Assert.Equal("a control", tab.CurrentView.Value);

        window.WindowTabs.CloseTab(tab);
        Assert.Null(tab.CurrentView.Value);
    }

    [Fact]
    public void SetViewFactory_WindowOpenedWithAMovedTab_AttachesIt()
    {
        var source = CreateWindow();
        var tab = source.WindowTabs.CreateTab();
        source.WindowTabs.MoveOut(tab);
        var factory = new RecordingViewFactory();

        CreateWindow(viewFactory: factory, firstTab: tab);

        Assert.Equal([tab], factory.Attached);
    }

    [Fact]
    public async Task Sort_InOneWindow_ResortsEveryTabOfThatWindow_AndNoTabOfTheOther()
    {
        ComposeServices();
        CreateFiles();
        var sorted = CreateWindow();
        var other = CreateWindow();
        var first = sorted.WindowTabs.ActiveTab.Value;
        var second = sorted.WindowTabs.CreateTab();
        var otherTab = other.WindowTabs.ActiveTab.Value;
        await OpenAsync(first, "a.jpg");
        await OpenAsync(second, "c.jpg");
        await OpenAsync(otherTab, "a.jpg");

        await sorted.WindowTabs.SortAsync(SortFilesBy.FileSize);

        Assert.Equal(BySize, NamesOf(first));
        Assert.Equal(BySize, NamesOf(second));
        Assert.Equal(ByName, NamesOf(otherTab));
        Assert.Equal(new FileOrder(SortFilesBy.Name, true), other.WindowSettings.FileOrder.Value);
    }

    [Fact]
    public async Task Sort_KeepsEachTabsShownFile()
    {
        ComposeServices();
        CreateFiles();
        var window = CreateWindow();
        var first = window.WindowTabs.ActiveTab.Value;
        var second = window.WindowTabs.CreateTab();
        await OpenAsync(first, "a.jpg");
        await OpenAsync(second, "c.jpg");

        await window.WindowTabs.SortAsync(SortFilesBy.FileSize);

        Assert.Equal(BySize, NamesOf(first));
        Assert.Equal("a.jpg", first.FileInfo.CurrentValue?.Name);
        Assert.Equal("a.jpg", ListedAtShownIndex(first));
        Assert.Equal(3, first.NavigationIndex.CurrentValue);
        Assert.Equal(BySize, NamesOf(second));
        Assert.Equal("c.jpg", second.FileInfo.CurrentValue?.Name);
        Assert.Equal("c.jpg", ListedAtShownIndex(second));
        Assert.Equal(2, second.NavigationIndex.CurrentValue);
    }

    [Fact]
    public async Task Sort_FreshTabsDoNothing_ASingleImageTabsListResortsAndTheImageStays_AndTheNextLoadUsesTheWindowsOrder()
    {
        ComposeServices();
        CreateFiles();
        var window = CreateWindow();
        var fresh = window.WindowTabs.ActiveTab.Value;
        var withoutFiles = window.WindowTabs.CreateTab();
        withoutFiles.Initialize();
        var single = window.WindowTabs.CreateTab();
        await OpenAsync(single, "a.jpg");
        // The active tab, so a sort that listed the active tab's shown folder again would list around the image
        var image = new ImageModel { FileInfo = new FileInfo(PathOf("c.jpg")) };
        single.DisplaySingleImage(image, SingleImageType.Clipboard);

        await window.WindowTabs.SortAsync(SortFilesBy.FileSize);

        Assert.Null(fresh.ImageIterator);
        Assert.Empty(withoutFiles.ImageIterator!.Files);
        // A Single Image tab's files are sorted in memory, and the Single Image stays shown
        Assert.Equal(BySize, NamesOf(single));
        Assert.Equal(SingleImageType.Clipboard, single.SingleImageType);
        Assert.Same(image, single.Model);

        await OpenAsync(fresh, "a.jpg");
        await OpenAsync(withoutFiles, "a.jpg");
        await OpenAsync(single, "a.jpg");

        Assert.Equal(BySize, NamesOf(fresh));
        Assert.Equal(BySize, NamesOf(withoutFiles));
        Assert.Equal(BySize, NamesOf(single));
    }

    [Fact]
    public async Task Sort_ChangedAgainWhileTheTabsResort_EndsInTheLatestOrder()
    {
        using var platform = new GatedPlatform();
        ComposeServices(platform);
        CreateFiles();
        var window = CreateWindow();
        var first = window.WindowTabs.ActiveTab.Value;
        var second = window.WindowTabs.CreateTab();
        await OpenAsync(first, "a.jpg");
        await OpenAsync(second, "c.jpg");

        // A sort by name compares names, so the first re-sort holds at the gate while the order changes again
        platform.Close();
        var descending = Task.Run(() => window.WindowTabs.SortAsync(ascending: false).AsTask());
        await platform.Comparing.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);
        var bySize = window.WindowTabs.SortAsync(SortFilesBy.FileSize).AsTask();
        platform.Open();
        await Task.WhenAll(descending, bySize);

        Assert.Equal(BySize.Reverse(), NamesOf(first));
        Assert.Equal(BySize.Reverse(), NamesOf(second));
        Assert.Equal("a.jpg", ListedAtShownIndex(first));
        Assert.Equal("c.jpg", ListedAtShownIndex(second));
    }

    [Fact]
    public async Task Sort_SideBySide_ShowsThePairAtTheShownFilesNewPlace()
    {
        ComposeServices();
        CreateFiles();
        var window = CreateWindow();
        window.WindowSettings.IsSideBySide.Value = true;
        var tab = window.WindowTabs.ActiveTab.Value;
        await OpenAsync(tab, "b.jpg");
        Assert.Equal("c.jpg", tab.SecondaryFileInfo.CurrentValue?.Name);

        await window.WindowTabs.SortAsync(SortFilesBy.FileSize);

        // b is first by size, beside d
        Assert.Equal("b.jpg", tab.FileInfo.CurrentValue?.Name);
        Assert.Equal("d.jpg", tab.SecondaryFileInfo.CurrentValue?.Name);
        Assert.Equal("d.jpg", tab.SecondaryModel?.FileInfo?.Name);
        Assert.Equal(TitlePhase.None, tab.Titles.Phase);
    }

    [Fact]
    public async Task Sort_WithoutSideBySide_ShowsNothingAgain()
    {
        ComposeServices();
        CreateFiles();
        var window = CreateWindow();
        var tab = window.WindowTabs.ActiveTab.Value;
        await OpenAsync(tab, "b.jpg");
        var shown = tab.Model;
        var shownChanges = new List<ShownChange>();
        using var _ = tab.Shown.Subscribe(shownChanges.Add);

        await window.WindowTabs.SortAsync(SortFilesBy.FileSize);

        Assert.Same(shown, tab.Model);
        Assert.Empty(shownChanges);
        Assert.Equal(TitlePhase.None, tab.Titles.Phase);
    }

    [Fact]
    public async Task Sort_PreloadsTheShownFilesNewNeighbours()
    {
        Settings.Navigation.PositiveIterations = 1;
        Settings.Navigation.NegativeIterations = 0;
        ComposeServices();
        // By name a is first and b after it; by size e comes after a
        CreateFiles(("a.jpg", 20), ("b.jpg", 10), ("c.jpg", 30), ("d.jpg", 40), ("e.jpg", 25));
        var window = CreateWindow();
        var tab = window.WindowTabs.ActiveTab.Value;
        await OpenAsync(tab, "a.jpg");
        await WaitUntilAsync(() => _decoded.Contains("b.jpg"));
        Assert.DoesNotContain("e.jpg", _decoded);

        await window.WindowTabs.SortAsync(SortFilesBy.FileSize);

        Assert.Equal(["b.jpg", "a.jpg", "e.jpg", "c.jpg", "d.jpg"], NamesOf(tab));
        await WaitUntilAsync(() => _decoded.Contains("e.jpg"));
    }

    [Fact]
    public async Task MoveTab_ToAWindowWithAnotherOrder_ResortsItOnce_AndKeepsItsShownFile()
    {
        ComposeServices();
        CreateFiles();
        var source = CreateWindow();
        var target = CreateWindow();
        await target.WindowTabs.SortAsync(SortFilesBy.FileSize);
        var tab = source.WindowTabs.CreateTab();
        var changes = new ConcurrentQueue<FileListChange>();
        tab.Initialize(changes.Enqueue);
        await OpenAsync(tab, "c.jpg");
        changes.Clear();

        source.WindowTabs.MoveTab(tab, target.WindowTabs);
        await tab.Resorting;

        Assert.Equal(BySize, NamesOf(tab));
        Assert.Equal(FileListChangeKind.Listed, Assert.Single(changes).Kind);
        Assert.Equal("c.jpg", tab.FileInfo.CurrentValue?.Name);
        Assert.Equal("c.jpg", ListedAtShownIndex(tab));
    }

    [Fact]
    public async Task MoveTab_ToAWindowWithTheSameOrder_ReportsNoListedChange()
    {
        ComposeServices();
        CreateFiles();
        var source = CreateWindow();
        var target = CreateWindow();
        var tab = source.WindowTabs.CreateTab();
        var changes = new ConcurrentQueue<FileListChange>();
        tab.Initialize(changes.Enqueue);
        await OpenAsync(tab, "c.jpg");
        var listed = tab.ImageIterator!.Files;
        changes.Clear();

        source.WindowTabs.MoveTab(tab, target.WindowTabs);
        await tab.Resorting;

        Assert.Empty(changes);
        Assert.Same(listed, tab.ImageIterator.Files);
    }

    [Fact]
    public async Task MoveOut_IntoANewWindow_TheTabFollowsThatWindowsOrder()
    {
        ComposeServices();
        CreateFiles();
        var source = CreateWindow();
        var tab = source.WindowTabs.CreateTab();
        await OpenAsync(tab, "c.jpg");
        source.WindowTabs.MoveOut(tab);
        var settings = WindowSettings.FromRecord(Settings);
        settings.FileOrder.Value = new FileOrder(SortFilesBy.FileSize, true);

        var window = CreateWindow(firstTab: tab, windowSettings: settings);
        await tab.Resorting;

        Assert.Equal(BySize, NamesOf(tab));
        Assert.Equal("c.jpg", ListedAtShownIndex(tab));

        // Its old window's order is no longer its own
        source.WindowSettings.FileOrder.Value = new FileOrder(SortFilesBy.Name, false);
        await tab.Resorting;
        Assert.Equal(BySize, NamesOf(tab));

        window.WindowSettings.FileOrder.Value = new FileOrder(SortFilesBy.FileSize, false);
        await tab.Resorting;
        Assert.Equal(BySize.Reverse(), NamesOf(tab));
    }

    [Fact]
    public async Task MoveTab_ThenTheSourceWindowCloses_ThrowsNothing()
    {
        ComposeServices();
        CreateFiles();
        var source = CreateWindow();
        var target = CreateWindow();
        var moved = source.WindowTabs.CreateTab();
        await OpenAsync(moved, "c.jpg");
        // Taken out for a new window that has not opened yet, so it still has its old window's settings
        var takenOut = source.WindowTabs.CreateTab();
        source.WindowTabs.MoveOut(takenOut);
        source.WindowTabs.MoveTab(moved, target.WindowTabs);

        // As the window closes: its tabs first, then its settings
        source.WindowTabs.TearDown();
        source.WindowSettings.Dispose();
        var thrown = await Record.ExceptionAsync(async () =>
        {
            await target.WindowTabs.SortAsync(SortFilesBy.FileSize);
            await OpenAsync(takenOut, "a.jpg");
            CreateWindow(firstTab: takenOut);
            await takenOut.Resorting;
        });

        Assert.Null(thrown);
        Assert.Equal(BySize, NamesOf(moved));
        Assert.Equal(ByName, NamesOf(takenOut));
    }

    [Fact]
    public async Task SideBySide_ToggledInOneWindow_RepairsEveryTabOfThatWindow_AndNoTabOfTheOther()
    {
        ComposeServices();
        CreateFiles();
        var toggled = CreateWindow();
        var other = CreateWindow();
        var first = toggled.WindowTabs.ActiveTab.Value;
        var second = toggled.WindowTabs.CreateTab();
        var otherTab = other.WindowTabs.ActiveTab.Value;
        await OpenAsync(first, "b.jpg");
        await OpenAsync(second, "c.jpg");
        await OpenAsync(otherTab, "b.jpg");

        toggled.WindowSettings.IsSideBySide.Value = true;

        // By name b shows beside c, and c beside d
        await WaitUntilAsync(() => first.SecondaryModel?.FileInfo?.Name is "c.jpg" &&
                                   second.SecondaryModel?.FileInfo?.Name is "d.jpg");
        Assert.Equal("b.jpg", first.FileInfo.CurrentValue?.Name);
        Assert.Equal("c.jpg", second.FileInfo.CurrentValue?.Name);
        Assert.True(first.IsShowingPair.CurrentValue);
        Assert.True(second.IsShowingPair.CurrentValue);
        Assert.False(otherTab.IsShowingPair.CurrentValue);
        Assert.Null(otherTab.SecondaryModel);
        Assert.False(other.WindowSettings.IsSideBySide.Value);
        Assert.False(Settings.ImageScaling.ShowImageSideBySide);
    }

    [Fact]
    public async Task Looping_ToggledInOneWindow_ChangesNextAndPrevOnlyThere()
    {
        ComposeServices();
        CreateFiles();
        var toggled = CreateWindow();
        var other = CreateWindow();
        var tab = toggled.WindowTabs.ActiveTab.Value;
        var otherTab = other.WindowTabs.ActiveTab.Value;
        // The first file, so only looping lets Prev run
        await OpenAsync(tab, "a.jpg");
        await OpenAsync(otherTab, "a.jpg");
        Assert.False(tab.CanNavigateBackwards.Value);

        toggled.WindowSettings.IsLooping.Value = true;

        Assert.True(tab.CanNavigateBackwards.Value);
        Assert.True(tab.CanNavigateForwards.Value);
        Assert.False(otherTab.CanNavigateBackwards.Value);
        Assert.Equal((false, true), tab.PairSettings());
        Assert.Equal((false, false), otherTab.PairSettings());

        toggled.WindowSettings.IsLooping.Value = false;

        Assert.False(tab.CanNavigateBackwards.Value);
        Assert.False(Settings.UIProperties.Looping);
    }

    [Fact]
    public async Task MoveTab_ToAWindowWithSideBySideOn_ShowsItsPairOnce_AndKeepsItsShownFile()
    {
        ComposeServices();
        CreateFiles();
        var source = CreateWindow();
        var target = CreateWindow();
        target.WindowSettings.IsSideBySide.Value = true;
        var tab = source.WindowTabs.CreateTab();
        await OpenAsync(tab, "b.jpg");
        Assert.Null(tab.SecondaryModel);
        var pairStates = new ConcurrentQueue<bool>();
        var shown = new ConcurrentQueue<ShownChange>();
        using var pairs = tab.IsShowingPair.Skip(1).Subscribe(pairStates.Enqueue);
        using var shownChanges = tab.Shown.Subscribe(shown.Enqueue);

        source.WindowTabs.MoveTab(tab, target.WindowTabs);

        await WaitUntilAsync(() => tab.SecondaryModel?.FileInfo?.Name is "c.jpg");
        Assert.Equal("b.jpg", tab.FileInfo.CurrentValue?.Name);
        Assert.Equal("b.jpg", ListedAtShownIndex(tab));
        Assert.Equal([true], pairStates);
        Assert.Equal([ShownChange.Image], shown);
        Assert.Equal(ByName, NamesOf(tab));
        // The window it left keeps its own setting
        Assert.False(source.WindowSettings.IsSideBySide.Value);
    }

    [Fact]
    public async Task MoveTab_ToAWindowWithLoopingOff_AtTheLastFile_NextIsNoLongerAvailable()
    {
        ComposeServices();
        CreateFiles();
        var source = CreateWindow();
        var target = CreateWindow();
        source.WindowSettings.IsLooping.Value = true;
        var tab = source.WindowTabs.CreateTab();
        await OpenAsync(tab, "d.jpg");
        Assert.True(tab.CanNavigateForwards.Value);

        source.WindowTabs.MoveTab(tab, target.WindowTabs);

        Assert.False(tab.CanNavigateForwards.Value);
        Assert.True(tab.CanNavigateBackwards.Value);
        Assert.Equal("d.jpg", tab.FileInfo.CurrentValue?.Name);

        // Its old window's looping is no longer its own
        source.WindowSettings.IsLooping.Value = false;
        source.WindowSettings.IsLooping.Value = true;
        Assert.False(tab.CanNavigateForwards.Value);

        target.WindowSettings.IsLooping.Value = true;
        Assert.True(tab.CanNavigateForwards.Value);
    }

    [Fact]
    public async Task MoveTab_ThenTheSourceWindowCloses_ThrowsNothing_ForEveryFollowedSetting()
    {
        var unhandled = new ConcurrentQueue<Exception>();
        using var recording = RecordUnhandled(unhandled);
        ComposeServices();
        CreateFiles();
        var source = CreateWindow();
        var target = CreateWindow();
        var moved = source.WindowTabs.CreateTab();
        await OpenAsync(moved, "b.jpg");
        // Taken out for a new window that has not opened yet, so it still has its old window's settings
        var takenOut = source.WindowTabs.CreateTab();
        source.WindowTabs.MoveOut(takenOut);
        source.WindowTabs.MoveTab(moved, target.WindowTabs);

        // As the window closes: its tabs first, then its settings
        source.WindowTabs.TearDown();
        source.WindowSettings.Dispose();
        var thrown = await Record.ExceptionAsync(async () =>
        {
            target.WindowSettings.IsSideBySide.Value = true;
            target.WindowSettings.IsLooping.Value = true;
            await target.WindowTabs.SortAsync(SortFilesBy.FileSize);
            // Its first load follows the settings, which are still the closed window's
            await OpenAsync(takenOut, "b.jpg");
            var settings = WindowSettings.FromRecord(Settings);
            settings.IsSideBySide.Value = true;
            settings.IsLooping.Value = true;
            CreateWindow(firstTab: takenOut, windowSettings: settings);
            await WaitUntilAsync(() => takenOut.SecondaryModel?.FileInfo?.Name is "c.jpg");
        });

        Assert.Null(thrown);
        Assert.DoesNotContain(unhandled, e => e is ObjectDisposedException);
        await WaitUntilAsync(() => moved.SecondaryModel is not null);
        Assert.True(moved.IsShowingPair.CurrentValue);
        Assert.Equal((true, true), moved.PairSettings());
        Assert.True(takenOut.IsShowingPair.CurrentValue);
        Assert.Equal((true, true), takenOut.PairSettings());
        Assert.Equal("b.jpg", takenOut.FileInfo.CurrentValue?.Name);
    }

    [Fact]
    public async Task CloseWindow_WithTabsFollowingIt_ThrowsNothing()
    {
        var unhandled = new ConcurrentQueue<Exception>();
        using var recording = RecordUnhandled(unhandled);
        // Nothing decodes ahead, so a re-pair decodes the partner itself
        Settings.Navigation.PositiveIterations = 0;
        Settings.Navigation.NegativeIterations = 0;
        ComposeServices();
        CreateFiles();
        var window = CreateWindow();
        var first = window.WindowTabs.ActiveTab.Value;
        var second = window.WindowTabs.CreateTab();
        await OpenAsync(first, "a.jpg");
        await OpenAsync(second, "c.jpg");
        // Taken out for a new window that has not opened yet, so it still has this window's settings
        var takenOut = window.WindowTabs.CreateTab();
        window.WindowTabs.MoveOut(takenOut);
        var partners = (First: HoldDecode("b.jpg"), Second: HoldDecode("d.jpg"));
        window.WindowSettings.IsSideBySide.Value = true;
        // Both re-pairs wait for their partner as the window closes
        await DecodeRequestedOf("b.jpg").Task.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);
        await DecodeRequestedOf("d.jpg").Task.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

        var thrown = Record.Exception(() =>
        {
            // As the window closes: its tabs first, then its settings
            window.WindowSettings.IsLooping.Value = true;
            window.WindowTabs.TearDown();
            window.WindowSettings.Dispose();
        });
        partners.First.SetResult(new ImageModel { FileInfo = new FileInfo(PathOf("b.jpg")) });
        partners.Second.SetResult(new ImageModel { FileInfo = new FileInfo(PathOf("d.jpg")) });
        // The partners the re-pairs waited for have arrived
        await WaitUntilAsync(() => _core.SharedCache.TryGetReady(new FileInfo(PathOf("b.jpg")), out _) &&
                                   _core.SharedCache.TryGetReady(new FileInfo(PathOf("d.jpg")), out _));
        // A follower of the closed window's settings, as the taken-out tab's first load subscribes: it hears nothing
        var heard = new List<bool>();
        using var late = takenOut.FollowWindowSetting(static w => w.IsSideBySide, false).Subscribe(heard.Add);

        Assert.Null(thrown);
        Assert.True(window.WindowSettings.IsDisposed);
        Assert.Empty(heard);
        Assert.Empty(unhandled);
        Assert.True(first.IsDisposed);
        Assert.True(second.IsDisposed);
        Assert.Null(first.SecondaryModel);
        Assert.Null(second.SecondaryModel);
    }

    [Fact]
    public async Task MoveTab_ToAWindowThatAgrees_DoesNothing()
    {
        ComposeServices();
        CreateFiles();
        var source = CreateWindow();
        var target = CreateWindow();
        var tab = source.WindowTabs.CreateTab();
        await OpenAsync(tab, "b.jpg");
        // Shown as a pair by a re-pair whose one shown mark is awaited, so no mark of it arrives later
        var paired = tab.Shown.FirstAsync(TestContext.Current.CancellationToken);
        foreach (var settings in new[] { source.WindowSettings, target.WindowSettings })
        {
            settings.IsSideBySide.Value = true;
            settings.IsLooping.Value = true;
        }
        await paired.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);
        Assert.Equal("c.jpg", tab.SecondaryModel?.FileInfo?.Name);
        var pairStates = new ConcurrentQueue<bool>();
        var shown = new ConcurrentQueue<ShownChange>();
        using var pairs = tab.IsShowingPair.Skip(1).Subscribe(pairStates.Enqueue);
        using var shownChanges = tab.Shown.Subscribe(shown.Enqueue);

        source.WindowTabs.MoveTab(tab, target.WindowTabs);

        Assert.Empty(pairStates);
        Assert.True(tab.CanNavigateForwards.Value);
        Assert.True(tab.CanNavigateBackwards.Value);
        // A re-pair runs after any before it, so the one this change starts shows that the move started none
        target.WindowSettings.IsSideBySide.Value = false;
        await WaitUntilAsync(() => tab.SecondaryModel is null && !shown.IsEmpty);
        Assert.Equal([false], pairStates);
        Assert.Equal([ShownChange.Image], shown);
        Assert.Equal("b.jpg", tab.FileInfo.CurrentValue?.Name);
    }

    [Fact]
    public void FollowWindowSetting_TabWithoutAWindow_GivesTheFallback()
    {
        Settings.ImageScaling.ShowImageSideBySide = true;
        Settings.UIProperties.Looping = true;
        using var tab = new TabViewModel(null!);
        var looping = new List<bool>();
        var order = new List<FileOrder>();
        var fallback = new FileOrder(SortFilesBy.Extension, false);

        using var followingLooping = tab.FollowWindowSetting(static w => w.IsLooping, false).Subscribe(looping.Add);
        using var followingOrder = tab.FollowWindowSetting(static w => w.FileOrder, fallback).Subscribe(order.Add);

        Assert.Equal([false], looping);
        Assert.Equal([fallback], order);
        // A tab without a window shows a pair and loops as the record says
        Assert.True(tab.IsShowingPair.CurrentValue);
        Assert.Equal((true, true), tab.PairSettings());
    }

    // The recording of the test whose flow runs, which work that test starts carries with it
    private static readonly AsyncLocal<ConcurrentQueue<Exception>?> Recording = new();

    // Records what R3 hands its unhandled-exception handler meanwhile, as an exception in a subscription goes there
    // instead of to the caller. The handler is the whole process's, so only what reaches it in this test's flow is
    // recorded: work another test left running carries that test's flow, and its exceptions are not this test's
    private static IDisposable RecordUnhandled(ConcurrentQueue<Exception> errors)
    {
        var previous = ObservableSystem.GetUnhandledExceptionHandler();
        Recording.Value = errors;
        ObservableSystem.RegisterUnhandledExceptionHandler(e =>
        {
            if (ReferenceEquals(Recording.Value, errors))
            {
                errors.Enqueue(e);
            }
            previous(e);
        });
        return Disposable.Create(() =>
        {
            ObservableSystem.RegisterUnhandledExceptionHandler(previous);
            Recording.Value = null;
        });
    }

    [Fact]
    public async Task Load_FreshTabOpeningAFolder_WhileTheWindowsOrderChanges_EndsInTheNewOrder()
    {
        using var platform = new GatedPlatform();
        ComposeServices(platform);
        CreateFiles();
        var window = CreateWindow();
        var tab = window.WindowTabs.ActiveTab.Value;

        platform.Close();
        var opening = Task.Run(() => tab.Load.LoadAsync(_folder).AsTask());
        await platform.Comparing.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);
        await window.WindowTabs.SortAsync(SortFilesBy.FileSize);
        platform.Open();
        Assert.True((await opening).IsLoaded);
        await tab.Resorting;

        Assert.Equal(BySize, NamesOf(tab));
        Assert.Equal(window.WindowSettings.FileOrder.Value, tab.ImageIterator!.Ordering.Order);
    }

    [Fact]
    public async Task Load_OpeningAFile_WhileTheWindowsOrderChanges_EndsInTheNewOrder()
    {
        using var platform = new GatedPlatform();
        ComposeServices(platform);
        CreateFiles();
        var window = CreateWindow();
        var tab = window.WindowTabs.ActiveTab.Value;

        platform.Close();
        var opening = Task.Run(() => tab.Load.LoadAsync(PathOf("c.jpg")).AsTask());
        await platform.Comparing.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);
        await window.WindowTabs.SortAsync(SortFilesBy.FileSize);
        platform.Open();
        Assert.True((await opening).IsLoaded);
        await tab.Resorting;

        Assert.Equal(BySize, NamesOf(tab));
        Assert.Equal("c.jpg", ListedAtShownIndex(tab));
        Assert.Equal(window.WindowSettings.FileOrder.Value, tab.ImageIterator!.Ordering.Order);
    }

    [Fact]
    public async Task Load_BrowsingTabListingAgain_WhileTheWindowsOrderChanges_EndsInTheNewOrder()
    {
        using var platform = new GatedPlatform();
        ComposeServices(platform);
        CreateFiles();
        var window = CreateWindow();
        var tab = window.WindowTabs.ActiveTab.Value;
        await OpenAsync(tab, "c.jpg");

        platform.Close();
        var relisting = Task.Run(() =>
            tab.Load.LoadAsync(PathOf("c.jpg"), new LoadOptions(Relist: true)).AsTask());
        await platform.Comparing.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);
        // The tab sorts the list it has while the relist lists in the order it began with
        var sorting = window.WindowTabs.SortAsync(SortFilesBy.FileSize).AsTask();
        platform.Open();
        Assert.True((await relisting).IsLoaded);
        await sorting;
        await tab.Resorting;

        Assert.Equal(BySize, NamesOf(tab));
        Assert.Equal("c.jpg", ListedAtShownIndex(tab));
        Assert.Equal(window.WindowSettings.FileOrder.Value, tab.ImageIterator!.Ordering.Order);
    }

    [Fact]
    public async Task Load_TabMovedToAWindowWithAnotherOrderWhileItLists_EndsInThatWindowsOrder()
    {
        using var platform = new GatedPlatform();
        ComposeServices(platform);
        CreateFiles();
        var source = CreateWindow();
        var target = CreateWindow();
        await target.WindowTabs.SortAsync(SortFilesBy.FileSize);
        var tab = source.WindowTabs.CreateTab();

        platform.Close();
        var opening = Task.Run(() => tab.Load.LoadAsync(PathOf("c.jpg")).AsTask());
        await platform.Comparing.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);
        source.WindowTabs.MoveTab(tab, target.WindowTabs);
        platform.Open();
        Assert.True((await opening).IsLoaded);
        await tab.Resorting;

        Assert.Equal(BySize, NamesOf(tab));
        Assert.Equal("c.jpg", ListedAtShownIndex(tab));
        Assert.Equal(target.WindowSettings.FileOrder.Value, tab.ImageIterator!.Ordering.Order);
    }

    [Fact]
    public async Task Load_GivenFilesListedInAnOlderOrder_EndsInTheWindowsOrder()
    {
        ComposeServices();
        CreateFiles();
        var window = CreateWindow();
        var tab = window.WindowTabs.ActiveTab.Value;
        // Listed by name, as App Start lists a folder, before the window's order became by size
        var byName = new FileOrdering(new FileOrder(SortFilesBy.Name, true), string.CompareOrdinal);
        var files = FileListRetriever.RetrieveFiles(new FileInfo(PathOf("a.jpg")), byName);
        await window.WindowTabs.SortAsync(SortFilesBy.FileSize);

        var result = await tab.Load.LoadAsync(PathOf("c.jpg"), new LoadOptions(Files: files, ListedWith: byName));
        Assert.True(result.IsLoaded);
        await tab.Resorting;

        Assert.Equal(BySize, NamesOf(tab));
        Assert.Equal("c.jpg", ListedAtShownIndex(tab));
        Assert.Equal(window.WindowSettings.FileOrder.Value, tab.ImageIterator!.Ordering.Order);
    }

    [Fact]
    public async Task Load_SideBySide_ResortedWhileThePartnerDecodes_CachesEachModelUnderItsFile_AndShowsTheListsPair()
    {
        ComposeServices();
        CreateFiles();
        var window = CreateWindow();
        window.WindowSettings.IsSideBySide.Value = true;
        var tab = window.WindowTabs.ActiveTab.Value;
        // By name b shows beside c; by size b is first, beside d
        var partner = HoldDecode("c.jpg");

        var opening = Task.Run(() => tab.Load.LoadAsync(PathOf("b.jpg")).AsTask());
        await DecodeRequestedOf("c.jpg").Task.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);
        await window.WindowTabs.SortAsync(SortFilesBy.FileSize);
        partner.SetResult(new ImageModel { FileInfo = new FileInfo(PathOf("c.jpg")) });
        Assert.True((await opening).IsLoaded);
        await tab.Resorting;

        Assert.Equal(BySize, NamesOf(tab));
        foreach (var name in BySize)
        {
            if (_core.SharedCache.TryGetReady(new FileInfo(PathOf(name)), out var cached))
            {
                Assert.Equal(name, cached.FileInfo?.Name);
            }
        }

        Assert.Equal("b.jpg", tab.Model?.FileInfo?.Name);
        Assert.Equal("d.jpg", tab.SecondaryModel?.FileInfo?.Name);
    }

    [Fact]
    public async Task Sort_SideBySide_ResortedBeforeAStepsPairShows_ShowsThePairAtTheIteratorsPosition()
    {
        Settings.Navigation.PositiveIterations = 0;
        Settings.Navigation.NegativeIterations = 0;
        ComposeServices();
        // By name a, b, c, d; by size c, a, b, d, so the shown pair a, b is still a pair, and the step's c is first
        CreateFiles(("a.jpg", 10), ("b.jpg", 20), ("c.jpg", 5), ("d.jpg", 30));
        var window = CreateWindow();
        window.WindowSettings.IsSideBySide.Value = true;
        var tab = window.WindowTabs.ActiveTab.Value;
        await OpenAsync(tab, "a.jpg");
        Assert.Equal("b.jpg", tab.SecondaryModel?.FileInfo?.Name);
        var iterator = tab.ImageIterator!;
        var partner = HoldDecode("d.jpg");

        // A Next to c and d, which makes c current before its pair shows
        var step = iterator.NavigateAsync(NavigateTo.Next, SkipAmount.One, new CancellationTokenSource()).AsTask();
        Assert.Equal("c.jpg", ListedAtShownIndex(tab));
        await window.WindowTabs.SortAsync(SortFilesBy.FileSize);
        await tab.Resorting;
        partner.SetResult(new ImageModel { FileInfo = new FileInfo(PathOf("d.jpg")) });
        await step.WaitAsync(WaitLimit, TestContext.Current.CancellationToken);

        Assert.Equal(["c.jpg", "a.jpg", "b.jpg", "d.jpg"], NamesOf(tab));
        Assert.Equal(iterator.Files[iterator.CurrentIndex].Name, tab.Model?.FileInfo?.Name);
        Assert.Equal(iterator.Files[iterator.SecondaryCurrentIndex].Name, tab.SecondaryModel?.FileInfo?.Name);
        Assert.Equal("c.jpg", tab.Model?.FileInfo?.Name);
        Assert.Equal("a.jpg", tab.SecondaryModel?.FileInfo?.Name);
    }

    [Fact]
    public async Task CheckMarks_DeriveFromTheWindowsFileOrder()
    {
        Settings.Sorting.SortPreference = (int)SortFilesBy.FileSize;
        Settings.Sorting.Ascending = false;
        var tabs = CreateWindow().WindowTabs;

        Assert.Equal([false, true, false, false, false, false, false], CheckMarksOf(tabs));
        Assert.False(tabs.IsAscending.Value);

        await tabs.SortAsync(SortFilesBy.Random);
        await tabs.SortAsync(ascending: true);

        Assert.Equal([false, false, false, false, false, false, true], CheckMarksOf(tabs));
        Assert.True(tabs.IsAscending.Value);
    }

    [Fact]
    public async Task CheckMarks_AreNotSharedBetweenWindows()
    {
        var sorted = CreateWindow();
        var other = CreateWindow();

        await sorted.WindowTabs.SortAsync(SortFilesBy.LastWriteTime);
        await sorted.WindowTabs.SortAsync(ascending: false);

        Assert.Equal([false, false, false, false, false, true, false], CheckMarksOf(sorted.WindowTabs));
        Assert.False(sorted.WindowTabs.IsAscending.Value);
        Assert.Equal([true, false, false, false, false, false, false], CheckMarksOf(other.WindowTabs));
        Assert.True(other.WindowTabs.IsAscending.Value);
    }

    // Name, FileSize, CreationTime, Extension, LastAccessTime, LastWriteTime, Randomization
    #region The gallery's look follows its window

    [Fact]
    public void Stretch_ChangedInOneWindow_ChangesTheAppearanceOfEveryTabThere_AndNoTabOfTheOther()
    {
        var changed = CreateStartedWindow();
        var other = CreateStartedWindow();
        var docked = changed.WindowTabs.ActiveTab.Value;
        var expanded = changed.WindowTabs.CreateTab();
        var otherDocked = other.WindowTabs.ActiveTab.Value;
        var otherExpanded = other.WindowTabs.CreateTab();
        ShowGallery(docked, GalleryMode.Docked);
        ShowGallery(expanded, GalleryMode.Expanded);
        ShowGallery(otherDocked, GalleryMode.Docked);
        ShowGallery(otherExpanded, GalleryMode.Expanded);
        var otherLooks = (Docked: otherDocked.Gallery.Appearance.Value, Expanded: otherExpanded.Gallery.Appearance.Value);
        var recordStretches = (Settings.Gallery.DockedGalleryStretchMode, Settings.Gallery.ExpandedGalleryStretchMode);

        changed.WindowSettings.Gallery.SetDockedStretchModeCommand.Execute(GalleryStretchMode.FillSquare);
        changed.WindowSettings.Gallery.SetExpandedStretchModeCommand.Execute(GalleryStretchMode.Square);

        Assert.Equal(GalleryStretchMode.FillSquare, docked.Gallery.Appearance.Value.Stretch);
        Assert.Equal(docked.Gallery.Appearance.Value.ItemHeight, docked.Gallery.Appearance.Value.ItemWidth);
        Assert.Equal(GalleryStretchMode.Square, expanded.Gallery.Appearance.Value.Stretch);
        Assert.Equal(otherLooks, (otherDocked.Gallery.Appearance.Value, otherExpanded.Gallery.Appearance.Value));
        Assert.Equal(recordStretches,
            (Settings.Gallery.DockedGalleryStretchMode, Settings.Gallery.ExpandedGalleryStretchMode));
    }

    [Fact]
    public void DisplayMode_ChangedInOneWindow_ChangesTheAppearanceOfEveryTabThere_AndNoTabOfTheOther()
    {
        var changed = CreateStartedWindow();
        var other = CreateStartedWindow();
        var first = changed.WindowTabs.ActiveTab.Value;
        var second = changed.WindowTabs.CreateTab();
        var otherTab = other.WindowTabs.ActiveTab.Value;
        foreach (var tab in new[] { first, second, otherTab })
        {
            ShowGallery(tab, GalleryMode.Expanded);
        }
        var otherLook = otherTab.Gallery.Appearance.Value;

        changed.WindowSettings.Gallery.SetExpandedDisplayModeCommand.Execute(GalleryDisplayMode.Masonry);

        Assert.Equal(GalleryDisplayMode.Masonry, first.Gallery.Appearance.Value.DisplayMode);
        Assert.Equal(GalleryDisplayMode.Masonry, second.Gallery.Appearance.Value.DisplayMode);
        Assert.Equal(otherLook, otherTab.Gallery.Appearance.Value);

        changed.WindowSettings.Gallery.ToggleExpandedDisplayModeCommand.Execute(Unit.Default);
        other.WindowSettings.Gallery.ToggleExpandedDisplayModeCommand.Execute(Unit.Default);

        // Each window toggles from its own mode, and to its own last tile mode
        Assert.Equal(GalleryDisplayMode.Columns, first.Gallery.Appearance.Value.DisplayMode);
        Assert.Equal(GalleryDisplayMode.Columns, second.Gallery.Appearance.Value.DisplayMode);
        Assert.Equal(other.WindowSettings.Gallery.ExpandedGalleryDisplayMode.Value,
            otherTab.Gallery.Appearance.Value.DisplayMode);
        Assert.True(GalleryDisplayModes.ShowsTiles(otherTab.Gallery.Appearance.Value.DisplayMode));
        Assert.Equal(GalleryDisplayMode.Columns, Settings.Gallery.ExpandedGalleryDisplayMode);
    }

    [Fact]
    public void ItemSize_ChangedInOneWindow_ChangesTheAppearanceOfEveryTabThere_AndNoTabOfTheOther()
    {
        var changed = CreateStartedWindow();
        var other = CreateStartedWindow();
        var docked = changed.WindowTabs.ActiveTab.Value;
        var expanded = changed.WindowTabs.CreateTab();
        var otherDocked = other.WindowTabs.ActiveTab.Value;
        var otherExpanded = other.WindowTabs.CreateTab();
        ShowGallery(docked, GalleryMode.Docked);
        ShowGallery(expanded, GalleryMode.Expanded);
        ShowGallery(otherDocked, GalleryMode.Docked);
        ShowGallery(otherExpanded, GalleryMode.Expanded);
        var otherLooks = (Docked: otherDocked.Gallery.Appearance.Value, Expanded: otherExpanded.Gallery.Appearance.Value);

        changed.WindowSettings.Gallery.DockedGalleryItemSize.Value = 120;
        changed.WindowSettings.Gallery.ExpandedGalleryItemSize.Value = 200;

        Assert.Equal(120, docked.Gallery.Appearance.Value.ItemHeight);
        Assert.Equal(200, expanded.Gallery.Appearance.Value.ItemHeight);
        Assert.Equal(otherLooks, (otherDocked.Gallery.Appearance.Value, otherExpanded.Gallery.Appearance.Value));
        Assert.Equal(GalleryDefaults.DefaultDockedGalleryHeight, Settings.Gallery.DockedGalleryItemSize);
        Assert.Equal(GalleryDefaults.DefaultExpandedGalleryHeight, Settings.Gallery.ExpandedGalleryItemSize);
    }

    [Fact]
    public void MoveTab_ToAWindowWithAnotherLook_TakesItsAppearanceOnce()
    {
        var source = CreateStartedWindow();
        var target = CreateStartedWindow();
        target.WindowSettings.Gallery.SetDockedStretchModeCommand.Execute(GalleryStretchMode.FillSquare);
        target.WindowSettings.Gallery.DockedGalleryItemSize.Value = 120;
        target.WindowSettings.Gallery.SetExpandedDisplayModeCommand.Execute(GalleryDisplayMode.Rows);
        var tab = source.WindowTabs.CreateTab();
        ShowGallery(tab, GalleryMode.Docked);
        var published = new List<GalleryAppearance>();
        using var appearances = tab.Gallery.Appearance.AsObservable().Skip(1).Subscribe(published.Add);

        source.WindowTabs.MoveTab(tab, target.WindowTabs);

        var appearance = Assert.Single(published);
        Assert.Equal(GalleryMode.Docked, appearance.Mode);
        Assert.Equal(GalleryStretchMode.FillSquare, appearance.Stretch);
        Assert.Equal(120, appearance.ItemHeight);

        // Its old window's look is no longer its own
        source.WindowSettings.Gallery.DockedGalleryItemSize.Value = 50;
        Assert.Equal(120, tab.Gallery.Appearance.Value.ItemHeight);
        tab.Gallery.Switcher.ToggleExpanded();
        Assert.Equal(GalleryDisplayMode.Rows, tab.Gallery.Appearance.Value.DisplayMode);
    }

    [Fact]
    public async Task MoveTab_ToAWindowWithLargerItems_AsksForItsThumbnailsAgainOnce()
    {
        ComposeServices();
        CreateFiles();
        var source = CreateStartedWindow();
        var target = CreateStartedWindow();
        target.WindowSettings.Gallery.ExpandedGalleryItemSize.Value = GalleryDefaults.MaxExpandedGalleryItemHeight;
        var tab = source.WindowTabs.CreateTab();
        ShowGallery(tab, GalleryMode.Docked);
        await OpenAsync(tab, "b.jpg");
        await WaitUntilAsync(() => IsLoaded(tab));
        var before = tab.Gallery.GalleryItems.ToArray();
        Assert.All(before, item => Assert.Equal((uint)GalleryDefaults.DefaultExpandedGalleryHeight, item.ThumbnailHeight));
        var changes = RecordItemChanges(tab);

        source.WindowTabs.MoveTab(tab, target.WindowTabs);

        await WaitUntilAsync(() => IsLoaded(tab) && !tab.Gallery.GalleryItems.Contains(before[0]));
        await tab.GalleryCoordinator!.Idle;
        Assert.Equal(ByName, tab.Gallery.GalleryItems.Select(item => item.FileInfo!.Name));
        Assert.All(tab.Gallery.GalleryItems,
            item => Assert.Equal((uint)GalleryDefaults.MaxExpandedGalleryItemHeight, item.ThumbnailHeight));
        // One clear of the old items and one add of the new ones
        Assert.Equal([NotifyCollectionChangedAction.Reset, NotifyCollectionChangedAction.Add], changes);
        await CloseAsync(source, target);
    }

    [Theory]
    [InlineData(GalleryDefaults.DefaultExpandedGalleryHeight)]
    [InlineData(120)]
    public async Task MoveTab_ToAWindowWithSmallerOrEqualItems_KeepsItsGalleryItems(double expandedSize)
    {
        ComposeServices();
        CreateFiles();
        var source = CreateStartedWindow();
        var target = CreateStartedWindow();
        target.WindowSettings.Gallery.ExpandedGalleryItemSize.Value = expandedSize;
        var tab = source.WindowTabs.CreateTab();
        ShowGallery(tab, GalleryMode.Docked);
        await OpenAsync(tab, "b.jpg");
        await WaitUntilAsync(() => IsLoaded(tab));
        var before = tab.Gallery.GalleryItems.ToArray();
        var changes = RecordItemChanges(tab);

        source.WindowTabs.MoveTab(tab, target.WindowTabs);
        await tab.GalleryCoordinator!.Idle;

        Assert.Equal(before, tab.Gallery.GalleryItems);
        Assert.Empty(changes);
        Assert.Equal(GalleryLoadingState.Loaded, tab.Gallery.LoadingState);
        await CloseAsync(source, target);
    }

    [Fact]
    public void MoveTab_BetweenWindows_SubscribesNoGalleryCommandTwice()
    {
        var source = CreateStartedWindow();
        var target = CreateStartedWindow();
        var tab = source.WindowTabs.CreateTab();
        tab.Gallery.Initialize(_core.GallerySettings);
        // A second Initialize, as a tab that moves is initialized again; the guard must keep it from subscribing twice
        tab.Gallery.Initialize(_core.GallerySettings);

        source.WindowTabs.MoveTab(tab, target.WindowTabs);
        target.WindowTabs.MoveTab(tab, source.WindowTabs);
        source.WindowTabs.MoveTab(tab, target.WindowTabs);
        tab.Gallery.ToggleExpandedCommand.Execute(Unit.Default);
        target.WindowSettings.Gallery.ToggleExpandedDisplayModeCommand.Execute(Unit.Default);

        // Run twice, each would have gone back
        Assert.Equal(GalleryMode.Expanded, tab.Gallery.Mode.Value);
        Assert.True(GalleryDisplayModes.ShowsTiles(tab.Gallery.Appearance.Value.DisplayMode));
    }

    // How crop, a Single Image and the Gallery File Limit hide an Expanded Gallery, or a Closed one
    public enum GalleryHiding
    {
        Crop,
        SingleImage,
        FileLimit,
        Closed
    }

    [Theory]
    [InlineData(GalleryHiding.Crop)]
    [InlineData(GalleryHiding.SingleImage)]
    [InlineData(GalleryHiding.FileLimit)]
    [InlineData(GalleryHiding.Closed)]
    public async Task Look_ChangedWhileTheGalleryIsHiddenOrClosed_ThrowsNothing_AndShowsWhenItShows(
        GalleryHiding hiding)
    {
        var unhandled = new ConcurrentQueue<Exception>();
        using var recording = RecordUnhandled(unhandled);
        var window = CreateStartedWindow();
        var tab = window.WindowTabs.ActiveTab.Value;
        ShowGallery(tab, GalleryMode.Expanded);
        var switcher = tab.Gallery.Switcher;
        switch (hiding)
        {
            case GalleryHiding.Crop:
            case GalleryHiding.SingleImage:
                switcher.SetHidden(true);
                break;
            case GalleryHiding.FileLimit:
                switcher.SetOverLimit(true);
                break;
            case GalleryHiding.Closed:
                await switcher.Close();
                break;
        }
        Assert.Equal(GalleryMode.Closed, tab.Gallery.Appearance.Value.Mode);

        var thrown = Record.Exception(() =>
        {
            window.WindowSettings.Gallery.SetExpandedDisplayModeCommand.Execute(GalleryDisplayMode.Columns);
            window.WindowSettings.Gallery.SetExpandedStretchModeCommand.Execute(GalleryStretchMode.Square);
            window.WindowSettings.Gallery.SetDockedStretchModeCommand.Execute(GalleryStretchMode.FillSquare);
            window.WindowSettings.Gallery.ExpandedGalleryItemSize.Value = 200;
        });
        Assert.Null(thrown);
        Assert.Equal(GalleryMode.Closed, tab.Gallery.Appearance.Value.Mode);

        switch (hiding)
        {
            case GalleryHiding.Crop:
            case GalleryHiding.SingleImage:
                switcher.SetHidden(false);
                break;
            case GalleryHiding.FileLimit:
                switcher.SetOverLimit(false);
                break;
            case GalleryHiding.Closed:
                switcher.ToggleExpanded();
                break;
        }

        var shown = tab.Gallery.Appearance.Value;
        Assert.Equal(GalleryMode.Expanded, shown.Mode);
        Assert.Equal(GalleryStretchMode.Square, shown.Stretch);
        Assert.Equal(200, shown.ItemHeight);
        Assert.Equal(200, shown.ItemWidth);
        Assert.Empty(unhandled);
    }

    #region The dock edge and the start-docked choice follow the window

    [Fact]
    public async Task DockAt_InOneWindow_MovesEveryDockedTabThere_AndNoTabOfTheOther()
    {
        var changed = CreateStartedWindow();
        var other = CreateStartedWindow();
        var asking = changed.WindowTabs.ActiveTab.Value;
        var docked = changed.WindowTabs.CreateTab();
        var otherDocked = other.WindowTabs.ActiveTab.Value;
        foreach (var tab in new[] { asking, docked, otherDocked })
        {
            ShowGallery(tab, GalleryMode.Docked);
        }
        var otherLook = otherDocked.Gallery.Appearance.Value;

        await asking.Gallery.Switcher.DockAt(GalleryDockPosition.Left);

        Assert.Equal(GalleryDockPosition.Left, changed.WindowSettings.Gallery.DockPosition.Value);
        foreach (var tab in new[] { asking, docked })
        {
            Assert.Equal(GalleryDockPosition.Left, tab.Gallery.Appearance.Value.Dock);
            Assert.True(tab.Gallery.IsLeftDocked.Value);
            Assert.False(tab.Gallery.IsBottomDocked.Value);
        }

        Assert.Equal(otherLook, otherDocked.Gallery.Appearance.Value);
        Assert.True(otherDocked.Gallery.IsBottomDocked.Value);
        Assert.Equal(GalleryDockPosition.Bottom, other.WindowSettings.Gallery.DockPosition.Value);
        Assert.Equal(GalleryDockPosition.Bottom, Settings.Gallery.DockPosition);
    }

    // Review Focus 5: no appearance at the old edge, and none Closed at the new one
    [Theory]
    [InlineData(GalleryMode.Closed)]
    [InlineData(GalleryMode.Docked)]
    [InlineData(GalleryMode.Expanded)]
    public async Task DockAt_PublishesOneAppearance_ForTheTabThatAsked(GalleryMode from)
    {
        var window = CreateStartedWindow();
        var tab = window.WindowTabs.ActiveTab.Value;
        ShowGallery(tab, from);
        var published = new List<GalleryAppearance>();
        using var appearances = tab.Gallery.Appearance.AsObservable().Skip(1).Subscribe(published.Add);

        await tab.Gallery.Switcher.DockAt(GalleryDockPosition.Left);

        var appearance = Assert.Single(published);
        Assert.Equal(from is GalleryMode.Expanded ? GalleryMode.Expanded : GalleryMode.Docked, appearance.Mode);
        Assert.Equal(GalleryDockPosition.Left, appearance.Dock);
        Assert.Equal(GalleryDockPosition.Left, window.WindowSettings.Gallery.DockPosition.Value);
    }

    // The edge the window already has: nothing moves, so only a Closed gallery publishes (the dock itself)
    [Theory]
    [InlineData(GalleryMode.Closed, 1)]
    [InlineData(GalleryMode.Docked, 0)]
    [InlineData(GalleryMode.Expanded, 0)]
    public async Task DockAt_AtTheEdgeTheWindowAlreadyHas_PublishesNothingForTheEdge(GalleryMode from, int expected)
    {
        var window = CreateStartedWindow();
        var tab = window.WindowTabs.ActiveTab.Value;
        var edge = window.WindowSettings.Gallery.DockPosition.Value;
        ShowGallery(tab, from);
        var published = new List<GalleryAppearance>();
        using var appearances = tab.Gallery.Appearance.AsObservable().Skip(1).Subscribe(published.Add);

        await tab.Gallery.Switcher.DockAt(edge);

        Assert.Equal(expected, published.Count);
        Assert.All(published, appearance => Assert.Equal(edge, appearance.Dock));
        Assert.Equal(edge, window.WindowSettings.Gallery.DockPosition.Value);
    }

    // ADR-0011: crop, a Single Image and the Gallery File Limit hide the gallery without changing its mode, so they
    // change nothing a new tab starts with either; a dock or undock by hand does
    [Theory]
    [InlineData(GalleryHiding.Crop)]
    [InlineData(GalleryHiding.SingleImage)]
    [InlineData(GalleryHiding.FileLimit)]
    public async Task CropOrSingleImageOrFileLimit_HidingTheGallery_DoesNotChangeStartsDocked(GalleryHiding hiding)
    {
        var window = CreateStartedWindow();
        window.WindowSettings.Gallery.StartsDocked.Value = true;
        var tab = window.WindowTabs.ActiveTab.Value;
        ShowGallery(tab, GalleryMode.Docked);
        var switcher = tab.Gallery.Switcher;
        Action<bool> hide = hiding is GalleryHiding.FileLimit ? switcher.SetOverLimit : switcher.SetHidden;

        hide(true);
        // Refused while hidden; docking at an edge only moves the edge
        await switcher.ToggleDocked();
        await switcher.Contract();
        await switcher.Close();
        await switcher.DockAt(GalleryDockPosition.Top);
        hide(false);

        Assert.True(window.WindowSettings.Gallery.StartsDocked.Value);
        Assert.Equal(GalleryMode.Docked, tab.Gallery.ShownMode.Value);
        Assert.Equal(GalleryDockPosition.Top, tab.Gallery.Appearance.Value.Dock);

        await switcher.Contract();

        Assert.False(window.WindowSettings.Gallery.StartsDocked.Value);
        Assert.False(Settings.Gallery.IsGalleryDocked);
    }

    [Fact]
    public void NewTab_StartsDockedPerItsOwnWindow()
    {
        var docked = CreateStartedWindow();
        var closed = CreateStartedWindow();
        docked.WindowSettings.Gallery.StartsDocked.Value = true;
        docked.WindowSettings.Gallery.DockPosition.Value = GalleryDockPosition.Right;
        var dockedTab = docked.WindowTabs.CreateTab();
        var closedTab = closed.WindowTabs.CreateTab();

        // As a tab opened for the first time starts
        TabNavigationInitializer.Initialize(_core, docked, dockedTab);
        TabNavigationInitializer.Initialize(_core, closed, closedTab);

        Assert.Equal(GalleryMode.Docked, dockedTab.Gallery.Mode.Value);
        Assert.Equal(GalleryDockPosition.Right, dockedTab.Gallery.Appearance.Value.Dock);
        Assert.Equal(GalleryMode.Closed, closedTab.Gallery.Mode.Value);
        Assert.False(Settings.Gallery.IsGalleryDocked);
    }

    // A tab that has not opened a source yet shows the start-up menu, which initializes its gallery: its dock commands
    // then set what the tab starts with when the ImageViewer opens
    [Fact]
    public void DockAtCommand_BeforeTheTabOpensASource_DocksAtThatEdgeWhenItOpens()
    {
        var window = CreateStartedWindow();
        var tab = window.WindowTabs.CreateTab();
        // As the start-up menu does when it loads
        tab.Gallery.Initialize(window.GallerySettings);

        tab.Gallery.DockAtCommand.Execute(GalleryDockPosition.Right);

        Assert.Equal(GalleryDockPosition.Right, window.WindowSettings.Gallery.DockPosition.Value);
        Assert.True(window.WindowSettings.Gallery.StartsDocked.Value);

        TabNavigationInitializer.Initialize(_core, window, tab);

        Assert.Equal(GalleryMode.Docked, tab.Gallery.Mode.Value);
        Assert.Equal(GalleryDockPosition.Right, tab.Gallery.Appearance.Value.Dock);
    }

    [Fact]
    public void ToggleDockedCommand_BeforeTheTabOpensASource_TogglesWhatItStartsWith()
    {
        var window = CreateStartedWindow();
        var tab = window.WindowTabs.CreateTab();
        // As the start-up menu does when it loads
        tab.Gallery.Initialize(window.GallerySettings);
        var startedDocked = window.WindowSettings.Gallery.StartsDocked.Value;

        tab.Gallery.ToggleDockedCommand.Execute(Unit.Default);

        Assert.Equal(!startedDocked, window.WindowSettings.Gallery.StartsDocked.Value);

        TabNavigationInitializer.Initialize(_core, window, tab);

        Assert.Equal(!startedDocked ? GalleryMode.Docked : GalleryMode.Closed, tab.Gallery.Mode.Value);
    }

    [Fact]
    public async Task MoveTab_ThenTheSourceWindowCloses_ADockByHandThrowsNothing()
    {
        var unhandled = new ConcurrentQueue<Exception>();
        using var recording = RecordUnhandled(unhandled);
        var source = CreateStartedWindow();
        var target = CreateStartedWindow();
        var moved = source.WindowTabs.CreateTab();
        ShowGallery(moved, GalleryMode.Closed);
        // Taken out for a new window that has not opened yet, so it still has its old window's settings
        var takenOut = source.WindowTabs.CreateTab();
        ShowGallery(takenOut, GalleryMode.Closed);
        source.WindowTabs.MoveOut(takenOut);
        source.WindowTabs.MoveTab(moved, target.WindowTabs);

        // As the window closes: its tabs first, then its settings
        source.WindowTabs.TearDown();
        source.WindowSettings.Dispose();
        var thrown = await Record.ExceptionAsync(async () =>
        {
            await takenOut.Gallery.Switcher.ToggleDocked();
            await takenOut.Gallery.Switcher.DockAt(GalleryDockPosition.Left);
            await moved.Gallery.Switcher.DockAt(GalleryDockPosition.Top);
        });

        Assert.Null(thrown);
        Assert.Empty(unhandled);
        Assert.Equal(GalleryMode.Docked, takenOut.Gallery.Mode.Value);
        // The moved tab writes to the window it is in now
        Assert.Equal(GalleryDockPosition.Top, target.WindowSettings.Gallery.DockPosition.Value);
        Assert.True(target.WindowSettings.Gallery.StartsDocked.Value);
        Assert.Equal(GalleryDockPosition.Top, moved.Gallery.Appearance.Value.Dock);
    }

    #endregion

    // A window whose gallery settings are started, as one that has opened
    private MainWindowViewModel CreateStartedWindow()
    {
        var window = CreateWindow();
        window.WindowSettings.Gallery.Start();
        return window;
    }

    // Closes the windows as a Main Window closes, its tabs first and then its settings, and waits until the gallery of
    // each tab has run what it queued, its cancel included, so the test leaves no work running on its folder
    private static async Task CloseAsync(params MainWindowViewModel[] windows)
    {
        var tabs = windows.SelectMany(static window => window.WindowTabs.Tabs.Value).ToArray();
        foreach (var window in windows)
        {
            window.WindowTabs.TearDown();
            window.WindowSettings.Dispose();
        }
        foreach (var tab in tabs)
        {
            if (tab.GalleryCoordinator is { } coordinator)
            {
                await coordinator.Idle;
            }
        }
    }

    private void ShowGallery(TabViewModel tab, GalleryMode mode)
    {
        tab.Gallery.Initialize(_core.GallerySettings);
        tab.Gallery.Switcher.Start(docked: mode is GalleryMode.Docked);
        if (mode is GalleryMode.Expanded)
        {
            tab.Gallery.Switcher.ToggleExpanded();
        }
    }

    private static bool IsLoaded(TabViewModel tab) =>
        tab.Gallery.LoadingState is GalleryLoadingState.Loaded && tab.Gallery.GalleryItems.Count == ByName.Length;

    private static ConcurrentQueue<NotifyCollectionChangedAction> RecordItemChanges(TabViewModel tab)
    {
        var changes = new ConcurrentQueue<NotifyCollectionChangedAction>();
        tab.Gallery.GalleryItems.CollectionChanged +=
            (in NotifyCollectionChangedEventArgs<GalleryItemViewModel> e) => changes.Enqueue(e.Action);
        return changes;
    }

    #endregion

    private static bool[] CheckMarksOf(TabOverviewViewModel tabs) =>
    [
        tabs.IsSortedByName.Value, tabs.IsSortedByFileSize.Value, tabs.IsSortedByCreationTime.Value,
        tabs.IsSortedByExtension.Value, tabs.IsSortedByLastAccessTime.Value, tabs.IsSortedByLastWriteTime.Value,
        tabs.IsSortedByRandomization.Value
    ];

    // Bounds each wait on a gate, so a failing test fails instead of hanging the run
    private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(10);

    // The tabs' services, which their Load lists the folder with in their window's order
    private void ComposeServices(IPlatformSpecificService? platform = null) =>
        _core.InitializeNavigation(TestTabServices.For(_core.SharedCache, platform: platform),
            new NavigationService());

    // Holds every name comparison while closed, so a test can change the order while a listing or a sort by name runs
    private sealed class GatedPlatform : TestTabServices.OrdinalPlatform, IDisposable
    {
        private readonly ManualResetEventSlim _open = new(true);
        private TaskCompletionSource _comparing = new(TaskCreationOptions.RunContinuationsAsynchronously);

        // Set once a comparison waits at the closed gate
        public Task Comparing => Volatile.Read(ref _comparing).Task;

        public void Close()
        {
            Volatile.Write(ref _comparing, new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
            _open.Reset();
        }

        public void Open() => _open.Set();

        public override int CompareStrings(string str1, string str2)
        {
            if (!_open.IsSet)
            {
                Volatile.Read(ref _comparing).TrySetResult();
                if (!_open.Wait(WaitLimit))
                {
                    throw new TimeoutException("The gate was never opened");
                }
            }

            return base.CompareStrings(str1, str2);
        }

        public void Dispose()
        {
            _open.Set();
            _open.Dispose();
        }
    }

    // Files whose order by size differs from their order by name: ByName and BySize, unless others are given
    private void CreateFiles(params (string Name, int Size)[] files)
    {
        Directory.CreateDirectory(_folder);
        if (files.Length is 0)
        {
            files = [("a.jpg", 30), ("b.jpg", 10), ("c.jpg", 20), ("d.jpg", 15)];
        }

        foreach (var (name, size) in files)
        {
            File.WriteAllBytes(PathOf(name), new byte[size]);
        }
    }

    private string PathOf(string name) => Path.Combine(_folder, name);

    // Holds the decode of the file named name until the test gives its model
    private TaskCompletionSource<ImageModel> HoldDecode(string name) =>
        _heldDecodes.GetOrAdd(name, static _ => new TaskCompletionSource<ImageModel>(
            TaskCreationOptions.RunContinuationsAsynchronously));

    private TaskCompletionSource DecodeRequestedOf(string name) =>
        _decodesRequested.GetOrAdd(name, static _ => new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously));

    private async Task OpenAsync(TabViewModel tab, string name)
    {
        var result = await tab.Load.LoadAsync(PathOf(name));
        Assert.True(result.IsLoaded);
    }

    private static string[] NamesOf(TabViewModel tab) => tab.ImageIterator!.Files.Select(f => f.Name).ToArray();

    private static string ListedAtShownIndex(TabViewModel tab) =>
        tab.ImageIterator!.Files[tab.ImageIterator.CurrentIndex].Name;

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
        while (!condition())
        {
            Assert.True(DateTime.UtcNow < deadline, "Timed out waiting");
            await Task.Delay(20, TestContext.Current.CancellationToken);
        }
    }

    private MainWindowViewModel CreateWindow(ITabHookups? hookups = null, TabViewModel? firstTab = null,
        ITabViewFactory? viewFactory = null, WindowSettings? windowSettings = null)
    {
        var vm = new MainWindowViewModel(_core, _core.Translation, _core.GlobalSettings!, _core.GallerySettings,
            firstTab, windowSettings);
        if (hookups is not null)
        {
            vm.WindowTabs.SetHookups(hookups);
        }
        if (viewFactory is not null)
        {
            vm.WindowTabs.SetViewFactory(viewFactory);
        }
        return vm;
    }

    private sealed class RecordingViewFactory : ITabViewFactory
    {
        public List<TabViewModel> Attached { get; } = [];

        public ITabViews Attach(TabViewModel tab)
        {
            Attached.Add(tab);
            return new FakeViews();
        }
    }

    private sealed class FakeViews : ITabViews
    {
        public BindableReactiveProperty<object?> Control { get; } = new();
        public IReadOnlyBindableReactiveProperty<object?> View => Control;
        public bool IsDisposed { get; private set; }
        public void Dispose() => IsDisposed = true;
    }

    private sealed class RecordingHookups : ITabHookups
    {
        public List<TabViewModel> Attached { get; } = [];
        public List<TabViewModel> Detached { get; } = [];

        public IDisposable Attach(TabViewModel tab)
        {
            Attached.Add(tab);
            return new Detaching(() => Detached.Add(tab));
        }

        private sealed class Detaching(Action detach) : IDisposable
        {
            public void Dispose() => detach();
        }
    }

    private sealed class RecordingFileWatcher : IFileWatcherService
    {
        public List<TabViewModel> Unwatched { get; } = [];
        public void Watch(TabViewModel tab, string? directory = null) { }
        public void Unwatch(TabViewModel tab) => Unwatched.Add(tab);
        public void RestartWatches() { }
    }

}
