# A gallery's appearance is derived once, per Tab

Architecture review candidate 8. Builds on ADR-0011.

## Goal

What a Docked or Expanded Gallery looks like comes from one pure Core function of its shown Gallery Mode, the dock position and the gallery settings. The result is a **Gallery Appearance** record, owned by the tab's `GalleryViewModel`. `GalleryAnimationControl` only applies records and animates between two of them. Its ten imperative setters, six bespoke animations and five settings subscriptions go away.

The point is maintainability: each mode's look, and each transition, becomes a plain table test. Fixing the cross-window conflict in `GallerySharedSettingsViewModel`'s TODO comes as a side effect, not as a goal. The appearance lives per tab only because Gallery Mode already does (ADR-0011).

Behaviour is unchanged except for the two settlements under "Decisions".

## Not in scope

- `Hoverbar.IsHoverbarVisible`'s other writers (`ToggleUIVisibility`, `SourceOpener`) keep writing it. Only the gallery's own write goes through the record.
- `LineSpacing` stays bound to the settings as it is today, because it doesn't depend on the mode.
- The thumbnail decode height. `GalleryCoordinator` already decodes at the larger of the two item sizes and doesn't read the applied look.
- The gallery settings themselves (both item sizes, both stretches, item spacing, dock position) stay app-wide, as ADR-0011 says.
- `VirtualizingGallery`'s follow-and-hold rules (candidate 7).

## Decisions

1. **The docked extent has one formula.** Today a Top/Bottom gallery is `DockedGalleryItemSize + 1 + 30` tall after a transition (`GalleryDefaults.GetDockedGalleryHeight`), but `size + 2 + 22` after the size slider moves (`UpdateDockedItemHeight`), so it jumps 7px. `GalleryHelper` and `WindowFitter` reserve space with the `GalleryDefaults` formulas, so those become the only ones: `GetDockedGalleryHeight` for Top/Bottom and `GetDockedGalleryWidth` for Left/Right.
2. **Expanded has no border.** Today the Expanded Gallery keeps whatever border the last dock edge left. Its appearance has a zero border.
3. **Transitions keep their current refit behaviour, including where it's uneven.** Docked → Closed doesn't refit the window, while Expanded → Closed does. The table makes this visible, and changing it is a separate decision.

## Changes

### 1. `GalleryAppearance` (Core)

New file `PicView.Core/Gallery/GalleryAppearance.cs`:

- `readonly record struct GalleryAppearanceInputs(double DockedItemSize, double ExpandedItemSize, GalleryStretchMode DockedStretch, GalleryStretchMode ExpandedStretch, double ItemSpacing, bool ShowBottomNavBar, bool ShowHoverNavigationBar)`.
- `readonly record struct GalleryAppearance` with the fields below, plus `static GalleryAppearance For(GalleryMode shownMode, GalleryDockPosition dock, GalleryAppearanceInputs inputs)`. `For` never reads `Settings`.
- New Core types: `enum GalleryOrientation { Horizontal, Vertical }`, and `readonly record struct GalleryInsets(double Left, double Top, double Right, double Bottom)` for the margin and border.

| Field | Docked Top/Bottom | Docked Left/Right | Expanded | Closed |
|---|---|---|---|---|
| `Mode`, `Dock` | as given | as given | as given | as given |
| `IsShown` | true | true | true | false |
| `Orientation` | Horizontal | Vertical | Vertical | Vertical |
| `ScrollsHorizontally` | true | false | true | false |
| `Wraps` | false | false | true | false |
| `ItemHeight` | docked size | docked size | expanded size | 0 |
| `ItemWidth` | `ItemHeight` for Square / Fill Square, otherwise `NaN` | same | same | `NaN` |
| `Stretch` | docked stretch | docked stretch | expanded stretch | docked stretch |
| `ItemSpacing` | 0 | 0 | `inputs.ItemSpacing` | 0 |
| `Margin` | 0 | 0 | 15, 40, 15, 5 | 0 |
| `Border` | 1px on the inner edge (bottom of Top, top of Bottom) | 1px on the inner edge (right of Left, left of Right) | 0 | 0 |
| `Extent` | docked size + `BorderSideThickness` + `VerticalScrollbarSize` | docked size + `BorderTopAndBottomThickness` + `HorizontalScrollbarSize` | `NaN` (fills the parent) | 0 |
| `ShowsHoverBar` | `!ShowBottomNavBar && ShowHoverNavigationBar` | same | false | false |

The Closed row's values are never applied, because a closed gallery is hidden. They're fixed so the record compares equal predictably.

`GalleryDefaults.GetDockedGalleryHeight`/`Width` are rewritten to take the item size, so `For` and `GalleryHelper` share one formula. `GalleryHelper` and `DockedGalleryWindowFitTests` pass `Settings.Gallery.DockedGalleryItemSize`.

### 2. `GalleryTransition` (Core)

In the same file or next to it: `readonly record struct GalleryTransition` and `static GalleryTransition? For(GalleryMode from, GalleryMode to, GalleryDockPosition dock, bool autoFit)`. It returns `null` when `from == to`.

| from → to | `Speed` | `TweensBothAxes` | `AppliesLookAtEnd` | `LocksWrap` | `RefitsAtEnd` | `RefitsEveryFrame` |
|---|---|---|---|---|---|---|
| Closed → Docked | Fast | false | false | false | true | false |
| Docked → Closed | Medium | false | – (hides) | false | false | false |
| Docked → Expanded | Medium | false | false | true | false | false |
| Expanded → Docked | Slow | false | **true** | true | true | Top/Bottom and `!autoFit` |
| Closed → Expanded | Medium | true | false | true | false | false |
| Expanded → Closed | Fast | true | – (hides) | true | true | false |

The speeds are the existing `GalleryDefaults` constants.

### 3. `GalleryViewModel` owns the appearance

- `Initialize(GallerySharedSettingsViewModel shared)` replaces `Initialize()`. It passes in the push-based `DockedGalleryItemSize`, `ExpandedGalleryItemSize`, `DockedGalleryStretchMode` and `ExpandedGalleryStretchMode`. It doesn't poll.
- `IReadOnlyBindableReactiveProperty<GalleryAppearance> Appearance` is recomputed whenever any of these change:
  - the switcher's `ShownMode`;
  - the dock position (the existing `EveryValueChanged` that drives `UpdateDockFlags`);
  - the four shared properties;
  - item spacing (the existing `EveryValueChanged`).

  The two hover-bar UI settings are read when the record is computed, as today's code reads them on a transition.
- `ItemSpacing` and its subscription are deleted. The panel takes spacing from the applied record.
- `TabNavigationInitializer`'s two calls and `DockedGalleryWindowFitTests` pass the core's `GallerySettings`.

### 4. `GalleryAnimationControl` applies and animates

- **`Apply(GalleryAppearance)`** is the only method that reads a record.
  - On `VirtualizingGallery` it writes `ItemHeight`, `ItemWidth`, `ItemSpacing`, `Orientation`, `IsExpanded` (from `Wraps`), `Margin`, and a new `ThumbnailStretch`.
  - On the control it writes `BorderThickness`, `DockPanel.Dock`, the viewer's scroll direction, `IsVisible`, and the tab's `Hoverbar.IsHoverbarVisible` (from `ShowsHoverBar`).
  - It remembers the record as `_applied`.
  - When `Dock` changes while docked, it keeps today's trick: set `WrapHeightOverride` to the extent for one render pass, then clear it. The `OnPropertyChanged` override goes.
- **The extent is set separately from `Apply`**, by `SetExtent(appearance)`. Along the dock axis it's a number; "fills the parent" becomes `NaN` on the axis that stretches and the parent's bounds on the other, as today.
- **`Animate(from, to, transition)`** is one method:
  1. apply `to` at the start unless the transition says `AppliesLookAtEnd`;
  2. lock the wrap height to the expanded wrap height when `LocksWrap`;
  3. tween width and/or height from the current extent to the target with `AnimationsHelper`;
  4. set the final extent;
  5. apply `to` at the end when `AppliesLookAtEnd`, or hide when `to` is Closed;
  6. unlock the wrap height and invalidate measure;
  7. scroll to the current item;
  8. refit when `RefitsAtEnd`.

  `RefitsEveryFrame` runs today's `Observable.EveryUpdate` refit loop around the tween. The macOS width fix after a side-docked Docked → Expanded stays as a named branch.
- **Without animation:** `Apply`, then `SetExtent`, scroll to the current item, and refit. This is today's `ShowWithoutAnimation`.
- **Mode changes** still come from `Switcher.Changes`. The target is `tab.Gallery.Appearance.Value`. The catch-up loop after an animation compares `_applied` with the target appearance, not only the mode.
- **Same-mode changes** (size slider, stretch combo, dock edge, spacing) come from one subscription to `tab.Gallery.Appearance`. It applies the record, sets the extent, invalidates measure and scrolls to the current item. A dock-edge change also refits. A record whose mode differs from `_applied.Mode` is ignored, because the mode change handles it. During an animation the record is dropped, and the catch-up picks it up.
- **Parent resize while expanded** calls `SetExtent(_applied)`.
- **Deleted:** `GetDockedSize`, `GetDocked/ExpandedMargin`, `IsHorizontalDock`, `SetExpandedLayout(Core)`, `SetExpandedThumbs` (both), `UpdateExpandedItemHeight`, `SetDockedLayout(Core)`, `SetDockedThumbPosition`, `UpdateDockedItemHeight`, `SetDockedThumbs`, `ApplyThumbSettings`, `UpdateLayoutForCurrentState`, the six transition methods, and the five subscriptions they used.

### 5. Bindings

- `VirtualizingGallery` gains `StyledProperty<Stretch> ThumbnailStretch`. The plan checks whether `ItemHeight`, `ItemWidth` and `ItemSpacing` already affect measure. If they don't, `Apply` invalidates measure once.
- `GalleryView.axaml`: the panel loses its `ItemHeight`, `ItemWidth` and `ItemSpacing` bindings. `LineSpacing` stays.
- `GalleryItem.axaml`: `Width`/`Height` bind to `$parent[customControls:VirtualizingGallery].ItemWidth/ItemHeight`, and the thumbnail's `Stretch` binds to the panel's `ThumbnailStretch`.
- `GalleryItem`'s constructor subscription to the app-wide stretch is deleted.
- The `GalleryStretchMode` → `Stretch` mapping becomes a static method in `PicView.Avalonia/Gallery`, used by `Apply`: Uniform and Square map to Uniform, UniformToFill and Fill Square to UniformToFill.

### 6. Deleted shared state

- `GallerySharedSettingsViewModel`: `ItemHeight`, `ItemWidth`, `ThumbnailStretch`, and the TODO.
- `IThumbnailLoader.GetThumbnailAsync(FileInfo, CoreViewModel)`. Nothing calls it, and it's the only reader of the shared `ItemHeight`. It's removed from the interface, `AvaloniaThumbnailLoader`, and the seven test fakes.

### 7. Docs

- ADR-0011 gets one sentence under Consequences: the applied look of a tab's gallery is its Gallery Appearance, derived per tab, while the gallery settings stay app-wide.
- `GLOSSARY.md` gets **Gallery Appearance**: how a tab's gallery looks in its shown Gallery Mode, derived from that mode, the dock position and the gallery settings. _Avoid_: gallery layout, gallery settings, thumb settings.

## Testing

Plain tests, with no Avalonia window:

- **`GalleryAppearanceTests`** (new): `[Theory]` rows over `GalleryAppearance.For`.
  - Every mode × dock edge gives the table in Change 1.
  - Square and Fill Square set `ItemWidth = ItemHeight`, and Uniform and UniformToFill set `NaN`.
  - The four hover-bar setting combinations decide `ShowsHoverBar` only when docked.
  - The docked extent follows the item size by the one formula, for several sizes.
- **`GalleryTransitionTests`** (new): each row of the table in Change 2. `RefitsEveryFrame` is true only for Expanded → Top/Bottom Docked with AutoFit off. The same mode returns `null`.
- **`GalleryViewModelTests`** (extended): `Appearance` follows the switcher's shown mode, including a hidden gallery (crop, Single Image) giving the Closed look. It also follows a dock-position change and a change to the shared item size or stretch.
- **`ThumbnailStretchTests`**:
  - `Thumbnail_ShownGalleryChangesStretch_TakesIt` and `Thumbnail_FillSquare_Crops` become plain tests of the stretch mapping, and `WithThumbnail` is deleted.
  - The combo-box tests stay headless, because they test the Settings window.
  - The loading theory doesn't change.

Headless:

- **`GalleryViewTests`** gets one fact: a docked tab's `GalleryItem` takes the panel's `ItemHeight`, `ItemWidth` and `ThumbnailStretch`.
- The existing `GalleryHidingTests`, `DockedGalleryWindowFitTests`, `GalleryViewTests` and `VirtualizingGalleryTests` pass unchanged, apart from the new `Initialize` argument and two comments in `VirtualizingGalleryTests` that refer to `GalleryAnimationControl`.

Run `dotnet test PicView.Tests\PicView.Tests.csproj`. The `CloseAsync` batch-resize and `Win32Clipboard` tests are known to be flaky, so they're re-run alone if they fail.

By hand, in the running app (tests can't check animation timing):

- Each of the six transitions at each dock edge.
- Expanded → Bottom Docked with AutoFit off: the image keeps refitting while the gallery shrinks.
- The docked and expanded size sliders, and both stretch combos, while docked and while expanded.
- Moving the dock edge while docked: no extra 0×0 collapse, and the window refits.
- Resizing the window while expanded.
- A Top/Bottom docked gallery keeps its height after the size slider moves (the 7px jump is gone).
