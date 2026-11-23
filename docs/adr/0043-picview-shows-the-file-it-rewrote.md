# PicView Shows the File It Rewrote

The **Shown File** is shown again after PicView rewrites or replaces it, and the tab's turn and effects are reset when they were written into the file; a replace never overwrites another file; a file PicView overwrites or replaces goes to the Recycle Bin first; every in-process write of a tab's shown image goes through one save module, `ShownFileWriter` in `PicView.Core/Saving`.

A writer (Save, Save As, Crop, the Resize dialog, rename to another extension, the Convert window, the Info-window resize, Optimize) describes a job: the source, the changes and the destination. The module reads the source, writes off the UI thread with the tab Busy, and picks the follow-up by the destination, so no writer touches the cache, the tab's Orientation, `FileRenames`, the tab's load or deletes the original itself.

- **Over itself.** The tab shows the file again through `ShowRewrittenAsync(turnApplied)`: the cached image and the gallery thumbnail are forgotten, the file is decoded again and shown in place, keeping the Side-by-Side pair and the page. The turn is reset when it was written into the file, and the effects, when the Effects Render was written, are reset with `EffectsViewModel.DiscardEffects` on the UI thread. A save that changed nothing (no turn, no effects, no edits) keeps the shown model and only forgets the cached image and thumbnail.
- **Replaces the original.** The new path replaces the original: histories follow (`FileRenames.Follow`), the tab loads the new file, then the original goes to the Recycle Bin, so the watcher removes a file that is no longer shown. A replace never overwrites another file: when the name is taken nothing is written or deleted, and the writer shows the "file already exists" tooltip.
- **Copy elsewhere.** Nothing changes in the tab.
- **The source** is chosen by the writer from two kinds. The Save Source (Effects Render, else the file, else the pixels of a Single Image, with the turn and flip) for Save, Save As, Resize, the Info-window resize, Convert and rename to another extension; the file on disk for Crop (ADR-0028) and Optimize.
- **A Single Image has no file**, so over itself and replace become Save As. Resize and Convert then work on a pasted image.
- **Recycle Bin Backup.** Before the module writes over a file that exists, it copies that file in PicView's temp folder and moves the copy to the Recycle Bin from there, so the folder watcher sees no extra file. The Recycle Bin's Restore therefore puts the copy in the temp folder, not beside the original. On a drive with no Recycle Bin, or when the copy fails, the backup is skipped, the failure is logged and the write goes ahead. The step carries a TODO for a future Undo/Redo, which should restore from this copy.
- **Over-itself writes encode to the system temp folder and then copy over the original**, never to a temp file in the watched folder, which the folder watcher would see. The copy is not atomic, and the Recycle Bin copy is the net.
- View-model tooltip messages (`TooltipHelper.ShowTooltipMessage(vm, ...)`) show in the Main Window: each follows its `ToolTipMessageSource`. They were silently dropped before, rename's included.
- With "reset zoom on change" on, any showing of the file resets the turn, as before, so an Optimize or another write that did not write the turn still loses it then.
- The crop of a Single Image keeps its own bitmap path (a copy to a new PNG), outside the module.

## Considered Options

- **A Core module with one seam (chosen).** A writer builds a job; the module in `PicView.Core` reads, changes, writes and follows up by destination. Only a Single Image's pixels, an Avalonia `Bitmap`, come through a small interface implemented in `PicView.Avalonia`. The Resize and Info view models already live in Core, and the tests stay plain xUnit with a fake source.
- **Grow `FileSavingService` in Avalonia.** Less code moves, but the Core view models would need an interface to reach it anyway, and its tests need headless Avalonia.
- **Every writer reads the Save Source.** The most uniform, but Crop would pick up effects, against ADR-0028, and Optimize would stop being lossless.
- **A module that only forwards callbacks**, with writers encoding through their own `MagickImage`. Shallow, and it leaves the drift between writers where it is.

## Consequences

- `Cache` leaves `IImageIterator`; only the iterator reads its cache. The `clearCache` flag of `ReloadAsync` stays, for the reshows of a Side-by-Side pair.
- Crop still ignores effects, and Optimize is still lossless.
- Batch resize writes files that are not shown and stays outside the module.
- Undo/Redo is a TODO at the backup step.
- Known gap: an over-itself write of an animated GIF or WebP, a multi-page TIFF or an SVG writes one raster frame, as Save did before; the Recycle Bin copy is the net.
- The Recycle Bin gets a copy for every over-itself write, kept in the temp folder when restored.
