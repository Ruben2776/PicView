# The Gallery Follows Its Tab's File List

A tab's **Gallery Items** change only because its file list changed: the tab's `ImageIterator` reports every change (a new list, a file inserted, removed, moved or replaced) to the gallery through a narrow delegate that `TabViewModel` passes to each iterator it creates, as `Preloader` takes its delegate. The gallery loads the first time it is shown, and nothing outside it starts, restarts, sorts or clears a load. Before this, six callers in two projects (`NavigationService`, `NavigationSubscriptions`, `QuickLoad`, `SettingsUpdater`, `ToggleUIVisibility` and `FolderChanges`) each decided when the items were stale, with their own guards, so every gallery fix landed in a caller and the gallery module had no tests of its own.

## Considered Options

- **Callers tell the gallery when to load**, as before: the rule for when the items are stale was copied six times and the copies drifted. A closed gallery kept the previous folder's items, and a sort during a load added the rest in the old order.
- **The iterator publishes only whole lists and the gallery compares them**: one writer, but every single-file change would compare lists of thousands of files.

## Consequences

- A folder switch while the gallery is closed drops its items, and the new folder loads when it opens. A sort during a load restarts the load in the new order.
- Showing a Single Image disposes the tab's iterator, but the gallery keeps its items. When the next iterator reports its first list, the gallery keeps them if it is the same files in the same order and reloads otherwise.
- The gallery's mode (closed, docked, expanded) is unchanged by this decision: the gallery only reads whether it is shown.
- The startup order of ADR-0005 holds: the gallery loads once navigation starts, after the image.
