using PicView.Core.Localization;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.SettingsManagement;

public static class LanguageUpdater
{
    public static void UpdateLanguage(TranslationViewModel translationViewModel,
        GlobalSettingsViewModel globalSettings, bool settingsExists)
    {
        if (settingsExists)
        {
            TranslationManager.LoadLanguage(Settings.UIProperties.UserLanguage ?? "en");
        }
        else
        {
            TranslationManager.DetermineAndLoadLanguage();
        }
        
        translationViewModel.UpdateLanguage();

        globalSettings.Rephrase();

        translationViewModel.IsShowingFadingUIButtons.Value = Settings.UIProperties.ShowAltInterfaceButtons
            ? TranslationManager.Translation.DisableFadeInButtonsOnHover
            : TranslationManager.Translation.ShowFadeInButtonsOnHover;

        translationViewModel.IsUsingTouchpad.Value = Settings.Zoom.IsUsingTouchPad
            ? TranslationManager.Translation.UsingTouchpad
            : TranslationManager.Translation.UsingMouse;
    }

    public static async Task UpdateLanguageAsync(TranslationViewModel translationViewModel,
        GlobalSettingsViewModel globalSettings, bool settingsExists)
    {
        await Task.Run(() =>
        {
#pragma warning disable MA0042
            UpdateLanguage(translationViewModel, globalSettings, settingsExists);
#pragma warning restore MA0042
        }).ConfigureAwait(false);
    }
}
