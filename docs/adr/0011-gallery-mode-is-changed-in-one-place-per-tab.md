# Gallery Mode Is Changed in One Place per Tab

Each tab has its own **Gallery Mode** (Closed, Docked or Expanded), and every change to it goes through one module per tab, `GalleryModeSwitcher`, which sits in Core next to `GalleryMode` and is owned by the tab's `GalleryViewModel`. It follows the pattern of ADR-0006: the rules live in one place, and the Avalonia side is an adapter. The gallery has no native work, so the whole switcher is in Core, and `GalleryAnimationControl` only animates the change the switcher publishes as `(from, to, animate)`. Before this, eight modules in two projects wrote a tab's mode, and the docked state lived in the app-wide `Settings.Gallery.IsGalleryDocked`, so cropping in one window closed the Docked Gallery of every tab in every window.

The switcher is the only writer. Its commands are `ToggleExpanded`, `ToggleDocked`, `Contract`, `Close`, `DockAt(position)` and `Start(docked)`; the XAML commands on `GalleryViewModel` are thin bindings to them. Closing the Expanded Gallery gives back the mode the tab had before it expanded. Whether a change animates is passed with the change, not stored in the mode, so `GalleryMode.DockedNoAnimation` is gone.

Crop and a Single Image hide the gallery without changing its mode. The switcher watches the tab's own state for them, so `CropService`, `UpdateImage` and `NavigationService` no longer touch the gallery. Hiding and showing never animate, and the switcher refuses mode changes while the gallery is hidden. Hiding the interface does not hide the gallery.

## Considered Options

- **The settings flag as the live docked state**, as before: it is shared by every tab and window, so one tab's crop, Single Image or toggle changed what every other tab showed, and each writer had to save and restore it by hand (`CropService` kept `_wasGalleryDocked` and the previous dock position).
- **Docked state per Main Window**: tabs of one window would still disturb each other, and each tab already has its own gallery view.
- **A view model with one command per gesture** (toggle, contract-or-close, open-or-close-docked, close, set dock position, set mode), as before: each command carried its own copy of the rules, and the rules read the shared flag.
- **Crop and Single Image write the mode and restore it afterwards**: every hider needs its own saved state, and a restore after a change made in between gives back the wrong mode.
- **Mode changes apply while hidden and show once the hider goes away**: a gallery could change while nobody can see it, and expanding during a Single Image would show the previous folder's items.

## Consequences

- `Settings.Gallery.IsGalleryDocked` only remembers whether a new tab starts docked: the last mode the user chose by hand in any tab. It is written and saved only by the user's commands, never by crop or a Single Image. New tabs, new windows and QuickLoad of an uninitialized tab start in it without animation, so ADR-0005's startup path keeps preparing the Docked Gallery before navigation starts.
- `Settings.Gallery.DockPosition` stays an app-wide preference and has no `Closed` value. A saved `Closed` loads as `Bottom`. Changing it moves every docked tab; docking puts only the tab that asked into Docked mode.
- Window fitting and the HoverBar read only whether the gallery is shown docked and where. A hidden gallery reserves no space. The `IsTop/Bottom/Left/RightDocked` flags are derived from the mode, whether it is shown and the dock position, and nothing else writes them.
- The gallery's Gallery Items (ADR-0010) follow the mode, not the hiders: a gallery hidden by crop or a Single Image keeps its items and comes back without reloading.
- `GalleryManager` is deleted. Thumbnail Stretch and item size stay app-wide.
- New tabs and consecutive pasted files are still refused while the active tab's gallery is docked. The shared state that blocked them is gone, so lifting that limit is its own follow-up.
