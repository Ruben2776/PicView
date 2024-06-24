using PicView.Core.Config;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PicView.Core.Localization;

[JsonSourceGenerationOptions(AllowTrailingCommas = true)]
[JsonSerializable(typeof(LanguageModel))]
internal partial class LanguageSourceGenerationContext : JsonSerializerContext;

/// <summary>
/// Helper class for managing language-related tasks.
/// </summary>
public static class TranslationHelper
{
    public static LanguageModel? Translation
    {
        get;
        private set;
    }

    public static string GetTranslation(string key)
    {
        if (Translation == null)
        {
            return key;
        }

        var propertyInfo = typeof(LanguageModel).GetProperty(key);
        return propertyInfo?.GetValue(Translation) as string ?? key;
    }

    /// <summary>
    /// Determines the language based on the specified culture and loads the corresponding language file.
    /// </summary>
    /// <param name="isoLanguageCode">The culture code representing the desired language.</param>
    public static async Task<bool> LoadLanguage(string isoLanguageCode)
    {
        var jsonLanguageFile = DetermineLanguageFilePath(isoLanguageCode);

        try
        {
            await LoadLanguageFromFileAsync(jsonLanguageFile).ConfigureAwait(false);
            return true;
        }
        catch (FileNotFoundException fnfEx)
        {
#if DEBUG
            Trace.WriteLine($"Language file not found: {fnfEx.Message}");
#endif
            return false;
        }
        catch (Exception ex)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(LoadLanguage)} exception:\n{ex.Message}");
#endif
            return false;
        }
    }

    public static IEnumerable<string> GetLanguages()
    {
        var languagesDirectory = GetLanguagesDirectory();
        return Directory.EnumerateFiles(languagesDirectory, "*.json", SearchOption.TopDirectoryOnly);
    }

    public static async Task ChangeLanguage(int language)
    {
        var choice = (Languages)language;
        var languageCode = choice.ToString().Replace('_', '-');
        SettingsHelper.Settings.UIProperties.UserLanguage = languageCode;
        await LoadLanguage(languageCode).ConfigureAwait(false);
        await SettingsHelper.SaveSettingsAsync().ConfigureAwait(false);
    }

    private static string DetermineLanguageFilePath(string isoLanguageCode)
    {
        var languagesDirectory = GetLanguagesDirectory();
        var matchingFiles = Directory.GetFiles(languagesDirectory, "*.json")
            .Where(file => Path.GetFileNameWithoutExtension(file)?.Equals(isoLanguageCode, StringComparison.OrdinalIgnoreCase) == true)
            .ToList();

        return matchingFiles.FirstOrDefault() ?? Path.Combine(languagesDirectory, "en.json");
    }

    private static async Task LoadLanguageFromFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Language file not found: {filePath}");
        }

        var jsonString = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
        var language = JsonSerializer.Deserialize(jsonString, typeof(LanguageModel), LanguageSourceGenerationContext.Default) as LanguageModel;
        Translation = language;
    }

    private static string GetLanguagesDirectory()
    {
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/Languages/");
    }
}
