# Actions Run Through One Seam

Everything that runs an **Action** (a Keybind, a button or menu item, a dialog, Core code) asks the window's `IActionRunner` to `Run` it by name. Before this, every Action was copied into four surfaces: a typed method on `IFunctionsMapper`, a forwarding `ReactiveCommand` on `MainWindowViewModel`, an arm in `FunctionsMapper`'s dispatch switch and an axaml command binding. Adding an Action meant touching six places, the view model's copies only passed calls through, and 29 of its commands were bound nowhere.

The names live as `const string`s in Core's `Actions` class. C# callers and the dispatch switch use the constants, and axaml passes them to the view model's one `RunActionCommand` through `{x:Static}`, so compiled bindings still check every name at build time. Which Actions exist is still decided by the keybinding categories and the **Hidden Actions**; a test checks that every constant is one of them and has an arm in the switch.

Next to `Run`, the seam keeps the few operations that are not Actions: `OpenPathAsync(path)`, which takes an argument; `StopRepeatedNavigation()`, which is what releasing a held Keybind does rather than something the user asks for; and `ShowStartUpMenu()`, which Core calls when a folder empties.

`FunctionsMapper` stays the pure dispatch table of ADR-0002: its arms only null-check and delegate, and its per-Action methods are private, so nothing can run an Action around `Run`.

## Considered Options

- **Keep the typed methods and generate the copies**: the copies would no longer be written by hand, but the interface would stay as wide as its implementation, and every caller would still depend on 135 members to run one Action.
- **An enum of Actions**: Actions are stored by name in `keybindings.json`, legacy names are aliased, and the favorite Actions (`OpenFavorite1`–`OpenFavorite10`) are parameterised, so an enum would need parsing and a mirror of each of those rules.
- **Bare strings guarded only by a test**: a typo in C# would build and a typo in axaml would only fail when the test ran. The constants make both build errors.

## Consequences

- A new Action is added in two places: a constant (in a category or `HiddenActionNames`) and an arm in the dispatch switch.
- Settings and window toggles that were only on `IFunctionsMapper` (such as `ToggleSubdirectories` or `Minimize`) are Hidden Actions now, so a user can bind them in `keybindings.json`.
- `Run` only dispatches. Input Mode rules such as "a playing motion photo disallows `Crop`" apply to Keybinds in `KeybindRouter`, not to clicks, as before.
- An unknown name, such as a stale one in a user's `keybindings.json`, is logged and does nothing.
