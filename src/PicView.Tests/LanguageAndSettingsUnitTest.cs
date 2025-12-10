using System.Text.Json;
using PicView.Core.Localization;
using PicView.Tests.LanguageTests;
using ZLinq;

namespace PicView.Tests;

public class LanguageAndSettingsUnitTest
{
    [Fact]
    public async Task CheckIfSettingsWorks()
    {
        LoadSettings();
        Assert.NotNull(Settings);
        var testSave = await SaveSettingsAsync();
        Assert.True(testSave);
    }

    [Fact]
    public async Task CheckLanguages()
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
    
        await CheckDanishLanguage();
        await CheckDutchLanguage();
        await CheckEnglishLanguage();
        await CheckGermanLanguage();
        await CheckFrenchLanguage();
        await CheckItalianLanguage();
        await CheckKoreanLanguage();
        await CheckPolishLanguage();
        await CheckBrazilianPortugueseLanguage();
        await CheckRomanianLanguage();
        await CheckRussianLanguage();
        await CheckSpanishLanguage();
        await CheckSwedishLanguage();
        await CheckTurkishLanguage();
        await CheckChineseSimplifiedLanguage();
        await CheckChineseTraditionalLanguage();
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
    public void ChangeLanguage()
    {
        LoadSettings();
        Assert.NotNull(Settings);
        
        // TODO: rewrite

        // var exists = await TranslationManager.LoadLanguage("en");
        // Assert.True(exists);
        // Assert.Equal("Image", TranslationManager.Translation.Image);
        // const Languages da = Languages.da;
        // await TranslationManager.ChangeLanguage((int)da);
        // Assert.Equal("Billede", TranslationManager.Translation.Image);
    }
    
    [Fact]
    public async Task CheckDanishLanguage()
    {
        await DanishUnitTest.CheckDanishLanguage();
    }
    
    [Fact]
    public async Task CheckDutchLanguage()
    {
        await DutchUnitTest.CheckDutchLanguage();
    }
    
    [Fact]
    public async Task CheckEnglishLanguage()
    {
        await EnglishUnitTest.CheckEnglishLanguage();
    }
    
    [Fact]
    public async Task CheckGermanLanguage()
    {
        await GermanUnitTest.CheckGermanLanguage();
    }
    
    [Fact]
    public async Task CheckFrenchLanguage()
    {
        await FrenchUnitTest.CheckFrenchLanguage();
    }

    [Fact]
    public async Task CheckItalianLanguage()
    {
        await ItalianUnitTest.CheckItalianLanguage();
    }

    [Fact]
    public async Task CheckKoreanLanguage()
    {
        await KoreanUnitTest.CheckKoreanLanguage();
    }

    [Fact]
    public async Task CheckPolishLanguage()
    {
        await PolishUnitTest.CheckPolishLanguage();
    }

    [Fact]
    public async Task CheckBrazilianPortugueseLanguage()
    {
        await BrazilianPortugueseUnitTest.CheckBrazilianPortugueseLanguage();
    }

    [Fact]
    public async Task CheckRomanianLanguage()
    {
        await RomanianUnitTest.CheckRomanianLanguage();
    }

    [Fact]
    public async Task CheckRussianLanguage()
    {
        await RussianUnitTest.CheckRussianLanguage();
    }
    
    [Fact]
    public async Task CheckSpanishLanguage()
    {
        await SpanishUnitTest.CheckSpanishLanguage();
    }

    [Fact]
    public async Task CheckSwedishLanguage()
    {
        await SwedishUnitTest.CheckSwedishLanguage();
    }

    [Fact]
    public async Task CheckTurkishLanguage()
    {
        await TurkishUnitTest.CheckTurkishLanguage();
    }

    [Fact]
    public async Task CheckChineseSimplifiedLanguage()
    {
        await ChineseSimplifiedUnitTest.CheckChineseSimplifiedLanguage();
    }

    [Fact]
    public async Task CheckChineseTraditionalLanguage()
    {
        await ChineseTraditionalUnitTest.CheckChineseTraditionalLanguage();
    }
}