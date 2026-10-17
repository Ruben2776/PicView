# PicView

An image viewer. This glossary covers the language of input (how key presses, mouse buttons and the wheel become things the viewer does), of tabs, and of saving.

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
A generic Action, such as `Confirm`, `Cancel` or `PlayPause`, whose meaning comes from the current Input Mode. When the mode gives it no meaning, its Keybind falls through to the ordinary Action bound to the same Keybind. `Cancel` (Escape) and `Copy` (the platform's copy keys) are fixed to their keys; the others can be rebound.
_Avoid_: Mode action, special key

**Blocking Input Mode**:
An Input Mode in which only the Actions it handles run; every other Keybind does nothing. Dialog, Renaming and Crop are blocking.
_Avoid_: Modal mode, exclusive mode

**Default Action**:
What `Confirm` does in the Dialog Input Mode: the top dialog's main button, such as saving a favorite or renaming a file. A dialog without one ignores `Confirm`. While a text box or a recording Keybind box in the dialog has focus, the keys it edits with stay with it instead of acting as Keybinds.
_Avoid_: Enter action, OK button

**Non-blocking Input Mode**:
An Input Mode that overrides specific Actions, disallows Actions that need a still image (such as `Crop`), and lets every other Keybind work as usual. Slideshow, Motion Photo and Animated Image are non-blocking.
_Avoid_: Overlay mode, passive mode

## Tabs

**Fresh Tab**:
A tab that has never browsed files. Opening a file into one continues from the remembered start-up directory when browsing subdirectories; showing a Single Image does not make a tab fresh again.
_Avoid_: New tab, uninitialized tab

**Single Image**:
An image a tab shows on its own, without the files around it to browse: a clipboard image, a base64 image, a temporary file or a URL download. It has its own model, and showing a file again ends it.
_Avoid_: Standalone image, loose image

## Saving

**Save Source**:
What a save writes out, decided by the save module rather than the caller: the shown file's image with effects applied when the effects window has one, else the file on disk, else the in-memory bitmap of a Single Image. The tab's rotation and flip are applied to each.
_Avoid_: Source file, input path
