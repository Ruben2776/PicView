# A Main Window Derives Its Loading Indicator

A Main Window's loading indicator is derived, and nothing sets it. `MainWindowViewModel.IsLoadingIndicatorShown` shows while the window's active tab is **Busy**, or while that tab's **Title Phase** is Loading or Downloading and the titlebar is hidden. The derivation is in Core, in the window's constructor, beside the interface visibility it reads (ADR-0021), so it is tested without a window.

A tab is Busy while an operation on it holds a busy claim. An operation takes one with `using var busy = tab.ClaimBusy();` and holds it for as long as it wants the indicator shown. Claims count, so the tab stays Busy until the last claim ends. A claim is released once, however often it is disposed, and `using` releases it when the operation throws, is cancelled or returns early. `TabTitles.SaysLoading` publishes whether the titles show Loading or Downloading; it moved there from `MainWindowLifecycle`. The operations that hold claims are crop save, optimize, setting the wallpaper, rename, duplicate and the image info window's rename.

Before this, the indicator was a writable bindable with seven writers. `MainWindowLifecycle` derived the Loading half and wrote it, and six operations wrote `true` and then `false` by hand. Each write overwrote the others: a rename that ended mid-Load hid the indicator the Load wanted shown, the image info rename's nested rename hid it while image info was still updating, and a throw in crop save or optimize, which had no `finally`, left it on. Setting the wallpaper didn't await the platform call, so the indicator cleared before the wallpaper was set and platform errors skipped the catch. ADR-0019 left these writers as the next slice.

## Considered Options

- **Claims on the Main Window.** A claim count per window is simpler, but the indicator would stay on while another tab is shown. Claims on the tab follow the active tab, as the Title Phase does.
- **Int tickets, as the Loading phase uses.** The Loading ticket exists because a shown image also ends the phase. A busy claim has only one way to end, so a ticket adds bookkeeping and still needs a `try/finally` at every caller.
- **A wrapper, `WhileBusyAsync(func)`.** It can't be misused, but the busy span is often only part of a method, and each call would allocate a closure.
- **Claims follow the titlebar rule too.** The titlebar says Loading and Downloading, but says nothing about a crop save or an optimize, so for those the indicator is the only sign of work.

## Consequences

- A busy claim shows the indicator whether or not the titlebar is shown; Loading and Downloading show it only while the titlebar is hidden.
- The indicator follows the active tab. Switching away from a tab that is still busy hides it, and switching back shows it again.
- Setting the wallpaper now awaits the platform call, so platform errors reach the toast.
- The gallery item's wallpaper calls still don't await the platform call. They show no indicator, and they belong with the other operations on a target file.
