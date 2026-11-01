# Back and Forward Follow What Each Tab Shows

**Date:** 2026-11-01
**Status:** Designed in conversation; this write-up is waiting for review.
**ADRs:** corrects one sentence of ADR-0001 and adds ADR-0012 (see [Decisions that change](#decisions-that-change)). ADR-0002, ADR-0005, ADR-0009 and ADR-0010 hold.

## Goal

- Back and Forward retrace what a tab actually showed, in the order it showed it, the way a browser does. They are the `FileHistoryPrev` and `FileHistoryNext` Actions, on the mouse's back and forward buttons by default. What they retrace includes Single Images (a clipboard, base64 or dropped temp-file image, or a URL download) and every page of a multi-page TIFF or an archive.
- "Open last file" in the start-up menu opens the file the user viewed last.
- The dropdown menu keeps listing recent files. Pins, the sort toggle and FileHistory.json stay as they are.

## Success criteria

1. In one tab, the user views A, then B, pastes a clipboard image, then opens C in another folder. Back shows the clipboard image, then B, then A. Forward then shows B, the same clipboard image, then C.
2. Back from page 3 of a multi-page TIFF or an archive shows page 2. Once the tab has left the archive, Back to one of its pages opens the archive again at that page.
3. The user views A, then B, then A again, then opens a new tab: "Open last file" opens A. It still does after a restart, with A pinned, with file history turned off, and after clearing the history.
4. Back and Forward never show another tab's images.
5. Images the iterator passes over are not recorded:
   - those whipped past while a navigation Keybind is held,
   - those Next 10 and Next 100 jump over,
   - any the user navigates away from before they show.
6. Viewing a file that's already in the dropdown moves it to the top. An archive is listed as the archive, never as one of its extracted temp files.
7. No file history list is read while a load thread changes it.

## Why it doesn't work today

- **There is one cursor for everything.** `FileHistoryManager` is a static list with a single `CurrentIndex`. Every tab and every Main Window moves it, from whichever thread its load runs on.
- **Back and Forward walk the list, not what was viewed.** The list holds each path once, in the order it was first seen. Viewing a path again only moves the cursor back to where that path was first added. So Forward after revisiting an old file goes to the files first seen after it, not to what the user saw next.
- **Single Images are never recorded.** The cursor stays on the file shown before the paste. Back from a clipboard image skips that file, and nothing ever reaches the clipboard image again. Stepping past either end opens `Settings.StartUp.LastFile` instead.
- **"Open last file" reads the wrong thing.**
  - The start-up menu button opens `GetLastEntry()`, the newest path *added*, not the one viewed last. If the user views A, B, then A again, it opens B.
  - Saving writes the pinned entries first, so after a restart the newest unpinned entry counts as the last one.
  - `MainWindow.OnDeactivated` sets `Settings.StartUp.LastFile` to the cursor's entry whenever a window loses focus. That's whichever tab or window loaded last, or the side-by-side partner (it's added after the first image), or null when file history is off or was just cleared.
  - App Start reads `Settings.StartUp.LastFile`, while the button reads the history first.
- **Archives leave temp files behind.** `RepopulateIterator` adds the first extracted temp file before `LoadFromArchiveAsync` adds the archive. So the dropdown lists files that are deleted when the tab leaves the archive.
- **There is no lock.** Load threads change the list, while the UI thread enumerates it live when the dropdown opens: `AllEntries` is `AsReadOnly()`, and `PinnedEntries` is a lazy `Where`.

## Approaches considered

Chosen: **record where a tab shows an image.** `TabViewModel.Display` and `DisplaySingleImage` already change a tab's shown models; no product code sets them any other way. So a tab records what it shows there, and nothing else records.

- **Record where each load starts.** Each `FileHistoryManager.Add` would become a push onto the tab's history, and the paste, drop and base64 paths would get pushes too. It's the smallest change, but:
  - It keeps the scattered calls behind today's bugs, and every new load path has to remember to push.
  - It records what was asked for, not what was shown, so a load that fails or is overtaken by a newer one still pushes.
  - Back and Forward would have to suppress the push at every call site.
- **Watch each tab from outside,** subscribing to its `Image` and `FileInfo` on the UI thread. It needs no lock and leaves the loaders alone, but:
  - Those properties also change for the thumbnail shown while an image loads, so it would have to guess which changes are views.
  - The subscriptions would live in the Avalonia layer, out of reach of Core tests.

## Design

### 1. Three things, three owners

- **View History** (new, `PicView.Core/Navigation/ViewHistory.cs`): what one tab has shown, oldest to newest, with a cursor on what it shows now.
  - Each `TabViewModel` has one. It lives only in memory, moves with its tab between Main Windows, and goes away when the tab closes.
  - It holds up to 100 entries. At most 10 of them are Single Images, whose images it keeps in memory, and a URL download counts as one.
  - A 101st entry drops the oldest entry. An 11th Single Image drops the oldest Single Image, wherever it sits.
- **File History** (`FileHistoryManager`, `FileHistoryViewModel`): the files, archives and URLs viewed most recently anywhere in PicView.
  - It's listed in the dropdown and saved in FileHistory.json: up to 50 entries, plus 5 pinned.
  - It loses its cursor, since only View History moves back and forward now.
  - The file history setting turns it on and off. The setting doesn't affect Back, Forward or Last File.
- **Last File** (`Settings.StartUp.LastFile`): the file, archive or URL that the active tab of the Active Main Window showed most recently. One module writes it (`PicView.Core/Navigation/LastFile.cs`). "Open last file" in the start-up menu and App Start's "open last file" only read it.

The Actions keep their names, `FileHistoryPrev` and `FileHistoryNext`, since keybindings.json stores Actions by name.

### 2. Recording what a tab shows

Three methods change what a tab shows, and each one records after showing:

- `Display(primary, secondary)`: files, side-by-side pairs, archive pages, a crop, a reload, a file changed on disk.
- `DisplaySingleImage(model, type)`: clipboard, base64 and dropped temp-file images, and now URL downloads.
- `ShowPage(page)` (new): turns a multi-page TIFF to a page. `ImageIterator.ShouldNavigateTiffEntry` calls it instead of setting the page and `model.Image` itself. The window still picks the new page up from `Model.Image`, as today.

| What the tab shows | View History entry | File History | Last File |
|---|---|---|---|
| A file (the first of a side-by-side pair) | the path | the path | the path |
| A page of a multi-page TIFF | the path and the page number | the path | the path |
| A file inside the tab's archive extraction folder | the archive's path, and that page's entry in the archive | the archive's path | the archive's path |
| A URL download | the URL, keeping the shown model | the URL | the URL |
| A clipboard, base64 or dropped temp-file image | the shown model, with its type and title | nothing | nothing |
| An empty model, as when a tab becomes fresh again | nothing | nothing | nothing |

- **Repeats collapse.** Showing the entry the cursor is on records nothing. So a reload, a crop, a file changed on disk and the side-by-side partner add no entries.
- **What counts as the same entry:**
  - for a file, the same path (compared ignoring case, as the file list does) and the same page;
  - for an archive page, the same archive and the same entry in it;
  - for a URL download, the same URL;
  - for a Single Image, the same model. Showing a Single Image again passes the kept model back, so it matches.
- **Archive pages are told apart by folder, not by a flag.** When a tab leaves an archive, `LoadFromFileAsync` shows the next file before it cleans up the extraction, so `IsArchived` is still true at that moment. A file counts as an archive page only when it sits in the tab's extraction folder. `ArchiveExtractionService` remembers which archive entry each file it extracts came from.
- **Passed-over images are not recorded.** An image counts once the tab shows it:
  - The thumbnail shown while an image loads doesn't count. An image the user navigates away from before it loads never shows, so it never counts.
  - Next 10, Next 100 and a gallery click record only where they land.
  - While a held Keybind repeats navigation (`ImageIterator.RepeatNavigateAsync`), the tab records nothing. When the key is released (`StopRepeatedNavigation`), the image the iterator stopped on is recorded as soon as it shows.
- **A slideshow records each image it shows**, since the user views them.
- **Callers that change:**
  - The nine `FileHistoryManager.Add` calls in `NavigationService`, `ImageIterator` and `QuickLoad` go. After that, `Add` has one caller: a tab's recording.
  - `LoadFromUrlAsync` sets `SourceURL` first and shows its image with `DisplaySingleImage(model, SingleImageType.Url)`. Today it calls `Display` and sets the type afterwards.

### 3. Back and Forward

- `FileHistoryPrev` steps the active tab's View History back one entry and shows it in that tab. `FileHistoryNext` steps forward. At either end they do nothing; they no longer open the last file.
- Showing something new while the cursor isn't on the newest entry drops the entries after the cursor, then adds the new one, as a browser does.
- **Showing an entry again:**
  - **A file** opens through `OpenPathAsync`, the same way a click in the dropdown opens it. It lands in its folder, with navigation, gallery and title as usual.
  - **A TIFF page:** if the tab already shows that file, it turns to the page with `ShowPage`. Otherwise the file opens at that page: the load sets the page on the model before `Display`, so another page never shows first or gets recorded.
  - **An archive page:** if the tab is still in that archive and the page's file is extracted, the tab moves to that file in its list. Otherwise the archive opens again with that page extracted and shown first (`LoadFromArchiveAsync` takes the entry to start at), and the rest are extracted as usual.
  - **A URL download or a Single Image** is shown again from the kept model, with no new download. It goes through the same steps as a paste: shown upright, fitted, with its title. For this, `UpdateImage.SetSingleImage` takes the kept model, type and title.
- **Steps follow what the tab shows, never what was asked for.** A step only marks a target. The cursor moves when the tab shows that entry, through the recording in section 2. So:
  - A step that's taken while another is under way counts from the earlier step's target, not from the cursor. Pressing Back three times quickly goes back three entries. If an entry between the cursor and the target shows on the way, the cursor moves to it instead of a new entry being added.
  - A step ends when its target shows, when anything else shows, or when its load finishes without showing the target. A load that fails leaves the cursor on the image that's still shown, and the next step counts from the cursor again.
  - Showing anything else, such as Next, a gallery click or a paste, is recorded as usual.
- **Missing files are skipped.** Before showing a file, TIFF or archive entry, a step checks that its file still exists, off the UI thread. If it doesn't, the step removes the entry and goes on in the same direction. A renamed file counts as missing.

### 4. File History

- Recording a file, archive or URL moves its entry to the newest end, or adds it there. Past 50 unpinned entries, the oldest unpinned one drops, as today. Pinned entries keep their order among the pinned.
- One lock guards the list. `AllEntries` and `PinnedEntries` return copies taken under it.
- The cursor members go:
  - `CurrentIndex`, `HasNext`, `HasPrevious` and `CurrentEntry`,
  - `GetNextEntry`, `GetPreviousEntry`, `GetFirstEntry`, `GetLastEntry` and `GetEntry`.
- These stay as they are: pin, unpin, remove, clear, rename, the sort toggle, the caps, and FileHistory.json's format.
- `FileHistoryViewModel` fills the dropdown as it does now, but from the copies. It marks as current the entry for what the active tab's View History cursor is on: its file, archive or URL. So an archive is marked while the tab browses it. Today it never is, because the tab shows an extracted temp file.
- With the file history setting off, nothing is recorded or shown, as today.

### 5. Last File

- `LastFile` writes `Settings.StartUp.LastFile` when the active tab of the Active Main Window records a file, archive or URL. That happens whether or not file history is on.
- These don't change it:
  - a tab that isn't the active one when it records, such as an extra tab that a multi-file paste opened;
  - a Single Image without a path.
- Closing a Main Window, and App Shutdown, still make that window's active tab's entry the Last File, as the glossary says, when that entry has a path. They read it from the tab's View History cursor through `LastFile`, instead of from `Model.FileInfo` or from File History's last entry.
- "Open last file" in the start-up menu (`UINavigationHelper.OpenLastFile`) reads only `Settings.StartUp.LastFile`. App Start already does.
- Removed:
  - `MainWindow.OnDeactivated` setting `Settings.StartUp.LastFile` from the history cursor, and the comments in `MainWindowLifecycle.CloseAsync` and `AppLifecycle.ShutdownAsync` that work around it.
  - `NavigationService.LoadLastFileAsync` and its `INavigationService` member, which nothing calls.
  - The `NavigationMode` enum, which nothing uses.

### 6. Performance

- Recording an image takes a lock, a comparison and at most one list write.
  - Entries are small values (a `readonly record struct`) in a list sized for its cap up front.
  - Recording a file allocates nothing new: its path is the `FileInfo.FullName` string the tab already has.
  - A repeat returns before anything else happens.
- While a held Keybind repeats navigation, recording is one flag check.
- Moving a File History entry to the newest end is a search and a move in a list of at most 55. Today's `Add` already searches that list.
- The dropdown copies the lists only when it opens.
- Writing the Last File costs two reference comparisons and a string assignment. The settings reach the disk only when they're saved, as today.
- File checks happen only when the user steps, off the UI thread: one `File.Exists` per entry tried.
- Each tab keeps at most 10 Single Images, freed when they drop out or the tab closes.
- Nothing new runs per frame.

### 7. Threads and errors

- `Display` runs on whichever thread a load finishes on. So View History takes a lock for every read and write, and so does File History. Callers get copies, never the live lists.
- `LastFile` reads the tab's Main Window and the Active Main Window at the moment the tab records. A tab without a Main Window, as in tests, never writes it.
- An entry that can't be shown again for any other reason, such as a file that no longer decodes, behaves like a failed load: nothing is shown, and the cursor stays where it is.
- Errors are logged with `DebugHelper`, as elsewhere.

## Decisions that change

- **ADR-0001** describes File History as "a chronological stream of recently opened files, archives, and web URLs with forward/backward traversal and entry pinning". Forward and backward traversal moves to each tab's View History. The modules stay separate, as the ADR decided. The implementation corrects that sentence and points to ADR-0012.
- **New ADR-0012, "View History follows what each tab shows":** Back and Forward work per tab and are recorded where a tab shows an image. It lists the two other approaches above as the options considered.
- **Behavior that changes for users:**
  - Back and Forward work per tab, and they no longer open the last file at either end.
  - The dropdown is ordered by when each entry was last viewed.
  - Turning file history off no longer turns off Back and Forward.

## Glossary

These entries are added to `GLOSSARY.md` at the repository root, under Tabs:

> **View History**:
> What a tab has shown, oldest to newest, with a cursor on what it shows now: each file, each page of a multi-page TIFF or an archive, and each Single Image. Back and Forward move through it and show that entry again in the same tab. Showing something new after going back drops the entries after the cursor. Images the tab only passed over, such as those passed while a navigation Keybind is held, are not in it. It lives only in memory and holds up to 100 entries, at most 10 of them Single Images. It moves with its tab and ends when the tab closes.
> _Avoid_: Navigation history, back stack, file history (for Back and Forward)
>
> **File History**:
> The files, archives and URLs viewed most recently in any tab, listed in the dropdown menu by when each was last viewed: up to 50, plus up to 5 pinned ones. It is remembered between runs, and turning file history off stops it recording. Back and Forward don't use it.
> _Avoid_: Recent files, history list
>
> **Last File**:
> The file, archive or URL that the active tab of the Active Main Window showed most recently. "Open last file" opens it, and so does App Start when it opens the last file. Closing a Main Window, and App Shutdown, make that window's active tab's file the Last File. A Single Image without a path never becomes it.
> _Avoid_: Last entry, current entry

## Build order

Each step leaves PicView working:

1. **The recording seam, with File History and Last File.**
   - `Display` and `DisplaySingleImage` record into File History and Last File.
   - File History gets its lock, its copies and its newest-end order.
   - Last File gets its one writer.
   - The scattered `Add` calls and the `OnDeactivated` write go.

   This step alone fixes "Open last file".
2. **View History on the same seam.** Back and Forward move onto it, Single Images included, and File History's cursor goes.
3. **Pages:** `ShowPage`, opening a TIFF at a page, the archive's entry map, and opening an archive at a page.
4. **Docs:** the glossary, ADR-0001 and ADR-0012.

## Testing

**`ViewHistoryTests`** (Core, the class on its own; a file-exists seam keeps the disk out):

- Repeats collapse, and showing something new after going back drops the forward entries.
- The caps:
  - a 101st entry drops the oldest entry;
  - an 11th Single Image drops the oldest Single Image, wherever it sits in the list.
- Steps:
  - the cursor moves only when the target shows;
  - a failed load leaves it in place;
  - three quick Backs go back three entries;
  - an entry shown on the way moves the cursor to it;
  - an unrelated show ends the step and is added.
- Missing files are skipped and removed, in both directions.
- Recording from several threads while another thread reads doesn't throw.

**Recording on `TabViewModel`** (`ViewHistoryRecordingTests`):

- A file records its path. Side by side, only the first image is recorded.
- A TIFF records its page on `Display`, and `ShowPage` records each page turn.
- A file in the tab's extraction folder records the archive and its entry. The first file shown after the tab leaves the archive records as a file.
- A clipboard image and a URL download record their models. An empty model records nothing.
- While repeat navigation runs, nothing is recorded. After `StopRepeatedNavigation`, the image the iterator stopped on is.

**Opening at a page:**

- An `ImageIterator` test opens a TIFF at a page, with the mocks `TiffNavigationTests` uses, and checks that no other page was shown first.
- A `NavigationService` test opens a small zip, written to a temp folder, again at its third page.

**`FileHistoryManagerTests`:**

- Revisiting moves an entry to the newest end.
- Pinned entries keep their order.
- An archive is recorded without its temp files (through `NavigationService`).
- The copies stay stable while other threads add.
- Saving and loading still round-trip.
- The old cursor test is replaced.

**`LastFileTests`:**

- The active tab of the Active Main Window writes it.
- A tab that isn't active, or a Single Image, doesn't.
- It's still written with file history off.
- The existing `AppLifecycleTests` for closing windows and App Shutdown keep passing.

**Headless, end to end:** view a file and paste a clipboard image. Back shows the file, and Forward shows the same clipboard image again.

`InputAllocationTests` still pass in Release. All other existing tests keep passing. Tests that use removed members are updated.

**By hand:**

1. In one tab, browse a folder and paste a screenshot.
2. Open an archive and turn three pages, then open a multi-page TIFF and turn two.
3. Walk all the way back with the mouse's back button, then forward again.
4. Close the tab, open a new one, and check that "Open last file" opens the TIFF.

## Out of scope

- Following renames inside View History. A renamed file is skipped as missing.
- Keeping View History between runs.
- Renaming the `FileHistoryPrev` and `FileHistoryNext` Actions.
- Building the dropdown's entries more cheaply. It creates them all again each time it opens, as today.
- File History entries for the files of a renamed folder (the existing TODO in `FileHistoryManager.Rename`).

## Files

Paths are relative to `src/`.

- **Core:**
  - `PicView.Core/Navigation/ViewHistory.cs` and `ViewHistoryEntry.cs` (new)
  - `PicView.Core/Navigation/LastFile.cs` (new)
  - `PicView.Core/ViewModels/TabViewModel.cs`
  - `PicView.Core/Navigation/ImageIterator.cs`
  - `PicView.Core/Navigation/NavigationService.cs` and `Interfaces/INavigationService.cs`
  - `PicView.Core/Navigation/NavigationMode.cs` (deleted)
  - `PicView.Core/ArchiveHandling/ArchiveExtractionService.cs`
  - `PicView.Core/FileHistory/FileHistoryManager.cs`
  - `PicView.Core/ViewModels/FileHistoryViewModel.cs`
- **Avalonia:**
  - `PicView.Avalonia/Navigation/UINavigationHelper.cs`
  - `PicView.Avalonia/Navigation/UpdateImage.cs`
  - `PicView.Avalonia/StartUp/QuickLoad.cs`
  - `PicView.Avalonia/StartUp/MainWindowLifecycle.cs` and `AppLifecycle.cs`
  - `PicView.Avalonia/CustomControls/MainWindow.cs`
- **Tests:**
  - new: `ViewHistoryTests`, `ViewHistoryRecordingTests`, `LastFileTests`, and a headless Back and Forward test;
  - updated: `FileHistoryManagerTests`, `TiffNavigationTests`, `NavigationServiceTests`, `AppLifecycleTests`.
- **Docs** (at the repository root):
  - `GLOSSARY.md`
  - `docs/adr/0001-separate-favorites-and-file-history.md`
  - `docs/adr/0012-view-history-follows-what-each-tab-shows.md` (new)
