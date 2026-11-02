# A Tab Derives Its Zoom Level

A **Tab**'s **Zoom Level**, the percent of its pixel size the shown image is shown at, is derived by one module the tab owns, `TabZoom` in `PicView.Core/Sizing`, from two inputs with one writer each: the **Fit Scale**, written by the window fitter's `Fit` through `SetFitScale`, and the **Zoom Factor**, how far the user has zoomed from the Fit, written by the image viewer through `SetFactor` whenever `ZoomPanControl` reports a scale change, and reset to 1 by the tab itself when it publishes a shown image while zoom resets on image change. `Level` is a read-only reactive int, `Convert.ToInt32(FitScale * Factor * 100)`, recomputed on either write and notifying only when the percent changes. `TabTitles` subscribes to it and marks on change, as ADR-0015 says. Nothing sets the level.

The factor reset happens inside `Publish`, before the Titles mark, so the reset rides the shown mark: a bound tab builds its Titles once per image, and once more only when the fit changes the percent. The window's reaction (ADR-0014) still resets the control's transform and refits; the control's reset reports a scale of 1, which the tab already holds, so nothing notifies twice.

Before this, `TabViewModel.ZoomLevel` was an `int` bindable written from three places: the reaction wrote `InitialZoom * 100` after each shown image, the Zoom to Fit toggle wrote the same after its refit, and a per-frame poll of the control's `Scale` in `ImageViewer` wrote `InitialZoom * Scale * 100` while the user zoomed, one rendered frame after the change. `ZoomPanControl` kept its own `ZoomLevel` that nothing read and bound a `FittingScale` that nothing used. ADR-0015 named the cost: an image change followed by the reaction's zoom write built a bound tab's Titles twice, the first time with the previous zoom. `InitialZoom` was the Fit Scale under a name the glossary avoids.

## Considered Options

- **`ZoomPanControl` owns the derivation**: it already multiplied its scale by the bound Fit Scale, so it could raise a level-changed event and the viewer could write the tab's level. One writer, but the Fit Scale would round-trip from the tab through a binding to the control and back, a tab without a viewer would derive nothing, and the fold into the shown change is impossible, since the control learns of a reset only in the reaction. Two builds per image would stay.
- **A stored int with one setter on the tab**, `SetZoom(fitScale, factor)`, called by the reaction and the wheel path: the smallest diff, but the reaction stays a writer of the level, which is the cost ADR-0015 named, and the two inputs stay scattered.
- **Compute on read for `Level`**, as the Titles do their strings: the Titles need a notification to mark, so the module would notify on every input write, and without comparing the derived int it would mark on same-percent image changes that today mark nothing. Comparing the int is the whole cost of storing it, so it is stored.
- **Fitting before the image shows**, so one build per image on every image: the Fit needs the window's screen and content size on the UI thread, and showing the image comes first (ADR-0005, ADR-0014). The fit follows the shown image, so the second build stays when the percent changes.

## Consequences

- The zoom level has one derivation. A search for writes to `ZoomLevel` finds none; the three writers, the poll, and the control's `ZoomLevel`, `UpdateZoomLevel` and `FittingScale` are deleted.
- `ZoomPanControl` reports its scale through one `ScaleChanged` event, raised from the one place that sets `Scale`, and keeps no level. A control reset outside the reaction, as for a Single Image or a Window Mode change, reaches the tab at once instead of a frame later.
- A wheel step allocates nothing on its way to the Titles' mark: one delegate invoke, one multiply, one int compare, and a push to a subscription that was made once.
- The orientation reset stays in the reaction, so ADR-0014's description of it holds. Moving it beside the factor reset in `Publish` is a candidate for a later slice.
- `InitialZoom` is gone; `tab.Zoom.FitScale` replaces it, also for the crop's aspect ratio.
- `PicView.Core` stays free of Avalonia types: the module is plain R3, and the control's event is wired in the viewer.
- ADR-0014 and ADR-0015 hold as written; ADR-0015's second build after a reset is gone.
