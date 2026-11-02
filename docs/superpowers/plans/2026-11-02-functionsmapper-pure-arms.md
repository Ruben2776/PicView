# FunctionsMapper Pure Arms Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make every private arm of `FunctionsMapper` a null check plus one delegating call, moving the rules, thread hops and `Application.Current` lookups into testable helpers.

**Architecture:** Scroll and zoom go behind `IZoomScrollTarget` with a `ViewerActions` helper that owns the UI-thread hop. Up/Down get `(vm, mainWindow)` overloads over a shared pure decision. Platform and file arms move into one `PlatformActions` helper that reads `vm.Core`.

**Tech Stack:** C# (net11.0), Avalonia, xUnit.v3, `Avalonia.Headless.XUnit`.

**Spec:** `docs/superpowers/specs/2026-11-02-functionsmapper-pure-arms-design.md`

## Global Constraints

- Behaviour unchanged: no Action added or removed, same thread per Action, `FunctionsMapper` table built once in the constructor.
- `new FunctionsMapper(null!, null!)` must still construct (tests do it): the constructor and `CreateFunction` must not touch `_vm` or `_mainWindow`.
- Input hot path: no per-press list, LINQ, closure or delegate allocation. `PicView.Tests/Input/InputAllocationTests.cs` runs in Release: `dotnet test PicView.Tests\PicView.Tests.csproj -c Release --filter "FullyQualifiedName~PicView.Tests.Input.InputAllocationTests"`.
- `NewTab` keeps its gallery-docked guard and TODO untouched. The ~62 pure forwards and the clipboard arms keep their current shape.
- File-scoped namespaces, braces on every `if`, `_camelCase` private fields, tests named `MethodOrScenario_Condition_ExpectedResult`.
- Run from `src`: `dotnet build PicView.Tests\PicView.Tests.csproj` / `dotnet test PicView.Tests\PicView.Tests.csproj --filter ...`. Never schedule polling timers around `dotnet test`.

## Review Focus

- No viewer shown (start-up menu, gallery, video): scroll and zoom Actions do nothing and do not throw. Pinned in Task 1.
- Keyboard Action arriving off the UI thread: scroll and zoom still run on the UI thread. Pinned in Task 1 (the `Run` hop is checked by code review; the target-taking methods are tested).
- Gallery expanded while the image is also scrollable: Up/Down navigate, not scroll. Pinned in Task 2.
- `Core.PlatformService` is null: platform Actions do nothing instead of throwing. Pinned in Task 3.
- No file open (`FileInfo` null): `OpenWith`, `OpenInExplorer`, `ShowFileProperties` and delete pass a null path through exactly as today. Pinned in Task 3.

---

### Task 1: Scroll and zoom behind `IZoomScrollTarget`

**Files:**
- Modify: `src/PicView.Avalonia/Input/IZoomScrollTarget.cs`
- Modify: `src/PicView.Avalonia/Views/UC/ImageViewer.axaml.cs` (near line 296, with the other explicit implementations)
- Create: `src/PicView.Avalonia/Input/ViewerActions.cs`
- Modify: `src/PicView.Avalonia/Functions/FunctionsMapper.cs` (arms `ScrollUp`, `ScrollDown`, `ScrollToTop`, `ScrollToBottom`, `ZoomIn`, `ZoomOut`)
- Modify: `src/PicView.Tests/Input/KeybindRouterTests.cs` (`FakeViewer`, line ~329) and `src/PicView.Tests/Input/InputAllocationTests.cs` (`CountingViewer`, line ~178): add the new members as no-ops
- Test: `src/PicView.Tests/Input/ViewerActionsTests.cs`

**Interfaces:**
- Produces on `IZoomScrollTarget`: `void ScrollToHome()`, `void ScrollToEnd()`, `void ZoomIn()`, `void ZoomOut()`. All are UI-thread only, like `ScrollLine`. `ImageViewer.ZoomIn()/ZoomOut()` already exist publicly and satisfy the interface.
- Produces in `PicView.Avalonia.Input.ViewerActions` (static):
  - `internal static void ScrollLine(IZoomScrollTarget? target, bool down)`, `ScrollToTop(IZoomScrollTarget?)`, `ScrollToBottom(IZoomScrollTarget?)`, `ZoomIn(IZoomScrollTarget?)`, `ZoomOut(IZoomScrollTarget?)`: no-op on null, otherwise one call on the target.
  - `public static ValueTask ScrollLine(MainWindowViewModel vm, bool down)`, `ScrollToTop(vm)`, `ScrollToBottom(vm)`, `ZoomIn(vm)`, `ZoomOut(vm)`: resolve `vm.WindowTabs.ActiveTab.CurrentValue.CurrentView.CurrentValue as IZoomScrollTarget`, then run the matching target method inline when `Dispatcher.UIThread.CheckAccess()` is true, else through `Dispatcher.UIThread.InvokeAsync`. Return `ValueTask.CompletedTask` when inline. Use static lambdas or a state-carrying overload so the inline path allocates nothing.

- [ ] **Step 1: Write the failing tests** in `ViewerActionsTests.cs` with a recording `IZoomScrollTarget` fake:
  - `ScrollLine_Down_CallsScrollLineTrue`, `ScrollLine_Up_CallsScrollLineFalse`
  - `ScrollToTop_CallsScrollToHome`, `ScrollToBottom_CallsScrollToEnd`
  - `ZoomIn_CallsZoomIn`, `ZoomOut_CallsZoomOut`
  - `AnyAction_NullTarget_DoesNothingAndDoesNotThrow` (theory over the five `internal` methods)
- [ ] **Step 2: Run** `dotnet test PicView.Tests\PicView.Tests.csproj --filter "FullyQualifiedName~ViewerActionsTests"`. Expected: build FAIL (members and class missing).
- [ ] **Step 3: Add the four members to `IZoomScrollTarget`**; implement `ScrollToHome`/`ScrollToEnd` explicitly in `ImageViewer` by calling `ImageScrollViewer.ScrollToHome()`/`ScrollToEnd()`; add the no-op members to `FakeViewer` and `CountingViewer`.
- [ ] **Step 4: Implement `ViewerActions`** as above.
- [ ] **Step 5: Replace the six arms** in `FunctionsMapper` with one-liners: `ScrollUp => ViewerActions.ScrollLine(_vm, down: false)`, `ScrollDown => ViewerActions.ScrollLine(_vm, down: true)`, and the matching `ScrollToTop`, `ScrollToBottom`, `ZoomIn`, `ZoomOut` calls. Remove the now-unused `Dispatcher`/`ImageViewer` usings only if nothing else needs them.
- [ ] **Step 6: Run** `ViewerActionsTests`, `KeybindRouterTests`, `ActionListContractTests`. Expected: PASS.
- [ ] **Step 7: Commit** "Run the scroll and zoom Actions through IZoomScrollTarget, so the mapper no longer reaches into the image viewer".

### Task 2: Up and Down without closures

**Files:**
- Modify: `src/PicView.Avalonia/Navigation/ArrowKeyNavigation.cs`
- Modify: `src/PicView.Avalonia/Functions/FunctionsMapper.cs` (`Up`, `Down`; delete `IsGalleryExpanded`, `CanScrollImage`, `NavigateDirectional`)
- Test: `src/PicView.Tests/Navigation/ArrowKeyNavigationTests.cs`

**Interfaces:**
- Consumes: `ViewerActions.ScrollLine(MainWindowViewModel, bool)` from Task 1; `RotationManager.RotateRight/RotateLeft(vm, mainWindow)`; `TabOverviewViewModel.NavigateDirectionalAsync(bool isKeyHeldDown, NavigateTo)`; `ImageViewer.IsImageTallerThanViewer`.
- Produces: `enum ArrowKeyAction { Navigate, Scroll, Rotate }` and `public static ArrowKeyAction Decide(bool isGalleryExpanded, bool canScroll)` (Scroll only when `Settings.Zoom.ScrollEnabled && canScroll` and the gallery is not expanded). The existing delegate-taking `Up`/`Down` call `Decide` so the rule has one copy. New `public static ValueTask Up(MainWindowViewModel vm, MainWindow mainWindow)` and `Down(vm, mainWindow)` evaluate the three inputs from `vm`, call `Decide`, then call the navigate/scroll/rotate helpers directly, with no delegates.

- [ ] **Step 1: Write failing tests** for `Decide`: `Decide_GalleryExpandedAndCanScroll_Navigates`, `Decide_ScrollEnabledAndCanScroll_Scrolls`, `Decide_ScrollDisabledAndCanScroll_Rotates`, `Decide_NotExpandedCannotScroll_Rotates` (set `Settings.Zoom.ScrollEnabled` as the existing tests do).
- [ ] **Step 2: Run** `--filter "FullyQualifiedName~ArrowKeyNavigationTests"`. Expected: build FAIL.
- [ ] **Step 3: Add `ArrowKeyAction`/`Decide`**, make the two existing `Up`/`Down` use it, keep their signatures. Run the existing tests: all still PASS.
- [ ] **Step 4: Add the `(vm, mainWindow)` overloads.**
- [ ] **Step 5: Replace the mapper arms** with `Up => ArrowKeyNavigation.Up(_vm, _mainWindow)` and `Down => ArrowKeyNavigation.Down(_vm, _mainWindow)`; delete the three helper methods.
- [ ] **Step 6: Run** `ArrowKeyNavigationTests`, `ActionListContractTests`, then the Release `InputAllocationTests` (command in Global Constraints). Expected: PASS.
- [ ] **Step 7: Commit** "Decide Up and Down in ArrowKeyNavigation, so the mapper no longer builds closures per press".

### Task 3: `PlatformActions`

**Files:**
- Create: `src/PicView.Avalonia/FileSystem/PlatformActions.cs`
- Modify: `src/PicView.Avalonia/Functions/FunctionsMapper.cs` (arms `OpenWith`, `OpenInExplorer`, `DeleteFile`, `DeleteFilePermanently`, `ShowFileProperties`, `SetAsLockScreen`, `SetAsLockscreenCentered`, `CheckForUpdates`, `ShowSettingsFile`, `ShowKeybindingsFile`, `ShowRecentHistoryFile`, `ShowFavoritesFile`)
- Test: `src/PicView.Tests/FileTests/PlatformActionsTests.cs`

**Interfaces:**
- Consumes: `IPlatformSpecificService.OpenWith(string)`, `LocateOnDisk(string)`, `ShowFileProperties(string)`, `SetAsLockScreen(string)`; `FileManager.DeleteFileWithOptionalDialog(bool recycle, string path, IPlatformSpecificService, MainWindow)`; `vm.Core.PlatformService` (nullable) and `vm.Core.AboutView.UpdateCurrentVersion()`.
- Produces (static `PlatformActions`, each `public static ValueTask X(MainWindowViewModel vm, ...)` that reads `vm.Core.PlatformService`, returns completed when it is null, and otherwise moves the arm's body unchanged, including `Task.Run` and `Dispatcher.UIThread` hops):
  - `OpenWith(vm)`, `OpenInExplorer(vm)`, `ShowFileProperties(vm)`, `SetAsLockScreen(vm)`, `DeleteFile(vm, mainWindow, bool recycle)`, `CheckForUpdates(vm)`, `ShowFile(vm, string path)`.
  - Each also has an `internal` overload taking `IPlatformSpecificService platform` plus the string path (`OpenWith(platform, path)` etc.) holding the logic, so tests need no view model.
- The two lock-screen arms are byte-identical in effect (both `Task.Run(() => SetAsLockScreen(FileInfo.FullName))`); confirm that with a diff before merging, then map both Actions to `SetAsLockScreen`.

- [ ] **Step 1: Write failing tests** with a test-local `IPlatformSpecificService` fake that records calls and throws `NotImplementedException` for the rest:
  - `OpenWith_Path_PassesPathToPlatform`, `OpenWith_NullPath_PassesNullThrough`, `OpenInExplorer_Path_CallsLocateOnDisk`
  - `SetAsLockScreen_Path_CallsPlatformOffTheCallingThread` (assert it ran, and on a different thread id)
  - `ShowFile_Path_CallsOpenWith`
  - `OpenWith_NullPlatform_DoesNothing` (public `vm`-less internal overload given `null` platform)
- [ ] **Step 2: Run** `--filter "FullyQualifiedName~PlatformActionsTests"`. Expected: build FAIL.
- [ ] **Step 3: Implement `PlatformActions`**; move each arm's body in unchanged.
- [ ] **Step 4: Replace the twelve arms** with one-line calls. `ShowSettingsFile => PlatformActions.ShowFile(_vm, CurrentSettingsPath)`, and likewise `KeybindingFunctions.CurrentKeybindingsPath`, `FileHistoryManager.CurrentFileHistoryFile`, `FavoritesManager.CurrentFavoritesFile`.
- [ ] **Step 5: Run** `PlatformActionsTests`, `ActionListContractTests`, `KeybindingsFavoritesTests`. Expected: PASS.
- [ ] **Step 6: Commit** "Move the platform and file Actions' lookups and thread hops out of FunctionsMapper into PlatformActions".

### Task 4: Audit and full verification

**Files:**
- Modify: `src/PicView.Avalonia/Functions/FunctionsMapper.cs` (only to remove leftover unused usings)
- Test: whole `PicView.Tests`

- [ ] **Step 1: Audit.** `rg "Application\.Current|Task\.Run|\bif \(" src/PicView.Avalonia/Functions/FunctionsMapper.cs`. Expected remaining hits: the clipboard null checks, `NewTab`'s guard, `Lifecycle` null checks in `NewWindow`/`ResetSettings`/`Restart`, `Search`/`NewWindow` single `Dispatcher` calls, and the `CreateFunction` favorite-slot pattern. Any other hit is a missed arm: move it.
- [ ] **Step 2: Run the full suite** `dotnet test PicView.Tests\PicView.Tests.csproj`. Expected: PASS. If `CloseAsync` batch-resize or `Win32Clipboard` tests fail, re-run them alone (known flaky) before treating them as regressions.
- [ ] **Step 3: Run Release `InputAllocationTests`.** Expected: PASS.
- [ ] **Step 4: Update the ADR.** Add one sentence to `docs/adr/0002-functions-mapper-pure-dispatch.md`: scroll and zoom reach the viewer through `IZoomScrollTarget`, and platform lookups through `PlatformActions`, so no arm resolves a view or `Application.Current` itself.
- [ ] **Step 5: Commit** "Record where FunctionsMapper's former rules went in ADR-0002".
