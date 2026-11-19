# Settings Start Writing After the Window Is Shown

`GlobalSettingsViewModel` subscribes nothing in its constructor; it only reads `Settings` into its bindables, so it is made with the first Main Window's view model without the cost of sixteen subscriptions (ADR-0005 puts the window first). The write-throughs (ADR-0020) start in two groups, when a change to the setting becomes possible. `InitializeActionSettings()` starts the fourteen an Action can toggle (looping, side by side, scrolling, zoom to fit, subdirectories, top most, the three interface settings, file history, full path in the title bar, taskbar progress, open in same window and constrain background): App Start calls it once the first Main Window is opened, before any input can arrive, so no Action checks anything, and then makes its own background and file-watcher followers (open in same window has had none since ADR-0037: each hand-over reads the setting). `InitializeExtraSettings()` starts `ShowFadeButtons`, `WindowMargin` and `GalleryMouseWheelBehavior`: `SettingsViewModel` calls it when it is made, and the keybindings view and the settings search still call it when they open. Both are idempotent and safe from any thread, and a second caller returns only once the subscriptions exist. A tab calls `InitializeActionSettings()` before it follows, since its relist reads the stored value; a later Main Window or tab finds it done.

A follower that subscribes before the window is shown is notified before the write-through, so it reads the published value, not `Settings`: `TabTitles` for the full path in the title, and `WindowFitter` for zoom to fit, scrolling and side by side.

## Considered Options

- **One delayed group**, all seventeen starting once the window is opened. Simpler, but the three nobody may ever touch still subscribe at every launch.
- **Each setting writes itself** when its value changes, with no subscription. Cheapest and order-safe, but it depends on the R3 fork allowing the override, and the extra settings would no longer wait for the Settings window.
- **Only the settings the Settings window reaches in the extra group.** Six of the fourteen are shown only there, but each has an Action a user can bind a key to, so a shortcut would change the value and never write it. They stay with the Actions' group.

## Consequences

- A new published setting picks its group by whether an Action can change it: if one can, it is in `InitializeActionSettings`; if only the Settings window can, it is in `InitializeExtraSettings`.
- No write-through, and none of App Start's own followers, starts before the first Main Window is opened; a window's and a tab's own followers still subscribe as they are made. A tab loading at App Start may start the write-throughs slightly before the window is shown on some paths (the window show is posted, not awaited, when AutoFit is off for a side-by-side pair or the size ping fails); no toggle can be lost either way, as both precede any input. A change before an initializer writes and saves nothing.
- A follower that reads the record synchronously calls `InitializeActionSettings()` before it subscribes, as the tab does; one that may subscribe earlier reads the published value.
- `GallerySharedSettingsViewModel.Start()` starts the gallery settings' write-throughs and commands in one group, called by App Start next to `InitializeActionSettings()`, because the gallery's own menus reach them without the Settings window. A tab opened at App Start follows the shared gallery bindables before the write-throughs start, so a gallery follower reads the published value, not `Settings`.
- Supersedes ADR-0020's "the fourteen are eager" and "made first in the constructor"; the rest of it holds, including that the write-through subscribes before a later follower, so that follower reads the new value.
- Superseded in part by ADR-0038: `InitializeActionSettings` starts thirteen settings, since top most moved to the Main Window's `WindowSettings`.
- Superseded in part by ADR-0040: `InitializeActionSettings` starts five settings, since eight more moved to the Main Window's `WindowSettings`.
