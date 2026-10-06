# PicView

A lightweight, fast image viewer with tabs, image preloading, editing, and gallery navigation.

## Language

**Clipboard Transfer**:
The transfer of image bitmap data, text, file references, or base64 encodings to or from the operating system clipboard.
_Avoid_: File duplication, clipboard command, clipboard animation

**File Duplication**:
Creating an on-disk copy of an image file and navigating to the newly generated file within the active viewer tab.
_Avoid_: Clipboard duplicate, copy-paste in place

**Paste Pipeline**:
The ordered ingestion sequence (Files -> Text/URLs/Base64 -> Bitmap) that decodes and routes clipboard contents into the viewer or tab model.
_Avoid_: Paste handler, multi-format paste, clipboard reader