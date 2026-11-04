# Settings Publish Their Changes

Fourteen **Settings** a menu toggles (looping, side by side, scrolling, zoom to fit, subdirectories, file history, full path in the title bar, taskbar progress, open in same window, constrain background, top most, and the three interface settings) are one writable bindable each on `GlobalSettingsViewModel`, made eagerly over the loaded record, and handed to every tab through `TabServices` and to every Main Window through its view model. Each has one write-through, made first in the constructor and skipping the stored value, that writes the static record and saves it once, coalesced, so two windows cannot overlap two writes of the file. Six of them derive their on/off menu text, re-derived by `Rephrase()` after a language load. What reacts to a change subscribes where its owner is: the tab derives Next and Prev and relists or re-pairs through its own Load and iterator, its Titles mark, the window's fitter fits, each Main Window follows top most and taskbar progress, the dropdown derives whether file history shows, and the app follows open in same window, the constrain setting and subdirectories once. The Action is the one writer, `setting.Value = !setting.Value` (ADR-0009), and no binding on a setting is `TwoWay`.

Before this, `SettingsUpdater` held ten toggles that each hand-wrote the static, one to three mirrors and a menu text, ran their effect on the active tab only and saved; `WindowFunctions.ToggleTopMost` reached the first window only; `ToggleUIVisibility` wrote three texts and one save; `MainWindowViewModel` mirrored four settings, `GlobalSettingsViewModel` three, `TranslationViewModel` six texts and `SettingsViewModel` six copies, two of which nothing updated after construction; `LanguageUpdater` wrote the six texts by hand; seven bindings were `TwoWay` beside an Action; `DropDownMenuViewModel` wrote its file history row from the static at five closes, so an open dropdown never followed the toggle; and `TabTitles.Refresh()` and `TabViewModel.RefreshCanNavigate()` existed because three settings notified nothing (ADR-0015, ADR-0017).

## Considered Options

- **A `Toggle` module per setting**, with a read-only `IsOn`, a read-only `Text` and `ToggleAsync()`, so a write outside the module does not compile, as ADR-0017's flags. The right guarantee, but it changes every binding's path twice over and adds a type where a bindable with a write-through gives the same single owner. Chosen instead: writable bindables, with the Action as the one writer in practice and `TwoWay` bindings removed.
- **A static set beside `Settings`**, created by `LoadSettings` and `SetDefaults`. No constructor or `TabServices` change, but axaml needs a source hop to bind it, tests share one process-wide set, and it runs against ADR-0019 and candidate 12, which hand shared state over rather than reach for it.
- **A notifying `AppSettings`** (`INotifyPropertyChanged` on the serialized classes). The readers are untouched, but the JSON record becomes reactive, no one owns the save or the menu text, and a setting still has no single owner.
- **Only the active tab relists or re-pairs**, as today, with the Action keeping that one effect. Other tabs stay stale; the Action keeps a hand-written effect. Chosen instead: every tab follows by its own subscription, one listing or one partner decode per tab with files, once per toggle.
- **Background tabs wait** until they become active. Correct at the cost of only the visible tabs, but adds a stale flag and an activation hook to the tab.
- **The Actions keep the save.** Fourteen saves stay in Avalonia and the module owns only the value. The module saves, coalesced, so two windows cannot overlap two writes.
- **The six texts stay on `TranslationViewModel`**, derived there. It would have to be given the settings, and the text is the setting's, not the language's.
- **The tab subscribes in its constructor.** A primary constructor cannot subscribe to the services it was given without a body, and a Fresh Tab has nothing to follow; `Initialize` is where it first gets files.
- **`SideBySidePair` takes the two settings as parameters**, so its tests stop writing statics. Out of scope: it widens into every caller of the helper and the iterator; the record keeps its readers in this slice.

## Consequences

- A new published setting is one bindable, one `Follow` and subscribers, never a mirror or a poke. Both pokes are deleted.
- The record keeps one writer per published setting, its write-through, and its roughly 400 plain readers; `SideBySidePair`, `FileAndDirectoryNavigator`, `FileHistoryManager` and `IPC` keep reading it.
- `TwoWay` bindings on a setting are not allowed: with a write-through, a checkbox that both binds `TwoWay` and runs the Action would toggle twice.
- The write-through subscribes first, so a later subscriber that reads the record sees the new value.
- The fourteen are eager; the extra settings (`InitializeExtraSettings`) stay lazy.
- Every Main Window follows top most; the setting is app-wide.
- The three interface settings publish their value; the window's shown state is derived from them and its Window Mode (ADR-0021).
- The Settings window's 24 plain values still save through the static as they did.
- ADR-0015 and ADR-0017 hold; their one outside call each is gone.
