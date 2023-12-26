using System.Diagnostics;
using System.IO;
using System.Text.Json;
using PicView.Core.Config;

namespace PicView.Core.Localization;

/// <summary>
/// Helper class for managing language-related tasks.
/// </summary>
public static class TranslationHelper
{
    public static string GetTranslation(string key)
    {
        // ReSharper disable once InvertIf
        if (Language is null)
            return string.Empty;

        return Language.TryGetValue(key, out var translation) ? translation : key;
    }

    /// <summary>
    /// Dictionary to store language key-value pairs.
    /// </summary>
    internal static Dictionary<string, string>? Language;

    /// <summary>
    /// Determines the language based on the specified culture and loads the corresponding language file.
    /// </summary>
    /// <param name="isoLanguageCode">The culture code representing the desired language.</param>
    public static async Task LoadLanguage(string isoLanguageCode)
    {
        var jsonLanguageFile = DetermineLanguageFilePath(isoLanguageCode);

        try
        {
            if (File.Exists(jsonLanguageFile))
            {
                var text = await File.ReadAllTextAsync(jsonLanguageFile).ConfigureAwait(false);
                Language = JsonSerializer.Deserialize<Dictionary<string, string>>(text);
            }
            else
            {
                // TODO: Recreate English file?
            }
        }
        catch (Exception exception)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(LoadLanguage)} exception:\n{exception.Message}");
#endif
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