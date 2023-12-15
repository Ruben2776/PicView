namespace PicView.Avalonia.Models;

internal static class LoadSettings
{
    internal static void StartLoading()
    {
        Task.Run(async () =>
        {
            await Core.Localization.TranslationHelper.DetermineLanguage("da").ConfigureAwait(false);
            await Core.Config.SettingsHelper.LoadSettingsAsync().ConfigureAwait(false);
        });
    }
}