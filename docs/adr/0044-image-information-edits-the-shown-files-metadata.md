# Image Information Edits the Shown File's Metadata

Every edit in Image Information (a typed tag, a dropdown, Date Taken, Remove metadata) writes to the file whose values the window shows. Afterwards the window reads that file again, so a box never shows a value that was not written. For a JPEG only the metadata segments are rewritten, and the image data and anything after it, such as a motion photo's video, are copied byte for byte. Any other format is decoded and encoded again. Orientation and Compression are read-only.

- **One target.** `ExifViewModel` keeps the `ImageModel` it last showed, and `CommitAsync` and `CommitChoiceAsync` write to that model's file, not to whatever the active tab shows when the write runs. When another image is shown meanwhile, the write still lands on the first file and nothing is read again.
- **Read again, not kept.** A typed value is not kept in the box: the box shows what the file holds after the write. A malformed value, or one the write failed for, goes back to the file's value. A dropdown whose pick is what the file already holds writes nothing.
- **JPEG in place.** `JpegMetadataWriter` replaces the Exif APP1 segment, or inserts one after the JFIF segment. XMP, IPTC, the ICC profile and comments are left out only when the edit removed them, as Remove metadata does with all but the ICC profile. Everything the metadata read (Ping) did not see is kept. A JPEG it cannot parse, or Exif larger than one segment (64 KB), falls back to encoding the image again.
- **Remove metadata keeps the colour profile.** It removes Exif, XMP, IPTC and comments; dropping the ICC profile would change the colours of a wide-gamut image.
- **Orientation and Compression only show.** Compression describes how the pixels are encoded, and changing the tag does not change the encoding. Writing Orientation would change how the tab shows the image, which belongs with the writes of ADR-0043, not with a tag edit. Their dropdowns open, every item shows the No cursor, and a pick goes straight back to the file's value.

## Considered Options

- **Encode every format again.** This was the old behaviour: every tag edit cost a JPEG some quality, and Magick.NET dropped the data after a motion photo's image.
- **A metadata library.** The well-known .NET metadata library only reads and is not ready for AOT. JPEG segments are simple enough to write by hand.
- **An Apply button that batches edits.** This would cut the number of encodes, but not the loss, and every edit already commits on its own.

## Consequences

- Tag edits stay outside the save module of ADR-0043. They write the file in place, with no Recycle Bin Backup and no reshow; the folder watcher shows the file again.
- The JPEG rewrite writes over the file in place, as the encode path did, not through a temp file in the folder, which the folder watcher would see as a rename of the Shown File.
- Lossy WebP, HEIC and AVIF still lose a little quality on each edit. WebP could get the same in-place treatment, since it is also a chunk format.
- Changing the Resolution Unit of a JPEG leaves the JFIF density unit as it was. Only an encode, for the other formats, writes it from the image's density.
