# A Load Reuses a Listing Another Tab Holds

A Load that would list a directory first asks whether another tab already lists it, and takes that tab's file list instead of reading and sorting the folder again. `TabLoad.ListedByAnotherTab` asks `DirectoryOrders.TabsListing` (ADR-0048) for the tabs on the **Listed Directory** and takes the first one whose list a listing now would give:

- it was listed with the same Include Subdirectories value as the setting has now (`TabViewModel.IsListedWithSubdirectories`, recorded with the Listed Directory);
- that tab's own file watch is on the Listed Directory itself (`IFileWatcherService.IsWatching`), so its list has followed every file event since it was listed;
- its list has the file being opened.

`IImageIterator.Snapshot()` gives the list and the ordering it is in under the iterator's lock, so a re-sort between two reads cannot hand over a list in one order named as another. The Load gives its own iterator that list with that ordering, and `FollowListedOrder` checks it against the directory's order as after any listing.

The two tabs then hold separate lists of the same `FileInfo` objects. Each applies the folder's events to its own list (ADR-0010, ADR-0014 and ADR-0019 are unchanged), so the lists stay equal without being one.

A reload (`LoadOptions.Relist`) never reuses: it is how the folder is read again from the disk. An archive is never reused, as each Archive Opening extracts to its own folder (ADR-0013). A Load does not wait for another tab's listing that is still running.

## Considered Options

- **One shared list per directory.** It would hold one array and apply each folder event once. `ImageIterator` keeps the list, the ordering, the tab's indices, its cache list and its gallery callback under one lock, and `FolderChanges` mutates and reacts in one step, so a second tab on a shared list would take the wrong branch on a delete, rename or overwrite the first already applied. It means splitting the iterator into a list and a per-tab cursor with no step in between that keeps the app correct. Reuse removes the second listing and sort, which is most of the cost, for a change confined to the Load. A shared list can be built on `DirectoryOrders` later, as its own decision.
- **Reuse whenever any tab lists the directory.** A tab opened on a file in a subfolder of its start-up directory watches only that subfolder, so its list of the start-up directory is not kept current for the sibling folders. Taking it would spread a list no watch maintains.
- **Join a listing still in progress.** It would help several files of one folder opened at once, but it ties one tab's Load to another's supersession (ADR-0019). Those Loads each list, as before.

## Consequences

- A second tab on a large folder opens without listing or sorting it, and both tabs compare the sizes and dates the same `FileInfo` objects cached, so they agree on a sort by size or date.
- `FileInfo` is not immutable. A `Refresh()` of one, as the Image Information window does for the shown file, changes the cached values the other tab's sorted list holds too.
- A reused list is as old as the other tab's. What its watch missed is missing here too, until a reload.
- Under Random the joining tab starts with the other tab's shuffle, and each shuffles its own list from then on (ADR-0048).
- When Include Subdirectories changes, every tab lists again from the disk, as a reload does, so a directory with several tabs is listed once per tab there.
- `IFileWatcherService.IsWatching` has a default that says no, so a watcher that cannot tell never lends a list.
- `ListAround` and the folder Load read Include Subdirectories once and record it with the Listed Directory. Files a caller listed (`LoadOptions.Files`) are taken as listed with the setting as it is when the Load gives them to the tab.
