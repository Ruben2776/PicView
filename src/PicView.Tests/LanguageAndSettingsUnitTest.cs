using System.Globalization;
using System.Text.Json;
using PicView.Avalonia.SettingsManagement;
using PicView.Core.Localization;
using PicView.Core.ViewModels;
using R3;
using ZLinq;

namespace PicView.Tests;

[Collection("Sequential")]
public class LanguageAndSettingsUnitTest
{
    public LanguageAndSettingsUnitTest()
    {
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
    }

    private sealed class MockFrameProvider : FrameProvider
    {
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) => callback.MoveNext(0);
    }

    [Fact]
    public async Task CheckIfSettingsWorks()
    {
        LoadSettings();
        Assert.NotNull(Settings);
        var testSave = await SaveSettingsAsync();
        Assert.True(testSave);
    }

    [Fact]
    public void CheckLanguages()
    {
        // Load the keys from the en.json file
        var enJsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/Languages/en.json");
        var enKeys = GetJsonKeys(enJsonPath);
    
        var languages = TranslationManager.GetLanguages();
    
        // Check each language file against en.json keys
        foreach (var language in languages)
        {
            if (language.FullName.Equals(enJsonPath, StringComparison.OrdinalIgnoreCase))
            {
                continue; // Skip the en.json file itself
            }

            var languageKeys = GetJsonKeys(language.FullName);
            var missingKeys = enKeys.Except(languageKeys).ToList();
            var extraKeys = languageKeys.Except(enKeys).ToList();

            Assert.False(missingKeys.Count != 0,
                $"Missing keys in {Path.GetFileName(language.FullName)}: {string.Join(", ", missingKeys)}");
            Assert.True(extraKeys.Count == 0,
                $"Extra keys in {Path.GetFileName(language.FullName)}: {string.Join(", ", extraKeys)}");
            Assert.True(enKeys.SetEquals(languageKeys), $"Key mismatch in {Path.GetFileName(language.FullName)}");
        }
    }

    private HashSet<string> GetJsonKeys(string filePath)
    {
        var jsonString = File.ReadAllText(filePath);
        var jsonDocument = JsonDocument.Parse(jsonString);
        var root = jsonDocument.RootElement;
    
        var keys = new HashSet<string>();
        foreach (var property in root.EnumerateObject())
        {
            keys.Add(property.Name);
        }
    
        return keys;
    }

    [Fact]
    public async Task ChangeLanguage()
    {
        LoadSettings();
        Assert.NotNull(Settings);

        var loadedEn = await TranslationManager.LoadLanguage("en");
        Assert.True(loadedEn);
        Assert.Equal("Image", TranslationManager.Translation.Image);

        var loadedDa = await TranslationManager.LoadLanguage("da");
        Assert.True(loadedDa);
        Assert.Equal("Billede", TranslationManager.Translation.Image);
    }

    [Fact]
    public void LanguageItem_ToString_ReturnsDisplayName()
    {
        var item = new LanguageItem("da", "Danish");
        Assert.Equal("Danish", item.ToString());
    }

    [Fact]
    public void ComboBox_WithDisplayMemberBinding_PopulatesSelectionBoxItemTemplate()
    {
        var cb = new global::Avalonia.Controls.ComboBox();
        var items = new List<LanguageItem>
        {
            new("da", "Danish"),
            new("en", "English")
        };
        cb.ItemsSource = items;
        cb.DisplayMemberBinding = new global::Avalonia.Data.Binding("DisplayName");
        cb.SelectedIndex = 0;

        Assert.NotNull(cb.SelectionBoxItemTemplate);
        Assert.Equal(items[0], cb.SelectionBoxItem);
    }

    [Theory]
    // Chinese variants
    [InlineData("zh-CN", "zh-CN")]
    [InlineData("zh-SG", "zh-CN")]
    [InlineData("zh-Hans", "zh-CN")]
    [InlineData("zh-TW", "zh-TW")]
    [InlineData("zh-HK", "zh-TW")]
    [InlineData("zh-MO", "zh-TW")]
    [InlineData("zh-Hant", "zh-TW")]
    [InlineData("zh-Hant-TW", "zh-TW")]
    // Serbian variants
    [InlineData("sr-Cyrl", "sr-Cyrl")]
    [InlineData("sr-Cyrl-RS", "sr-Cyrl")]
    [InlineData("sr-Latn", "sr-Latn")]
    [InlineData("sr-Latn-RS", "sr-Latn")]
    [InlineData("sr-Latn-BA", "sr-Latn")]
    // German regions
    [InlineData("de", "de")]
    [InlineData("de-DE", "de")]
    [InlineData("de-AT", "de")]
    [InlineData("de-CH", "de")]
    [InlineData("de-LI", "de")]
    // Portuguese
    [InlineData("pt-BR", "pt-br")]
    [InlineData("pt-PT", "pt-br")]
    [InlineData("pt", "pt-br")]
    // Supported standard cultures
    [InlineData("da-DK", "da")]
    [InlineData("fr-FR", "fr")]
    [InlineData("fr-CA", "fr")]
    [InlineData("es-ES", "es")]
    [InlineData("es-MX", "es")]
    [InlineData("it-IT", "it")]
    [InlineData("ja-JP", "ja")]
    [InlineData("ko-KR", "ko")]
    [InlineData("nl-NL", "nl")]
    [InlineData("pl-PL", "pl")]
    [InlineData("ro-RO", "ro")]
    [InlineData("ru-RU", "ru")]
    [InlineData("sv-SE", "sv")]
    [InlineData("tr-TR", "tr")]
    [InlineData("ca-ES", "ca")]
    [InlineData("he-IL", "he")]
    [InlineData("hu-HU", "hu")]
    [InlineData("sl-SI", "sl")]
    [InlineData("en-US", "en")]
    [InlineData("en-GB", "en")]
    // Unsupported/custom cultures fallback to English
    [InlineData("fi-FI", "en")]
    [InlineData("ar-SA", "en")]
    [InlineData("is-IS", "en")]
    [InlineData("sw-KE", "en")]
    [InlineData("xx-YY", "en")]
    public void DetermineCorrectLanguage_WithSpecificCultures_ReturnsExpectedAvailableLanguage(
        string cultureName,
        string expectedLanguageCode)
    {
        var availableLanguages = TranslationManager.GetLanguages()
            .Select(x => Path.GetFileNameWithoutExtension(x.Name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains(expectedLanguageCode, availableLanguages);

        var customCulture = new CultureInfo(cultureName);
        var originalCulture = CultureInfo.CurrentUICulture;

        try
        {
            // Simulate startup where thread UI culture reflects the user's culture
            CultureInfo.CurrentUICulture = customCulture;

            var chosenFromStartup = TranslationManager.DetermineCorrectLanguage();
            Assert.Equal(expectedLanguageCode, chosenFromStartup);
            Assert.Contains(chosenFromStartup, availableLanguages);

            // Also test explicit parameter passing
            var chosenFromParameter = TranslationManager.DetermineCorrectLanguage(customCulture);
            Assert.Equal(expectedLanguageCode, chosenFromParameter);
            Assert.Contains(chosenFromParameter, availableLanguages);
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Fact]
    public void EveryAvailableLanguageFile_CanBeDetermined_OrFallsBackToValidAvailableLanguage()
    {
        var availableLanguages = TranslationManager.GetLanguages()
            .Select(x => Path.GetFileNameWithoutExtension(x.Name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.NotEmpty(availableLanguages);

        foreach (var file in TranslationManager.GetLanguages())
        {
            var code = Path.GetFileNameWithoutExtension(file.Name);
            var culture = new CultureInfo(code);
            var chosen = TranslationManager.DetermineCorrectLanguage(culture);

            Assert.Contains(chosen, availableLanguages);
        }
    }

    [Theory]
    [InlineData("da-DK", "da")]
    [InlineData("de-DE", "de")]
    [InlineData("zh-TW", "zh-TW")]
    [InlineData("pt-BR", "pt-br")]
    [InlineData("sr-Latn-RS", "sr-Latn")]
    [InlineData("fi-FI", "en")]
    public async Task DetermineAndLoadLanguage_UponStartup_SetsUserLanguageAndLoadsTranslations(
        string cultureName,
        string expectedLanguageCode)
    {
        LoadSettings();
        Assert.NotNull(Settings);

        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo(cultureName);

            await TranslationManager.DetermineAndLoadLanguage();

            Assert.Equal(expectedLanguageCode, Settings.UIProperties.UserLanguage);
            Assert.NotNull(TranslationManager.Translation);
            Assert.False(string.IsNullOrWhiteSpace(TranslationManager.Translation.Image));
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Fact]
    public async Task LanguageUpdater_StartupWithNoSettings_SelectsCorrectLanguageAndUpdatesViewModel()
    {
        LoadSettings();
        Assert.NotNull(Settings);

        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("da-DK");

            var vm = new TranslationViewModel();
            await LanguageUpdater.UpdateLanguageAsync(vm, settingsExists: false);

            Assert.Equal("da", Settings.UIProperties.UserLanguage);
            Assert.Equal("Billede", vm.Image.Value);
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }
}
