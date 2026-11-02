# FunctionsMapper arms are pure dispatch

Architecture review candidate 9. Enforces ADR-0002 and ADR-0009.

## Goal

Every private arm of `FunctionsMapper` only null-checks and delegates in one call. Rules, threading choices and `Application.Current` lookups live in helpers, where they can be tested. Behaviour is unchanged, no Action is added or removed, the table is still built once, and the press path stays allocation-free.

## Not in scope

- The ~62 arms that already forward in one call keep their helper signatures. No uniform `(vm, mainWindow)` signature.
- `NewTab` keeps its gallery-docked guard and TODO as it is (a temporary rule).
- The clipboard arms keep their service null check, which ADR-0002 allows.

## Changes

### 1. Scroll and zoom behind `IZoomScrollTarget`

- `IZoomScrollTarget` gains `ScrollToHome()`, `ScrollToEnd()`, `ZoomIn()` and `ZoomOut()`. `ScrollLine` already exists. `ImageViewer` implements them explicitly, as it does `ScrollLine`.
- Like `ScrollLine`, these must be called on the UI thread. Keyboard Actions can arrive off it, so a new static helper `ViewerActions` (in `PicView.Avalonia/Input`) owns the hop: it takes the active tab's `CurrentView` as an `IZoomScrollTarget` and does nothing when the view is not one, then dispatches to the UI thread when it is not on it.
- `ViewerActions` has `ScrollLine(vm, down)`, `ScrollToTop(vm)`, `ScrollToBottom(vm)`, `ZoomIn(vm)` and `ZoomOut(vm)`. The arms `ScrollUp`, `ScrollDown`, `ScrollToTop`, `ScrollToBottom`, `ZoomIn` and `ZoomOut` become one call each. The `ImageViewer` casts leave the mapper.
- Today's `ZoomIn`/`ZoomOut` run on the calling thread. The hop is kept only if `ImageViewer.ZoomIn` is UI-bound. The plan checks this and keeps the existing thread behaviour either way.

### 2. Up and Down

- `ArrowKeyNavigation` gains `Up(vm, mainWindow)` and `Down(vm, mainWindow)` overloads. They supply the gallery, scroll and rotate inputs that `IsGalleryExpanded`, `CanScrollImage`, `NavigateDirectional`, `ScrollUp`/`ScrollDown` and `RotateRight`/`RotateLeft` supply today.
- The existing predicate-based `Up` and `Down` stay as the tested core.
- `IsGalleryExpanded`, `CanScrollImage` and `NavigateDirectional` are deleted from the mapper.
- No delegate is allocated per press: the overloads use static lambdas or cached delegates (`InputAllocationTests`, Release, guards this).

### 3. `PlatformActions`

One static helper in `PicView.Avalonia/FileSystem`, taking the view model and window. It reads `vm.Core` (no `Application.Current.DataContext` lookup) and owns the `Task.Run` and UI-thread choices.

- `OpenWith`, `OpenInExplorer`, `DeleteFile(recycle)`, `ShowFileProperties`, `SetAsLockScreen`, `CheckForUpdates`, and `ShowFile(path)` for the settings, keybindings, recent history and favorites files.
- `SetAsLockScreen` and `SetAsLockscreenCentered` run the same code today. Both Action names stay and map to the one method. The plan confirms the two bodies are identical before merging them.
- `DeleteFile` and `DeleteFilePermanently` become `PlatformActions.DeleteFile(vm, mainWindow, recycle: true/false)`.
- `Search` and `NewWindow` move their UI hop into a helper next to what they open (`MainWindow.AddFileSearchDialog`, `MainWindowLifecycle.OpenNewAsync`) only if the existing owner already has an obvious method. Otherwise they stay as one-line arms, since one `Dispatcher.UIThread.InvokeAsync` call is delegation.

## Testing

- `ViewerActions`: a fake `IZoomScrollTarget` (as in `KeybindRouterTests`) for scroll and zoom, and for the no-viewer case.
- `ArrowKeyNavigation.Up/Down(vm, mainWindow)`: the existing core tests keep passing, plus one test per overload that the right inputs are used where a view model can be built.
- `PlatformActions`: only where it can run without a live window (null file, `Core` present or not). Anything that needs the OS stays untested, as today.
- Existing `ActionListContractTests` stays green (every Action has an arm). `InputAllocationTests` runs in Release.
- Final check: no private arm in `FunctionsMapper` has an `if`, a closure, a `Dispatcher` call (except the two above, if left) or a `Task.Run`.

## Risks

- A moved UI hop changes which thread an Action runs on. Each hop moves unchanged.
- `ZoomIn`/`ZoomOut` thread behaviour (see section 1).
- Merging the lock-screen arms assumes they are identical, and the plan verifies it.
