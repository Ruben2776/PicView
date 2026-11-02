# A Tool Window or App Window Is Placed and Remembered Once

Every **Tool Window** and **App Window** is described once, by `WindowInitializer`, in a `SecondaryWindowSpec` that carries its title, its placement and whether Escape closes it. The placement is a `SecondaryWindowPlacement`: `Remembered` (opens where it was last closed, and saves where it closes), `CenterScreen` or `CenterOwner`. `SecondaryWindowOpener` applies the spec in one sequence. It sets the title, places the window once before showing it, and subscribes the one Escape handler. Its one `Closing` handler writes the window's bounds, runs the caller's `OnClosing`, saves, and then disposes the window once. The order is read in one method and no longer depends on which subscriber came first.

Which remembered size and position count as valid is `RememberedPlacement` in `PicView.Core.Config`, a pure function with its two directions, `Restore` and `Remember`. A size applies only when the window can resize and both values are finite and strictly inside the window's limits. A position applies when both values are remembered. A maximized window restores as maximized and nothing else. With nothing usable remembered, the window centers on screen. The rule is tested in plain xUnit, without a window.

The platform windows only build their tree. `GenericWindowHelper`, the `PrintWindow` base class and the two `IWindowProperties` overloads of `WindowFunctions` are deleted, 29 platform window constructors lost their init call, and four `IWindowProvider` methods lost a config parameter. Every title is combined with the app name once, in the spec; About keeps its exception for Japanese and Vietnamese. Resize and Convert are remembered now, with a new `ResizeWindowConfig` and `ConvertWindowConfig`, like every other Tool Window.

Before this, a window was placed twice, by different rules: its platform constructor read the config through `GenericWindowHelper`, then the spec's `Configure` placed it again through `WindowFunctions`, and Print had a fourth path in `PrintWindow`. Saving was split across two modules, the helper writing the bounds and the spec saving, and it worked only because the constructor subscribed to `Closing` before the opener did. Keybindings and Batch Resize were disposed twice. There were two Escape implementations, one of which never told the **Main Window** to ignore the still-held key. Three windows carried the app name twice, and three titles differed by platform: Convert on Linux, Resize on macOS. The width lock was asked for by Effects only and did nothing, since the window sizes to its content and the width is not a number when the lock reads it. It was dropped.

## Considered Options

- **Move the helper call into the opener's `Configure` step** and drop it from the constructors. Cheapest, and it gets to one placement. It keeps the static grab-bag, the `KeyUp` Escape that never marks the window, the unclamped position and the save split by subscription order, so the next slice (or a dock-or-float Tool Window) would reopen it.
- **A base window class that places itself**, taking the properties and title as constructor arguments. The constructors still have to pass them, so a placement fix would still land in three platform projects, which is what the review found.
- **Keep Resize and Convert centered on their owner.** They were, and the spec could say so. Remembering them costs two small config classes and makes every Tool Window behave the same way.
- **A fallback kind on `Remembered`**, so that Resize and Convert center on their owner the first time. One field for one first open; every other remembered window centers on screen, and the two join them.
- **Keep the width lock** as a spec flag. It is a no-op on the one window that asks for it.

## Consequences

- A new remembered window is a config class and a spec in `WindowInitializer`. Nothing in a platform project changes, and a placement fix lands in one place.
- A first open with nothing remembered centers on screen, for Resize and Convert too, which were centered on their owner before.
- A negative remembered position clamps to zero, as it did for Settings and Image Information. This is a known compromise for a monitor left of or above the primary one. Widening it is a later slice.
- Escape closes a window on `KeyDown` in every window that has it on, and marks it, so the **Main Window** ignores the still-held key when focus returns. Keybindings turns Escape off, since it records keys.
- `OnClosing` runs after the bounds are written and before the save, so a caller that records extra state there (Settings' last tab, Print's print settings) is saved with the bounds.
- This completes ADR-0007's openers. It decides only how a floating window is placed and remembered, not who owns it. Tool Windows may later dock or float, so ownership stays as ADR-0007 has it, and `SecondaryWindowPlacement` is a floating window's placement, not a statement about where a Tool Window lives.
- The File Associations window keeps its own `Closing` handler, which disposes a view model rather than placing the window. `ToggleTopMost` is not changed.
- The opener and its spec keep their `Secondary` names, although the glossary avoids that word. Renaming them is not this decision.
