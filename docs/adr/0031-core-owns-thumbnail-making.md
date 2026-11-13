# Core Owns Thumbnail Making; Avalonia Only Decodes

Every Thumbnail PicView shows comes from one module, `Thumbnails` in `PicView.Core.Thumbnailing`, one for the whole app, made at App Start and handed to every tab in `TabServices`. Gallery Items, the Stand-in, File History and the drag preview ask it; none of them decides which cache to check, which height to use, whether the shell has a thumbnail, or how a format is read. A Thumbnail is always a bitmap no taller than the height asked for, never the full image.

It offers two kinds. A **Quick Thumbnail** (`GetQuickAsync`) is a kept one or one the OS shell or EXIF has ready; it never decodes and is never kept, so a Stand-in never waits behind gallery scrolling. A made Thumbnail (`GetAsync`) decodes when nothing tall enough is kept. Made thumbnails are kept per owner: a tab's id, `Thumbnails.AppOwner` for File History, or none for a drag preview. Only the tallest one asked for is kept for each file, and it answers every request up to its height. A tab releases its own when it closes or becomes fresh; the file watcher forgets a file's when the file changes; the gallery keeps a renamed folder's file under its new path.

Before decoding, a made request takes a lease on `SharedImageCache` (`TryUseReady`), which holds a reference while it reads, and scales down the image already there when it is a still bitmap. Animations, motion photos and SVG drawings are decoded instead. Decoding takes one of a few shared slots and runs on the thread pool, so the caller's thread never blocks and the disk never takes more than a few reads at once. A request still waiting for its slot is cancelled with its token, which is how gallery items that scroll out of view give their slot up. A file that fails is not remembered and is tried again next time.

The platform work sits behind `IThumbnailDecoder`, with three operations: a ready-made thumbnail, a decode at a height, and a scale-down of a decoded still image. `AvaloniaThumbnailDecoder` is given the platform service once, so every caller gets the shell's thumbnails; tests use a fake decoder with the real `Thumbnails` and `SharedImageCache`.

The drag preview of an SVG stays vector, by the user's ruling. It keeps its own branch outside `Thumbnails`, which only hands out bitmaps.

## Consequences

- `IThumbnailLoader`, `AvaloniaThumbnailLoader`, `IThumbnailCache` and `ThumbnailHelper` are deleted. `TabServices` carries `Thumbnails` instead of a thumbnail cache and loader, and `CoreViewModel.SharedThumbnails` reaches it.
- `GetThumbnails` keeps the format knowledge (shell, EXIF, RAW preview, Skia, ImageMagick, the SVG fallback) and takes the platform service, not a `CoreViewModel`.
- The gallery height, the taller of the docked and expanded item sizes, is worked out once, in `Thumbnails.GalleryHeight`.
- The gallery still detects the motion photo badge itself, after a thumbnail is made, not when a kept one is served.
- File History thumbnails used to be the full decoded image when it was in memory, and were made again on every read without the shell's thumbnail. They are now 150 px Thumbnails kept for the whole run.
- A new place that shows a thumbnail asks `Thumbnails` for a quick or a made one and says who keeps it.

## Considered Options

- **The whole module in `PicView.Avalonia`, with Core seeing only an interface** (the old `IThumbnailLoader`). Rejected: the cache, height and shell rules are where the bugs were, and in Avalonia they could only be tested through mocks of the loader, which about twenty test suites each wrote again.
- **One request with a "do not decode" flag** instead of quick and made thumbnails. Rejected: the difference is one the Stand-in depends on, so it is named.
- **Rasterizing an `SvgDrawing` already in memory** for the scale-down. Rejected: decoding the file through the SVG fallback already makes that bitmap, off the UI thread.
- **Converting the drag preview's SVG to a bitmap** so it too comes from `Thumbnails`. Rejected by the user: the preview should stay sharp.
