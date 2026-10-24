# Keep FunctionsMapper as a Pure Function Map

`FunctionsMapper` implements `IFunctionsMapper` to bridge `PicView.Core` ViewModels and shortcut keybindings to `PicView.Avalonia` UI actions. Although helper classes like `UINavigationHelper` may appear to be thin pass-throughs from `FunctionsMapper`, `FunctionsMapper` is intentionally restricted to a pure dispatch table that performs only null checks and delegates all orchestration and UI workflow logic to dedicated helper modules. Do not inline navigation, dialog, or state-management logic into `FunctionsMapper`.

## Superseded In Part

The typed methods of `IFunctionsMapper` are replaced by one `Run(action)` seam (ADR-0009). The rule above is unchanged: `FunctionsMapper` stays a pure dispatch table.
