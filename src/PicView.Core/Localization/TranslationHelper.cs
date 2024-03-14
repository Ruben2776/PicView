using PicView.Core.Config;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PicView.Core.Localization;

[JsonSourceGenerationOptions(AllowTrailingCommas = true)]
[JsonSerializable(typeof(LanguageModel))]
internal partial class LanguageSourceGenerationContext : JsonSerializerContext
{
}

/// <summary>
/// Helper class for managing language-related tasks.
/// </summary>
public static class TranslationHelper
{
    public static string GetTranslation(string key)
    {
        if (Language == null)
        {
            return string.Empty;
        }

        var propertyInfo = typeof(LanguageModel).GetProperty(key);
        if (propertyInfo == null)
        {
            return string.Empty;
        }

        return propertyInfo.GetValue(Language) as string ?? string.Empty;
    }

    /// <summary>
    /// Dictionary to store language key-value pairs.
    /// </summary>
    internal static LanguageModel? Language;

    /// <summary>
    /// Determines the language based on the specified culture and loads the corresponding language file.
    /// </summary>
    /// <param name="isoLanguageCode">The culture code representing the desired language.</param>
    public static async Task<bool> LoadLanguage(string isoLanguageCode)
    {
        var jsonLanguageFile = DetermineLanguageFilePath(isoLanguageCode);

        try
        {
            if (File.Exists(jsonLanguageFile))
            {
                await Deserialize(jsonLanguageFile).ConfigureAwait(false);
            }
            else
            {
                var languagesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/Languages/");

                var file = Directory.GetFiles(languagesDirectory, "*.json").FirstOrDefault();
                if (file != null)
                {
                    await Deserialize(file).ConfigureAwait(false);
                }
                else
                {
                    throw new FileNotFoundException();
                }
            }
        }
        catch (Exception exception)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(LoadLanguage)} exception:\n{exception.Message}");
#endif
            return false;
        }

        return true;

        async Task Deserialize(string file)
        {
            var jsonString = await File.ReadAllTextAsync(file).ConfigureAwait(false);
            var language = JsonSerializer.Deserialize(
                    jsonString, typeof(LanguageModel), LanguageSourceGenerationContext.Default)
                as LanguageModel;
            Language = language;
        }
    }

    private static string DetermineLanguageFilePath(string isoLanguageCode)
    {
        var languagesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/Languages/");

        var matchingFiles = Directory.GetFiles(languagesDirectory, "*.json")
            .Where(file => Path.GetFileNameWithoutExtension(file)?.Equals(isoLanguageCode, StringComparison.OrdinalIgnoreCase) == true)
            .ToList();

        return matchingFiles.Count > 0 ? matchingFiles.First() :
            // If no exact match is found, default to English
            Path.Combine(languagesDirectory, "en.json");
    }

    public static async Task ChangeLanguage(int language)
    {
        var choice = (Languages)language;
        SettingsHelper.Settings.UIProperties.UserLanguage = choice.ToString().Replace('_', '-');
        await LoadLanguage(SettingsHelper.Settings.UIProperties.UserLanguage).ConfigureAwait(false);

        await SettingsHelper.SaveSettingsAsync().ConfigureAwait(false);
    }
}