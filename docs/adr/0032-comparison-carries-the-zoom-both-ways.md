# Comparison Carries the Zoom Both Ways

Comparing is a **Tab**'s own: `TabViewModel.SetComparing` turns it on or off for that tab only, and the tab has a `ComparisonViewModel` exactly while it compares and shows something. Its Tab View is then a fourth kind, `TabViewKind.Comparison`, shown in the same `NavigableView` as the image viewer: the adapter, `TabViews.Set` (ADR-0022), puts a `ComparisonViewer` in the view's slot in place of the tab's kept `ImageViewer`, so the gallery, the click arrows and the hover bar stay as they were. A comparison viewer is made per comparison; when comparing turns off, or the tab shows nothing and later a new comparison is made, the adapter first puts the other viewer in the slot, which takes the old viewer's zoom preview out of the view's overlay, and then disposes the old viewer.

Toggling comparison keeps the zoom. Both viewers read and write the tab's Zoom Factor, `TabZoom.Factor` (ADR-0016): the shown one writes it as the user zooms, and when the slot changes from one viewer to the other, the incoming viewer is shown at the factor first, through `INavigableContent.ShowFactor`, without animation. A comparison viewer that is not loaded yet takes the factor when it loads. In the synced mode the left control writes the factor and both synced controls show it. A viewer already at the factor is left alone, since setting the scale again re-centres the zoom control's child.

This differs from the crop on purpose. ADR-0027 carries the zoom one way, from the viewer to the crop, because zooming in a crop is for placing a selection and the viewer should come back as the user left it. Comparing is another way of looking at the same images, which the user turns on and off to see a detail both ways, so the detail they zoomed to stays in view.

## Considered Options

- **One-way, as the crop**: the comparison starts at the viewer's zoom and the viewer comes back as it was left. Turning comparing off would lose the place the user zoomed to in the comparison, which is where the difference they were looking at is.
- **Each viewer keeps its own zoom**, with nothing carried. Turning comparing on would jump to whatever the comparison last showed, or to the fit, away from the detail the user was looking at.
- **The view position carried too**, as a `ZoomView` like the crop's. The stacked comparison lays its pair out at the fitted size while the image viewer lays out a scroll viewer, so a position does not map across without the layout of both; the scale is what the title and the zoom preview report, and it is enough to keep the detail's size.

## Consequences

- ADR-0022 holds, amended: a fourth kind, `Comparison`, shown in `NavigableView`. The tab derives it after Crop and before the rule for the image viewer, so a crop opened while comparing shows the crop control, and closing it shows the comparison again.
- ADR-0016's Zoom Factor has three writers, the shown view's control: the image viewer's, the crop's (ADR-0027) or the comparison viewer's.
- `INavigableContent` has `ShowFactor`. `ImageViewer` sets its zoom control's scale at once; `ComparisonViewer` sets the stacked control's, or both synced controls'.
- Comparing stays on when the tab closes its image or shows nothing, so the next image it shows compares. Side-by-side is still global; a tab shows a pair when side-by-side is on or the tab compares, which `TabViewModel.IsShowingPair` publishes for its Next and Prev, its Titles and the window's fit.
- The window fits the pair beside each other only for side-by-side and for a comparison in the synced mode; the other modes stack the pair, so the window fits one image (`WindowFitter.ShowsPairBeside`).
