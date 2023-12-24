using PicView.Core.Config;
using PicView.Core.Localization;

namespace PicView.Avalonia.Models;

internal static class LoadSettings
{
    internal static void StartLoading()
    {
        Task.Run(async () =>
        {
            await SettingsHelper.LoadSettingsAsync().ConfigureAwait(false);
            await TranslationHelper.LoadLanguage(SettingsHelper.Settings.UIProperties.UserLanguage).ConfigureAwait(false);
        });
    }
}