# Gallery Opens Centered Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** In a folder of more than 200 images, the Docked Gallery opens centered on the current image, and its startup work goes to the thumbnails around that image first.

**Architecture:**
- **Core.** `GalleryCoordinator` makes every Gallery Item at once. It reads the details of the current file and the 20 files on each side before adding the items, then reads the rest in batches of 20, nearest the current image first.
  - Details go onto the existing items, as one `GalleryFileDetails` reference each.
  - After each batch, the coordinator raises `GalleryViewModel.SizesChanged`.
- **Panel.** `VirtualizingGallery` follows the current item until the user scrolls, then holds the item in the middle of the view in place.
  - It gets its bounds and offsets from a plain `GalleryLayout`.
  - It creates containers outward from the item it follows or holds, and sets the scroll offset after the layout pass.
- **View.** `ImageViewer` takes its tab and hands it to `GalleryView.Initialize`, which subscribes once.

**Tech Stack:** C# preview / .NET 11, Avalonia 12.1.4, R3, ObservableCollections 3.3.4, xUnit.v3, Avalonia.Headless.XUnit.

**Spec:** `docs/superpowers/specs/2026-10-30-gallery-opens-centered-design.md`, and `docs/adr/0010-gallery-follows-its-tabs-file-list.md` for the decision it changes. Mirror the shape of `docs/superpowers/plans/2026-10-30-gallery-mode-switcher.md`.

## Global Constraints

- Code paths below are relative to `src/`. Run `dotnet` from `src/` and `git` from the repository root.
- Work on a branch off `dev`, for example `gallery-opens-centered`. `dev` is the default branch, so never commit to it.
- The first commit also adds the spec and this plan, which are not committed yet.
- `PicView.Core` stays AOT-compatible: no reflection.
- Code style:
  - file-scoped namespaces,
  - braces on every `if` and loop,
  - `_camelCase` private fields,
  - test names in the form `MethodOrScenario_Condition_ExpectedResult`.
- Test classes that touch `Settings` use `[Collection("Sequential")]` and call `SetDefaults()` in their constructor. Headless test classes use `[Collection("Sequential")]`.
- Never schedule polling or timeout timers around `dotnet test`. Run it in the background and wait for its completion notice (AGENTS.md).
- Commit messages are an imperative sentence, as in the repository's history, and end with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- Values from the spec: 20 neighbors on each side (41 files), batches of 20, and a 1-pixel tolerance for telling the panel's own offset changes apart.
- Every change to the Gallery Items, and every details write to an item already added, runs on the coordinator's queue.
- A view's DataContext is set once, and nothing handles `DataContextChanged`.
- Out of scope (spec):
  - cancelling decodes that have already started,
  - unsubscribing a `GalleryView` whose `ImageViewer` is replaced,
  - the `Dispatcher` wrappers in `GalleryAnimationControl`,
  - a better width estimate than a square,
  - size reads that follow the scroll position,
  - `MainWindow.RefreshViewBinding`.

**Decisions this plan adds to the spec** (to confirm with the user during review):

- **A `Starting` loading state.** It covers the time between a load starting and its items being added. Any change to the file list during that time starts the load again, because there are no items to apply it to yet.
- **Orientation and expansion changes.** The panel goes back to following when its orientation or `IsExpanded` changes, because a held distance belongs to the old scroll axis.
- **Which item gets the first container.** Containers are created outward from the current item while following, even when the view is clamped at an end of the strip, so the current thumbnail is always decoded first.
- **Ties when picking the next items to read.** When two unread items are equally near the current image, the one after it is read first.

## Review Focus

1. **Docking at another edge, or expanding, after the user scrolled.** The strip should center the current image in its new layout. Test in Task 6, Step 1.
2. **Resizing the window while following.** The current item should stay centered. Test in Task 6, Step 1.
3. **A folder change after the user scrolled.** The new folder's strip should center its current image. Test in Task 6, Step 1.
4. **Deleting the item the strip is holding.** The strip should not jump. Test in Task 6, Step 1.
5. **Pasting several files into new tabs.** Each new tab's gallery should show its own tab's items, even though it's set up before its tab is selected. Test in Task 7, Step 1.

---

### Task 1: Prove that an offset set after a layout pass is laid out before anything else runs

**Files:**
- Create: `PicView.Tests/Gallery/ScrollOffsetTimingTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: nothing. Task 6, Step 4 relies on what this proves.

- [ ] **Step 1: Write the test**

```csharp
/// <summary>
/// Pins the Avalonia behavior the gallery panel's centering relies on: an offset set when a layout pass completes is
/// laid out before any other queued work, so the frame committed after that layout already shows it.
/// </summary>
[Collection("Sequential")]
public class ScrollOffsetTimingTests
{
    [AvaloniaFact]
    public void OffsetSetWhenALayoutPassCompletes_IsLaidOutBeforeOtherQueuedWork()
    {
        var content = new Border { Width = 4000, Height = 50 };
        var scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = content
        };
        var window = new Window { Width = 200, Height = 50, Content = scrollViewer };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var offsetSet = false;
        var passesAfterOffset = 0;
        bool? laidOutFirst = null;
        scrollViewer.LayoutUpdated += (_, _) =>
        {
            if (offsetSet)
            {
                passesAfterOffset++;
                return;
            }
            offsetSet = true;
            scrollViewer.SetCurrentValue(ScrollViewer.OffsetProperty, new Vector(500, 0));
            Dispatcher.UIThread.Post(() => laidOutFirst = passesAfterOffset > 0, DispatcherPriority.Send);
        };
        content.Width = 5000;
        Dispatcher.UIThread.RunJobs();

        Assert.True(laidOutFirst);
        Assert.Equal(new Vector(500, 0), scrollViewer.Offset);
        window.Close();
    }
}
```

- [ ] **Step 2: Run it**

Run: `dotnet test PicView.Tests\PicView.Tests.csproj --filter "FullyQualifiedName~PicView.Tests.Gallery.ScrollOffsetTimingTests"`
Expected: PASS.

**If it fails, stop and tell the user.** A failure means Avalonia lays the new offset out in a later job, and a frame can render between the two layouts. Task 6, Step 4 then needs another way to apply the offset, and nothing should be built on it until the user agrees on one.

- [ ] **Step 3: Commit**

```bash
git add src/PicView.Tests/Gallery/ScrollOffsetTimingTests.cs docs/superpowers/specs/2026-10-30-gallery-opens-centered-design.md docs/superpowers/plans/2026-10-30-gallery-opens-centered.md
git commit -m "Pin when Avalonia lays out an offset set after a layout pass"
```

---

### Task 2: Gallery items hold their file's details as one reference

**Files:**
- Modify: `PicView.Core/Gallery/GalleryFileDetails.cs`
- Modify: `PicView.Core/ViewModels/GalleryItemViewModel.cs:29-30`
- Modify: `PicView.Core/Gallery/GalleryCoordinator.cs:198-209` (`Relocate`) and `:412-468` (`CreateItem`)
- Modify: `PicView.Avalonia/CustomControls/VirtualizingGallery.cs:271-275`
- Modify: `PicView.Tests/Gallery/GalleryFollowsFilesTests.cs:241-242`
- Test: `PicView.Tests/Gallery/GalleryItemDetailsTests.cs` (new)

**Interfaces:**
- Produces:
  - `public sealed record GalleryFileDetails(uint PixelWidth, uint PixelHeight, string? XmpPacket)`, with `public static GalleryFileDetails Unreadable { get; }` (0 × 0, no XMP packet). `Read(FileInfo)` is unchanged.
  - `GalleryItemViewModel.Details`: `public GalleryFileDetails? Details { get; }`, null until read, read with `Volatile.Read`.
  - `GalleryItemViewModel.SetDetails`: `internal void SetDetails(GalleryFileDetails details)` writes with `Volatile.Write`, then sets `ImageSize.Value = StringExtensions.CombineSize(details.PixelWidth, details.PixelHeight)`.
  - `PixelWidth` and `PixelHeight` are removed from `GalleryItemViewModel`.

- [ ] **Step 1: Write the failing test**

```csharp
public class GalleryItemDetailsTests
{
    [Fact]
    public void SetDetails_NewDetails_ReplacesThemAndTheSizeText()
    {
        var item = new GalleryItemViewModel();
        var details = new GalleryFileDetails(300, 200, null);

        item.SetDetails(details);

        Assert.Same(details, item.Details);
        Assert.Equal(StringExtensions.CombineSize(300, 200), item.ImageSize.Value);
    }
}
```

- [ ] **Step 2: Run it to verify it fails**

Run: `dotnet test PicView.Tests\PicView.Tests.csproj --filter "FullyQualifiedName~PicView.Tests.Gallery.GalleryItemDetailsTests"`
Expected: the build fails, because `GalleryItemViewModel` has no `SetDetails` or `Details`.

- [ ] **Step 3: Make `GalleryFileDetails` a sealed record class with `Unreadable`, and add `Details` and `SetDetails` to `GalleryItemViewModel`**

Then move the remaining callers onto `Details`:

- **`CreateItem(file, details, maxHeight)`** sets its texts from `GetThumbData` as before, passing `details?.PixelWidth ?? 0` and `details?.PixelHeight ?? 0`, then calls `SetDetails` when it has details.
  - Its thumbnail loader stops capturing `xmpPacket`. When it runs, it passes `item.Details?.XmpPacket` to `MotionPhotoDetector.TryDetect`.
- **`Relocate`** passes `old.Details` to `CreateItem`.
- **`VirtualizingGallery.CalculateBounds`** reads `Details` once per item: `item is GalleryItemViewModel { Details: { PixelHeight: > 0 } details }`.
- **The test at `GalleryFollowsFilesTests.cs:241-242`** waits for `GalleryItems[1].Details?.PixelWidth == 3u` and asserts `Details!.PixelHeight`.

- [ ] **Step 4: Run the gallery tests**

Run: `dotnet test PicView.Tests\PicView.Tests.csproj --filter "FullyQualifiedName~PicView.Tests.Gallery"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/PicView.Core src/PicView.Avalonia/CustomControls/VirtualizingGallery.cs src/PicView.Tests/Gallery
git commit -m "Keep a gallery item's file details as one reference"
```

---

### Task 3: The coordinator adds every item at once and reads sizes outward from the current image

**Files:**
- Modify: `PicView.Core/Navigation/ImageIterator.cs:37`
- Modify: `PicView.Core/Gallery/GalleryLoadingState.cs`
- Modify: `PicView.Core/ViewModels/GalleryViewModel.cs`
- Modify: `PicView.Core/Gallery/GalleryCoordinator.cs` (`Enqueue`, `FilesChanged`, `Listed`, `OnShownChanged`, `StartLoad`, `Load`)
- Test: `PicView.Tests/Gallery/GalleryFollowsFilesTests.cs`

**Interfaces:**
- Consumes: `GalleryItemViewModel.Details` and `SetDetails`, and `GalleryFileDetails.Unreadable` (Task 2).
- Produces:
  - **`GalleryLoadingState`** becomes `NotLoaded, Starting, Loading, Loaded`.
    - `Starting`: the load is reading the neighborhood, and no items are added yet.
    - `Loading`: every item is in, and sizes are still being read.
    - `Loaded`: every item's details have been read.
  - **`GalleryViewModel.SizesChanged`**: `public Observable<Unit> SizesChanged { get; }`, backed by a `Subject<Unit>` that `Dispose` disposes. The coordinator raises it with `internal void RaiseSizesChanged()`.
  - **`GalleryCoordinator.ReadDetails`**: `internal Func<FileInfo, GalleryFileDetails?> ReadDetails { get; set; } = GalleryFileDetails.Read;`. This is the spec's details-reader parameter. It's a property because `TabViewModel.Initialize` builds the coordinator.
  - **The `Listed` change** carries the iterator's `CurrentIndex` as `FileListChange.Index`.

- [ ] **Step 1: Write the failing tests**

Add these helpers to `GalleryFollowsFilesTests`:

```csharp
private static string[] Names(int start, int count) =>
    Enumerable.Range(start, count).Select(i => $"{i:D3}.jpg").ToArray();

private static RecordingDetailsReader UseRecordingReader(TabViewModel tab)
{
    var reader = new RecordingDetailsReader();
    tab.GalleryCoordinator!.ReadDetails = reader.Read;
    return reader;
}

/// <summary>Each change to the tab's Gallery Items: its action and how many items it added.</summary>
private static List<(NotifyCollectionChangedAction Action, int Added)> RecordChanges(TabViewModel tab)
{
    var changes = new List<(NotifyCollectionChangedAction, int)>();
    tab.Gallery.GalleryItems.CollectionChanged += (in NotifyCollectionChangedEventArgs<GalleryItemViewModel> e) =>
    {
        var added = e.Action is NotifyCollectionChangedAction.Add ? (e.IsSingleItem ? 1 : e.NewItems.Length) : 0;
        lock (changes)
        {
            changes.Add((e.Action, added));
        }
    };
    return changes;
}

/// <summary>
/// Stands in for reading files. It records each read in order and gives every file 200 × 100 unless told otherwise.
/// Once paused, it holds every read after the first <c>afterReads</c> until released.
/// </summary>
private sealed class RecordingDetailsReader
{
    private readonly Lock _lock = new();
    private readonly List<string> _reads = [];
    private readonly ManualResetEventSlim _released = new(true);
    private int _started;
    private int _readsBeforePause = int.MaxValue;

    public Func<string, GalleryFileDetails?> DetailsOf { get; set; } = _ => new GalleryFileDetails(200, 100, null);

    /// <summary>How many reads have begun, held ones included.</summary>
    public int Started => Volatile.Read(ref _started);

    public string[] Reads
    {
        get
        {
            lock (_lock)
            {
                return [.. _reads];
            }
        }
    }

    public void Pause(int afterReads)
    {
        _readsBeforePause = afterReads;
        _released.Reset();
    }

    public void Release() => _released.Set();

    public GalleryFileDetails? Read(FileInfo file)
    {
        if (Interlocked.Increment(ref _started) > _readsBeforePause)
        {
            _released.Wait();
        }
        lock (_lock)
        {
            _reads.Add(file.Name);
        }
        return DetailsOf(file.Name);
    }
}
```

Then add the tests:

```csharp
[Fact]
public async Task FilesListed_TheChangeCarriesTheCurrentIndex()
{
    var changes = new List<FileListChange>();
    using var iterator = new ImageIterator(_cache, _thumbnails, new MockThumbnailLoader(), CreateTab(), changes.Add);

    iterator.Initialize(await FilesAsync("a.jpg", "b.jpg", "c.jpg"), 2);

    Assert.Equal(2, Assert.Single(changes).Index);
}

[Fact]
public async Task FilesListed_CurrentImageFarAlong_AddsEveryItemAtOnceAndReadsOutwardFromIt()
{
    var tab = CreateTab();
    var reader = UseRecordingReader(tab);
    var changes = RecordChanges(tab);
    Show(tab);

    tab.ImageIterator.Initialize(await FilesAsync(Names(0, 200)), 150);
    await WaitUntilLoadedAsync(tab, Names(0, 200));

    Assert.Equal((NotifyCollectionChangedAction.Add, 200), changes[0]);
    var reads = reader.Reads;
    Assert.Equal(200, reads.Distinct().Count());
    Assert.Equal(Names(130, 41).ToHashSet(), reads[..41].ToHashSet());
    Assert.Equal(Names(120, 10).Concat(Names(171, 10)).ToHashSet(), reads[41..61].ToHashSet());
    Assert.Equal(Names(99, 11).Concat(Names(191, 9)).ToHashSet(), reads[81..101].ToHashSet());
}

[Fact]
public async Task CurrentImageChanged_WhileSizesAreRead_ReadsAroundTheNewOne()
{
    var tab = CreateTab();
    var reader = UseRecordingReader(tab);
    reader.Pause(afterReads: 61);
    Show(tab);

    tab.ImageIterator.Initialize(await FilesAsync(Names(0, 200)), 150);
    // A held read means the batch after the first 61 reads was already chosen around image 150
    await WaitUntilAsync(() => reader.Started > 61);
    tab.NavigationIndex.Value = 10;
    reader.Release();

    await WaitUntilLoadedAsync(tab, Names(0, 200));
    // Reads 62-81 were chosen before the change; the batch after them is around image 10
    Assert.Equal(Names(1, 20).ToHashSet(), reader.Reads[81..101].ToHashSet());
}

[Fact]
public async Task SizesRead_WhileLoading_AreWrittenOntoTheSameItemsOneSignalPerBatch()
{
    var tab = CreateTab();
    UseRecordingReader(tab);
    var changes = RecordChanges(tab);
    var signals = 0;
    using var subscription = tab.Gallery.SizesChanged.Subscribe(_ => Interlocked.Increment(ref signals));
    Show(tab);

    tab.ImageIterator.Initialize(await FilesAsync(Names(0, 200)), 150);
    await WaitUntilAsync(() => tab.Gallery.GalleryItems.Count == 200);
    var items = tab.Gallery.GalleryItems.ToArray();
    await WaitUntilLoadedAsync(tab, Names(0, 200));

    Assert.Equal(items, tab.Gallery.GalleryItems);
    Assert.All(items, item => Assert.Equal(200u, item.Details!.PixelWidth));
    Assert.DoesNotContain(changes, change => change.Action is NotifyCollectionChangedAction.Replace);
    Assert.Equal(8, signals); // 159 items after the neighborhood, in batches of 20
}

[Fact]
public async Task FileWithoutReadableDetails_IsReadOnceAndStaysSquare()
{
    var tab = CreateTab();
    var reader = UseRecordingReader(tab);
    reader.DetailsOf = name => name == "045.jpg" ? null : new GalleryFileDetails(200, 100, null);
    Show(tab);

    tab.ImageIterator.Initialize(await FilesAsync(Names(0, 50)));
    await WaitUntilLoadedAsync(tab, Names(0, 50));

    Assert.Equal(1, reader.Reads.Count(name => name == "045.jpg"));
    Assert.Same(GalleryFileDetails.Unreadable, tab.Gallery.GalleryItems[45].Details);
}
```

- [ ] **Step 2: Run them to verify they fail**

Run: `dotnet test PicView.Tests\PicView.Tests.csproj --filter "FullyQualifiedName~PicView.Tests.Gallery.GalleryFollowsFilesTests"`
Expected: the build fails, because `ReadDetails` and `SizesChanged` don't exist.

- [ ] **Step 3: Add `Starting` and `SizesChanged`, and pass `Index: CurrentIndex` on the `Listed` change in `ImageIterator.Initialize`**

- [ ] **Step 4: Rework the load in `GalleryCoordinator`**

- **The queue.** Change `Enqueue(Action)` to return its continuation as a `Task`, and add `private Task<T?> Enqueue<T>(Func<T> work)`. Both log and swallow exceptions as today, so awaiting them never throws.
- **Starting a load.** `StartLoad(int currentIndex)` cancels the old load, clears the items, sets `Starting`, and runs `LoadAsync(files, currentIndex, ct)` on the thread pool. The callers pass:
  - `Listed` (now `Listed(files, change.Index)`): the change's index,
  - `OnShownChanged`: `_tab.NavigationIndex.CurrentValue`,
  - `FilesChanged` during `Starting` or `Loading`: `_tab.NavigationIndex.CurrentValue`. `Loading` keeps today's restart until Task 4. `Starting` always restarts.
- **`LoadAsync`** runs in this order:
  1. Make every item with `CreateItem(file, null, maxHeight)`.
  2. Clamp the index to the list. Read the details of that file and the 20 on each side in parallel through `ReadDetails`, then call `SetDetails` with each result, or `Unreadable` for null. These items aren't shared yet, so this step doesn't need the queue.
  3. On the queue: if the load wasn't canceled, `AddRange` the items and set `Loading`.
  4. Repeat these batch steps until a batch comes back empty:
     - On the queue, pick the batch with `NextUnread(BatchSize)`.
     - Read its details in parallel with `ParallelOptions { CancellationToken = ct, MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) }`.
     - On the queue, if the load wasn't canceled, `SetDetails` each item and call `Gallery.RaiseSizesChanged()` once.
  5. On the queue, if the load wasn't canceled, set `Loaded`.

  Constants: `NeighborsEachSide = 20`, `BatchSize = 20`. Like `Load` today, `LoadAsync` ignores `OperationCanceledException` and logs any other exception with `DebugHelper.LogDebug`. After a failure the waiting items stay square, and the state stays `Loading`.
- **`NextUnread(int count)`** runs on the queue. Starting from `center = Math.Clamp(_tab.NavigationIndex.CurrentValue, 0, Count - 1)`, it walks outward: center, center + 1, center − 1, center + 2, center − 2, and so on. It collects items whose `Details` is null until it has `count` of them or has passed both ends.

- [ ] **Step 5: Run the gallery tests**

Run: `dotnet test PicView.Tests\PicView.Tests.csproj --filter "FullyQualifiedName~PicView.Tests.Gallery"`
Expected: PASS, including the old `..._WhileLoading_...` tests, which still restart until Task 4.

- [ ] **Step 6: Commit**

```bash
git add src/PicView.Core src/PicView.Tests/Gallery/GalleryFollowsFilesTests.cs
git commit -m "Add every gallery item at once and read sizes outward from the current image"
```

---

### Task 4: Sorts and edits during a load apply to the items

**Files:**
- Modify: `PicView.Core/Gallery/GalleryCoordinator.cs` (`FilesChanged`, `Listed`, `FillDetails`)
- Modify: `docs/adr/0010-gallery-follows-its-tabs-file-list.md:12`
- Test: `PicView.Tests/Gallery/GalleryFollowsFilesTests.cs`

**Interfaces:**
- Consumes: `RecordingDetailsReader`, `Names` and `UseRecordingReader` (Task 3).
- Produces: no new names.

- [ ] **Step 1: Write the failing tests**

Replace `SameFilesInAnotherOrder_WhileLoading_LoadsThemAgainInTheNewOrder` and `FileInserted_WhileLoading_LoadsAgainWithTheFile`, and change `FileInserted_Loaded_ReadsItsDetailsFromTheChange` to the third test below:

```csharp
[Fact]
public async Task SameFilesInAnotherOrder_WhileLoading_SortsTheItems()
{
    var tab = CreateTab();
    var reader = UseRecordingReader(tab);
    reader.Pause(afterReads: 21); // the first file and the 20 after it
    Show(tab);
    var files = await FilesAsync(Names(0, 300));
    tab.ImageIterator.Initialize(files);
    await WaitUntilAsync(() => tab.Gallery.GalleryItems.Count == 300);
    var items = tab.Gallery.GalleryItems.ToArray();

    files.Reverse();
    tab.ImageIterator.Initialize(files);
    await IdleAsync(tab);
    reader.Release();

    await WaitUntilLoadedAsync(tab, files.Select(file => file.Name).ToArray());
    // Enumerable.Reverse: array.Reverse() binds to the in-place span overload in C# 14
    Assert.Equal(Enumerable.Reverse(items), tab.Gallery.GalleryItems);
}

[Fact]
public async Task FileInserted_WhileLoading_InsertsItsItem()
{
    var tab = CreateTab();
    var reader = UseRecordingReader(tab);
    reader.Pause(afterReads: 21);
    Show(tab);
    var files = await FilesAsync(Names(0, 300));
    var inserted = (await FilesAsync("150a.jpg"))[0];
    tab.ImageIterator.Initialize(files);
    await WaitUntilAsync(() => tab.Gallery.GalleryItems.Count == 300);
    var items = tab.Gallery.GalleryItems.ToArray();

    tab.ImageIterator.Insert(inserted, string.CompareOrdinal);
    await IdleAsync(tab);
    reader.Release();

    await WaitUntilLoadedAsync(tab, tab.ImageIterator.Files.Select(file => file.Name).ToArray());
    Assert.Equal(301, tab.Gallery.GalleryItems.Count);
    Assert.Equal(items, tab.Gallery.GalleryItems.Where(item => item.FileInfo!.Name != "150a.jpg"));
}

[Fact]
public async Task FileInserted_Loaded_WritesTheChangesDetailsOntoItsItem()
{
    // Arrange as in the old FileInserted_Loaded_ReadsItsDetailsFromTheChange: a loaded "a.jpg", and a real 3 × 2 "b.png"
    tab.ImageIterator.Insert(file, string.CompareOrdinal, new FileDetailsSource(file, waitUntilReady: false));
    await IdleAsync(tab);
    var item = tab.Gallery.GalleryItems[1];

    await WaitUntilAsync(() => item.Details is not null);
    Assert.Same(item, tab.Gallery.GalleryItems[1]);
    Assert.Equal((3u, 2u), (item.Details!.PixelWidth, item.Details.PixelHeight));
}

[Fact]
public async Task FileInserted_BeforeTheItemsAreAdded_LoadsAgainWithTheFile()
{
    var tab = CreateTab();
    var reader = UseRecordingReader(tab);
    reader.Pause(afterReads: 0);
    Show(tab);
    var files = await FilesAsync(Names(0, 30));
    var inserted = (await FilesAsync("015a.jpg"))[0];
    tab.ImageIterator.Initialize(files);
    await WaitUntilAsync(() => tab.Gallery.LoadingState is GalleryLoadingState.Starting);

    tab.ImageIterator.Insert(inserted, string.CompareOrdinal);
    await IdleAsync(tab);
    reader.Release();

    await WaitUntilLoadedAsync(tab, tab.ImageIterator.Files.Select(file => file.Name).ToArray());
    Assert.Equal(31, tab.Gallery.GalleryItems.Count);
}
```

- [ ] **Step 2: Run them to verify they fail**

Run: `dotnet test PicView.Tests\PicView.Tests.csproj --filter "FullyQualifiedName~PicView.Tests.Gallery.GalleryFollowsFilesTests"`
Expected:
- `SortsTheItems` fails because the items are new objects.
- `InsertsItsItem` fails because the items are new objects.
- `WritesTheChangesDetailsOntoItsItem` fails on `Assert.Same`.
- `BeforeTheItemsAreAdded` passes already; it pins Task 3's restart.

- [ ] **Step 3: Apply changes during a load in `GalleryCoordinator`**

- **`FilesChanged`:** `Starting` → `StartLoad(_tab.NavigationIndex.CurrentValue)`. `Loading` or `Loaded` → `Edit(change)`. The `Loading when Replaced` case goes away.
- **`Listed`:** sorts with `SortItems` when the state is `Loading` or `Loaded` and the files are the same.
- **`FillDetails(item, file, source)`:**
  - It reads through `source.GetAsync()` or `ReadDetails(file)`.
  - On the queue, it calls `item.SetDetails(details ?? GalleryFileDetails.Unreadable)` and `Gallery.RaiseSizesChanged()`, instead of swapping in a new item.
  - Update its doc comment.
- **`Replace`** still builds a new item, then calls `FillDetails`.
- **The class summary and `FilesChanged`'s doc comment** say a loading gallery edits its items like a loaded one, and a load that hasn't added its items yet starts again.

- [ ] **Step 4: Update ADR-0010**

In `docs/adr/0010-gallery-follows-its-tabs-file-list.md:12`, replace "A sort during a load restarts the load in the new order." with:

> A sort or an edit during a load applies to the items, and the load keeps reading sizes outward from the current image.

- [ ] **Step 5: Run the gallery tests**

Run: `dotnet test PicView.Tests\PicView.Tests.csproj --filter "FullyQualifiedName~PicView.Tests.Gallery"`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add src/PicView.Core/Gallery/GalleryCoordinator.cs src/PicView.Tests/Gallery/GalleryFollowsFilesTests.cs docs/adr/0010-gallery-follows-its-tabs-file-list.md
git commit -m "Apply sorts and edits during a gallery load instead of restarting it"
```

---

### Task 5: `GalleryLayout` computes the bounds and the offsets that center or hold an item

**Files:**
- Create: `PicView.Avalonia/CustomControls/GalleryLayout.cs`
- Test: `PicView.Tests/Gallery/GalleryLayoutTests.cs`

**Interfaces:**
- Consumes: `GalleryItemViewModel.Details` and `SetDetails` (Task 2).
- Produces:

```csharp
public readonly record struct GalleryLayoutOptions(Orientation Orientation, bool IsExpanded, double ItemHeight,
    double ItemWidth, double ItemSpacing, double LineSpacing, double WrapHeightOverride);

/// Where each Gallery Item sits, and the offsets along the scroll axis that center or hold one.
public sealed class GalleryLayout
{
    public IReadOnlyList<Rect> Bounds { get; }
    public Size Extent { get; }
    public bool ScrollsVertically { get; }        // !IsExpanded && Orientation is Vertical
    public void Calculate(IReadOnlyList<object?> items, GalleryLayoutOptions options, Size availableSize);
    public void Clear();
    public double Middle(int index);              // the item's center along the scroll axis
    public double CenterOffset(int index, double viewportLength);
    public double HoldOffset(int index, double distanceFromMiddle, double viewportLength);
    public int IndexNearest(double position);     // -1 without bounds
    public (int First, int Last) IndicesIn(Rect area); // (-1, -1) when none intersect
}
```

- **`Calculate`** is `VirtualizingGallery.CalculateBounds` moved here unchanged, including Task 2's single read of `Details`. Its return value becomes `Extent`.
- **The offset methods** clamp to `[0, max(0, extent − viewportLength)]` along the scroll axis.
  - `CenterOffset` returns `Middle(index) − viewportLength / 2`, clamped.
  - `HoldOffset` returns `Middle(index) − distanceFromMiddle − viewportLength / 2`, clamped.
- **`IndexNearest`** uses a binary search, because `Middle` never decreases in index order. On a tie, it returns the lower index.
- **`IndicesIn`** is the scan from `MeasureOverride`'s step 3, early exit included.

- [ ] **Step 1: Write the failing tests**

```csharp
public class GalleryLayoutTests
{
    private static readonly GalleryLayoutOptions Docked =
        new(Orientation.Horizontal, IsExpanded: false, ItemHeight: 100, ItemWidth: double.NaN, 0, 0, double.NaN);

    private static GalleryItemViewModel Item(uint width, uint height)
    {
        var item = new GalleryItemViewModel();
        item.SetDetails(new GalleryFileDetails(width, height, null));
        return item;
    }

    private static object?[] Squares(int count) => Enumerable.Range(0, count).Select(_ => (object?)Item(100, 100)).ToArray();

    [Fact]
    public void Calculate_HorizontalDocked_PlacesItemsSideBySideAtTheirAspectRatio()
    {
        var layout = new GalleryLayout();
        layout.Calculate([Item(200, 100), Item(100, 100), new GalleryItemViewModel()], Docked, new Size(1000, 100));
        Assert.Equal([new Rect(0, 0, 200, 100), new Rect(200, 0, 100, 100), new Rect(300, 0, 100, 100)], layout.Bounds);
        Assert.Equal(new Size(400, 100), layout.Extent);
    }

    [Fact]
    public void Calculate_VerticalDocked_StacksItemsAtTheFullWidth()
    {
        var layout = new GalleryLayout();
        layout.Calculate(Squares(2), Docked with { Orientation = Orientation.Vertical }, new Size(150, 1000));
        Assert.Equal([new Rect(0, 0, 150, 100), new Rect(0, 100, 150, 100)], layout.Bounds);
        Assert.True(layout.ScrollsVertically);
    }

    [Fact]
    public void Calculate_Expanded_WrapsIntoColumnsAsWideAsTheirWidestItem()
    {
        var layout = new GalleryLayout();
        layout.Calculate([Item(200, 100), Item(100, 100), Item(100, 100)],
            Docked with { IsExpanded = true, ItemSpacing = 10, LineSpacing = 5 }, new Size(1000, 250));
        Assert.Equal([new Rect(0, 0, 200, 100), new Rect(0, 110, 200, 100), new Rect(205, 0, 100, 100)], layout.Bounds);
    }

    [Theory]
    [InlineData(5, 400)] // middle 550
    [InlineData(0, 0)]   // clamped at the start
    [InlineData(9, 700)] // clamped at the end: 1000 − 300
    public void CenterOffset_TenSquaresInA300View_CentersOrClamps(int index, double expected)
    {
        var layout = new GalleryLayout();
        layout.Calculate(Squares(10), Docked, new Size(300, 100));
        Assert.Equal(expected, layout.CenterOffset(index, 300));
    }

    [Fact]
    public void CenterOffset_StripShorterThanTheView_IsZero()
    {
        var layout = new GalleryLayout();
        layout.Calculate(Squares(2), Docked, new Size(300, 100));
        Assert.Equal(0, layout.CenterOffset(1, 300));
    }

    [Fact]
    public void HoldOffset_WidthsChangeToTheLeft_MovesByTheSameAmount()
    {
        var items = Squares(10);
        var layout = new GalleryLayout();
        layout.Calculate(items, Docked, new Size(300, 100));
        foreach (var item in items[..5])
        {
            ((GalleryItemViewModel)item!).SetDetails(new GalleryFileDetails(200, 100, null));
        }
        layout.Calculate(items, Docked, new Size(300, 100));
        Assert.Equal(900, layout.HoldOffset(5, 0, 300)); // was 400, plus five items 100 wider
    }

    [Fact]
    public void HoldOffset_WidthsChangeToTheRight_KeepsTheOffset()
    {
        var items = Squares(10);
        var layout = new GalleryLayout();
        foreach (var item in items[6..])
        {
            ((GalleryItemViewModel)item!).SetDetails(new GalleryFileDetails(200, 100, null));
        }
        layout.Calculate(items, Docked, new Size(300, 100));
        Assert.Equal(400, layout.HoldOffset(5, 0, 300));
    }

    [Theory]
    [InlineData(550, 5)]
    [InlineData(-50, 0)]
    [InlineData(5000, 9)]
    public void IndexNearest_TenSquares_PicksTheItemWhoseMiddleIsNearest(double position, int expected)
    {
        var layout = new GalleryLayout();
        layout.Calculate(Squares(10), Docked, new Size(300, 100));
        Assert.Equal(expected, layout.IndexNearest(position));
    }
}
```

- [ ] **Step 2: Run them to verify they fail**

Run: `dotnet test PicView.Tests\PicView.Tests.csproj --filter "FullyQualifiedName~PicView.Tests.Gallery.GalleryLayoutTests"`
Expected: the build fails, because `GalleryLayout` doesn't exist.

- [ ] **Step 3: Implement `GalleryLayout` and `GalleryLayoutOptions` in `PicView.Avalonia/CustomControls/GalleryLayout.cs`**

- [ ] **Step 4: Run them to verify they pass**

Run: the command from Step 2.
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/PicView.Avalonia/CustomControls/GalleryLayout.cs src/PicView.Tests/Gallery/GalleryLayoutTests.cs
git commit -m "Add GalleryLayout for the gallery's bounds and scroll offsets"
```

---

### Task 6: The panel follows the current item and holds what the user scrolled to

**Files:**
- Modify: `PicView.Avalonia/CustomControls/VirtualizingGallery.cs`
- Modify: `PicView.Avalonia/CustomControls/NavigateAbleItemsViewer.cs:32, 162-249`
- Test: `PicView.Tests/Gallery/VirtualizingGalleryTests.cs`

**Interfaces:**
- Consumes: `GalleryLayout` and `GalleryLayoutOptions` (Task 5), `GalleryItemViewModel.SetDetails` (Task 2), and what Task 1 proved.
- Produces:
  - **`VirtualizingGallery.CenterCurrentItem`**: `public void CenterCurrentItem()`, a center request.
  - **`NavigateAbleItemsViewer.ScrollToCenterOfCurrentItem()`** keeps its signature. It calls `CenterCurrentItem` on its panel, posting at `Render` priority from another thread, as today.

- [ ] **Step 1: Write the failing tests**

Add a helper, then the tests. Tests 5 to 8 below are Review Focus 1 to 4.

```csharp
private sealed record Strip(Window Window, NavigateAbleItemsViewer Viewer, VirtualizingGallery Gallery,
    AvaloniaList<GalleryItemViewModel> Items, List<int> Prepared)
{
    public ScrollViewer ScrollViewer => Gallery.FindAncestorOfType<ScrollViewer>()!;
    public double ViewportWidth => ScrollViewer.Viewport.Width;

    public void ScrollTo(double x)
    {
        ScrollViewer.SetCurrentValue(ScrollViewer.OffsetProperty, new Vector(x, 0));
        Dispatcher.UIThread.RunJobs();
    }
}

/// <summary>
/// <paramref name="count"/> unsized items, so 100-wide squares, in a 1000-wide horizontal Docked Gallery. The current
/// item is <paramref name="currentIndex"/>, and the strip records the index of every container it prepares.
/// </summary>
private static Strip ShowStrip(int count, int currentIndex, bool show = true)
{
    var items = new AvaloniaList<GalleryItemViewModel>(Enumerable.Range(0, count).Select(_ => new GalleryItemViewModel()));
    var gallery = new VirtualizingGallery { ItemHeight = 100, Orientation = Orientation.Horizontal };
    var viewer = new NavigateAbleItemsViewer
    {
        ItemsPanel = new FuncTemplate<Panel?>(() => gallery),
        ItemTemplate = new FuncDataTemplate<GalleryItemViewModel>((_, _) => new Border()),
        ItemsSource = items,
        CurrentItemIndex = currentIndex
    };
    var strip = new Strip(new Window { Width = 1000, Height = 150, Content = viewer }, viewer, gallery, items, []);
    viewer.ContainerPrepared += (_, e) => strip.Prepared.Add(e.Index);
    if (show)
    {
        strip.Window.Show();
        Dispatcher.UIThread.RunJobs();
    }
    return strip;
}

private static void Widen(IEnumerable<GalleryItemViewModel> items, VirtualizingGallery gallery)
{
    foreach (var item in items)
    {
        item.SetDetails(new GalleryFileDetails(200, 100, null));
    }
    gallery.InvalidateMeasure();
    Dispatcher.UIThread.RunJobs();
}
```

The tests below share these values:

- Item `i` spans `100·i` to `100·i + 100`, so its middle is at `100·i + 50`.
- "Centered on 150" means `Offset.X == 15050 − ViewportWidth / 2`, within a tolerance of `1`.
- Each test ends with `strip.Window.Close()`.

1. **`Shown_CurrentItemFarAlong_CreatesItsContainerFirstAndCentersIt`:** `ShowStrip(200, 150)`.
   - `Prepared[0] == 150`.
   - Every prepared index is in `[140, 160]`.
   - The strip is centered on 150.
2. **`WidthsChangeToTheLeft_WhileFollowing_KeepsTheCurrentItemCentered`:** `Widen(strip.Items.Take(150), strip.Gallery)`.
   - `Offset.X == 30050 − ViewportWidth / 2`.
3. **`ScrolledByTheUser_WidthsChangeToTheLeft_HoldTheMiddleItemUntilACenterRequest`:**
   - `ScrollTo(10550 − ViewportWidth / 2)`, so item 105 sits in the middle of the view.
   - `Widen(strip.Items.Take(50), strip.Gallery)`, then `Offset.X == 15550 − ViewportWidth / 2`.
   - `Viewer.ScrollToCenterOfCurrentItem()` and `RunJobs()`, then `Offset.X == 20050 − ViewportWidth / 2`.
4. **`MeasuredWithoutAViewport_CreatesNoContainersUntilShown`:** `ShowStrip(200, 150, show: false)`, then `Viewer.ApplyTemplate()` and `Viewer.Measure(new Size(1000, 150))`.
   - `Prepared` is empty.
   - After `Window.Show()` and `RunJobs()`: `Prepared[0] == 150` and the strip is centered on 150.
5. **`DockedAtTheSide_AfterTheUserScrolled_CentersTheCurrentItem`:**
   - `ScrollTo(0)`.
   - Then `Viewer.SetVerticalScrolling()`, `Gallery.Orientation = Orientation.Vertical`, `Window.Width = 150`, `Window.Height = 1000`, and `RunJobs()`.
   - `Offset.Y == 15050 − Viewport.Height / 2`.
6. **`WindowResized_WhileFollowing_KeepsTheCurrentItemCentered`:** `Window.Width = 600` and `RunJobs()`.
   - The strip is still centered on 150.
7. **`ItemsReset_AfterTheUserScrolled_CentersTheCurrentItem`:**
   - `ScrollTo(0)`.
   - `Items.Clear()`, `Items.AddRange` 200 new items, and `RunJobs()`.
   - The strip is centered on 150.
8. **`HeldItemRemoved_KeepsTheOffset`:**
   - `ScrollTo(10550 − ViewportWidth / 2)`.
   - `Items.RemoveAt(105)` and `RunJobs()`.
   - `Offset.X` is unchanged.

- [ ] **Step 2: Run them to verify they fail**

Run: `dotnet test PicView.Tests\PicView.Tests.csproj --filter "FullyQualifiedName~PicView.Tests.Gallery.VirtualizingGalleryTests"`
Expected:
- Tests 1 to 7 fail, because nothing in the panel centers or holds yet.
- Test 8 may already pass. It pins behavior the change has to keep.
- The five existing tests still pass.

- [ ] **Step 3: Move the panel onto `GalleryLayout`**

- `_itemBounds` becomes a `GalleryLayout`. `GetItemBounds`, the snap points, `ArrangeOverride` and `RecycleAllItems` read or clear the layout instead.
- `CalculateBounds` is deleted.

- [ ] **Step 4: Add following and holding to `VirtualizingGallery`**

State:
- `_isFollowing`, initially `true`,
- `_heldItem` (`object?`) and `_heldDistance`,
- `_pendingOffset` (`double?`),
- `_offsetSetByPanel` (`double`, `NaN` when none),
- `_scrollViewer`.

Measure the scroll axis as X, or as Y when `ScrollsVertically`.

**Follow** when `_isFollowing` is set to `true`, which happens:
- in `CenterCurrentItem`,
- in `OnAttachedToVisualTree`, which also finds `_scrollViewer` with `FindAncestorOfType<ScrollViewer>()` and subscribes `LayoutUpdated`,
- on a Reset in `OnItemsChanged`,
- when `Orientation` or `IsExpanded` changes.

**Detach:** `OnDetachedFromVisualTree` also unsubscribes `LayoutUpdated` and sets `_viewport = default`.

**Hold** when `OnEffectiveViewportChanged` sees all three of these, before it stores the new viewport:
- `_viewport` had a size, so a viewport arriving for the first time isn't a scroll,
- the position along the axis moved by more than 1 pixel,
- the new position isn't within 1 pixel of `_offsetSetByPanel`. When it is, clear `_offsetSetByPanel` instead.

Then set `_isFollowing = false`.

**`MeasureOverride`**, after `_layout.Calculate`:

1. If `_viewport` has no width or height, return the extent and create no containers.
2. Pick the target offset and the anchor item:
   - while following with `CurrentItemIndex` in range: `CenterOffset(current)`, anchored on the current item;
   - while holding with `_heldItem` still in the items (`IndexOf`): `HoldOffset(held, _heldDistance)`, anchored on the held item;
   - otherwise: the viewport's position, anchored on the item nearest the view's middle.
3. If the target is more than 1 pixel from the viewport's position, set `_pendingOffset`.
4. Move the viewport to the target, inflate it by `ItemHeight * 2` as today, and take `IndicesIn` that area.
5. Recycle the containers outside that range, as today.
6. Create containers from the anchor, clamped into the range, outward: anchor, +1, −1, +2, −2, and so on. Keep `_realizedItems` sorted by index.
7. Hold the item at `IndexNearest(target + length / 2)` by reference, with `_heldDistance = Middle(it) − (target + length / 2)`.

**`LayoutUpdated`:** if `_pendingOffset` is set, clear it. Then set `_scrollViewer.Offset` along the axis with `SetCurrentValue`, keeping the other axis, and store the offset read back from the scroll viewer in `_offsetSetByPanel`.

- [ ] **Step 5: Make `NavigateAbleItemsViewer.ScrollToCenterOfCurrentItem` a center request**

It calls `(ItemsPanelRoot as VirtualizingGallery)?.CenterCurrentItem()`. Delete `ScrollToCenterOfCurrentItemInternal`, `DeferScrollToCenterUntilMeasured`, `OnDeferredScrollChanged` and `_isScrollToCenterDeferred`.

- [ ] **Step 6: Run the gallery tests**

Run: `dotnet test PicView.Tests\PicView.Tests.csproj --filter "FullyQualifiedName~PicView.Tests.Gallery"`
Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add src/PicView.Avalonia/CustomControls src/PicView.Tests/Gallery/VirtualizingGalleryTests.cs
git commit -m "Center the current gallery item in the panel and hold what the user scrolled to"
```

---

### Task 7: `ImageViewer` hands its tab to `GalleryView`, which subscribes once

**Files:**
- Modify: `PicView.Avalonia/Views/UC/ImageViewer.axaml.cs:28-33`
- Modify: `PicView.Avalonia/Views/Gallery/GalleryView.axaml.cs`
- Modify the ten `new ImageViewer()` call sites:
  - `PicView.Avalonia/Clipboard/ClipboardPasteService.cs:99`
  - `PicView.Avalonia/DragAndDrop/DragAndDropManager.cs:126, 199, 217`
  - `PicView.Avalonia/Navigation/SourceOpener.cs:61`
  - `PicView.Avalonia/Navigation/UpdateImage.cs:182`
  - `PicView.Avalonia/StartUp/QuickLoad.cs:83, 331`
  - `PicView.Avalonia/Views/Main/MainView.axaml.cs:100`
  - `PicView.Avalonia/Views/UC/StartUpMenu.axaml.cs:66`
- Test: `PicView.Tests/Gallery/GalleryViewTests.cs`

**Interfaces:**
- Consumes: `GalleryViewModel.SizesChanged` (Task 3).
- Produces:
  - **`ImageViewer(TabViewModel tab)`**: sets `DataContext = tab`, then calls `InitializeComponent()`, `GalleryView.Initialize(tab)`, `ImageControlHelper.TriggerScalingModeUpdate(MainImage, true)` and `Loaded += OnLoaded`.
  - **The parameterless constructor** becomes `[Obsolete("For the XAML designer only: pass the tab the viewer shows.", error: true)] public ImageViewer() => InitializeComponent();`. The error makes the compiler find every call site.
  - **`GalleryView.Initialize(TabViewModel tab)`**: `public void Initialize(TabViewModel tab)` returns when `GalleryItemsControl` is null, the designer case. Otherwise it subscribes once to:
    - the Gallery Items, through a lambda that passes `tab` to `OnGalleryItemsChanged(TabViewModel tab, in NotifyCollectionChangedEventArgs<GalleryItemViewModel> e)`,
    - `NavigateGalleryCommand`, `ItemSpacing` and `LineSpacing`, as `SubscribeTo` does today,
    - `SizesChanged`, which calls `PostRelayout()`.

- [ ] **Step 1: Write the failing tests**

- Extend `WithGalleryView` so a test can choose which tab the view gets. The overload takes `Func<MainWindowViewModel, TabViewModel> tabOf`, and the test action also receives the `MainWindowViewModel`.
- Build the view with `var view = new GalleryView(); view.Initialize(tab);` instead of setting its DataContext.
- Replace the helper's comment with: "The gallery view loads its XAML only when the application's DataContext is a `CoreViewModel`."
- Add the Review Focus 5 test:

```csharp
[AvaloniaFact]
public void Initialize_ForATabThatIsNotTheActiveOne_ShowsThatTabsItems()
{
    WithGalleryView(vm =>
    {
        var first = vm.WindowTabs.ActiveTab.CurrentValue;
        var second = vm.WindowTabs.CreateTab();
        vm.WindowTabs.SelectTab(first);
        return second;
    }, (tab, vm, viewer) =>
    {
        var active = vm.WindowTabs.ActiveTab.CurrentValue;
        Assert.NotSame(tab, active);
        var own = new GalleryItemViewModel { FileInfo = new FileInfo("own.jpg") };

        tab.Gallery.GalleryItems.Add(own);
        active.Gallery.GalleryItems.Add(new GalleryItemViewModel { FileInfo = new FileInfo("other.jpg") });
        Dispatcher.UIThread.RunJobs();

        Assert.Equal([own], viewer.Items.Cast<GalleryItemViewModel>());
    });
}
```

- [ ] **Step 2: Run them to verify they fail**

Run: `dotnet test PicView.Tests\PicView.Tests.csproj --filter "FullyQualifiedName~PicView.Tests.Gallery.GalleryViewTests"`
Expected: the build fails, because `GalleryView` has no `Initialize`.

- [ ] **Step 3: Change `GalleryView`**

- Delete `OnDataContextChanged`, `SubscribeTo` and the `_tab` field. `OnGalleryItemsChanged` replaces `CurrentValueOnCollectionChanged`, takes the tab as a parameter, and has no null check for it.
- In the range branch of Add, delete everything after the `InsertRange`/`AddRange`: the `navIndex` check, the `CurrentItemIndex` assignment and the `LayoutUpdated` handler.
- In Reset, delete the `LayoutUpdated` handler.
- `SyncCurrentItem` only calls `UpdateCurrentVisualState()`. Its comment drops "and centers it once laid out".
- `PostRelayout()` lets only one post wait at a time, using an `int` flag with `Interlocked.Exchange`. The post runs at `DispatcherPriority.Background`: it clears the flag, then calls `GalleryItemsControl.ItemsPanelRoot?.InvalidateMeasure()`.

- [ ] **Step 4: Give `ImageViewer` its two constructors, and pass each call site's tab**

At `DragAndDropManager.cs:126`, take the tab from `tabOverview.ActiveTab.CurrentValue`, which the local `currentView` comes from. Every other site already has its tab in a local.

- [ ] **Step 5: Build, then run the gallery tests**

Run: `dotnet build PicView.Avalonia.Win32\PicView.Avalonia.Win32.csproj`, then `dotnet test PicView.Tests\PicView.Tests.csproj --filter "FullyQualifiedName~PicView.Tests.Gallery"`
Expected: the build succeeds with no `new ImageViewer()` left, and the tests PASS.

- [ ] **Step 6: Commit**

```bash
git add src/PicView.Avalonia src/PicView.Tests/Gallery/GalleryViewTests.cs
git commit -m "Pass a tab to its ImageViewer and set up its gallery view once"
```

---

### Task 8: Run everything and check the app by hand

**Files:** none, unless a check fails.

- [ ] **Step 1: Run the whole suite**

Run: `dotnet test PicView.Tests\PicView.Tests.csproj`
Expected: PASS. Report the totals, and quote any failure with its output.

- [ ] **Step 2: Check the app by hand with the user**

Run: `dotnet run --project PicView.Avalonia.Win32\PicView.Avalonia.Win32.csproj -- "<image 150 of a folder of 200+ images>"`, with the Docked Gallery shown at startup.

These checks need someone watching the window:

1. The first strip is centered on image 150, and its thumbnail appears first.
2. The strip stays centered while the rest of the sizes arrive.
3. Scrolling the strip during the load keeps the middle item in place, and the next navigation centers the current image again.
4. Open another folder while docked: the new strip opens centered.
5. Open the Expanded Gallery: the current image's column is centered.
6. Dock left: the strip is centered vertically.
7. Switch the Docked Gallery's Thumbnail Stretch to Square: the strip is still centered and doesn't shift.
8. Paste three image files: each new tab's gallery shows its own folder.
