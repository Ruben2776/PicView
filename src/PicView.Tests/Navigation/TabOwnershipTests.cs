using System.Collections.Concurrent;
using ImageMagick;
using PicView.Core.FileSorting;
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
                return new ValueTask<ImageModel>(held.Task);
            }

            return ValueTask.FromResult(new ImageModel { FileInfo = file });
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
        _core.Services!.GlobalSettings.IsSideBySide.Value = true;
        CreateFiles();
        var window = CreateWindow();
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
        _core.Services!.GlobalSettings.IsSideBySide.Value = true;
        CreateFiles();
        var window = CreateWindow();
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
        _core.Services!.GlobalSettings.IsSideBySide.Value = true;
        // By name a, b, c, d; by size c, a, b, d, so the shown pair a, b is still a pair, and the step's c is first
        CreateFiles(("a.jpg", 10), ("b.jpg", 20), ("c.jpg", 5), ("d.jpg", 30));
        var window = CreateWindow();
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
