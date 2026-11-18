# Each Main Window Owns Its Slideshow

Each **Main Window** has one **Slideshow**, `MainWindowViewModel.Slideshow`, a plain class in `PicView.Core/Navigation`. It is built from what the window already has: whether its active tab can go forwards, `TabOverviewViewModel.NextFile`, the window's `IWindowModeSwitcher` and its `WindowModeState`. It ticks on a `TimeProvider` from `CoreViewModel`, and it posts each tick to the thread it was started on. `InputModes.StateOf` reads only the window's own slideshow, so a slideshow puts only its own window in the Slideshow **Input Mode**. Escape, Close, the `Slideshow` Action and the menu entries act on the window they were used in. Closing a window disposes its slideshow first in `MainWindowLifecycle.CloseAsync`. App Shutdown closes every window that way. The one part every window shares is the screensaver: `CoreViewModel.Screensaver`, a `ScreensaverHold`, turns it off at the first hold and back on when the last hold is released. It runs on the UI thread, because Windows keeps that setting per thread. Leaving the slideshow's fullscreen any other way, such as toggling fullscreen or minimizing, stops the slideshow. That stop is posted, so it does not change the Window Mode while the mode is still being published. Each run has a token, so a tick or subscription from a run that already ended does nothing.

Before this, `PicView.Avalonia/Navigation/Slideshow.cs` kept the running flag, the timer and the fullscreen subscription in statics, which contradicted the glossary's "the single mode a window is in". While any window ran a slideshow, every Main Window was in the Slideshow Input Mode. Three defects followed:
- Escape in another window stopped the slideshow with that window's view model, so the slideshow window stayed fullscreen.
- Crop was disallowed and PlayPause did nothing in every window, and a rebound Close in another window stopped the slideshow instead of closing that window.
- Closing the slideshow window with the title-bar X left the timer ticking on a torn-down tab, and every other window still in the Slideshow mode.

## Considered Options

- **One owner field on the static**, naming the window that started the slideshow. It fixes the three defects, but it keeps one slideshow for the whole app, while each window should be able to run its own.
- **A per-window object on `MainWindow`** that keeps `DispatcherTimer`. It is the smallest move, but its rules stay in Avalonia, so its tests need a headless window and a real timer, and callers that have only the view model have to reach the window.
- **Folding the slideshow into `WindowModeSwitcher`**, since `WindowModeState` already knows when a window is fullscreen because of a slideshow. A timer there breaks ADR-0006: the switcher only changes modes.

## Consequences

- Several windows can run slideshows at once, each with its own interval.
- Pressing Slideshow while one runs stops it, even on the last file. Before, the next-file check came first, so the press did nothing there.
- A slideshow's tick now runs `NextFile` on the UI thread. The `System.Timers.Timer` ran it on a thread-pool thread.
- A slideshow timer setting below one second uses one second. The old timer threw on an interval of zero.
- The rules are tested in plain xUnit, with a clock the test moves.
- `TooltipHelper` still keeps its state in statics of the same shape; changing that is left for later.
- Pausing with PlayPause is still a TODO. While a slideshow runs, PlayPause is claimed and does nothing.
