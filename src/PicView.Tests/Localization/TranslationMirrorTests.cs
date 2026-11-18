using PicView.Core.Localization;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.Localization;

/// <summary>
/// <see cref="TranslationViewModel" /> mirrors <see cref="LanguageModel" /> by hand. This checks the mirror: each
/// bindable shows the key of its own name, or the key it is listed here as meaning to read.
/// </summary>
[Collection("Sequential")]
public class TranslationMirrorTests
{
    // Bindable name -> the LanguageModel key it is meant to read. Add an entry only for a rename that is meant.
    private static readonly Dictionary<string, string> Aliases = new(StringComparer.Ordinal)
    {
        ["AboutWindow"] = "InfoWindow",
        ["FileCut"] = "Cut",
        ["Fnumber"] = "FNumber",
        ["Paste"] = "FilePaste",
        ["ReadAbleAspectRatio"] = "AspectRatio",
        ["ScrollDisabled"] = "ScrollingDisabled",
        ["ScrollEnabled"] = "ScrollingEnabled",
        ["SelectFile"] = "OpenFileDialog",
        ["UnFlip"] = "Unflip",
    };

    // Change the first letter's case.
    private static readonly HashSet<string> CaseChanged = ["File", "Files"];

    // Derived from a setting, written by LanguageUpdater.
    private static readonly HashSet<string> Exempt = ["IsShowingFadingUIButtons", "IsUsingTouchpad"];

    [Fact]
    public void UpdateLanguage_EveryBindable_ShowsItsOwnKeyOrItsListedAlias()
    {
        var model = new LanguageModel();
        foreach (var property in typeof(LanguageModel).GetProperties())
        {
            if (property.PropertyType == typeof(string))
            {
                property.SetValue(model, property.Name);
            }
        }

        var previous = TranslationManager.Swap(model);
        try
        {
            var vm = new TranslationViewModel();
            vm.UpdateLanguage();

            var wrong = new List<string>();
            foreach (var property in typeof(TranslationViewModel).GetProperties())
            {
                if (property.PropertyType != typeof(BindableReactiveProperty<string?>) ||
                    Exempt.Contains(property.Name))
                {
                    continue;
                }

                var shown = ((BindableReactiveProperty<string?>)property.GetValue(vm)!).Value;
                var expected = Aliases.GetValueOrDefault(property.Name, property.Name);
                var comparison = CaseChanged.Contains(property.Name)
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal;
                if (!string.Equals(shown, expected, comparison))
                {
                    wrong.Add($"{property.Name} shows '{shown}', expected '{expected}'");
                }
            }

            Assert.True(wrong.Count == 0, string.Join(Environment.NewLine, wrong));
        }
        finally
        {
            TranslationManager.Swap(previous);
        }
    }

    [Fact]
    public void Aliases_EveryEntry_NamesARealBindableAndARealKey()
    {
        foreach (var (bindable, key) in Aliases)
        {
            Assert.NotNull(typeof(TranslationViewModel).GetProperty(bindable));
            Assert.NotNull(typeof(LanguageModel).GetProperty(key));
        }
    }
}
