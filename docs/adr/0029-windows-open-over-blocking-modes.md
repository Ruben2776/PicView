# Windows Open Over Blocking Modes

A **Window Action** is an Action whose only effect is to open a window of its own, or bring it forward when it is open: `AboutWindow`, `SettingsWindow`, `ImageInfoWindow`, `EffectsWindow`, `ConvertWindow`, `KeybindingsWindow`, `BatchResizeWindow`, `ResizeWindow` and `FileAssociationsWindow`. Core names them in one place, `KeybindCategoryDefinition.IsWindowAction`, a hand-written pattern so the Keybind path does not allocate. The Crop and Dialog **Blocking Input Modes** let a Window Action's Keybind run: `InputMode.Allows` says so, and `Keymap.Resolve` already runs exactly what a blocking mode allows. The window works outside the main view, so the crop stays as it was behind it, and an open pop-up stays open. An open drop-down or main menu closes, as it already did for any window, in `SecondaryWindowOpener`. Renaming still allows nothing. In Dialog only the keyboard runs them: mouse buttons and the wheel still do nothing while a dialog is open, since clicks belong to the dialog, and a focused text box keeps the keys it edits with, so while typing only a chorded Window Keybind gets through. `Print` is not a Window Action, since it may print at once instead of showing its preview, nor are `ShowFileProperties` and `OpenWith`, which open the operating system's dialogs.

Before this, a blocking mode let through only what it handled itself, so the Settings Keybind did nothing while cropping or while the main menu was open.

## Considered Options

- **Per-mode allow lists**, each blocking mode naming every Action it lets through. The window list would be repeated in Crop and Dialog, and a new window could be added to one and missed in the other.
- **A flag per Action** (safe in blocking modes, needs a still image, …) in one table. More general than two classes need.
- **A separate Menu Input Mode**, split from Dialog and blocking less. It would fix menus only, not Crop, and adds a mode for one difference.

## Consequences

- ADR-0027's "every other Keybind still does nothing while cropping" now excepts Window Actions.
- A new window Action joins `IsWindowAction`. A test pins it to the keybindings view's ToolWindows category plus `FileAssociationsWindow`, so one added to that category without it fails.
