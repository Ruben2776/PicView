# PicView

A lightweight, fast image viewer with tabs, image preloading, editing, and gallery navigation.

## Language

**Clipboard Transfer**:
The transfer of image bitmap data, text, file references, or base64 encodings to or from the operating system clipboard.
_Avoid_: File duplication, clipboard command, clipboard animation

**File Duplication**:
Creating an on-disk copy of an image file and navigating to the newly generated file within the active viewer tab.
_Avoid_: Clipboard duplicate, copy-paste in place

**Paste Pipeline**:
The ordered ingestion sequence (Files -> Text/URLs/Base64 -> Bitmap) that decodes and routes clipboard contents into the viewer or tab model.
_Avoid_: Paste handler, multi-format paste, clipboard reader

**Favorite**:
A user-curated bookmark to a folder on disk, optionally owning a **Keybind** in the **Keymap** for instant navigation.
_Avoid_: Pinned history entry, favorite file, file bookmark

**Keybind**:
A single input chord: a key, mouse button, or wheel direction, plus modifiers. Wheel directions name the physical movement of the wheel.
_Avoid_: Shortcut, hotkey, binding

**Action**:
A canonical, bindable name for something the viewer can do (e.g. `ToggleFullscreen`), possibly reachable through aliases.
_Avoid_: Command, function

**Keymap**:
The complete assignment of **Keybinds** to owners, where an owner is either an **Action** or a **Favorite**. Each Keybind has at most one owner; assigning a Keybind takes it from its previous owner.
_Avoid_: Shortcuts, custom shortcuts, keybindings dictionary

**File History**:
The image files, archives, and web URLs viewed most recently in any tab, ordered by when each was last viewed, with entry pinning. Back and Forward don't use it; they step through each tab's **View History**.
_Avoid_: Favorites, folder bookmarks

**Custom Title Bar**:
The title bar PicView draws itself (tabs, menus, address bar, window caption buttons) on platforms without a native menu bar. It is identical on Windows and Linux, and Windows is its reference. macOS uses the native menu bar instead.
_Avoid_: Win title bar, Linux titlebar, platform title bar

**Gallery Appearance**:
How a tab's gallery looks in its shown Gallery Mode, derived from that mode, the dock position and the gallery settings.
_Avoid_: Gallery layout, gallery settings, thumb settings
