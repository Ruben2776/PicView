# The Gallery Opens Centered on the Current Image

**Date:** 2026-10-30
**Status:** Designed in conversation; this write-up is waiting for review.
**ADRs:** contradicts one consequence of ADR-0010 (see [Decisions that change](#decisions-that-change)). ADR-0005 and ADR-0011 hold.

## Goal

When PicView starts with the Docked Gallery shown, in a folder of more than 200 images, the gallery:

- opens centered on the current image, and
- spends its startup work (reading image sizes, decoding thumbnails) on the thumbnails around the current image first, not on the first files of the folder.

This applies to every load: the first open, a folder change while the gallery is shown, and the Expanded Gallery.

Along the way, `GalleryView` stops reacting to DataContext changes. A view's DataContext is set once and never changes, so the view sets itself up once, during initialization.

## Success criteria

1. The first frame that shows thumbnails is already centered on the current image.
2. No distant thumbnail is decoded before the nearby ones, and the current image's thumbnail is decoded first.
3. The strip stays centered while the remaining sizes arrive. Once the user scrolls it, it stops re-centering, and the item in the middle of the view stays where it is.

## Why it doesn't today

- `GalleryCoordinator.Load` reads each file's pixel size in batches of 20, starting from the first file, and adds each batch as soon as it's read.
- Every batch makes `VirtualizingGallery` lay out at offset 0. It creates containers for the first screenful, and each container starts loading its thumbnail as it's created (`NavigateAbleItemsViewer.InitializeNavItem` calls `GalleryItem.LoadImage`). So the first files' thumbnails take the thumbnail slots until the batch with the current image arrives.
- `GalleryView` centers once, when the batch holding the current index arrives, through a one-shot `LayoutUpdated` handler.
- `ScrollToCenterOfCurrentItem` does nothing when `VirtualizingGallery.GetItemBounds` returns null. Bounds exist only after a layout that saw the items, and `RecycleAllItems` clears them on an orientation change, a Reset or Move, and when the panel leaves the visual tree. When the bounds do exist, it clamps against `ScrollViewer.Extent`, which only catches up after arrange.
- Decodes that have started finish even after their container is recycled, because `GetThumbnailAsync` takes no cancellation token.

## Approaches considered

**Loading.** Chosen: add every item at once, then read sizes outward from the current image. The alternative was to lay out nothing until the current item's size is read, then lay out the items around it. That keeps the layout exact and the coordinator unchanged, but the wait grows with the image's position in the folder. The chosen way costs a change to ADR-0010's load, thumbnails that can change width after they appear, and a panel that corrects its scroll position.

**Centering and holding.** Chosen: the panel owns both.

- Avalonia's own scroll anchoring (`ScrollViewer.RegisterAnchorCandidate`) would need less code for holding, but centering still needs the panel's request. Avalonia 12.1.4 documents only the interface, so its rules, such as not anchoring at offset 0, would need proving first.
- Re-centering after each batch of sizes is the smallest change. But it keeps the null-bounds race and still lays out at offset 0 first. It can also render a frame between a width change and the re-center, and it can't hold a position the user scrolled to.

## Design

### 1. Core: every item at once, then sizes outward

`GalleryCoordinator` loads like this:

1. **Every item at once.** A load makes one item per file without opening any of them. The name, file size and date come from the file list's cached attributes. It adds them all in one `AddRange`. An item whose size isn't read yet is laid out as a square.
2. **The current image's neighbors first.** Before that `AddRange`, the coordinator reads the details of the current file and the 20 files on each side of it, in parallel, and writes them into their items. That's 41 files, fewer near the ends of the list. The other items start without details.
3. **Then outward.** It reads the rest in batches of 20. Each batch is the 20 unread items nearest the current image: the next 10 on each side. Near an end of the list, the other side makes up the difference. Each batch is chosen on the coordinator's queue from the items as they are at that moment, so navigation and edits during the load steer it.
4. **Details go onto the existing items.** Items aren't replaced, so their containers and thumbnails stay put.
   - `GalleryFileDetails` (pixel size and XMP packet) becomes a sealed record class. Each item holds its details in one field, null until read, instead of the separate `PixelWidth` and `PixelHeight` setters.
   - A file whose details can't be read gets empty details (0 × 0, no XMP packet). It counts as read, stays square, and isn't read again.
   - The item's `ImageSize` text updates when its details are written.
   - After each batch, the coordinator raises a new sizes-changed signal on `GalleryViewModel` once.
5. **The current index comes with the list.** The coordinator can run before the iterator sets `NavigationIndex`, so `ImageIterator.Initialize` puts its `CurrentIndex` on the `Listed` change (`FileListChange.Index`), and the neighborhood read uses it. The later batches read `NavigationIndex`, and so does a load that starts when the gallery is shown. An index out of range is clamped to the list.
6. **Edits during a load apply as they do after it.**
   - Insert, remove, replace, relocate (a folder rename) and sort all apply to the items, and the size reads carry on.
   - Listing a different folder still drops the items and starts over.
   - `FillDetails` (for an inserted or replaced file) writes the details onto its item and raises the signal, instead of swapping in a new item.
   - Replace still builds a new item for a file whose contents changed, so its thumbnail loads again.
7. **Loading state.**
   - `NotLoaded`: no items yet.
   - `Loading`: every item is in, and sizes are still being read.
   - `Loaded`: every item's details have been read.

   Only the coordinator and tests read it.
8. **Motion photo badges.** The thumbnail loader takes the XMP packet from the item's details when the thumbnail loads. If the details aren't read yet, `MotionPhotoDetector.TryDetect` gets null and reads the packet itself, as it already does.
9. **Test seam.** The coordinator takes its details reader as a parameter, `GalleryFileDetails.Read` by default, so tests can record and pause the reads.

The reads run off the queue in parallel, as today. Every change to the Gallery Items, and every details write to an item already added, runs on the coordinator's queue. The neighborhood's details are written before the items are added, so no other thread sees those items yet.

### 2. Panel: `VirtualizingGallery` centers and holds

The panel is always in one of two states.

**Following:** every layout centers the current item, `NavigateAbleItemsViewer.CurrentItemIndex`. A new panel starts out following. It goes back to following when:

- its items are cleared (a Reset),
- it's attached to the visual tree again, as after a tab switch,
- it gets a center request. A `CurrentItemIndex` change and every `ScrollToCenterOfCurrentItem` call are center requests.

**Holding:** the panel stops centering when the scroll offset changes in a way it didn't cause. That covers the wheel, the scroll bar, middle-click auto-scroll, and `ScrollItemIntoView` for keyboard selection in the Expanded Gallery. While holding, it keeps the item nearest the middle of the view at the same distance from the middle as sizes change around it. To tell its own offset changes apart, the panel remembers the offset the scroll viewer took when the panel set it, after coercion. Any offset more than one pixel away from that wasn't the panel's.

Each layout:

1. Computes every item's bounds and the extent, as today.
2. Decides where the view goes: centered on the current item while following, or with the held item back at its distance from the middle while holding. The result is clamped to the extent just computed, never to `ScrollViewer.Extent`.
3. Creates containers for that view, not for the current one. It starts with the item at the view's center and works outward. Containers request their thumbnails in the order they're created, and the thumbnail slots (`ThumbnailSlots`, a `SemaphoreSlim`) serve waiting requests first in, first out, so the current image's thumbnail is decoded first.
4. After the layout pass, once the scroll viewer's extent has caught up, sets the offset with `SetCurrentValue`.

Special cases:

- **No viewport yet** (zero size): the panel creates no containers and waits. Today it falls back to the start of the strip and loads those thumbnails.
- **`CurrentItemIndex` out of range:** the index and the items can disagree for a moment during edits, and a tab with no files has -1. The panel leaves the offset alone and lays out the current view, as today.
- **The held item is tracked by reference,** not by index, because edits shift indices. If it's removed, the offset stays where it is, and the next layout holds whichever item is then nearest the middle.
- **The vertical Docked Gallery, and Square or Fill Square:** bounds never change during a load, so holding never moves anything.

**`GalleryLayout`** is a new plain class beside the panel that does the bounds and offset math:

- item bounds and the extent for the horizontal and vertical Docked Gallery and the Expanded Gallery's columns,
- the offset that centers an item,
- the offset that holds an item at its distance from the middle.

The panel calls it, so these rules get unit tests without a window or the dispatcher.

**`NavigateAbleItemsViewer.ScrollToCenterOfCurrentItem`** keeps its name and becomes a center request to the panel.

- Its math goes, along with `DeferScrollToCenterUntilMeasured`, `OnDeferredScrollChanged` and `_isScrollToCenterDeferred`.
- A request made before the panel exists isn't lost, because a new panel starts out following.
- The calls in `GalleryAnimationControl` and `GalleryHelper.CenterGallery` stay as they are, and so do `ScrollItemIntoView` and `GetItemBounds`.

### 3. View: `GalleryView` gets its tab from its `ImageViewer`

Nothing handles `DataContextChanged`.

- **`ImageViewer` takes its tab in its constructor.**
  - It sets its DataContext to the tab once, then calls `GalleryView.Initialize(tab)` right after `InitializeComponent()`.
  - The ten `new ImageViewer()` call sites pass the tab they already have.
  - A parameterless constructor stays for the XAML designer only. It loads the XAML and nothing else.
  - The view can't look up the active tab instead. `TabOverviewViewModel.CreateTab(file)` adds the tab, which creates its `ImageViewer` through `TabCreated`, and only then selects it. That's why 22f344fd9 stopped using the active tab.
- **`GalleryView.Initialize(tab)` subscribes once.** It subscribes to the Gallery Items, `NavigateGalleryCommand`, `ItemSpacing`, `LineSpacing` and the sizes-changed signal. At startup, and whenever files open into a tab, the tab's `ImageViewer` is created before navigation lists the files, so the view subscribes before the coordinator can add items. `OnDataContextChanged` goes away, along with its re-subscribe branch and the collection handler's null check for the tab.
- **The add handler loses its centering hack:** the check for whether a batch holds the current index, setting `CurrentItemIndex` by hand, and the one-shot `LayoutUpdated` handler. Setting `CurrentItemIndex` by hand could also replace its binding to `NavigationIndex`. Nothing replaces them.
- **Reset** still clears and refills the items, but without its `LayoutUpdated` handler.
- **`SyncCurrentItem`** still marks the current item after an insert, remove or move, but no longer re-centers. While following, the panel re-centers anyway. **This changes today's behavior:** if the user has scrolled the strip, an edit that leaves the current index alone no longer pulls it back. An insert or remove before the current image shifts the index, and a `CurrentItemIndex` change is a center request, so it still re-centers, as today.
- **The sizes-changed signal** posts one re-layout of the panel to the UI thread at a time, so a burst of batches costs one layout.
- **Collection changes** keep today's blocking `Invoke`, which keeps them in order.

### 4. Threads and errors

- **Writing and reading details.** The coordinator's queue writes an item's details with `Volatile.Write`, and the UI thread reads them with `Volatile.Read`. The panel reads each item's details once per layout and takes width and height from that one read, so a layout never pairs a new width with an old height.
- **A new load** cancels the old reads through the existing cancellation token, and a batch for a dropped list isn't written.
- **An item removed while its details are being read** still gets them. Nothing shows it, so this is harmless.
- **A failure inside the size loop** is logged with `DebugHelper`, like the rest of `Load`. The items still waiting stay square, the loading state stays `Loading`, and the gallery keeps working.

## Decisions that change

**This contradicts ADR-0010, which says "A sort during a load restarts the load in the new order".** It's worth reopening because every item is now in the list from the start of a load. A restart would throw away items that are already right, and clearing and re-adding them would lose the gallery's place. The consequence becomes: *A sort or an edit during a load applies to the items, and the load keeps reading sizes outward from the current image.* The implementation updates the ADR.

ADR-0005's startup order holds: the gallery still loads once navigation starts, after the image.

## Assumption to verify first

In step 4, the panel sets the offset right after a layout pass. This assumes Avalonia lays out an offset set at that point before the next frame renders.

The implementation's first task checks it with a headless test: set the offset after a layout pass, run one render tick, and check that the tick laid out the new offset.

If the assumption is wrong:

- the first frame shows an empty strip,
- arriving widths can make the strip jitter for a frame.

In that case, step 4 is redesigned before anything is built on it.

## Testing

**First:** the headless check above.

**Core** (`GalleryFollowsFilesTests`, with a details reader the test records and can pause):

- With 200 files and the current image at 150:
  - The first change adds all 200 items at once.
  - The first reads are file 150 and the 20 on each side.
  - Each later batch is the next 10 on each side.
- When the user navigates while sizes are being read, the next batch is read around the new current image.
- Details are written onto the same item objects, with no Replace events, and the sizes-changed signal fires once per batch.
- A file whose details can't be read is read once and stays square, and the load still reaches `Loaded`.
- The `Listed` change carries the iterator's current index.
- Two tests are renamed to match the new behavior, and their final checks are unchanged:
  - `SameFilesInAnotherOrder_WhileLoading_LoadsThemAgainInTheNewOrder` → `SameFilesInAnotherOrder_WhileLoading_SortsTheItems`
  - `FileInserted_WhileLoading_LoadsAgainWithTheFile` → `FileInserted_WhileLoading_InsertsItsItem`

**`GalleryLayout`** (plain unit tests):

- Bounds for the horizontal and vertical Docked Gallery, and for the Expanded Gallery's columns.
- Centering an item:
  - clamped at both ends,
  - with a strip shorter than the view.
- Holding: when widths change to the left of the held item, the offset moves by the same amount. Widths changing to its right don't move it.

**Panel** (headless, `VirtualizingGalleryTests`):

- With 200 items and the current item at 150:
  - The first container created is item 150's.
  - Nothing near item 0 is ever created.
  - The offset centers item 150.
- When widths change to its left, it stays centered.
- After a scroll the panel didn't make, the middle item stays put, and nothing re-centers until the next center request.
- A center request made before there's a viewport is carried out after the first layout.

**`GalleryViewTests`:** the tests create the view and call `Initialize(tab)` instead of setting its DataContext. There are no tests for DataContext changes.

All other existing tests keep passing unchanged.

**By hand:** open image 150 of a real folder of more than 200 images, with the Docked Gallery shown at startup. Then repeat:

- after a folder change while docked,
- in the Expanded Gallery,
- docked left,
- with Square stretch.

Each time, the strip should open centered, the current thumbnail should appear first, and the strip should stay centered until you scroll it.

## Out of scope

- Cancelling decodes that have already started when their container is recycled (`GetThumbnailAsync` takes no token).
- Unsubscribing a `GalleryView` whose `ImageViewer` is replaced. It stays subscribed to its own tab, as today.
- The `Dispatcher` wrappers around `GalleryAnimationControl`'s `ScrollToCenterOfCurrentItem` calls.
- A better width estimate than a square for items whose size isn't read yet.
- Size reads that follow the user's scroll position. They follow the current image.
- `MainWindow.RefreshViewBinding`, which sets a moved tab's view's DataContext to the same tab again.

## Files

Paths are relative to `src/`.

- **Core:**
  - `PicView.Core/Gallery/GalleryCoordinator.cs`
  - `PicView.Core/Gallery/GalleryFileDetails.cs`
  - `PicView.Core/ViewModels/GalleryItemViewModel.cs`
  - `PicView.Core/ViewModels/GalleryViewModel.cs`
  - `PicView.Core/Navigation/ImageIterator.cs`
- **Avalonia:**
  - `PicView.Avalonia/CustomControls/VirtualizingGallery.cs`
  - `PicView.Avalonia/CustomControls/GalleryLayout.cs` (new)
  - `PicView.Avalonia/CustomControls/NavigateAbleItemsViewer.cs`
  - `PicView.Avalonia/Views/Gallery/GalleryView.axaml.cs`
  - `PicView.Avalonia/Views/UC/ImageViewer.axaml.cs`
  - the ten `new ImageViewer()` call sites
- **Tests:**
  - `PicView.Tests/Gallery/GalleryFollowsFilesTests.cs`
  - `PicView.Tests/Gallery/VirtualizingGalleryTests.cs`
  - `PicView.Tests/Gallery/GalleryViewTests.cs`
  - `PicView.Tests/Gallery/GalleryLayoutTests.cs` (new)
- **Docs:** `docs/adr/0010-gallery-follows-its-tabs-file-list.md` (at the repository root).
