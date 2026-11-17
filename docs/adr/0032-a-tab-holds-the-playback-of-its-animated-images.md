# A Tab Holds the Playback of Its Animated Images

A **Tab** holds one **Animation Playback** per animated GIF, WebP or AVIF it shows: `TabViewModel.Animation.Primary` for the shown image and `Secondary` for the image beside it when viewing side by side. A playback is plain state in `PicView.Core/Animation`: whether it is paused, its speed, its position and, once a decoder has opened the file, its `FrameTimeline`, the one place that turns a position into a frame (a duration of zero or less counts as 100 ms). The tab writes the playbacks where it writes what it shows (ADR-0014): `ShowModels` makes a new one for each animated side before setting `Image` and `ImageType`, `UpdateShownFile` replaces the one whose file it follows, and a **Stand-in** ends both. `PicBox` binds a `Playback`; when it changes, or the control enters the visual tree, an `AnimationPlayer` opens the file and builds its `IAnimationDecoder` off the UI thread and is handed to `CustomVisualHandler`, which advances the playback by the time since its last frame, draws the frame it is on, and is the one that disposes the player, on the compositor thread. `Render` opens nothing; while a player runs it does not draw the still image under it. Side by side, both images play, and the tab's `TogglePause`, `Step` and `SetSpeed` apply to both.

Before this, `PicBox.Render` opened a new `FileStream` on the file and built a new `GifInstance`, `WebpInstance` or `AvifInstance` from it on every draw, without disposing the previous stream, and the handler reset its clock to zero for each new instance. Each instance had its own copy of the frame clock. There was nowhere to keep pause, speed or position, since anything about a playing animation was lost on the next draw. Three defects followed: resizing the window restarted the animation, re-parsed the file and left a file handle open per layout pass; an instance whose constructor threw was dereferenced anyway, null or already disposed; and a WebP whose frame durations were all zero divided by zero on every composition frame. Instances were also disposed on the UI thread while the compositor thread could be decoding with them.

## Considered Options

- **The image viewer holds a player in Avalonia**, commanded through the viewer as a motion photo is (`ImageViewer.ToggleMotionPhotoPlayPause`). Less plumbing, but pause and speed live in a view, their tests need a headless window, and a later keybind or speed label has to reach into the viewer instead of the tab, where the state a user commands lives (ADR-0014 to ADR-0022).
- **Fix in place**: `PicBox` remembers what it built its instance for, and the three defects are patched where they are. Cheap, but three clocks stay and pause has no home.

## Consequences

- A Stand-in ends the playbacks: `ShowStandIn`, and so `ShowLoading`, sets both to null, so the animation of the image it replaced does not play over a loading thumbnail or an Effects render. The next `Display` makes new ones.
- Pause and speed do not carry to the next image: each shown file gets a new playback, playing at speed 1.0. Whether they should is a question for issue 222.
- The zoom previewer and the crop control show the still image. Their `PicBox` controls bind no playback.
- Stepping back on a long GIF replays it from its first frame, since the GIF decoder draws each frame over the one before it.
- The playback keeps its position, pause and speed when its `PicBox` leaves the visual tree and comes back, as on a tab switch; the decoder is opened again.
- Nothing calls `TogglePause`, `Step` or `SetSpeed` yet. Issue 222 adds the Actions, keybinds and interface; `InputMode.AnimatedImage` stays derived from the image type.
- `PicView.Core/Animation` holds no Avalonia or SkiaSharp type and stays AOT-compatible; the frame clock is tested there without a bitmap.
