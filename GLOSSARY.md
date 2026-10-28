# PicView

An image viewer. This glossary covers the language of input (how key presses, mouse buttons and the wheel become things the viewer does), of tabs, and of saving.

## Input

**Action**:
A named thing the viewer can do in response to input, such as `Next` or `ZoomIn`.
_Avoid_: Function, command, shortcut

**Hidden Action**:
An Action that can be bound in the Keymap but is not listed in the keybindings view, such as `Rotate90` or `ToggleSubdirectories`. Buttons and menus run it like any other Action.
_Avoid_: Internal action, unlisted action

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
A generic Action, such as `Confirm`, `Cancel` or `PlayPause`, whose meaning comes from the current Input Mode. When the mode gives it no meaning, each lower-ranked Non-blocking Input Mode that also applies is asked in turn, then Normal, and when none gives it a meaning, its Keybind falls through to the ordinary Action bound to the same Keybind. A Blocking Input Mode asks no other mode. This is why Escape stops a playing motion photo, then a slideshow, before it closes the window. `Cancel` (Escape) and `Copy` (the platform's copy keys) are fixed to their keys; the others can be rebound.
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

## Windows

**Main Window**:
A window that shows tabs of images. PicView can have several: one opens at App Start, and more open as a new window or when a tab is dragged out of one. Closing one that is not the last ends only that window. Closing the last one is App Shutdown, unless Batch Resize is open: then PicView keeps running until Batch Resize closes too.
_Avoid_: Viewer window, image window

**Tool Window**:
A window that works on images. Most work on what their Main Window shows: Print, Effects, Image Information, Resize Image and Convert. A Main Window has at most one of each, and they close with it. Batch Resize works on files the user picks instead: PicView has at most one, it belongs to no Main Window, and it stays open when the Main Window it was opened from closes.
_Avoid_: Secondary window, child window, dialog

**App Window**:
A window that belongs to PicView as a whole rather than to a Main Window: Settings, Keybindings, About and File Associations. There is at most one of each, opened from any Main Window, and it stays open until it is closed or App Shutdown.
_Avoid_: Secondary window, global window, singleton window

**App Start**:
What runs once when the PicView process starts, around its first Main Window: loading the settings, the Keymap, file history and favorites. A Main Window opened later never repeats it.
_Avoid_: Initialization, boot

**App Shutdown**:
What runs once when the PicView process ends, whether the last Main Window closes (or Batch Resize, when it outlives them), the user quits, or an update is installed. It closes every Main Window, saves what they share and deletes the temp files. The most recently active Main Window is the one whose file and size are remembered.
_Avoid_: Exit, quit, close

## Tabs

**Tab**:
A place in a Main Window that shows one image and browses the files around it. It belongs to exactly one Main Window at a time. Dragging it out moves it, onto the Main Window it is dropped on or into a new one opened for it, and closing or moving it never touches the other tabs. A Main Window always has at least one tab, and its tab strip shows only when it has two or more, so the last tab can be neither closed nor dragged out.
_Avoid_: Page, document, view, detach (for moving a tab)

**Fresh Tab**:
A tab that has never browsed files, or whose files were all deleted, which makes it fresh again: it shows the start-up menu and no longer follows that folder. Opening a file into one continues from the remembered start-up directory when browsing subdirectories; showing a Single Image does not make a tab fresh again.
_Avoid_: New tab, uninitialized tab, empty tab

**Single Image**:
An image a tab shows on its own, without the files around it to browse: a clipboard image, a base64 image, a temporary file or a URL download. It has its own model, and showing a file again ends it.
_Avoid_: Standalone image, loose image

## Images

**Side-by-Side Pair**:
Two images a tab shows beside each other when Side-by-Side is on: the shown image and its partner, the next image in the list. The last image has no partner unless the list loops, and a file with no partner shows alone. Paired navigation moves by two images at a time.
_Avoid_: Dual view, two-up, double view

## Gallery

**Docked Gallery**:
The strip of thumbnails along one edge of a Main Window, top, bottom, left or right, beside the image.
_Avoid_: Bottom gallery, gallery bar

**Expanded Gallery**:
The gallery filling the Main Window in place of the image.
_Avoid_: Full gallery, fullscreen gallery

**Thumbnail Stretch**:
How a gallery's thumbnails are shaped and filled. Uniform shows each whole image in its own proportions, Uniform to Fill fills the thumbnail and crops what spills over, Square shows the whole image inside a square, and Fill Square fills a square and crops what spills over. The Docked Gallery and the Expanded Gallery each have their own, and every Main Window uses the same two.
_Avoid_: Gallery stretch, stretch mode

## Saving

**Save Source**:
What a save writes out, decided by the save module rather than the caller: the Effects Render when there is one for the shown file, else the file on disk, else the in-memory bitmap of a Single Image. The tab's rotation and flip are applied to each. Setting a wallpaper writes out the same Save Source, and a Print Job prints it.
_Avoid_: Source file, input path

## Sizing

**AutoFit**:
The setting of a Normal window where the window takes its size from the image its active tab shows, worked out before the window resizes so it never visibly resizes twice, and again whenever another tab becomes active. A tab that shows no image, such as a Fresh Tab's start-up menu, never changes the window's size: it fills what the window leaves it after the Reserved Space instead. With it off, the window keeps the size the user gave it and the image fits inside it. Turning AutoFit on or off applies to every window, while resizing a window by hand turns it off for only that window.
_Avoid_: Auto-size, auto resize, Manual

**Window Mode**:
The state a window is in, exactly one at a time: Normal, Minimized, Maximized or Fullscreen. Fullscreen covers the screen, Maximized fills the work area, and a Normal window follows the shown image with AutoFit, else keeps the size the user gave it. A maximized window fits the image to itself, as a Normal window without AutoFit does. Leaving Fullscreen or Minimized gives back the mode the window had before it, so a maximized window that went fullscreen is maximized again; leaving Maximized gives back Normal. Nothing is resized while a window is minimized. Each window has its own Window Mode, whether the user changes it in PicView or through the operating system's window controls. The settings only remember the mode a new window starts in, which is the last one the user chose in any window; they never remember Minimized, nor Fullscreen and Maximized together. A window opened while PicView is running never starts fullscreen. A slideshow shows its window fullscreen without changing the remembered mode, and stopping it gives back the mode the window had.
_Avoid_: Scaling mode, sizing mode, Manual

**Zoom to Fit**:
The setting that lets an image scale past its native pixel size to fill the Available Area. With it off, an image is never shown larger than 100%.
_Avoid_: Stretch, upscale

**Reserved Space**:
The parts of the window that are not the image: titlebar, tab strip, bottom bar and a Docked Gallery. It is taken away before the image is fitted. The tab strip is part of it only while the window has two or more tabs, so the window is fitted again whenever the strip shows or hides, with AutoFit on or off.
_Avoid_: UI size, chrome, container

**Available Area**:
The largest area the image may fill: the screen in fullscreen, the work area minus the Screen Margin with AutoFit, else the window's current size, each minus the Reserved Space.
_Avoid_: Max size, container size

**Work Area**:
The part of a window's own screen that the taskbar, dock and menu bar leave free. Each window uses the screen it is on now, so windows on different monitors fit and center independently.
_Avoid_: Monitor size, screen size, working area

**Stay Centered**:
The setting that keeps an AutoFit window centered in its Work Area as it changes size. With it off, the window keeps its own center as it resizes.
_Avoid_: Keep centered, center window

**Screen Margin**:
The total gap AutoFit keeps free around the window, beyond what the taskbar, dock and menu bar already take. It looks the same at any display scaling.
_Avoid_: Window margin, padding

**Fit**:
The result of fitting an image, or a side-by-side pair, into the Available Area: its shown size, its Fit Scale, its Scroll Area, and the window size that AutoFit applies. A side-by-side pair is fitted as one image made of both, scaled to the same height. A pair whose second size is not known yet has no Fit, so the window waits instead of resizing twice.
_Avoid_: Target size, image size, layout

**Scroll Area**:
With scrolling on, the area the image scrolls in. It fills the Available Area, except with AutoFit, where it takes the image's shown width and as much of its height as the Available Area allows. The image keeps its Fit Scale and sits centered in it.
_Avoid_: Scroll viewer size, scrollable area

**Fit Scale**:
The factor that takes an image from its pixel size to its shown size when it is fitted. It becomes the starting zoom.
_Avoid_: Aspect ratio, initial zoom

**Quarter Turn**:
A rotation by a multiple of 90°, the only rotation a tab has. One or three quarter turns swap the image's width and height for fitting.
_Avoid_: Rotation angle, arbitrary rotation

**Orientation**:
A tab's Quarter Turn and whether its image is flipped. It goes back to upright and unflipped when a Single Image is shown, and when the tab moves to another image while zoom resets on image change.
_Avoid_: Rotation, transform, ScaleX

## Effects

**Effects Render**:
One file rendered with the current effects. It exists only while some effect differs from its default, and belongs to the file it was rendered from in the active tab: navigating to another image, or switching to another tab, discards it.
_Avoid_: Processed image, effected image, edited image

## Printing

**Print Job**:
The Save Source of the tab a Main Window shows, the Print Settings, and the Printed Page they make, until it is sent to a Printer. The Print window prepares one with a preview, following the Main Window's active tab and that tab's navigation, so what is sent is always the page the preview shows; printing with the preview off and Save as PDF from the menu send one straight away with the remembered Print Settings. There is none while Side-by-Side is on.
_Avoid_: Print session, print dialog

**Printed Page**:
The page as it comes out of the Printer: the paper in its orientation, the margins, and the image placed inside them by the Scale Mode. The preview shows the same Printed Page that is sent.
_Avoid_: Print image, print output, render

**Print Settings**:
The Printer, paper, orientation, Scale Mode, color mode, copies and margins of a Print Job. The last ones used in the Print window are remembered for every later Print Job, whether or not it opens the Print window.
_Avoid_: Print options, page setup

**Printer**:
Where a Print Job is sent: one of the system's printers, or Save as PDF. A remembered Printer stays the same when the language changes.
_Avoid_: Print engine, device

**Save as PDF**:
The Printer that writes the Printed Page to a PDF file instead of paper. It is always first among the Printers.
_Avoid_: PDF printer, PDF export

**Scale Mode**:
How the image is placed inside the margins of a Printed Page. Fit shows all of it as large as it fits (not the window's Fit), Fill covers the area and crops what spills over, Stretch fills the area without keeping the image's proportions, and Center keeps the image at its size at 300 DPI, shrinking it only when it does not fit.
_Avoid_: Print scaling, fit mode

## Batch Resize

**Batch Resize Job**:
The files, output folder, conversion, compression, quality, resize and thumbnails of one Batch Resize run, fixed when it starts. Changing the settings while it runs changes only the next one. A file whose output would overwrite an existing file is copied to a backup folder first.
_Avoid_: Batch settings, batch options

**Batch Thumbnail**:
An extra, smaller copy of each file a Batch Resize Job writes, into its own subfolder of the output folder. It is resized from the source file by a percentage, a width or a height, and takes the job's conversion, quality and compression. A job has at most seven.
_Avoid_: Thumb, preview
