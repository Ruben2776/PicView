# Keybind and Keymap Stay in PicView.Avalonia

The **Keymap** (resolution, conflict rules, defaults, persistence) and the `Keybind` type it is built on live in `PicView.Avalonia/Input`; only the **Action** list (canonical names, categories, aliases) lives in `PicView.Core`. Moving `Keybind` to Core would require a Core-owned mirror of Avalonia's ~170-value `Key` enum that must track every Avalonia upgrade, and input chords are a UI concern anyway. The test project references Avalonia, so the Keymap stays testable through its interface without a running UI.

## Consequences

Core cannot call the Keymap, so favorites changes reach it by pull: `FavoritesManager` exposes a version counter that the Keymap checks before resolving, and on rebuild a **Favorite**'s Keybind takes the chord from any **Action** holding it.
