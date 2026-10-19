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
What a save writes out, decided by the save module rather than the caller: the Effects Render when there is one for the shown file, else the file on disk, else the in-memory bitmap of a Single Image. The tab's rotation and flip are applied to each. Setting a wallpaper writes out the same Save Source.
_Avoid_: Source file, input path

## Sizing

**AutoFit**:
The window setting where the window takes its size from the shown image, worked out before the window resizes so it never visibly resizes twice. With it off, the image fits inside whatever size the window already has.
_Avoid_: Auto-size, auto resize

**Zoom to Fit**:
The setting that lets an image scale past its native pixel size to fill the Available Area. With it off, an image is never shown larger than 100%.
_Avoid_: Stretch, upscale

**Reserved Space**:
The parts of the window that are not the image: titlebar, tab strip, bottom bar and a docked gallery. It is taken away before the image is fitted.
_Avoid_: UI size, chrome, container

**Available Area**:
The largest area the image may fill: the screen in fullscreen, the work area minus the Screen Margin with AutoFit, else the window's current size, each minus the Reserved Space.
_Avoid_: Max size, container size

**Screen Margin**:
The total gap AutoFit keeps free around the window, beyond what the taskbar, dock and menu bar already take. It looks the same at any display scaling.
_Avoid_: Window margin, padding

**Fit**:
The result of fitting an image, or a side-by-side pair, into the Available Area: its shown size, its Fit Scale, the scrollable area's size, and the window size that AutoFit applies. A side-by-side pair is fitted as one image made of both, scaled to the same height. A pair whose second size is not known yet has no Fit, so the window waits instead of resizing twice.
_Avoid_: Target size, image size, layout

**Fit Scale**:
The factor that takes an image from its pixel size to its shown size when it is fitted. It becomes the starting zoom.
_Avoid_: Aspect ratio, initial zoom

**Quarter Turn**:
A rotation by a multiple of 90°, the only rotation a tab has. One or three quarter turns swap the image's width and height for fitting.
_Avoid_: Rotation angle, arbitrary rotation

## Effects

**Effects Render**:
One file rendered with the current effects. It exists only while some effect differs from its default, and belongs to the file it was rendered from in the active tab: navigating to another image, or switching to another tab, discards it.
_Avoid_: Processed image, effected image, edited image
