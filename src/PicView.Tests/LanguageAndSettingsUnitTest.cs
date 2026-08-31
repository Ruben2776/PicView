using System.Text.Json;
using PicView.Core.Localization;
using ZLinq;

namespace PicView.Tests;

[Collection("Sequential")]
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
}