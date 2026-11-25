# A Suggestion Names Its Setting Row by Key

Each Setting Row of the Settings window carries a `SettingKey` (`ui:SearchProperties.Key="Theme"` on its `searchBorder`), and `SettingsSearchIndex.Build()` makes one Suggestion for each key and one for each category. Picking a Suggestion looks its row up by the key. Typing fills the Suggestion list and touches nothing else: the page is never filtered, dimmed or scrolled by the Settings Search, and the category list is never narrowed. A pick scrolls the row to the middle, pulses it once (the accent border and a faint accent background, in and out over about 1.5 s), moves focus into it, clears the box and closes the list.

Before this, a Suggestion and its row were tied by text. Each row had a keyword string built in `SettingsSearchData`, and a pick put the Suggestion's name in the search box, which searched again and dimmed every row whose string did not hold that name. The dimming stayed until the page was clicked. The strings had drifted: "Navigate backwards" was suggested but in no row's string, so picking it dimmed the whole page; the background row's string held every theme word, so "Dark theme" lit two rows; two strings were joined with no space between them; and the two headers that are a `TextBlock` never dimmed, since the dim style was written for a `StackPanel`. Three rows had a string and no Suggestion, and several Suggestions led to one row.

## Considered Options

- **Fix the keyword strings and keep the text tie.** Each drift is one edit, but nothing stops the next one, and a pick still leaves the page in a state the user has to dismiss.
- **Dim while typing, pulse on a pick.** The list already shows what matches, and scrolling to the nearest match on every key press made the page jump.
- **A Suggestion for each choice**, such as one for each theme. Several entries then lead to one row. The choices are Search Keywords of their row instead, so "backwards" finds "When deleting a file".
- **Keywords on the row, in axaml.** The index needs them too, and they are translated text, so they would be bound from a view model as before. One place, in Core, builds a row's name and keywords together.
- **A string key.** An enum is checked by the XAML compiler where a row is marked, and a test can ask for every member.

## Consequences

- A new Setting Row is a `SettingKey` member, the key on its `searchBorder`, and an `Add` line in `SettingsSearchIndex.Build()`. `SettingsSearchIndexTests` fails when a key has no Suggestion, and `SettingsSearchViewTests` when a key is on no row or on two.
- A block that held several settings is split, so each has its own `searchBorder`: the two delete confirmations, the docked gallery toggle and the motion photo badges, the four gallery sliders, the two thumbnail stretches, and AutoFit and the window margin.
- A row's name is the text the index gives it, which for a row without a label of its own is the nearest translation there is: "Open file history" for the File History toggle, "Open files in the same window" for the Opening Mode, "Orientation" for the docked gallery's edge.
- `SettingsSearchData`, `SettingsViewModel.SearchData`, `SelectedSuggestion` and the `searchMatch`, `searchDim` and `searchHeaderDim` styles are gone. `SearchProperties.Keywords` is replaced by `SearchProperties.Key`. This supersedes ADR-0036 where it says `SearchData` is a bindable; the Suggestions are still rebuilt on `LanguageChanged`.
- The controller tells `SettingsViewModel` which keys and categories are on the page (`SetSettingsOnPage`), so File associations is not suggested outside Windows.
- Matching is by word: every word typed must be in the name, the Search Keywords or the category's name. A category Suggestion is found by its own name and keywords only.
- The pulse is the row's two brush transitions: the controller adds the `searchPulse` class and a tinted background, and takes both away after 1.1 s. A row's border and background are now Transparent rather than unset, so the transitions have a brush to start from.
- The suggestion list opens and closes with the Suggestions, not on a key press, and Enter in the box no longer steps through matches.
