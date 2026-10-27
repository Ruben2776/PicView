# App Start and App Shutdown Run Once; Main Windows Open and Close Through One Module

**App Start** and **App Shutdown** run once per process, in `AppLifecycle`, and every **Main Window** opens and closes through `MainWindowLifecycle`, whichever way it opens (at App Start, as a new window, or as a detached tab). Before this, app-wide steps and per-window steps were mixed in one sequence that each caller put together by hand. Opening a second window reloaded file history from disk, dropping the entries of the session so far, and closing any window deleted the temp files of every window.

## Considered Options

- **Reload the shared files (file history, favorites, Keymap) whenever a Main Window opens**, as before. A new window then picked up changes another PicView process had saved while "open in same window" was off. That syncing was already unreliable, because each process overwrites the history file when it closes, and it cost every open window the history it had not saved yet. Keeping several processes in sync is not a goal.
- **One opener for every secondary window, per Main Window**, as before. Each Main Window could then open its own Settings, Keybindings and About window, although those belong to PicView as a whole.

## Consequences

- The shared files load at App Start and are never read again while PicView runs. Closing any Main Window still saves them, so a crash loses less, but only App Shutdown deletes temp files and exits.
- **App Windows** (Settings, Keybindings, About, File Associations) have one opener for the whole app and no owner. **Tool Windows** (Print, Effects, Image Information, Resize Image, Convert) have one opener per Main Window and close with it on every platform.
- Batch Resize is the one Tool Window that belongs to the whole app: it works on files the user picks rather than on what a Main Window shows, so it has one opener for the whole app and no owner, like the App Windows. A long run should not be lost by closing the window it was started from.
- Closing the last Main Window, quitting and installing an update all go through App Shutdown. It closes every Main Window, and the most recently active one decides the remembered file and size. The exception is closing the last Main Window while Batch Resize is open: the App Windows close, Batch Resize stays open and PicView keeps running, and closing Batch Resize then runs App Shutdown. Quitting and installing an update still cancel a running Batch Resize.
- Restarting (including resetting the settings) is App Shutdown followed by a new PicView started with no file, whose App Start follows the settings as usual, rather than reopening the active tab's file. Resetting deletes the settings file after App Shutdown has saved it, so the new App Start finds none.
- The platform `App` files keep no reference to a first window. The print preview opens on the Main Window that asked for it, while taskbar progress and later launches go to the most recently active one, since Core reports download progress without knowing its window.
- The startup order inside `QuickLoad` (ADR-0005) is unchanged.
