# PicView

An image viewer. This glossary covers the language of input: how key presses, mouse buttons and the wheel become things the viewer does.

## Input

**Action**:
A named thing the viewer can do in response to input, such as `Next` or `ZoomIn`.
_Avoid_: Function, command, shortcut

**Keybind**:
One input chord: a key or mouse button (including the wheel) together with its modifiers.
_Avoid_: Hotkey, shortcut, key binding

**Keymap**:
The assignment of Keybinds to Actions that the user can view and rebind.
_Avoid_: Keybindings, shortcut map

**Input Mode**:
The single mode a window is in, which decides what its Keybinds do. It follows from the window's own UI and its active tab, and when several apply, the highest-ranked one wins.
_Avoid_: Context, state, focus, dialog mode

**Contextual Action**:
A generic Action, such as `Confirm`, `Cancel`, `PlayPause` or `Copy`, whose meaning comes from the current Input Mode. When the mode gives it no meaning, its Keybind falls through to the ordinary Action bound to the same Keybind.
_Avoid_: Mode action, special key

**Blocking Input Mode**:
An Input Mode in which only the Actions it handles run; every other Keybind does nothing. Dialog, Renaming and Crop are blocking.
_Avoid_: Modal mode, exclusive mode

**Non-blocking Input Mode**:
An Input Mode that overrides specific Actions, disallows Actions that need a still image (such as `Crop`), and lets every other Keybind work as usual. Slideshow, Motion Photo and Animated Image are non-blocking.
_Avoid_: Overlay mode, passive mode
