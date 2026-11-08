# The Last Image Shows in the Pair Before It

With Side-by-Side on and the list not looping, going to the last image shows the **Side-by-Side Pair** that ends with it: the image before it on the left and the last image on the right. This holds for every route to an index: opening a file (`TabLoad`, `QuickLoad`, `ArchiveOpenings`), jumping to it (a gallery click, `SkipToIndexAsync`), reloading it, the Shown File being deleted beside it, and turning Side-by-Side on while it is shown. With looping, the last image pairs with the first. Only a list of one image shows alone. The tab's position is the left image, and the gallery marks both images.

The rule is one pure function, `SideBySidePair.PairAt(index, count, sideBySide, looping)`, and the iterator is its only caller: `IImageIterator.IterateToIndexAsync` shows the image at an index together with its partner, and `IterateToIndicesAsync` leaves the interface. `PartnerOf` is deleted. Paired stepping still moves by two images and clamps so that it always ends on a full pair; `Navigate` returns only the stepped index and `PairAt` adds the partner.

Before this, five modules decided the partner themselves, and they disagreed at the last image. The jump paths clamped to the pair before it, the opening paths showed the last file alone, and a one-image list after a delete or reload showed nothing new, so a deleted file stayed on screen.

## Considered Options

- **The last image shows alone.** This was the glossary's old rule, and the opening paths followed it. It keeps the opened or clicked file as the left image, but it shows a single image in a side-by-side view, and turning Side-by-Side on at the last image would show nothing beside it.
- **A shared helper that callers call before choosing a method.** It would keep both iterator methods and add a seventh place that knows the rule.

## Consequences

- Deleting the partner, which is a Shown File too, shows the image beside it again with its new partner, instead of keeping the deleted image on screen.
- Opening the last file of a folder or archive decodes, or extracts, the image before it first, and the opened file shows on the right.
- View History, the recent files and the last file record the left image, as for every pair, so opening the last file records the image before it. Going back to it, or reopening it at startup, shows the same pair.
- Only the modules that decide the pair or the position read the published settings: `ImageIterator`, `FolderChanges`, `TabLoad`, `QuickLoad`, `ArchiveOpenings` and the tab's `CanNavigateForwards`. Titles, conversion, printing and the window fitter still read the static settings.
- The iterator reads Side-by-Side and looping from the tab's published settings (ADR-0020), and falls back to the static settings for a tab without them.
- The tab publishes `PartnerIndex` (-1 when the image shows alone) beside `NavigationIndex`, so the gallery can mark both images even when the list loops back to the first.
