# A Tab Derives Its Titles

A **Tab**'s **Titles** (the titlebar text, its tooltip, the window title and the tab's name) are derived by one module the tab owns, `TabTitles` in `PicView.Core/Titles`, from the tab's own state: the Shown File and its model, the file's index and the count of the tab's files, the zoom level, the **Title Phase**, whether the tab is cropping or shows a Single Image, and the two title settings. Nothing outside the tab writes a title. The strings are worked out when a binding or a reader asks for them: every input only marks them stale and notifies, and the next read builds all four once and keeps them until the next mark. A tab whose titles nothing binds builds nothing, a wheel zoom builds at most once per frame, and an image change followed by the window's zoom reset builds once, not twice.

The inputs reach the module from inside the tab: `Publish` marks after the image bindable is set, so showing the image still comes first (ADR-0014); the tab's `Crop` setter enters and leaves the Cropping phase; `SetLoading`, `SetDownloading(text)` and `SetError(text)` on the tab enter the other phases, and the next shown image ends them; three subscriptions on the tab's `ZoomLevel`, `NavigationIndex` and `MaxIndex`, made once per tab, follow zoom and list changes. One public `Refresh()` remains for the two settings that have no change notification, `ShowFullPathInTitleBar` and `ShowImageSideBySide`, called by their toggles. `ImageTitleFormatter` stays the implementation that formats the strings.

Before this, `UpdateTabTitle()` was called from 13 modules at 22 sites, `SetLoading` and `SetNewTabTitle` from 9 more, and the title bindables were written directly by `NavigationService` (download progress and error), `CropService` and `StartUpMenu`. Each caller guessed when the title was stale; `MainView` patched a blank title when a tab was selected and `MainWindow` set it by hand after a tab moved. `UpdateTabTitle` also returned early unless the tab `IsSelected`, a flag only ever set to true, so the guard gated a tab until its first selection and nothing after. The error title for a failed download was written to the tab's name alone and overwritten by the `finally` that followed it. `TabTooltip` had no reader left and is deleted.

## Considered Options

- **Keep `UpdateTabTitle` and reduce its callers**: the tab's own methods call it and the outside callers go. It leaves the rule "call this when the title may be stale" as a convention, which is how 22 call sites accumulated, and it builds the strings on every call, including once on `Shown` and once again when the window resets the zoom.
- **A reactive derivation**, the four strings as read-only bindables combined from the inputs, as `TabOrientation` derives its bindables: one writer by construction, but every input change builds the strings whether or not anything reads them, and an image change builds them twice, once with the previous image's zoom. The title is on the image-change path, so it does the least work instead.
- **The zoom write as the only trigger after an image shows**: one build per image change, but `ZoomLevel` notifies only when its value changes, so two images that both fit at 100% leave the previous title in place; a renamed Shown File never changes the zoom; and a Core test of the title would have to write the zoom by hand to get one.
- **No image as a Title Phase**, set by `BecomeFreshAsync`, `StartUpMenu` and `MainView`: a tab that shows no file and no Single Image already knows it shows nothing, so it is derived, and the three writers go.
- **Phases set on the module by anyone**: the tab stays the one writer of its state (ADR-0014), so the phases are methods on the tab and the module's setters are internal.

## Consequences

- A new input to a title is a mark inside the tab or one of its three subscriptions, never a call from another module. `Refresh()` is the one call allowed from outside, and it goes when the settings publish their changes.
- The window's reaction to a shown image (ADR-0014) no longer sets the title; it still writes `ZoomLevel`, which marks the titles. The zoom level's own three writers are unchanged here and are the next slice.
- A tab derives its titles whether or not it is the active tab; the window binds the active tab's. The selection and moved-tab patches in `MainView` and `MainWindow`, and `StartUpMenu`'s writes, are deleted.
- Error holds all four strings until the next load, instead of being lost.
- `TabTooltip` is deleted. The bindings and readers of the other four move to `tab.Titles`.
- Core stays free of Avalonia types; the module is plain `INotifyPropertyChanged`, and Avalonia binds it as it binds the R3 bindables.
- ADR-0012 holds: the module only reads. ADR-0005's startup order holds: the first title is derived when the titlebar first reads it.
