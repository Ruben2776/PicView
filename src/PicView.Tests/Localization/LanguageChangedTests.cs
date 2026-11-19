using PicView.Core.Localization;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.Localization;

/// <summary>
/// A language change is announced once, after the translations are set, and text kept elsewhere follows it.
/// </summary>
[Collection("Sequential")]
public class LanguageChangedTests
{
    public LanguageChangedTests()
    {
        // GlobalSettingsViewModel reads the settings record when it is made
        SetDefaults();
    }

    private static LanguageModel Model() => new() { File = "file", Files = "files" };

    [Fact]
    public void UpdateLanguage_WhenDone_FiresLanguageChangedOnceAfterTheTextIsSet()
    {
        var model = Model();
        model.About = "Om";
        var previous = TranslationManager.Swap(model);
        try
        {
            var vm = new TranslationViewModel();
            var seen = new List<string?>();
            using var subscription = vm.LanguageChanged.Subscribe(_ => seen.Add(vm.About.Value));

            vm.UpdateLanguage();

            Assert.Equal(["Om"], seen);
        }
        finally
        {
            TranslationManager.Swap(previous);
        }
    }

    [Fact]
    public void FollowLanguage_LanguageChanges_RephrasesTheMenuTexts()
    {
        var first = Model();
        first.FileHistoryEnabled = "on-1";
        first.FileHistoryDisabled = "off-1";
        var previous = TranslationManager.Swap(first);
        try
        {
            var translation = new TranslationViewModel();
            var settings = new GlobalSettingsViewModel(() => ValueTask.FromResult(true));
            settings.FollowLanguage(translation.LanguageChanged);

            var second = Model();
            second.FileHistoryEnabled = "on-2";
            second.FileHistoryDisabled = "off-2";
            TranslationManager.Swap(second);
            translation.UpdateLanguage();

            Assert.Contains(settings.FileHistoryText.Value, new[] { "on-2", "off-2" });
        }
        finally
        {
            TranslationManager.Swap(previous);
        }
    }
}
