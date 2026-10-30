# Gallery Mode Switcher Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Every change to a tab's Gallery Mode goes through one per-tab `GalleryModeSwitcher` in Core, and the app-wide `Settings.Gallery.IsGalleryDocked` stops being live state.

**Architecture:** `GalleryModeSwitcher` (Core, owned by `GalleryViewModel`) holds the tab's mode, the mode to give back when the Expanded Gallery closes, and whether crop or a Single Image hides the gallery. It publishes each change of the shown mode as `GalleryModeChange(From, To, Animate)`. `GalleryAnimationControl` subscribes to those changes and only animates. `TabViewModel` tells the switcher when it is hidden from its own `Crop` and `SingleImageType` setters, so no caller touches the gallery.

**Tech Stack:** C# preview / .NET 11, R3, Avalonia, xUnit.v3.

**Spec:** `docs/adr/0011-gallery-mode-is-changed-in-one-place-per-tab.md` and the **Gallery Mode** entry in `GLOSSARY.md`. Pattern to mirror: `docs/adr/0006-window-mode-adapters-do-only-native-work.md` and `PicView.Avalonia/WindowBehavior/WindowModeSwitcher.cs` (optional `Func<ValueTask<bool>>? saveSettings`, defaulting to `SaveSettingsAsync`).

## Global Constraints

- All paths below are relative to `src/`. Run `dotnet` commands from `src/`.
- `PicView.Core` stays AOT-compatible: no reflection; use pattern matching, not `Enum.IsDefined(Type, object)`.
- File-scoped namespaces, braces on every `if`, `_camelCase` private fields, test names `MethodOrScenario_Condition_ExpectedResult`.
- Tests that touch `Settings` go in `[Collection("Sequential")]` and call `SetDefaults()` in the constructor.
- Nothing outside `GalleryModeSwitcher` changes a tab's mode. Nothing outside `GalleryViewModel` writes `IsTop/Bottom/Left/RightDocked`.
- `Settings.Gallery.IsGalleryDocked` is written only when a user command changes the tab's docked choice, followed by one save. Crop, Single Image, `Start` and tab switches never write it.
- Hiding and showing (crop, Single Image) never animate. `Start` never animates. User commands animate.
- Hiding the interface does not touch the gallery.
- Thumbnail Stretch, item size and `DockPosition` stay app-wide.
- Out of scope: lifting the "no new tab while docked" block in `FunctionsMapper.NewTab` and `ClipboardPasteService`. Keep both, reading the new `IsDocked`.

## Review Focus

1. **A command during an animation.** `GalleryAnimationControl.OnGalleryModeChanged` returns early while `IsInAnimation`, so a second toggle mid-animation is lost and the control drifts from the switcher. Expected: after any animation the control matches `ShownMode`. Covered by the catch-up in Task 3, Step 3, checked by hand in Task 5.
2. **Pasting an image while the Expanded Gallery is open.** Expected: the gallery hides without animation, and opening a file from that folder again shows the Expanded Gallery with its items. Test in Task 2.
3. **Crop with a Docked Gallery.** Expected: the crop area is sized without the gallery's Reserved Space, and leaving crop gives the gallery back in the same place. Task 2, Step 7 orders `CropService`. Checked by hand in Task 5.
4. **Two windows, or two tabs.** Expected: docking, cropping or pasting in one never changes the other's gallery. Switcher test in Task 1. Checked by hand in Task 5.
5. **Settings saved by an older version with `DockPosition = 0` (Closed).** Expected: it loads as `Bottom`, and whether new tabs start docked is decided by `IsGalleryDocked` alone. Test in Task 4.

---

### Task 1: GalleryModeSwitcher and its rules

**Files:**
- Create: `PicView.Core/Gallery/GalleryModeChange.cs`
- Create: `PicView.Core/Gallery/GalleryModeSwitcher.cs`
- Test: `PicView.Tests/Gallery/GalleryModeSwitcherTests.cs`

**Interfaces:**
- Produces:
  - `public readonly record struct GalleryModeChange(GalleryMode From, GalleryMode To, bool Animate);`
  - `public sealed class GalleryModeSwitcher(Func<ValueTask<bool>>? saveSettings = null) : IDisposable`
    - `ReadOnlyReactiveProperty<GalleryMode> Mode`: the tab's mode, never `DockedNoAnimation`.
    - `ReadOnlyReactiveProperty<bool> IsDocked`: `Mode is Docked`, or `Mode is Expanded` and it gives back `Docked`.
    - `ReadOnlyReactiveProperty<bool> IsHidden`
    - `ReadOnlyReactiveProperty<GalleryMode> ShownMode`: `Closed` while hidden, else `Mode`.
    - `Observable<GalleryModeChange> Changes`: one item per change of `ShownMode`.
    - `void Start(bool docked)`: `Docked` or `Closed`, `Animate = false`, no setting written, no save.
    - `void SetHidden(bool hidden)`: `Animate = false`.
    - `void ToggleExpanded()`: Closed → Expanded (gives back Closed). Docked → Expanded (gives back Docked). Expanded → what it gives back. Never writes the setting.
    - `ValueTask ToggleDocked()`: Closed → Docked. Docked → Closed. Expanded stays Expanded and flips what it gives back between Docked and Closed.
    - `ValueTask Contract()`: Expanded → what it gives back. Docked → Closed. Closed → nothing.
    - `ValueTask Close()`: any mode → Closed, and it gives back Closed.
    - `ValueTask DockAt(GalleryDockPosition position)`: sets `Settings.Gallery.DockPosition = position`. Closed → Docked. Docked stays. Expanded stays and gives back Docked.
  - Whenever a command changes `IsDocked`, set `Settings.Gallery.IsGalleryDocked = IsDocked` and await `saveSettings` once. `DockAt` also saves once when only the position changed.
  - While `IsHidden`, `ToggleExpanded`, `ToggleDocked`, `Contract` and `Close` do nothing. `DockAt` sets only the position, and saves.

- [ ] **Step 1: Write the failing tests** in `GalleryModeSwitcherTests` (`[Collection("Sequential")]`, `SetDefaults()` in the constructor). Use a `CountingSave` helper whose `Func<ValueTask<bool>>` increments `Count` and returns `true`, and a `List<GalleryModeChange>` filled from `Changes`.

```csharp
[Fact] public void Start_Docked_DocksWithoutAnimationOrSave()
// sut.Start(true) → Mode Docked; changes == [new(Closed, Docked, false)]; save.Count 0; Settings.Gallery.IsGalleryDocked unchanged (false)

[Fact] public void ToggleExpanded_FromClosed_ExpandsWithAnimation()
// changes == [new(Closed, Expanded, true)]; save.Count 0

[Fact] public void ToggleExpanded_FromExpandedAfterDocked_GivesBackDocked()
// Start(true); ToggleExpanded(); ToggleExpanded() → Mode Docked; save.Count 0

[Fact] public async Task Contract_FromExpandedAfterClosed_Closes()
[Fact] public async Task Contract_FromExpandedAfterDocked_GivesBackDocked()
// save.Count 0, Settings.Gallery.IsGalleryDocked unchanged

[Fact] public async Task Contract_FromDocked_ClosesAndRemembersUndocked()
// Settings.Gallery.IsGalleryDocked = true; Start(true); await Contract() → Mode Closed; IsGalleryDocked false; save.Count 1

[Fact] public async Task ToggleDocked_FromClosed_DocksAndRemembersDocked()
// Mode Docked; changes.Last() == new(Closed, Docked, true); IsGalleryDocked true; save.Count 1

[Fact] public async Task ToggleDocked_FromExpanded_StaysExpandedAndFlipsWhatItGivesBack()
// ToggleExpanded(); await ToggleDocked() → Mode Expanded, IsDocked true, IsGalleryDocked true;
// await Contract() → Mode Docked

[Fact] public async Task Close_FromExpandedAfterDocked_ClosesAndRemembersUndocked()
// Start(true); ToggleExpanded(); await Close() → Mode Closed; IsDocked false; IsGalleryDocked false; save.Count 1

[Fact] public async Task DockAt_FromClosed_DocksAtPosition()
// await DockAt(Left) → Mode Docked; Settings.Gallery.DockPosition Left; IsGalleryDocked true

[Fact] public void SetHidden_WhileDocked_ShowsClosedWithoutAnimationAndKeepsMode()
// Start(true); changes.Clear(); SetHidden(true) → ShownMode Closed; Mode Docked; changes == [new(Docked, Closed, false)]

[Fact] public void SetHidden_False_GivesBackShownModeWithoutAnimation()
// Start(true); SetHidden(true); changes.Clear(); SetHidden(false) → changes == [new(Closed, Docked, false)]

[Fact] public async Task Commands_WhileHidden_AreRefused()
// Start(true); SetHidden(true); ToggleExpanded(); await ToggleDocked(); await Contract(); await Close()
// → Mode Docked; save.Count 0

[Fact] public async Task DockAt_WhileHidden_SetsOnlyThePosition()
// SetHidden(true); await DockAt(Top) → DockPosition Top; Mode Closed

[Fact] public async Task TwoSwitchers_DoNotAffectEachOther()
// a, b; await a.ToggleDocked(); b.Mode Closed; a.SetHidden(true); b.IsHidden false
```

- [ ] **Step 2: Run the tests and check they fail**

Run: `dotnet test PicView.Tests\PicView.Tests.csproj --filter "FullyQualifiedName~PicView.Tests.Gallery.GalleryModeSwitcherTests"`
Expected: build error, because `GalleryModeSwitcher` does not exist yet.

- [ ] **Step 3: Implement `GalleryModeChange` and `GalleryModeSwitcher`** to the interface above. Back the read-only properties with `ReactiveProperty<T>` fields. Emit `Changes` from a `Subject<GalleryModeChange>`, only when `ShownMode` actually changes. Keep what Expanded gives back in a private `GalleryMode _giveBack` (`Closed` or `Docked`). Give the class an XML summary that points to ADR-0011.

- [ ] **Step 4: Run the tests and check they pass**

Run: same as Step 2. Expected: all pass.

- [ ] **Step 5: Commit**

```bash
git add PicView.Core/Gallery/GalleryModeChange.cs PicView.Core/Gallery/GalleryModeSwitcher.cs PicView.Tests/Gallery/GalleryModeSwitcherTests.cs
git commit -m "Add GalleryModeSwitcher, one per tab"
```

---

### Task 2: Route every gallery mode writer through the switcher

**Files:**
- Modify: `PicView.Core/ViewModels/GalleryViewModel.cs` (most of it)
- Delete: `PicView.Core/Gallery/GalleryManager.cs`
- Modify: `PicView.Core/Gallery/GallerySettingsConverter.cs` (delete `UpdateDockPositionProperties`)
- Modify: `PicView.Core/Gallery/GalleryCoordinator.cs:51-54`
- Modify: `PicView.Core/ViewModels/TabViewModel.cs:67,78` (`SingleImageType`, `Crop` setters)
- Modify: `PicView.Core/Navigation/NavigationService.cs:505`
- Modify: `PicView.Avalonia/Navigation/UpdateImage.cs:162`
- Modify: `PicView.Avalonia/UI/ToggleUIVisibility.cs:66-75`
- Modify: `PicView.Avalonia/Crop/CropService.cs`
- Modify: `PicView.Avalonia/StartUp/TabNavigationInitializer.cs:40-62` (delete `PrepareDockedGallery`)
- Modify: `PicView.Avalonia/Navigation/SourceOpener.cs:46,52`, `PicView.Avalonia/StartUp/QuickLoad.cs:90,132`
- Modify: `PicView.Avalonia/Functions/FunctionsMapper.cs:520-530,1004`, `PicView.Avalonia/Clipboard/ClipboardPasteService.cs:86`
- Modify: XAML command and flag bindings in `Views/Gallery/GalleryView.axaml`, `Views/Gallery/GalleryItem.axaml`, `Views/Main/MainView.axaml`, `Views/Menu/DropDownMenu.axaml`, `Views/Menu/CustomTitleBar.axaml`, `Views/Config/GallerySettingsView.axaml`, `Views/UC/Buttons/GalleryShortcut.axaml`
- Test: `PicView.Tests/Gallery/GalleryViewModelTests.cs`, plus a new `PicView.Tests/Gallery/GalleryHidingTests.cs`

**Interfaces:**
- Consumes: `GalleryModeSwitcher` and `GalleryModeChange` from Task 1.
- Produces on `GalleryViewModel`:
  - `GalleryViewModel(Func<ValueTask<bool>>? saveSettings = null)`
  - `GalleryModeSwitcher Switcher`
  - Read-only `Mode`, `ShownMode`, `IsDocked`: pass-throughs of the switcher's. They replace `ActiveGalleryMode` and `IsGalleryDocked`, which are removed.
  - `IsGalleryExpanded` = `ShownMode is Expanded`, and `IsDockedGalleryVisible` = `ShownMode is Docked`. Both are derived, not settable.
  - `IsTopDocked` / `IsBottomDocked` / `IsLeftDocked` / `IsRightDocked` = `ShownMode is Docked` and `Settings.Gallery.DockPosition` is that side. They are recomputed on `ShownMode` and on `Observable.EveryValueChanged(Settings.Gallery, g => g.DockPosition)`. Keep them as `BindableReactiveProperty<bool>` that only `GalleryViewModel` writes.
  - Commands, thin over the switcher: `ToggleExpandedCommand`, `ToggleDockedCommand`, `ContractCommand`, `CloseCommand`, `DockAtCommand` (`ReactiveCommand<GalleryDockPosition>`). They replace `ToggleGalleryCommand`, `OpenOrCloseDockedGalleryCommand`, `ContractToDockedOrCloseGalleryCommand`, `CloseGalleryCommand` and `SetDockPositionCommand`. `SetGalleryModeCommand` and `SetActiveGallery` are deleted. `NavigateGalleryCommand` and `OpenSelectedItemCommand` stay.
  - `SelectedGalleryItemIndex` is reset to -1 when `ShownMode` leaves Expanded, as today.

- [ ] **Step 1: Rewrite `GalleryViewModelTests`** for the new names. The three existing tests reach Expanded with `gallery.Switcher.ToggleExpanded()`. They leave it, respectively, toward Docked (`Start(true)` first, then `ToggleExpanded()` twice), toward Closed (`await gallery.Switcher.Close()`), and through `ContractCommand.Execute(Unit.Default)`. Each still asserts `SelectedGalleryItemIndex == -1`. Pass a counting save to the constructor so no test writes the settings file. Add:

```csharp
[Fact] public void DockFlags_WhenDockedAtLeft_OnlyLeftIsTrue()
// Settings.Gallery.DockPosition = Left; gallery.Switcher.Start(true) → IsLeftDocked true, the other three false

[Fact] public void DockFlags_WhenHidden_AllFalse()
// Start(true); Switcher.SetHidden(true) → all four false, IsDockedGalleryVisible false
```

- [ ] **Step 2: Write `GalleryHidingTests`** (`[Collection("Sequential")]`) on `new TabViewModel(null!)`, as `ArchiveExtractionServiceTests` builds tabs:

```csharp
[Fact] public void SingleImage_WhileExpanded_HidesAndKeepsMode()
// tab.Gallery.Switcher.ToggleExpanded(); tab.SingleImageType = SingleImageType.Clipboard
// → ShownMode Closed; Mode Expanded

[Fact] public void LeavingSingleImage_GivesBackShownMode()
// ...then tab.SingleImageType = SingleImageType.None → ShownMode Expanded

[Fact] public void Crop_WhileDocked_HidesUntilCropIsCleared()
// Start(true); tab.Crop = new CropViewModel() → ShownMode Closed; tab.Crop = null → ShownMode Docked
```

- [ ] **Step 3: Run both test classes and check they fail**

Run: `dotnet test PicView.Tests\PicView.Tests.csproj --filter "FullyQualifiedName~PicView.Tests.Gallery.GalleryViewModelTests|FullyQualifiedName~PicView.Tests.Gallery.GalleryHidingTests"`
Expected: build errors on `Switcher`, `ContractCommand` and `ShownMode`.

- [ ] **Step 4: Rebuild `GalleryViewModel` on the switcher** to the interface above. Delete `GalleryManager.cs`, `GallerySettingsConverter.UpdateDockPositionProperties`, and every `Observable.EveryValueChanged(gallery, g => g.IsGalleryDocked)` subscription. Dispose the switcher in `Dispose`.

- [ ] **Step 5: Hide from the tab's own state.** Turn `TabViewModel.SingleImageType` and `TabViewModel.Crop` into properties with backing fields. Both setters call `Gallery.Switcher.SetHidden(Crop is not null || SingleImageType is not SingleImageType.None)`.

- [ ] **Step 6: Make the gallery follow `Mode`.** `GalleryCoordinator` subscribes to `tab.Gallery.Mode` instead of `ActiveGalleryMode`, keeping `mode is not GalleryMode.Closed`. Items follow the mode, not hiding.

- [ ] **Step 7: Remove the writers in the callers**
  - `NavigationService.cs:505` and `UpdateImage.SetSingleImage:162`: delete the `ActiveGalleryMode` line. Setting `SingleImageType` hides the gallery now.
  - `ToggleUIVisibility.ToggleInterface`: delete the `if (Settings.Gallery.IsGalleryDocked) … else …` block when showing the interface.
  - `CropService`: delete `_wasGalleryDocked`, `_previousDockPosition` and both `Settings.Gallery` writes. In `StartCropControlAsync`, set `tabViewModel.Crop` first (on the UI thread), then call `mainWindow.WindowFitter.Refit()`, then read `vm.ImageWidth/ImageHeight` for the crop control's size. This way the size excludes the hidden gallery. In `CloseCropControl`, call `Refit()` after `tabViewModel.Crop = null`, unconditionally.
  - `TabNavigationInitializer.PrepareDockedGallery`: delete it. At `QuickLoad.cs:90` and `:132`, and at `SourceOpener.cs:46` (the uninitialized tab), call `tab.Gallery.Switcher.Start(Settings.Gallery.IsGalleryDocked)` in its place, before `Initialize`. At `SourceOpener.cs:52` (the iterator recreated after a Single Image), delete the call, because the tab keeps its mode.
  - `FunctionsMapper.ToggleGallery` → `Switcher.ToggleExpanded()`. `FunctionsMapper.OpenCloseDockedGallery` → `await Switcher.Contract()`, which is the Action's behaviour today. `FunctionsMapper.NewTab:1004` and `ClipboardPasteService:86` read `Gallery.IsDocked.CurrentValue`.
  - XAML: rename the command bindings (`ToggleGalleryCommand` → `ToggleExpandedCommand`, `ContractToDockedOrCloseGalleryCommand` → `ContractCommand`, `OpenOrCloseDockedGalleryCommand` → `ToggleDockedCommand`, `CloseGalleryCommand` → `CloseCommand`, `SetDockPositionCommand` → `DockAtCommand`). Change `Gallery.IsGalleryDocked.Value` → `Gallery.IsDocked.Value`. In `GalleryView.axaml:10`, bind `ActiveGalleryMode` to `Gallery.ShownMode.Value` (Task 3 replaces this binding).

- [ ] **Step 8: Build, then run the gallery tests**

Run: `dotnet build PicView.Tests\PicView.Tests.csproj`, then `dotnet test PicView.Tests\PicView.Tests.csproj --filter "FullyQualifiedName~PicView.Tests.Gallery"`
Expected: build has 0 errors; all gallery tests pass, including `GalleryFollowsFilesTests`.

- [ ] **Step 9: Check that no writer is left**

Run: `grep -rn "ActiveGalleryMode\|Settings.Gallery.IsGalleryDocked\s*=\|GalleryManager" --include=*.cs --include=*.axaml PicView.Core PicView.Avalonia`
Expected: matches only in `GalleryModeSwitcher.cs` (the setting write), `GalleryView.axaml:10` and `GalleryAnimationControl.cs`.

- [ ] **Step 10: Commit**

```bash
git add -A PicView.Core PicView.Avalonia PicView.Tests
git commit -m "Change a tab's gallery mode only through its switcher"
```

---

### Task 3: GalleryAnimationControl only animates

**Files:**
- Modify: `PicView.Avalonia/CustomControls/GalleryAnimationControl.cs`
- Modify: `PicView.Avalonia/Views/Gallery/GalleryView.axaml:10`
- Modify: `PicView.Core/Gallery/GalleryMode.cs` (remove `DockedNoAnimation`)

**Interfaces:**
- Consumes: `GalleryViewModel.ShownMode`, `GalleryViewModel.Switcher.Changes` and `GalleryModeChange` from Tasks 1–2.

- [ ] **Step 1: Replace the styled property with the switcher's changes.** Delete `ActiveGalleryModeProperty` / `ActiveGalleryMode` and the binding at `GalleryView.axaml:10`. In `SetupSubscriptions`, take the `TabViewModel` from `DataContext`. Subscribe to `tab.Gallery.Switcher.Changes`, posting each change to the UI thread. Keep the shown mode in a private `_shownMode` field, which replaces `_previousMode` and every read of `ActiveGalleryMode`.

- [ ] **Step 2: Apply a change from its `Animate` flag.** `OnGalleryModeChanged(GalleryModeChange change)`:
  - `Animate == false`: set `_shownMode = change.To`, call `UpdateLayoutForCurrentState()`, and when `To is Docked`, scroll to the current item as `ClosedToDockedNoAnim` does. Then `Refit()`. `ClosedToDockedNoAnim` is folded into this path and deleted.
  - Otherwise: run the existing `(From, To)` animation switch without the `DockedNoAnimation` cases.
  - On load, when `tab.Gallery.ShownMode.CurrentValue is not Closed`, apply `new GalleryModeChange(Closed, ShownMode, false)`. A tab started docked before its view loaded then shows without animation (ADR-0005).

- [ ] **Step 3: Catch up after an animation.** In the `finally` of `OnGalleryModeChanged`, if `_shownMode != tab.Gallery.ShownMode.CurrentValue`, apply `new GalleryModeChange(_shownMode, ShownMode.CurrentValue, false)`. A change that arrives during an animation is still dropped, as today, but the control no longer stays wrong.

- [ ] **Step 4: Stop reading or writing gallery state.**
  - Delete the `if (!Settings.Gallery.IsGalleryDocked) return;` guards in `ClosedToDocked`.
  - Delete the `if (!Settings.Gallery.IsGalleryDocked) return;` guards in the docked-without-animation path.
  - Delete the writes to `IsTop/Bottom/Left/RightDocked` in `SetDockedThumbPosition`; keep its layout work.
  - Remove `DockedNoAnimation` from `GalleryMode`.

- [ ] **Step 5: Build and run the gallery tests**

Run: `dotnet build PicView.Tests\PicView.Tests.csproj`, then `dotnet test PicView.Tests\PicView.Tests.csproj --filter "FullyQualifiedName~PicView.Tests.Gallery"`
Expected: build has 0 errors; all pass (`GalleryViewTests` is the headless coverage).

- [ ] **Step 6: Commit**

```bash
git add PicView.Avalonia/CustomControls/GalleryAnimationControl.cs PicView.Avalonia/Views/Gallery/GalleryView.axaml PicView.Core/Gallery/GalleryMode.cs
git commit -m "Animate the gallery from its switcher's changes"
```

---

### Task 4: Dock position without Closed, and readers of the docked choice

**Files:**
- Modify: `PicView.Core/Gallery/GalleryDockPosition.cs` (remove `Closed = 0`; keep the other values' numbers)
- Modify: `PicView.Core/Config/AppSettings.cs:262` (default `GalleryDockPosition.Bottom`)
- Modify: `PicView.Core/Config/SettingsManager.cs` `EnsureSettings` (around line 262)
- Modify: `PicView.Avalonia/CustomControls/GalleryAnimationControl.cs:52,444` (remove the `Closed` cases)
- Modify: `PicView.Core/ViewModels/MainWindowViewModel.cs`, `PicView.Core/ViewModels/TranslationViewModel.cs:395-410`, `PicView.Avalonia/SettingsManagement/LanguageUpdater.cs:27-29`
- Modify: `Views/Main/MainView.axaml:334`, `Views/Config/GallerySettingsView.axaml:105`, `Views/UC/PopUps/QuickEditingDialog.axaml:228`
- Modify: `PicView.Avalonia/Views/UC/HoverBar.axaml.cs:87-95,215`
- Test: `PicView.Tests/LanguageAndSettingsUnitTest.cs` (or a new `PicView.Tests/Gallery/DockPositionSettingsTests.cs`)

**Interfaces:**
- Consumes: `GalleryViewModel.IsDocked`, `GalleryViewModel.IsDockedGalleryVisible` from Task 2.
- Produces: `MainWindowViewModel.DockedGalleryToggleHeader : ReadOnlyReactiveProperty<string?>`. It follows the active tab: `HideDockedGallery` while `IsDocked`, else `ShowDockedGallery`. `TranslationViewModel.IsShowingDockedGallery` is removed, because one `TranslationViewModel` is shared by every window.

- [ ] **Step 1: Write the failing migration test**

```csharp
[Fact] public void EnsureSettings_DockPositionZero_LoadsAsBottom()
// var s = GetDefaults(); s.Gallery.DockPosition = (GalleryDockPosition)0; EnsureSettings(s)
// → s.Gallery.DockPosition == GalleryDockPosition.Bottom
```

- [ ] **Step 2: Run it and check it fails**

Run: `dotnet test PicView.Tests\PicView.Tests.csproj --filter "FullyQualifiedName~EnsureSettings_DockPositionZero_LoadsAsBottom"`
Expected: FAIL (the value stays 0).

- [ ] **Step 3: Remove `Closed` and migrate it.** In `EnsureSettings`, after the version block: `if (existingSettings.Gallery.DockPosition is not (Bottom or Top or Left or Right)) DockPosition = Bottom;`. Remove the `Closed` cases in `GalleryAnimationControl`.

- [ ] **Step 4: Follow the active tab in the readers**
  - `MainWindowViewModel`: build `DockedGalleryToggleHeader` from `WindowTabs.ActiveTab.Select(t => t.Gallery.IsDocked).Switch()`, combined with `Translation.HideDockedGallery` / `Translation.ShowDockedGallery`. Bind the three XAML headers that read `Translation.IsShowingDockedGallery` to it. `GallerySettingsView` binds through `MainWindows.ActiveWindow.Value.DockedGalleryToggleHeader.Value`.
  - Delete `IsShowingDockedGallery` with its subscription in `TranslationViewModel`, and its line in `LanguageUpdater`.
  - `HoverBar`: replace the `Settings.Gallery.IsGalleryDocked` subscription and read with the window's `WindowTabs.ActiveTab.Select(t => t.Gallery.IsDockedGalleryVisible).Switch()`.

- [ ] **Step 5: Run the whole test project**

Run: `dotnet test PicView.Tests\PicView.Tests.csproj`
Expected: all pass. Then run `grep -rn "IsGalleryDocked" --include=*.cs --include=*.axaml PicView.Core PicView.Avalonia`. Expected: only `AppSettings.cs`, `GalleryModeSwitcher.cs` and the `Start(Settings.Gallery.IsGalleryDocked)` call sites.

- [ ] **Step 6: Commit**

```bash
git add -A PicView.Core PicView.Avalonia PicView.Tests
git commit -m "Remember only the start mode and dock position of the gallery"
```

---

### Task 5: Check the gallery by hand

- [ ] **Step 1: Run the app** (Windows, `PicView.Avalonia.Win32`). Check each line in turn:
  1. Start with the gallery docked in the settings. The Docked Gallery shows without animation, and its items load after the image.
  2. Toggle the Expanded Gallery from Docked and back. Then press the gallery key twice quickly during the animation. The gallery ends matching the last command.
  3. Crop with a Docked Gallery. The crop area fills the space the gallery took. Leave crop, and the gallery is back at the same edge without animation.
  4. With the Expanded Gallery open, paste an image. The gallery hides without animation. Open a file from that folder, and the Expanded Gallery is back with its items.
  5. Open a second window, dock its gallery, then crop in the first. The second window's gallery stays.
  6. Hide the interface with a Docked Gallery. The gallery stays.
  7. Close the gallery and restart. The app starts undocked.

- [ ] **Step 2: Note anything that fails** as a bug against the owning task before finishing the branch.
