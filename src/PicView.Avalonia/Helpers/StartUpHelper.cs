using PicView.Core.Config;
using PicView.Core.Localization;

namespace PicView.Avalonia.Helpers;

public static class StartUpHelper
{
    public static void InitializeSettings()
    {
        Task.Run(async () =>
        {
            await SettingsHelper.LoadSettingsAsync();
            await TranslationHelper.LoadLanguage(SettingsHelper.Settings.UIProperties.UserLanguage);
        });
    }
}