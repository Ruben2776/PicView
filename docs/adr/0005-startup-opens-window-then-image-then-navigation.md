# Startup Shows the Window, Then the Image, Then Initializes Navigation

Opening a source at startup follows a fixed order, tuned by hand for how the first seconds feel, and lives in `QuickLoad` (`PicView.Avalonia/StartUp`): first the window, then the image, and only then navigation (file list, iterator, cache, file watcher, subscriptions). With `AutoFit` off the window is shown immediately; with `AutoFit` on the image's pixel size is pinged first and the window is sized to it before it is shown, so it never visibly resizes. When `ShowImageSideBySide` is on, the file list has to be retrieved first to know which file sits beside the opened one; with `AutoFit` on both images are pinged and the window is sized for the pair before it is shown, and with it off the images are shown right after retrieval. Initializing navigation before the window and image would delay both, so navigation is not set up when the window is created.

## Considered Options

- **Initialize navigation when the window is created and open every source through the Core load** (`NavigationService.RepopulateIterator` already shows the image before listing files): it removes the startup path, but `RepopulateIterator` knows nothing of window sizing and must not, since window sizing is UI and UI logic never goes into `PicView.Core`. The window would be shown at the wrong size and resized once the image arrived.

## Consequences

- Every caller opens a source through one Avalonia entry point; it runs the startup order only at startup. A tab opened later is initialized first and loaded through Core, since its window is already visible and sized.
- Archives and URLs at startup show the window and then load through Core, so there is one URL download and one archive load.
