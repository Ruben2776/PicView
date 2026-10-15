# Contextual Actions Fall Through to Ordinary Keybinds

The **Keymap** has two layers: ordinary Keybinds, where each Keybind has one **Action**, and contextual Keybinds bound to **Contextual Actions** (`Confirm`, `Cancel`, `PlayPause`, `Copy`), whose meaning comes from the window's **Input Mode**. A Keybind may be in both layers: the Contextual Action runs when the current Input Mode (or, among non-blocking modes, the next-ranked one) gives it a meaning, and otherwise the Keybind falls through to its ordinary Action. This is why Enter can be both `Confirm` and `ToggleFullscreen`, and why Esc stops a motion photo, then a slideshow, then reaches `Close`.

## Considered Options

- **One owner per Keybind across all modes** (the flat keymap as it was): Esc, Enter and Space would collide constantly, since many modes want them. Mode keys stayed hard-coded instead, which made them impossible to show or rebind; `GalleryClick` showed the cost, claiming Enter even when the gallery was not expanded.
- **A separate keymap section per Input Mode**, each with its own mode-specific Actions (`SaveCrop`, `ToggleMotionPhoto`, …): every mode would need its own bindings for what is really the same intent, and a user rebinding "confirm" would have to do it once per mode.

## Consequences

- `keybindings.json` gains a section for contextual Keybinds alongside the ordinary ones.
- The keybindings window shows Contextual Actions in their own category, and its conflict check only compares them with each other.
