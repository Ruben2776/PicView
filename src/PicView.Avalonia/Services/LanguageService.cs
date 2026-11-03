using System.Globalization;
using Avalonia;
using Avalonia.Threading;
using PicView.Avalonia.SettingsManagement;
using PicView.Core.Localization;
using PicView.Core.ViewModels;
using ZLinq;

namespace PicView.Avalonia.Services;

public class LanguageService : ILanguageService
{
    public async ValueTask UpdateLanguageAsync(string languageCode)
    {
        Settings.UIProperties.UserLanguage = languageCode;
        var core = await Dispatcher.UIThread.InvokeAsync(() => Application.Current.DataContext as CoreViewModel);
        await LanguageUpdater.UpdateLanguageAsync(core.Translation, true).ConfigureAwait(false);
    }

    public IEnumerable<(string Code, string DisplayName)> GetAvailableLanguages()
    {
        return TranslationManager.GetLanguages().ToArray()
                .Select(filePath =>
                {
                    var langCode = Path.GetFileNameWithoutExtension(filePath.Name);
                    return (LanguageCode: langCode, DisplayName: NativeLanguageName(langCode));
                })
                .OrderBy(x => x.DisplayName, StringComparer.Ordinal)
                .Select(x => (x.LanguageCode, x.DisplayName));
    }

    /// <summary>
    /// Names each language in itself ("English", "Deutsch", "中文（中国）"), as language pickers usually do.
    /// <see cref="CultureInfo.DisplayName"/> follows the operating system's UI language instead,
    /// so on a Chinese Windows every name stayed Chinese after switching PicView to English.
    /// </summary>
    internal static string NativeLanguageName(string languageCode)
    {
        var culture = new CultureInfo(languageCode);
        var name = culture.NativeName;
        // Some languages write their own name in lower case ("español", "français").
        return name.Length == 0 ? languageCode : culture.TextInfo.ToUpper(name[0]) + name[1..];
    }
}
