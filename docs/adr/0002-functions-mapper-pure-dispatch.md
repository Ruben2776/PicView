# Keep FunctionsMapper as a Pure Function Map

`FunctionsMapper` implements `IFunctionsMapper` to bridge `PicView.Core` ViewModels and shortcut keybindings to `PicView.Avalonia` UI actions. Although helper classes like `UINavigationHelper` may appear to be thin pass-throughs from `FunctionsMapper`, `FunctionsMapper` is intentionally restricted to a pure dispatch table that performs only null checks and delegates all orchestration and UI workflow logic to dedicated helper modules. Do not inline navigation, dialog, or state-management logic into `FunctionsMapper`.

The rules that used to sit in arms live in helpers: scrolling and zooming reach the image viewer through `IZoomScrollTarget` and `ViewerActions`, Up and Down are Contextual Actions given their meaning by the Input Mode (ADR-0023), and the Actions that ask the operating system about the shown file go through `PlatformActions`. No arm resolves a view, `Application.Current` or a thread itself.

## Superseded In Part

The typed methods of `IFunctionsMapper` are replaced by one `Run(action)` seam, and the interface is now `IActionRunner` (ADR-0009). The rule above is unchanged: `FunctionsMapper` stays a pure dispatch table.
