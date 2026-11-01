# View History Follows What Each Tab Shows

Each **Tab** keeps its own **View History**, and Back and Forward step through the active tab's View History and show each entry again in that tab. They are still the `FileHistoryPrev` and `FileHistoryNext` Actions, since `keybindings.json` stores Actions by name. Before this, they walked **File History**: one static list with a single cursor, which every tab and every Main Window moved from whichever thread its load ran on. It held each path once, in the order it was first added, and never a Single Image, so Forward after revisiting a file went to the files first seen after it, and stepping past either end opened the last file.

A tab records its View History in the only three methods that show an image, `TabViewModel.Display`, `DisplaySingleImage` and `ShowPage`. Each records once the image is shown, so recording never delays showing. The same recording feeds File History and the **Last File**.

A step only marks a target, and the cursor moves when the tab shows that entry. So the history follows what is on screen, even when loads fail or overtake each other.

A rename reaches every history before a tab shows the renamed file. `FileRenames` is the one entry point: it renames in File History, the Last File and the View History of every tab in every Main Window, and the folder watcher, `RenameHelper` and `ResizeImageViewModel` call it before the tab shows the file under its new name. Otherwise that showing would be recorded as a new view, and the tab's forward entries would be dropped.

## Considered Options

- **Record where each load starts**: each `FileHistoryManager.Add` would become a push onto the tab's history, and the paste, drop and base64 paths would get pushes too. It is the smallest change, but it keeps the scattered calls behind the old bugs, and every new load path has to remember to push. It records what was asked for, not what was shown, so a load that fails or is overtaken by a newer one still pushes, and Back and Forward would have to suppress the push at every call site.
- **Watch each tab's bindings from outside**: subscribing to a tab's `Image` and `FileInfo` on the UI thread needs no lock and leaves the loaders alone. But those properties also change for the thumbnail shown while an image loads, so the watcher would have to guess which changes are views, and the subscriptions would live in the Avalonia layer, out of reach of Core tests.

## Consequences

- A new way of showing an image has to go through one of the three methods, or the tab never records it.
- `FileHistoryManager.Add` has one caller: a tab's recording.
- Back and Forward are per tab, and they no longer open the last file at either end.
- The file history setting only governs File History: Back, Forward and the Last File work with it off.
- Only stepping by one turns a TIFF's page. A jump such as Next 10 goes to the file it was given.
- A load that Back or Forward opens must finish only once its image shows. Otherwise the step ends before its target shows, and the tab records the target as a brand-new view, dropping the forward entries. `NavigationService.LoadFromFileAsync` used to return early for a file in another folder; it now waits until that file shows.
