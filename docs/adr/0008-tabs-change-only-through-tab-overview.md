# A Main Window's Tabs Change Only Through TabOverviewViewModel in Core

Every change to a **Main Window**'s tabs (create, first open, select, close, move, reorder, and tearing them all down when the window closes) goes through that window's `TabOverviewViewModel` in Core. Each **Tab** keeps one reference to the window that owns it, and only this module writes it, so closing a tab always reaches the window it is in now. Before this, the tab list had four writers (`TabOverviewViewModel`, `MainWindow.MoveTab`, `TabNavigationInitializer.InitializeDetachedWindow` and `DraggableTabControl`), and a moved tab kept the close action and image subscriptions of the window it left. Its close button then disposed a tab still shown in another window, its image changes refit the wrong window, and closing a Main Window that was not the last never disposed its tabs.

The window's UI hookups for a tab (frame-provider subscriptions, refits, the start-up menu) are UI work, so they sit behind an adapter that Avalonia provides and tests fake. The module attaches them on a tab's first open, not when the tab or window is created, which keeps the startup order of ADR-0005, and moves them along with the tab.

## Considered Options

- **An Avalonia module that owns the tabs and drives `TabOverviewViewModel`**: the hookups are Avalonia's, but which window owns a tab and what closing or moving it means are not UI rules. In Avalonia they could only be tested with a headless window, and Core navigation, which already reads a tab's window, would keep a second, unowned copy of the answer.
- **The tab strip edits the tab list itself**, as `DraggableTabControl` did when reordering, dragging a tab out and previewing a drop: every edit bypassed the active tab and the owner, and a drag preview put the tab in the target window's list before anything was dropped. The strip now only reports what the user did, and the preview stays inside the strip.
- **A provisional insert during the drag preview** that changes no owner: hovering is not a move, and it would widen the module's interface for a visual effect.

## Consequences

- Dropping a tab on another Main Window and dropping it elsewhere are one move. A window opened by a move starts with that tab rather than a blank one that is replaced.
- The module refuses to close or move a window's last tab. The tab strip hides with one tab, so the UI cannot ask for it anyway.
- The window refits whenever its tab strip shows or hides and whenever another tab becomes active, because the module reports both. A tab showing no image never changes the window's size.
- `MainWindowLifecycle` asks the module to tear down a closing window's tabs instead of cleaning them up itself (ADR-0007 is otherwise unchanged).
