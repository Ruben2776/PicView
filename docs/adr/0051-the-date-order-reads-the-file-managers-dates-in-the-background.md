# The Date Order Reads the File Manager's Dates in the Background

The Date field of a **File Order** (ADR-0050) is the date the file manager shows for a file. On Windows that is Explorer's Date column: the date taken of a file that has one, else the earlier of its creation and last write time, and nothing for a file with no usable time. The file system gives the second and third at no cost, but a date taken has to be read from each file, which through the shell costs about 3 ms a file: 8 seconds for a folder of 2,694 files read one at a time, and as long again on a second pass, as Windows keeps none of it.

So a folder in a Date order is listed at once by what the file system says, and its dates are read after, in the background. `FileDates` (`PicView.Core/FileSorting`) is one object for the whole application, in `TabServices.Dates`. `ReadAsync` reads the dates of the files that have none read yet, up to eight at a time, through `IFileDateSource`, and keeps each by path with the last write time it was read at, so a file written since is read again. `Known(file)` gives a date read, and `FileOrdering` takes it before the file system's dates (`DateOf`), through the `knownDate` it is made with: the tab's `OrderingWith` gives every ordering it makes `FileDates.Known`.

A tab starts the read where its files come to be in a Date order: after every list its Load gives it (`FollowListedOrder`) and after a re-sort (`ResortOnceAsync`), through `TabViewModel.ReadDates`. When the read ends, the tab checks whether its files are still in order with the dates now known (`FileOrdering.IsSorted`) and only when they are not asks for a re-sort (`ResortAsync(redate: true)`), which sorts though the order's name has not changed (`IImageIterator.Resort(ordering, again: true)`). The re-sort keeps the Shown File, as every re-sort does.

On Windows the source is `ShellFileDates` (`PicView.Core.WindowsNT/FileSorting`), offered by the Win32 app's platform service as `IPlatformSpecificService.FileDates`. It reads `System.ItemDate` from the file's shell item (`IShellItem2::GetFileTime`), the very value Explorer sorts by, so the rule above is Explorer's own and not PicView's copy of it. A file Explorer shows no date for is known to have none, and goes before every dated file. macOS and Linux offer no source, and there the Date field is the earlier of the two file system times.

## Considered Options

- **Read the dates before the list shows.** The order would be right from the start. Eight seconds, or two with the reads side by side, is too long to hold a folder back for, and the first image would wait with it where the Load lists first.
- **Read the date taken with PicView's own metadata reader and apply Explorer's rule.** It would work on every platform. It would also be PicView's reading of a rule Windows does not document, and wrong wherever the two differ; the shell's own value cannot differ from Explorer's.
- **Keep the dates in a file between runs.** A folder opened again after a restart would be in its final order at once. It needs a store, its invalidation and its pruning, for a wait of a second or two once per session.
- **Sort again whenever a read ends.** Simpler than checking. A folder whose dates were all read before, or that has no dates taken, would publish a Listed change that moves nothing, and its gallery would follow it (ADR-0010).

## Consequences

- A folder with dates taken, in a Date order, opens in the file system's order and moves into Explorer's a moment later: 1.8 seconds for 2,694 files on eight cores, with a re-sort of 7 ms. The gallery hears one more Listed change. A folder with none, or whose dates were read earlier in the session, does not move.
- Checked against a real folder of 2,693 images, 572 with a date taken: the first fourteen files matched the Explorer window sorted by Date, ascending.
- The read is not cancelled when the tab moves on: it ends in a second or two, and what it read serves the next tab on that folder. A tab closed meanwhile is not sorted.
- A file created or renamed while a folder is browsed is placed by the file system's dates until the folder is listed again, as its date is not read on its own.
- Two tabs on one folder each start a read; the second finds the dates read and only checks its order.
- Files that tie on the date go by name, then by path (ADR-0039), which may not be how Explorer breaks the tie.
- An ordering made without the dates, as App Start makes one before the tab's Load (`QuickLoad`), sorts by the file system's dates, and the tab's check uses its own ordering, which has them.
- An archive's pages are not read: their dates are their entries' (ADR-0039), stamped on each extracted file, and a date taken inside a page would put the files in another order than the entries.
- The dates are kept for the session, up to 200,000 files, past which all are dropped and read again as needed.
- `ShellFileDates` calls the shell through its vtable from pool threads, as `ExplorerFolderOrder` does (ADR-0050). It was run on Windows 11 on x64 from the test host. The Native AOT build and arm64 were not run.
- Tested with a fake `IFileDateSource`. Nothing automated reads a real file's shell date.
