# PicView

An image viewer. This glossary covers the language of input (how key presses, mouse buttons and the wheel become things the viewer does), of tabs, of saving, and of settings.

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
A generic Action, such as `Confirm`, `Cancel`, `PlayPause`, `NextFrame`, `SpeedUp` or the arrows `Up`, `Down`, `Left` and `Right`, whose meaning comes from the current Input Mode. When the mode gives it no meaning, each lower-ranked Non-blocking Input Mode that also applies is asked in turn, then Normal, and when none gives it a meaning, its Keybind falls through to the ordinary Action bound to the same Keybind. A Blocking Input Mode asks no other mode. This is why Escape stops a playing motion photo, before it closes the window, and why the arrow keys move through the Expanded Gallery but otherwise scroll a tall image, rotate it, or go to the previous or next file. `Cancel` (Escape) and `Copy` (the platform's copy keys) are fixed to their keys; the others can be rebound.
_Avoid_: Mode action, special key

**Blocking Input Mode**:
An Input Mode in which only the Actions it handles run; every other Keybind does nothing. Dialog, Renaming and Crop are blocking. Crop handles its Contextual Actions and the zoom Actions, `ZoomIn`, `ZoomOut` and `ResetZoom`, since the crop view zooms. Crop and Dialog also let Window Actions run.
_Avoid_: Modal mode, exclusive mode

**Default Action**:
What `Confirm` does in the Dialog Input Mode: the top dialog's main button, such as saving a favorite or renaming a file. A dialog without one ignores `Confirm`. While a text box or a recording Keybind box in the dialog has focus, the keys it edits with stay with it instead of acting as Keybinds.
_Avoid_: Enter action, OK button

**Non-blocking Input Mode**:
An Input Mode that overrides specific Actions, disallows Actions that need a still image (such as `Crop`), and lets every other Keybind work as usual. Motion Photo, Animated Image and Gallery are non-blocking.
_Avoid_: Overlay mode, passive mode

**Window Action**:
An Action whose only effect is to open a window of its own, or bring it forward: About, Settings, Image Information, Effects, Convert, Keybindings, Batch Resize, Resize and File Associations. Crop and Dialog let them run, since the window works outside the main view; an open menu closes as the window opens, and an open pop-up stays. Print is not one, since it may print at once.
_Avoid_: Tool Window Action, since Settings, About, Keybindings and File Associations are not Tool Windows

## Windows

**Main Window**:
A window that shows tabs of images. PicView can have several, all in one process: one opens at App Start, and more open as a new window, when a tab is dragged out of one, or when PicView is started again while it runs and the Opening Mode is New Window. Closing one that is not the last ends only that window. Closing the last one is App Shutdown, unless Batch Resize is open: then PicView keeps running until Batch Resize closes too.
What sits inside a Main Window (its buttons, menus, views and pop-ups) acts on that Main Window, even while another one is the Active Main Window.
_Avoid_: Viewer window, image window

**Active Main Window**:
The Main Window the user focused most recently. Only what sits outside every Main Window acts on it, such as Effects and the macOS app menu.
_Avoid_: Current window, focused window

**Tool Window**:
A window that works on images. Most work on what their Main Window shows: Print, Image Information, Resize Image and Convert. A Main Window has at most one of each, and they close with it. Effects works on the active tab of the Active Main Window instead, following it as the user switches windows: PicView has at most one, and it stays open until it is closed or the last Main Window closes. Batch Resize works on files the user picks: PicView has at most one, it belongs to no Main Window, and it stays open when the Main Window it was opened from closes.
_Avoid_: Secondary window, child window, dialog

**Interface Visibility**:
Which of a Main Window's bars show, their heights, and whether the hover bar may show. Derived from the window's own three interface settings and its Window Mode: fullscreen hides every bar; otherwise the titlebar follows the interface setting, the bottom bar shows only when the interface shows and its setting is on, and the hover bar may show when its setting is on and no bottom bar shows. The window derives it; nothing sets it.
_Avoid_: UI shown, hidden UI, toggle UI (as a state)

**App Window**:
A window that belongs to PicView as a whole rather than to a Main Window: Settings, Keybindings, About and File Associations. There is at most one of each, opened from any Main Window, and it stays open until it is closed or App Shutdown.
_Avoid_: Secondary window, global window, singleton window

**App Start**:
What runs once when the PicView process starts, around its first Main Window: loading the settings, the Keymap, file history and favorites. A Main Window opened later never repeats it. Starting PicView again while it runs does not run it again either: the new process hands its Source over to the running PicView and exits, and the running one opens it where the Opening Mode says: in a new tab of the Active Main Window, in place of what its active tab shows, or in a new Main Window. Only when the running PicView does not take the Source, as once App Shutdown has begun, does the new process run its own App Start.
_Avoid_: Initialization, boot, instance (for a PicView process)

**App Shutdown**:
What runs once when the PicView process ends, whether the last Main Window closes (or Batch Resize, when it outlives them), the user quits, an update is installed, or PicView restarts. It closes every Main Window, saves what they share and deletes the temp files. The most recently active Main Window is the one whose file is remembered and whose Window Settings are stored.
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

**Shown File**:
The file of the image a tab shows, and the file of the one beside it when viewing side by side. The tab answers whether a path is one, and follows it when it is renamed, overwritten or changed on disk, without showing anything new.
_Avoid_: Current file, active file

**Stand-in**:
An image a tab shows in place of its model's image without changing what it shows: a Quick Thumbnail while the next image loads, or an Effects Render. It is never recorded, and the tab publishes no change for it.
_Avoid_: Placeholder, preview image

**Source**:
What a Tab can be asked to show, given as text: a file path, a folder, an archive, or a URL.
_Avoid_: Path (for the whole set), input, argument

**Load**:
One load of a Source into a Tab: it says Loading, shows the image, then lists the files around it, watches their folder and primes the cache, and ends once the image shows, a newer Load supersedes it, or it fails. A Tab has at most one current Load; a newer one supersedes the older, which then changes nothing. Showing the image comes before listing.
_Avoid_: Navigation load, repopulate, initialize navigation, open (for the Core step)

**Titles**:
The four strings a tab derives from what it shows: the titlebar text, its tooltip, the window title and the tab's name. They follow the Shown File, its place in the tab's files, the zoom level, the Title Phase and the title settings, and they are worked out when something reads them, so a tab nobody looks at works out nothing. The tab derives them; nothing sets them.
_Avoid_: Set the title, update the title, tab title (for the whole set)

**Title Phase**:
A state a tab is told it is in, which stands in for its derived Titles until it ends: Loading, until the next image shows; Downloading, with its progress, until the image shows or fails; Error, until the next load; and Cropping, which replaces only the titlebar text and its tooltip and ends when the crop closes. No image is not a phase: a tab that shows nothing derives it.
_Avoid_: Loading title, temporary title, title override

**Busy**:
A tab is Busy while an operation on it holds a busy claim: a crop save, an optimize, setting the wallpaper or the lock screen image, a rename or a duplicate. It ends when the last claim ends, however the operation ends. A Main Window shows its loading indicator while its active tab is Busy, or while the tab's Title Phase is Loading or Downloading and the titlebar is hidden.
_Avoid_: loading indicator shown, busy flag

**Tab View**:
Which of four views a tab shows, derived from its own state: the crop control while a crop is open; otherwise the comparison viewer while the tab compares and shows something; otherwise the image viewer while the tab shows something or its Title Phase is Loading or Downloading; otherwise the start-up menu. The tab derives it; one adapter per Main Window makes the control it shows, and nothing assigns a view into a tab. The image viewer and the comparison viewer are shown in the same `NavigableView`, which holds the gallery, the click arrows, the hover bar and the gallery shortcut around them; the crop control and the menu are shown without one.
_Avoid_: Current view (as something set), switch to the viewer, switch to the start-up menu, restore the view

**View History**:
What a tab has shown, oldest to newest, with a cursor on what it shows now: each file, each page of a multi-page TIFF or an archive, and each Single Image. Back and Forward move through it and show that entry again in the same tab. Showing something new after going back drops the entries after the cursor. Images the tab never showed, such as those Next 10 jumps over, are not in it, and renaming a file or folder renames its entries. It lives only in memory and holds up to 100 entries, at most 10 of them Single Images. It moves with its tab and ends when the tab closes.
_Avoid_: Navigation history, back stack, file history (for Back and Forward)

**File History**:
The files, archives and URLs viewed most recently in any tab, listed in the dropdown menu by when each was last viewed: up to 50, plus up to 5 pinned ones. It is remembered between runs, renaming a file or folder renames its entries, and turning file history off stops it recording, the Last File and the Last Session too. Clearing it clears both too. Back and Forward don't use it.
_Avoid_: Recent files, history list

**Last File**:
The file, archive or URL that the active tab of the Active Main Window showed most recently. "Open last file" opens it, and so does App Start when it opens the last file. Closing a Main Window, and App Shutdown, make that window's active tab's file the Last File, while File History is on. With it off, or once it is cleared, there is none. A Single Image without a path never becomes it, and renaming its file, or the folder it's in, renames it.
_Avoid_: Last entry, current entry

**Last Session**:
The tabs of the last Main Window to close, in their order, each by the file, archive or URL it showed, and which of them was active. It is recorded when a Main Window closes, and at App Shutdown from the most recently active one, as Last File is. Fresh Tabs and Single Images without a path are left out, and an archive is remembered without its page. "Continue where you left off" opens it at App Start without a Source: the active tab first, then the others in the background, each in its place. A tab whose file is gone is skipped, and with nothing left, or with File History off, the start-up menu shows.
_Avoid_: TabFileHistory, restored tabs, workspace

## Images

**Side-by-Side Pair**:
Two images a tab shows beside each other when its Main Window's Side-by-Side is on: the shown image and its partner, the next image in the list. Going to the last image, whether by opening it, jumping to it, reloading it or turning Side-by-Side on, shows the pair that ends with it: the image before it and the last image. With looping, the last image pairs with the first instead. Only a list of one image shows alone. Paired navigation moves by two images at a time and always ends on a full pair. The gallery marks both images of the pair.
_Avoid_: Dual view, two-up, double view

**Thumbnail**:
A bitmap of a file no taller than a height asked for, never the full image: a Gallery Item's, a File History entry's, or a drag preview's. A tab keeps the ones it asks for until it closes, File History keeps its own for the whole run, and a drag preview keeps none. Only the tallest one asked for is kept for each file, and it serves every request up to its height. When the file's image is already in memory as a still image, it is scaled down instead of read again. A Tile Thumbnail is kept apart from these, by the app and not for long.
_Avoid_: Thumb, preview (for the bitmap)

**Quick Thumbnail**:
A Thumbnail that is there at once or not at all: one kept already, or else one the OS shell or the file's EXIF data has ready. It never decodes the image and is never kept, so it may be absent. A Stand-in shows one.
_Avoid_: Fast thumbnail, quick preview

**Tile Thumbnail**:
A Thumbnail made for the size a Masonry or Rows tile shows it at, in device pixels, so the tile is sharp. The app keeps the most recently used ones up to a memory budget and drops the oldest first, whichever tab asked. A tile whose Tile Thumbnail was dropped shows its ordinary Thumbnail until a new one is made.
_Avoid_: Sharp thumbnail, full-size thumbnail, HQ thumbnail

**Animation Playback**:
The paused state, speed and position of one animated file a Tab shows. A Tab has one for the shown image and one for the image beside it when viewing side by side. Each shown file gets a new one, which starts playing from its first frame at the Tab's Playback Speed; a Stand-in ends them. It always loops.
_Avoid_: Animation instance, GIF instance, animation state

**Playback Speed**:
How fast a Tab plays its animated images, as a multiple of the speed the file asks for: 0.25×, 0.5×, 1×, 2× or 4×. `SpeedUp` and `SlowDown` move one step, `ResetSpeed` returns to 1×. It belongs to the Tab, so it carries to the next animated image shown there, and a new Tab starts at 1×. It is not a saved setting. Whether an image is paused does not carry.
_Avoid_: Frame rate, frame delay, animation speed

**Frame Step**:
Pausing an animated image and moving it one frame forward or back, `NextFrame` and `PrevFrame`. It wraps from the last frame to the first and back.
_Avoid_: Frame advance, fast-forward, rewind, seek

**AI Metadata**:
What an AI image generator stored in the image about how it made it: the prompt, the negative prompt, the generation settings and the raw text or JSON they came from. Automatic1111 and its forks, ComfyUI, NovelAI and InvokeAI write it, in a PNG text chunk, the Exif UserComment, the JPEG comment, or hidden in the pixels. Only the Automatic1111 kind in a PNG's `parameters` chunk or a JPEG's or WebP's UserComment can be edited.
_Avoid_: Stable Diffusion metadata, SD data, generation data, PNG info

## Archives

**Archive Opening**:
One open of an archive in a Tab: it prepares the archive, shows the start page, extracts the rest in the background, and ends finished or stopped. A newer Archive Opening in the same tab stops it. Next past the last extracted page waits for the page, and gives up when the opening is stopped rather than finished.
_Avoid_: Extraction (for the whole open), archive load, archive session

**Open Archive**:
The archive a Tab reads now: the one whose page it shows, with the folder its pages are extracted to. Leaving it for a file, a folder or another archive deletes that folder once the new image shows.
_Avoid_: Current archive, last opened archive, temp zip directory

## Gallery

**Gallery Mode**:
The state a tab's gallery is in, exactly one at a time: Closed, Docked or Expanded. Each tab has its own, and closing the Expanded Gallery gives back the mode the tab had before it expanded. Cropping, showing a Single Image or a file list at or over the Gallery File Limit hides the gallery without changing its mode, and the mode cannot change while it is hidden; hiding the interface does not hide it. The tab's Main Window remembers whether a new tab of that window starts docked, which is the last choice the user made by hand in a tab of that window, and where every Docked Gallery of that window sits.
_Avoid_: Gallery state, `ActiveGalleryMode`, `IsGalleryDocked` (as a tab's current state)

**Gallery File Limit**:
The number of files at which a tab hides its gallery: a tab whose file list has that many files or more shows no gallery and loads no Gallery Items, so a folder of a hundred thousand images browses without the gallery's load. It is a Setting with no place in the Settings window, set in the settings file; 0 or less is no limit, and it is 0 by default.
_Avoid_: Gallery threshold, max gallery size, `HideGalleryAtFileCount` (in prose)

**Docked Gallery**:
The strip of thumbnails along one edge of a Main Window, top, bottom, left or right, beside the image.
_Avoid_: Bottom gallery, gallery bar

**Expanded Gallery**:
The gallery filling the Main Window in place of the image. While it is shown, the window is in the Gallery Input Mode, which gives the arrows, `Confirm` and `Cancel` their gallery meanings.
_Avoid_: Full gallery, fullscreen gallery

**Gallery Items**:
The thumbnails a tab's gallery shows: one for each file the tab browses, always in the order of its file list. They change only when that list changes, which a re-sort does too, or when the tab enters a Main Window whose gallery look needs taller thumbnails than they were loaded at, which loads them again at the new height, once. A tab keeps them while it shows a Single Image, and a gallery that is closed when its tab moves to another folder drops them and loads the new folder when it opens.
_Avoid_: Thumbnails list, gallery list

**Thumbnail Stretch**:
How a gallery's thumbnails are shaped and filled. Uniform shows each whole image in its own proportions, Uniform to Fill fills the thumbnail and crops what spills over, Square shows the whole image inside a square, and Fill Square fills a square and crops what spills over. The Docked Gallery and the Expanded Gallery each have their own. It is a Window Setting, so each Main Window has its own two.
_Avoid_: Gallery stretch, stretch mode

**Gallery Display Mode**:
How the Expanded Gallery arranges its Gallery Items: Grid, Masonry or Rows. All three scroll down. Grid fills rows of cells of one size, so the thumbnails line up in even columns; a cell is a square when the Thumbnail Stretch is, and otherwise the columns share the width. Masonry fills equal-width columns, each thumbnail in its own proportions. Rows fills rows of equal height across the whole width. It is a Window Setting, so each Main Window has its own, and the Docked Gallery has none. Masonry and Rows always fill and crop, so Thumbnail Stretch applies only to Grid. The window also remembers which of Masonry and Rows was used last, which is what the Expanded Gallery's switch button goes to from Grid.
_Avoid_: Gallery layout, view mode, Gallery Mode (that is Closed, Docked or Expanded)

## Saving

**Save Source**:
What a save writes out, decided by the save module rather than the caller: the Effects Render when there is one for the shown file, else the file on disk, else the in-memory bitmap of a Single Image. The tab's rotation and flip are applied to each. Setting a wallpaper or a lock screen image writes out the same Save Source, and a Print Job prints it. A Gallery Item that is the Shown File is set as wallpaper or lock screen image from the Save Source too; any other Gallery Item is set from its file on disk.
_Avoid_: Source file, input path

**Save Module**:
`ShownFileWriter`, in Core: every in-process write of a tab's shown image goes through it. A writer describes the job (what to read, what to change, where to write) and the module reads the Save Source or the file on disk, writes it off the UI thread with the tab Busy, and shows the result by where it went. Over itself, it shows the file again with its cached image and thumbnail forgotten and resets the turn and effects that were written into it; replacing the original, it shows the new file and then recycles the original (keeping it when the recycle fails, deleting it only on a drive with no Recycle Bin), and writes nothing when the new name is taken; a copy elsewhere changes nothing in the tab, and a copy onto the Shown File counts as over itself. A Single Image has no file, so over itself and replace become Save As; the Info-window resize does nothing for one. Batch resize is outside it.
_Avoid_: Saver, save service (for the Core module)

**Recycle Bin Backup**:
The copy the Save Module makes of a file before it writes over it. The copy is made in PicView's temp folder, not beside the original, and recycled quietly from there, with no question asked, so the Recycle Bin's Restore puts it in the temp folder. When the temp folder's drive has no Recycle Bin or the recycle fails, there is no backup: the failure is logged and the write goes ahead.
_Avoid_: Undo copy, .bak file

## Settings

**Setting**:
A value the user chooses that PicView remembers between runs, stored in one record saved whole. The four menu toggles and the Opening Mode choice, which are App Settings, publish their changes: one bindable each, which writes the stored value and saves, and what reacts to it subscribes. The rest are read from the record. A Window Setting is stored only when its window closes. Every Setting is one or the other.
_Avoid_: Option, preference, flag, mirror (for the bindable), flip (for toggling)

**Window Setting**:
A Setting that belongs to one Main Window: the Window Mode, AutoFit, top most, Stay Centered, the window's last Normal size and position, the File Order, Side-by-Side, looping, Zoom to Fit, scrolling, the three interface settings, the image background and whether it is constrained to the image, and the gallery's look: the Thumbnail Stretch and item size of the Docked Gallery and of the Expanded Gallery, the Gallery Display Mode and which of Masonry and Rows was used last, the edge a Docked Gallery sits at, and whether a new tab starts docked. Each Main Window has its own values, in its `WindowSettings`, and changing one in a window changes nothing in another. A window is seeded from the settings record at App Start, and from its opener's values when it opens while PicView runs (or from the record when no Main Window is open). Nothing is written to the record when a Window Setting changes: the window stores them when it closes, so the last window to close decides what is stored, and at App Shutdown with several windows open, the most recently active one. A crash loses the session's changes to them. The Settings window shows and edits those of the Active Main Window.
_Avoid_: Per-window option, window preference, local setting

**File Order**:
The order a Main Window's tabs list their files in: a field (name, size, extension, creation, last access or last write time, or Random) and a direction. It is a Window Setting, so each Main Window has its own, and a sort from the menu sets that of its window; every tab of the window follows it, re-sorting the files it browses in memory and keeping the Shown File, and a tab that moves to another window takes that window's. Files that tie on the field go by name, then by path, so a file created or renamed lands in one place only; under Random a new file goes last and a renamed one keeps its place. An archive's pages follow it too, from what the archive says of each entry. A tab with no window uses the settings file's order.
_Avoid_: Sort order, sort preference, sorting

**App Setting**:
A Setting that belongs to PicView as a whole: every Setting that is not a Window Setting. One value serves every Main Window, and a change is written to the record and saved at once.
_Avoid_: Global setting, shared setting

**Opening Mode**:
The App Setting that decides where a Source opens when it is pasted, dropped, or handed to the running PicView, as by double-clicking a file: New Window, Same Window or New Tab. New Tab opens it in a new tab at the end of the strip, which becomes active, or in the active tab when that is a Fresh Tab. Same Window and New Window replace what the active tab shows, except that a hand-over in New Window opens a new Main Window. Pasting or dropping several files opens the first as the mode says and makes it active; each other file opens in the background, in a new tab under New Tab, and in a new Main Window under Same Window and New Window. Opening from inside PicView, as from File History, Favorites or the open dialog, always replaces what the active tab shows.
_Avoid_: Open in same window, open in new tab (for the setting)

**Settings Search**:
The search box of the Settings window. Typing fills a list of Suggestions and changes nothing else: the page is not filtered, dimmed or scrolled, and the category list stays whole. Every word typed must be found, in any order and ignoring case. Picking a Suggestion, by clicking it or with Enter on the selected one (the first, until the arrow keys choose another), ends the search: the box is cleared, the list closes, and the page goes where the Suggestion leads, so nothing is left to dismiss.
_Avoid_: Filter, settings filter, highlight (for what a pick does)

**Suggestion**:
One entry of the Settings Search list. There is one for each Setting Row, shown with its category beside it, and one for each category. A Setting Row's Suggestion scrolls the row to the middle of the page, pulses it once and moves focus into it; a category's scrolls to the top of that category and pulses nothing. Categories are listed first, then rows whose name has every word typed, then rows found by their Search Keywords. A row or category the Settings window does not show on this system is not suggested.
_Avoid_: Search result, match, hit

**Setting Row**:
One labelled control of the Settings window, or the few controls that set one thing, such as the four buttons for the edge a Docked Gallery sits at. Each has a key that its Suggestion names, so the two are tied by the key and not by matching text. A category that is a single row, as Language, Application shortcuts and File associations are, has no Setting Row: its category Suggestion leads to it.
_Avoid_: Search block, search border, setting block

**Search Keyword**:
A word that finds a Suggestion without being its name: the choices of a Setting Row, such as "Navigate backwards" for "When deleting a file", the name of its group, and English synonyms. A choice is never a Suggestion of its own.
_Avoid_: Tag, search tag

## Sizing

**AutoFit**:
The setting of a Normal window where the window takes its size from the image its active tab shows, worked out before the window resizes so it never visibly resizes twice, and again whenever another tab becomes active. A tab that shows no image, such as a Fresh Tab's start-up menu, never changes the window's size: it fills what the window leaves it after the Reserved Space instead. With it off, the window keeps the size the user gave it and the image fits inside it. It is a Window Setting: turning it on or off, or resizing the window by hand, which turns it off, affects only that window.
_Avoid_: Auto-size, auto resize, Manual

**Window Mode**:
The state a window is in, exactly one at a time: Normal, Minimized, Maximized or Fullscreen. Fullscreen covers the screen, Maximized fills the work area, and a Normal window follows the shown image with AutoFit, else keeps the size the user gave it. A maximized window fits the image to itself, as a Normal window without AutoFit does. Leaving Fullscreen or Minimized gives back the mode the window had before it, so a maximized window that went fullscreen is maximized again; leaving Maximized gives back Normal. Nothing is resized while a window is minimized. Each window has its own Window Mode, whether the user changes it in PicView or through the operating system's window controls. The Window Mode is a Window Setting: a window opened while PicView runs starts in the mode of the window it was opened from, and the settings remember the mode of the last Main Window closed, which App Start reads, and so does a window opened while no Main Window is open; they never remember Minimized, nor Fullscreen and Maximized together. A window opened while PicView is running never starts fullscreen.
_Avoid_: Scaling mode, sizing mode, Manual

**Zoom to Fit**:
The setting that lets an image scale past its native pixel size to fill the Available Area. With it off, an image is never shown larger than 100%. It is a Window Setting, so each Main Window has its own.
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
The setting that keeps an AutoFit window centered in its Work Area as it changes size. With it off, the window keeps its own center as it resizes. It is a Window Setting, so each Main Window has its own; the Settings window edits that of the Active Main Window.
_Avoid_: Keep centered, center window

**Placement**:
Where a Main Window sits. At App Start, an AutoFit window opens centered in the Work Area of the screen it opens on; a window that keeps its size opens at its last position; and a window that starts fullscreen or maximized opens in that state on the last screen it was closed from. A window opened while PicView runs opens on top of the window it was opened from, and takes its size from that window, not from the settings record, and a window opened for a dragged-out tab opens where the cursor is, with AutoFit or without. From then on, an AutoFit window is placed after each change of its size, a Fit included: centered in its Work Area with Stay Centered, else on its own center. Turning AutoFit on places it the same way; turning it off leaves it where it is. Returning to Normal from fullscreen or maximized centers an AutoFit window in the Work Area of the screen it is on, and gives a window that keeps its size its previous size and position back. A fullscreen or maximized window, or one that keeps its size, is otherwise not moved. The window places itself; nothing centers it, except App Start once and the user's Center action.
_Avoid_: Center the window (as something a path does), window position (for the rule)

**Screen Margin**:
The total gap AutoFit keeps free around the window, beyond what the taskbar, dock and menu bar already take. It looks the same at any display scaling.
_Avoid_: Window margin, padding

**Fit**:
The result of fitting an image, or a side-by-side pair, into the Available Area: its shown size, its Fit Scale, its Scroll Area, and the window size that AutoFit applies. A side-by-side pair is fitted as one image made of both, scaled to the same height. A pair whose second size is not known yet has no Fit, so the window waits instead of resizing twice. The window derives it whenever one of its inputs changes; nothing asks for it.
_Avoid_: Target size, image size, layout, refit (as something a path asks for)

**Scroll Area**:
With scrolling on, the area the image scrolls in. It fills the Available Area, except with AutoFit, where it takes the image's shown width and as much of its height as the Available Area allows. The image keeps its Fit Scale and sits centered in it.
_Avoid_: Scroll viewer size, scrollable area

**Fit Scale**:
The factor that takes an image from its pixel size to its shown size when it is fitted. With the Zoom Factor it gives the Zoom Level.
_Avoid_: Aspect ratio, initial zoom

**Zoom Factor**:
How far the user has zoomed from the Fit: 1 as fitted, 2 at twice the fitted size. A wheel step, a pinch and the zoom Actions change it. It goes back to 1 when the tab shows another image while zoom resets on image change, and when the user resets the zoom.
_Avoid_: Scale, user zoom, zoom amount

**Zoom Level**:
The percent of its pixel size an image is shown at: its Fit Scale times its Zoom Factor, times 100, rounded. 100 means one screen pixel per image pixel. The title shows it. The tab derives it; nothing sets it.
_Avoid_: Zoom percentage, zoom value

**Pixel Info**:
A comparison mode that shows A alone and reads the pixel under the pointer: its place in the image's own pixels, counted from 0 at the top left however the image is rotated or flipped, and its color as hex, with the alpha only when the pixel is not opaque. The pixels are the image's as PicView decodes it: turned upright by its EXIF orientation and converted to sRGB, so a file stored turned, or with another color profile, reads as it is shown and not as it is stored. The compare bar writes the reading and keeps the last one while the pointer is off the image. Its own button turns it on, and off again to the mode it was turned on from; it is not in the cycle of the other five modes. An image without pixels, such as an SVG, gives a place and no color.
_Avoid_: Color picker, pointer position, coordinates mode

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
