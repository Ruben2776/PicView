# A Load Follows the File Manager's Order

With **Follow Explorer Sorting** on, a Load that lists a folder first asks the platform's file manager how it shows that folder, and gives the **Listed Directory** that order (issue #328). On Windows the file manager is Explorer: a folder sorted by date modified, descending, in an open Explorer window is browsed in that order in PicView.

The seam is `IFolderOrderSource` (`PicView.Core/FileSorting`): `OrderOf(directory)` gives the `FileOrder` the file manager shows the folder in, or null. A platform offers one through `IPlatformSpecificService.FolderOrders`, which is null by default, and `AppLifecycle` gives it to the one `DirectoryOrders` (ADR-0048). macOS and Linux offer none, so nothing is asked there.

`DirectoryOrders.FollowAsync(directory)` asks, off the caller's thread, and waits no longer than half a second. With an answer, a directory tabs already list takes that order and those tabs start sorting again; the answer is returned, and the Load lists in it and hands it to `TabViewModel.ListDirectory`, so a directory no tab listed starts with it. Without an answer the directory keeps the order it has, or starts with the **Default File Order**. So at a Load the order is, in turn: the file manager's, the directory's own, the default.

The Load asks where it lists: `TabLoad.ListAroundAsync` for a file and for a reload, `ListFolderAsync` for an opened folder, and `QuickLoad` where App Start lists before the tab's Load does (an opened folder, and Side-by-Side), which passes what it took in `LoadOptions.FollowedOrder`. A Load that steps to a file already in the tab's list lists nothing and asks nothing. An archive is never asked for.

Follow Explorer Sorting is an App Setting, `Sorting.FollowExplorerSorting` in the settings file, on by default, so a settings file from before it follows. It publishes its changes as `GlobalSettingsViewModel.IsFollowingExplorerSorting` (ADR-0020). Turned on, `DirectoryOrders.FollowAllAsync` asks for every folder a tab lists now. Turned off, every directory keeps the order it has.

## On Windows

`ExplorerFolderOrder` (`PicView.Core.WindowsNT/FileSorting`) is the Windows source, offered by the Win32 app's platform service. It enumerates the shell's open windows (`IShellWindows`), and for each one whose folder view is on the directory reads the first sort column with its direction and the group column with its direction (`IFolderView2::GetSortColumns`, `GetGroupBy`). The folder a view shows is compared by its file system path, without case, so a search result, a library or another path to the same folder is not a match.

`ExplorerSort` turns what was read into a `FileOrder`, and is the part that is unit tested:

| Explorer column | File Order field |
|---|---|
| Name | Name |
| Size | File size |
| Date modified | Last write time |
| Date created | Creation time |
| Date accessed | Last access time |
| Date | Date |
| Type, Date taken and every other column | none: not followed |

Date (`System.ItemDate`) is the column a picture folder is sorted by unless another is chosen. Explorer shows in it the date taken of a file that has one, and else the earlier of its creation and last write time; a file with no usable time has no date and is listed first when ascending. The Date field of a File Order (`FileOrdering.DateOf`) gives the same: the date read from Explorer once it is read (ADR-0051), and until then what the file system says, the earlier of the two times, and no date, which goes before every dated file, for a file with either of them missing.

A view grouped by another column than it is sorted by, or with its groups in the other direction, is not followed, since the groups come first and no one field and direction gives that order. When several views show the folder, as two Explorer windows or two Explorer tabs do, the folder is followed in the order of the one the user sees and last used: the tab shown in the Explorer window that is furthest in front (`ExplorerSort.Followed`). A tab behind another in its window is passed over, and when the view in front is in no File Order the folder is not followed, as another window's order is not what the user sees. Only when every view of the folder is a tab behind another do they have to agree.

The interfaces are called through their vtables with function pointers. `IShellWindows` is an `IDispatch` interface, which source-generated COM does not declare, and function pointers need no marshalling code under Native AOT.

## Considered Options

- **Take the literal item order of the Explorer view.** It would match Explorer for any column and any grouping. No `FileOrdering` reproduces such a list, so a created or renamed file could only be appended, as under Random, and a re-sort could not restore it (ADR-0039's guarantee). A `FileOrder` keeps inserts where a sort would put them, at the cost of the columns it has no field for.
- **Read the saved view state of a folder from the registry.** It would work with Explorer closed. The format is undocumented binary and could change with a Windows update; the live window is the documented way.
- **A sort field of its own, "Explorer", in the menu.** It would need a second field beside it for when Explorer has no answer. A setting leaves the sort menu as it is, and the menu's sort still works on top of it.
- **Follow the file manager's sort as it changes.** Explorer gives no cheap event for it, so it would be polled. The order is read once per Load, and a reload reads it again.
- **Make the file manager's order the Default File Order too.** One folder opened from a date-sorted Explorer window would then change how every other folder opens. Only a sort made in PicView writes the default.

## Consequences

- The file manager's order wins over a sort made in PicView earlier: opening another file from the folder, or reloading, asks again and re-sorts every tab on the directory. A sort from the menu then holds until the next Load that lists the folder.
- A menu sort writes to the Default File Order only what was chosen, the field or the direction (`DirectoryOrders.SortAsync`'s `asDefault`). Choosing Descending in a folder Explorer shows by size makes that folder size descending and the default's direction descending, and leaves the default's field as it was.
- Stepping to the next or previous folder is a Load that lists, so with an Explorer window on that folder the order can change, field and direction, from one folder to the next.
- The first image does not wait for the answer when it shows before the listing, which is every Load of one image. Where the Load lists first, an opened folder and Side-by-Side, the image waits for the answer, at most the half second.
- An ask that outruns the limit is left to finish on its pool thread, and the file manager is not asked again until it has, so a hung Explorer holds one thread and not one per Load. Loads meanwhile are not followed.
- Any failure of the source is logged with `DebugHelper.LogDebug` and reads as no answer.
- The answer is only as good as the source's mapping: a view the source cannot express as a `FileOrder` gives null, and the folder opens as if no window showed it.
- `LoadOptions.FollowedOrder` is the one way a caller that lists for the Load can pass the order on; files given without it are of a directory that starts with the default.
- `WindowSettingRecordReadsTests` lets `Sorting.FollowExplorerSorting` be read anywhere, as `IncludeSubDirectories` is: both are App Settings of their own and not the Default File Order.
- Dates taken are read after the list shows, in the background, and the files sorted again where that changes their order (ADR-0051), as reading one through the shell costs about 3 ms a file.
- Date is a field like the others: `SortFilesByDate` is its Action, in the sort category of the keybindings, with an item in the sort submenus and the macOS native menu, labeled with the existing `Date` translation. It is 7 in `Sorting.SortPreference`, which a version from before it reads as Name.
- An archive's pages go by their entry's modified time under Date, as under the other date fields, and an extracted page's file, stamped with that time on its creation and last write time, has the same date.
- Explorer's Type column sorts by the type's name, so `.jpg` and `.jpeg` are one type; mapping it to the Extension field would give an order that is nearly Explorer's, which reads as a bug, so it is left out.
- The views were first required to agree, which left a folder unfollowed whenever a second Explorer window or a forgotten tab showed it in another order, as the first real use found. The view in front wins instead. Telling it needs a call that only works from a single-threaded apartment (`IOleWindow::GetWindow`, which is input-synchronous), so `ExplorerFolderOrder` reads the views on a thread of its own in one, and it rests on undocumented behaviour: each Explorer tab has a `ShellTabWindowClass` window in its Explorer window, the first of which is the tab shown. Where that stops holding, every tab counts as shown and the window in front decides alone.
- Which window is furthest in front is its place among the desktop's top-level windows, so an Explorer window used last is followed though PicView was opened from elsewhere.
- Only the first sort column is read, and files that tie on it go by name, then by path (ADR-0039), which may not be how Explorer breaks the tie.
- The item is in the sort submenu of the main context menu and of the title bar menu, hidden where no file manager is asked (`GlobalSettingsViewModel.CanFollowExplorerSorting`), and absent from the macOS native menu. `ToggleFollowExplorerSorting` is its Action, with no default key.
- The other languages show the English text for the item until it is translated; Danish has its own.
- Tested with a fake `IFolderOrderSource`, and `ExplorerSort` with views given as data. `QuickLoad`'s two asks at App Start are not covered by a test.
- The live read was checked on Windows 11 from the test host, from a pool thread: against one Explorer window sorted by name and by Date, against two Explorer windows on one folder in different orders, each brought to the front in turn, and with the front one's tab put behind a new tab. Grouped views, an elevated PicView and the Native AOT build were not run.
