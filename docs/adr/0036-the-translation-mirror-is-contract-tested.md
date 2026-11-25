# The Translation Mirror Is Contract-Tested

`TranslationViewModel` still mirrors `LanguageModel` by hand, one bindable and one `X.Value = t.X` line per translated string shown in axaml, and three tests now hold the mirror to its contract. `TranslationMirrorTests` fills a model where each text is its own key and asserts that each bindable shows the key of its own name, or the key its entry in the test's alias table names; nine renames are listed there (`AboutWindow` reads `InfoWindow`, `FileCut` reads `Cut`, `Fnumber` reads `FNumber`, `Paste` reads `FilePaste`, `ReadAbleAspectRatio` reads `AspectRatio`, `ScrollDisabled` and `ScrollEnabled` read `Scrolling…`, `SelectFile` reads `OpenFileDialog`, `UnFlip` reads `Unflip`), `File` and `Files` compare ignoring case, and the two setting-derived texts are exempt. `LanguageFileTests` asserts every model key exists in `en.json`. `TranslationBindingTests` scans the axaml for a binding on a translation that stops short of `.Value`. A language change is announced once: `TranslationViewModel.LanguageChanged` fires as the last line of `UpdateLanguage()`, on the thread that loaded the language, and whatever keeps translated text of its own subscribes to it. `GlobalSettingsViewModel` rephrases its six menu texts from it (`FollowLanguage`), and `SettingsViewModel` rebuilds its search tags and suggestions from it. The double-click choices keep no text: the combo box has three fixed items bound to the translations.

Before this, nothing checked the mirror and it had drifted: the Effects window headed its vignette slider "Vibrance" (`Vignette.Value = t.Vibrance`) and its vibrance slider "Tint" (an axaml copy slip), Batch Resize read the singular `File` key for `Files`, `ErrorLoadingImage` had no key in any language file, and thirty bindings bound the bindable rather than its `.Value`, so their text never followed a language change. The nine deliberate renames looked the same as the typo. `LanguageUpdater` called `GlobalSettingsViewModel.Rephrase()` by hand after a load, and nothing told the Settings window, whose search tags, suggestions and double-click choices were built once and stayed in the old language.

## Considered Options

- **Remove the mirror: bind through one notifying holder of the model.** One C# edit per string instead of three, but it rebinds some 988 bindings to `Translation.Value.X`. Deferred, not rejected; the tests here are not wasted by it.
- **Remove the mirror: generate the bindables.** Compiled bindings need declared properties and Core is AOT, so this is the repo's first source generator. Deferred likewise.
- **A static event on `TranslationManager`.** Nothing to hand over, but it is process-wide state that tests share, against ADR-0019 and ADR-0020.
- **`LanguageUpdater` calls each keeper by hand**, as it called `Rephrase()`. Every new keeper is a poke to remember, which is how the Settings window went stale.
- **Rebuild the double-click choices on `LanguageChanged`.** Replacing a combo box's item list clears its selection, and the two-way selected index would write that back to the setting. Fixed items bound to the translations follow the language with nothing to rebuild.
- **The alias table in product code**, as attributes or a table `UpdateLanguage()` reads. That is the start of removing the mirror; while the mirror is hand-written the table belongs with the test that needs it.

## Consequences

- A new translated string shown in axaml is still a model property, a bindable, an assignment and a key per language file, but a wrong assignment, a missing English key or a binding without `.Value` fails a test.
- An alias is added to the test's table only for a rename that is meant. A failure is fixed in the mirror, not listed.
- Anything that keeps translated text subscribes to `LanguageChanged` and re-reads; nothing is poked from `LanguageUpdater`. A subscriber runs off the UI thread on a language change, so it sets bindables and touches no controls.
- `SettingsViewModel.SearchData` is a bindable; its bindings read `SearchData.Value.X`.
- `IsShowingFadingUIButtons` and `IsUsingTouchpad` stay on `TranslationViewModel`, written by `LanguageUpdater` and the Settings window's two commands.
- Only English is checked for missing keys. Another language file missing a key shows nothing for it, as before.
- The roughly 330 C# reads of `TranslationManager.Translation` that use a text at once and keep nothing are unchanged.
- Superseded in part by ADR-0040: `GlobalSettingsViewModel` rephrases one menu text, the File History text; the other five are derived per window in `WindowSettings` from its setting and the translation bindables.
- Superseded in part by ADR-0046: `SettingsSearchData` and `SettingsViewModel.SearchData` are gone; a Suggestion names its Setting Row by key, and the Suggestions are still rebuilt from `LanguageChanged`.
