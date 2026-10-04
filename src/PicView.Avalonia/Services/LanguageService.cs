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
                    var displayName = new System.Globalization.CultureInfo(langCode).DisplayName;
                    return (LanguageCode: langCode, DisplayName: displayName);
                })
                .OrderBy(x => x.DisplayName, StringComparer.Ordinal)
                .Select(x => (x.LanguageCode, x.DisplayName));
    }
}
