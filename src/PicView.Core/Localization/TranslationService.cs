using System.Diagnostics;
using System.Text.Json;

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

        return Language.TryGetValue(key, out var translation) ? translation :
            // Return the key itself if the translation is not found (or handle as appropriate)
            key;
    }

    /// <summary>
    /// Dictionary to store language key-value pairs.
    /// </summary>
    internal static Dictionary<string, string>? Language;

    /// <summary>
    /// Determines the language based on the specified culture and loads the corresponding language file.
    /// </summary>
    /// <param name="culture">The culture code representing the desired language.</param>
    public static async Task DetermineLanguage(string culture)
    {
        string jsonLanguageFile;

        switch (culture)
        {
            case "da":
            case "da-DK":
                jsonLanguageFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/Languages/da.json");
                break;

            case "de":
            case "de-DE":
            case "de-CH":
            case "de-AT":
            case "de-LU":
            case "de-LI":
                jsonLanguageFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/Languages/de.json");
                break;

            case "es":
            case "es-ES":
            case "es-GT":
            case "es-CR":
            case "es-MX":
            case "es-PA":
            case "es-DO":
            case "es-VE":
            case "es-CO":
            case "es-PE":
            case "es-AR":
            case "es-CL":
            case "es-EC":
            case "es-UY":
            case "es-PY":
            case "es-BO":
            case "es-HN":
            case "es-NI":
            case "es-PR":
                jsonLanguageFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/Languages/es.json");
                break;

            case "ko":
            case "ko-KR":
                jsonLanguageFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/Languages/ko.json");
                break;

            case "zh":
            case "zh-CN":
                jsonLanguageFile =
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/Languages/zh-CN.json");
                break;

            case "zh-TW":
                jsonLanguageFile =
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/Languages/zh-TW.json");
                break;

            case "pl":
            case "pl-PL":
                jsonLanguageFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/Languages/pl.json");
                break;

            case "fr":
            case "fr-FR":
                jsonLanguageFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/Languages/fr.json");
                break;

            case "it":
            case "it-IT":
            case "it-CH":
                jsonLanguageFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/Languages/it.json");
                break;

            case "ru":
            case "ru-RU":
                jsonLanguageFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/Languages/ru.json");
                break;

            case "ro":
            case "ro-RO":
                jsonLanguageFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/Languages/ro.json");
                break;

            default:
                jsonLanguageFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/Languages/en.json");
                break;
        }

        await LoadLanguage(jsonLanguageFile).ConfigureAwait(false);
    }

    public static async Task LoadLanguage(string jsonLanguageFile)
    {
        try
        {
            if (File.Exists(jsonLanguageFile))
            {
                await Read(jsonLanguageFile).ConfigureAwait(false);
            }
            else
            {
                // TODO: Recreate English file?
            }
        }
        catch (Exception exception)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(Read)} exception:\n{exception.Message}");
#endif
        }
    }

    private static async Task Read(string path)
    {
        try
        {
            var text = await File.ReadAllTextAsync(path).ConfigureAwait(false);
            Language = JsonSerializer.Deserialize<Dictionary<string, string>>(text);
        }
        catch (Exception exception)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(Read)} exception:\n{exception.Message}");
#endif
        }
    }
}