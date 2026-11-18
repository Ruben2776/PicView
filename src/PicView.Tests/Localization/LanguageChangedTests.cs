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
        first.LoopingEnabled = "on-1";
        first.LoopingDisabled = "off-1";
        var previous = TranslationManager.Swap(first);
        try
        {
            var translation = new TranslationViewModel();
            var settings = new GlobalSettingsViewModel(() => ValueTask.FromResult(true));
            settings.FollowLanguage(translation.LanguageChanged);

            var second = Model();
            second.LoopingEnabled = "on-2";
            second.LoopingDisabled = "off-2";
            TranslationManager.Swap(second);
            translation.UpdateLanguage();

            Assert.Contains(settings.LoopingText.Value, new[] { "on-2", "off-2" });
        }
        finally
        {
            TranslationManager.Swap(previous);
        }
    }
}
