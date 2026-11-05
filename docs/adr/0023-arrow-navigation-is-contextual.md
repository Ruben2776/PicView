# Arrow Navigation Is Contextual

`Up`, `Down`, `Left` and `Right` are **Contextual Actions**, and the **Expanded Gallery** is a Non-blocking **Input Mode**, Gallery, ranked above Motion Photo, Animated Image and Slideshow, since it covers the image, and below the Blocking modes. While the active tab's gallery is shown expanded, the Gallery mode moves the selection with the four arrows, opens the selected item with `Confirm` and closes the gallery with `Cancel`. In the Normal mode `Up` and `Down` scroll the image one line when scrolling is on and the image is taller than the viewer. Every other press falls through (ADR-0004) to the ordinary Action on the same Keybind: the default Keymap binds the arrow keys and WASD to `RotateRight`, `RotateLeft`, `Prev` and `Next` as well. `ArrowKeyNavigation` is deleted, and no module on the Keybind path asks whether the gallery is expanded.

Before this, three modules asked it themselves: `ContextualActionHandler` gave `Confirm` its gallery meaning in the Normal mode, `ArrowKeyNavigation` chose between moving through the gallery, scrolling and rotating for the ordinary `Up` and `Down`, and `TabOverviewViewModel.NavigateDirectionalAsync` moved the gallery selection instead of the file for `Next` and `Prev`. Escape was not claimed in the gallery, so it left fullscreen or closed the window.

## Considered Options

- **A mode overrides ordinary Actions.** After the Keymap resolves a Keybind, each active Non-blocking mode is asked whether it takes the ordinary Action, so the gallery would take `Next`, `Prev`, `Up` and `Down`. It needs ADR-0004 amended and lets any mode redefine any Action, which is harder to see in the keybindings window than a Contextual Action.
- **Hidden gallery Actions in the Keymap.** `Keymap.Resolve(keybind, InputMode.Gallery)` would return Actions such as `GalleryUp`. They could not be shown or rebound, and the Keymap would hold mode logic.
- **Contextual `Confirm` and `Cancel` only.** Smallest, but two of the three checks would stay.

## Consequences

- `Up` and `Down` are no longer ordinary Actions; `RotateRight` and `RotateLeft` are shown in the Scroll and Rotate category instead, and the four arrows in the Contextual category.
- Settings version 3.1 migrates keybindings files: each Keybind bound to `Up` or `Down` also gets the ordinary `RotateRight` or `RotateLeft`, unless it has an ordinary Action, so it keeps rotating when nothing claims it, and the default `Left` and `Right` Keybinds are added where the contextual layer leaves them free. A Contextual Action in a file's ordinary section does not replace what its contextual section binds.
- `Next` and `Prev` reached another way, such as the wheel or a button, change the tab's image behind the gallery, and the gallery's selection follows the tab's file (ADR-0010).
- The Gallery mode disallows Actions that need a still image, like the other Non-blocking modes, and leaves `PlayPause` to the modes below it.
