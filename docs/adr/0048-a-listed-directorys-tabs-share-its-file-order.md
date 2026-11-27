# A Listed Directory's Tabs Share Its File Order

The **File Order** belongs to the **Listed Directory**, not to a Main Window (issue #328 asks for a folder to be browsed in the order the folder has, which a window's one order cannot give two folders). `DirectoryOrders` (`PicView.Core/FileSorting`) is one object for the whole application, handed to every tab in `TabServices.Orders`. It maps a Listed Directory to a `FileOrder` and to the tabs that list it, held weakly. Every tab that lists a directory, in any Main Window, lists it in that directory's order, and a tab of another directory is not touched.

The Listed Directory is the directory a tab's Load listed its files from: the file's own folder, the opened folder, the `RootDirectory` when the listing came from it, or the archive file for an Open Archive. It is recorded where the Load gives the iterator its list (`TabLoad.ShowThenListAsync`, `RelistAsync`), through `TabViewModel.ListDirectory`, and is null for a tab that lists nothing. `DirectoryOrders.KeyOf` names it by its full path without a trailing separator, compared without case. A caller that gives a Load ready-made files says where they were listed from in `LoadOptions.ListedDirectory`; without it the file's own folder is taken.

`TabViewModel.CurrentOrdering()` is the order of the tab's Listed Directory with the platform's name comparison, and `OrderingFor(directory)` is the one a Load lists a directory with before the tab lists it. A sort from the menu (`TabOverviewViewModel.SortAsync`) sets the order of the active tab's Listed Directory through `DirectoryOrders.SortAsync`, which asks every tab listing it to re-sort (`TabViewModel.ResortAsync`) and ends when all have. `DirectoryOrders.SetAsync` does the same without touching the default. After every list a Load gives the tab, `FollowListedOrder` still compares the list's order with the directory's and re-sorts when they differ, so a sort made while the Load listed is not lost.

A directory has an order only while a tab lists it. The first tab to list it gives it the **Default File Order**, which is the `Sorting` group of the settings file (`SortPreference`, `Ascending`) read through `FileOrder.FromAppSettings`; when its last tab leaves, the entry goes. A menu sort also writes the Default File Order (`FileOrder.StoreAsDefault`), at once, so a directory opened afterwards, and every directory after a restart, starts in the order last chosen. A tab that lists nothing shows and sets the Default File Order alone.

The sort menu's check marks are the order of the active tab's Listed Directory: `TabOverviewViewModel` follows the active tab, its `ListedDirectory`, and `DirectoryOrders.Changed`.

## Considered Options

- **Keep the File Order a Window Setting (ADR-0039).** One order per window cannot follow two folders that are sorted differently, which is what issue #328 asks for, and two windows on one folder could disagree about it.
- **The File Order per tab.** ADR-0039 rejected it because the menu acts on a window. With the directory as owner the menu acts on the active tab's directory, and two tabs on one folder cannot disagree, which a per-tab order would allow.
- **One shared file list per directory.** `ImageIterator` holds the list, the ordering, the tab's indices, its cache list and its gallery callback under one lock, and `FolderChanges` mutates and reacts in one step. Sharing the list means splitting it into a list and a per-tab cursor with no step in between that keeps the app correct. Sharing the order gives the same behaviour to the user; the list stays the tab's.
- **Remember each directory's order for the session, or in the settings file.** A manual sort would then survive leaving the folder. Forgetting it with its last tab needs no store and no pruning, and the Default File Order carries the last choice forward.
- **A menu sort that does not write the default.** Nothing else sets the default (the Settings window has no sort control), so it could never change.

## Consequences

- Supersedes the ownership parts of ADR-0039: the File Order is no longer a Window Setting, `WindowSettings.FileOrder` is gone, a window neither seeds nor stores an order, and a tab that moves to another Main Window keeps the order of its directory. ADR-0039's `FileOrdering`, its tie-break, the Random rules, the archive entry ordering and stamping, and `FollowListedOrder` stand.
- Amends ADR-0038 (the File Order leaves the Window Settings) and ADR-0020's note on the sort.
- A sort in one Main Window re-sorts tabs of other Main Windows that list the same directory. With Side-by-Side on there, their pair is shown again, which can enter the Loading Title Phase in a window the user is not looking at.
- Choosing the order a directory already has does nothing, apart from Random, which shuffles again in every tab listing it. Each tab shuffles its own list, so two tabs on one directory under Random show different orders. This is the one case where they do not agree.
- Sorting one folder changes the default, so the next folder opened, and every folder after a restart, comes up in that order. Tabs already on other directories keep theirs until they list again.
- With Include Subdirectories on, a tab rooted at a folder and a tab opened in one of its subfolders list different Listed Directories with separate orders, though they show some of the same files.
- An Open Archive's Listed Directory is the archive file, so two tabs on one archive sort together while each keeps its own Archive Opening and extraction folder (ADR-0013).
- A renamed folder keeps its order under its new name: the first tab to follow the rename carries it there (`FolderChanges.FollowListedDirectory`).
- Stepping to the next or previous folder sorts the sibling folders by the order of the directory being left, as `CurrentOrdering()` gives it, and the destination takes its own order.
- Batch Resize lists in the order of the directory it is pointed at.
- The Default File Order is an App Setting written when a sort is made and saved with the settings file; the settings file keeps its shape and its version.
- `WindowSettingRecordReadsTests` now fails on any read of the record's `Sorting` group, apart from `IncludeSubDirectories`, outside `FileOrder`, `AppSettings` and `SettingsManager`.
- The check marks can be set from the thread a Load or a sort runs on, as other values a tab publishes are.
- Not run by hand in the app: the check marks in the macOS native menu.
