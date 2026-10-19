# Window Mode Is Changed in One Place; Platform Adapters Only Do Native Work

Each window has its own **Window Mode**, and every change to it (from PicView, from the operating system's window controls, from a slideshow, or at startup) goes through one Avalonia module, `WindowModeSwitcher`, whose rules live in Core next to `WindowMode`. The platform adapters (Windows, Linux, macOS) only enter native window states and handle their own quirks; they never run a whole mode change themselves. The Linux version is unreleased and untested, and its quirks are expected to differ from Windows; with whole-mode-change modules per platform, each quirk would fork the full change again, and the forks would drift apart, as the Windows and Linux copies already started to.

## Considered Options

- **A full mode change per platform, sharing helpers** (as `Win32Window`, `LinuxWindow` and `MacOSWindow` did): each platform kept its own copy of the settings, view-model flags, interface hiding, refit and save, so a fix in one copy did not reach the others, and nothing tested a change.
- **The settings flags as the live mode**: they are shared by every window, so one window going fullscreen changed what the others believed about themselves, and a slideshow had to write the setting back by hand to avoid saving it.

## Consequences

- A new platform quirk goes into that platform's adapter, not into `WindowModeSwitcher`. If a quirk seems to need a different rule, change the Core rule for every platform.
- The settings only remember the mode a new window starts in; they are not read as a window's current mode.
