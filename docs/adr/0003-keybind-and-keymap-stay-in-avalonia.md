# Keybind and Keymap Stay in PicView.Avalonia

The **Keymap** (resolution, conflict rules, defaults, persistence) and the `Keybind` type it is built on live in `PicView.Avalonia/Input`; only the **Action** list (canonical names, categories, aliases) lives in `PicView.Core`. Moving `Keybind` to Core would require a Core-owned mirror of Avalonia's ~170-value `Key` enum that must track every Avalonia upgrade, and input chords are a UI concern anyway. The test project references Avalonia, so the Keymap stays testable through its interface without a running UI.

## Consequences

Core cannot call the Keymap, so the Keymap knows nothing of favorites. Instead each of the first ten **Favorite** slots has an **Action** (`OpenFavorite1`–`OpenFavorite10`) whose Keybinds the Keymap stores like any other; `FavoritesManager` in Core owns which folder is in which slot, and `FunctionsMapper` opens the folder of the slot when the Action runs.
