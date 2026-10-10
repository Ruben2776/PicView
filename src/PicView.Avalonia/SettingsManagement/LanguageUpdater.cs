using PicView.Core.Localization;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.SettingsManagement;

public static class LanguageUpdater
{
    public static void UpdateLanguage(TranslationViewModel translationViewModel, bool settingsExists)
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
        translationViewModel.SubscribeToDynamicTranslationUpdates();

        translationViewModel.IsFlipped.Value = TranslationManager.Translation.Flip;
        translationViewModel.IsShowingUI.Value = !Settings.UIProperties.ShowInterface ? translationViewModel.ShowUI.CurrentValue : translationViewModel.HideUI.CurrentValue;
    
        translationViewModel.IsScrolling.Value = Settings.Zoom.ScrollEnabled ?
            TranslationManager.Translation.ScrollingEnabled : TranslationManager.Translation.ScrollingDisabled;
    
        translationViewModel.IsShowingDockedGallery.Value = Settings.Gallery.IsGalleryDocked ?
            TranslationManager.Translation.HideDockedGallery :
            TranslationManager.Translation.ShowDockedGallery;
    
        translationViewModel.IsLooping.Value = Settings.UIProperties.Looping
            ? TranslationManager.Translation.LoopingEnabled
            : TranslationManager.Translation.LoopingDisabled;
    
        translationViewModel.IsShowingBottomToolbar.Value = Settings.UIProperties.ShowBottomNavBar
            ? TranslationManager.Translation.HideBottomToolbar
            : TranslationManager.Translation.ShowBottomToolbar;
    
        translationViewModel.IsShowingFadingUIButtons.Value = Settings.UIProperties.ShowAltInterfaceButtons
            ? TranslationManager.Translation.DisableFadeInButtonsOnHover
            : TranslationManager.Translation.ShowFadeInButtonsOnHover;

        translationViewModel.IsShowingHoverNavigationBar.Value = Settings.UIProperties.ShowHoverNavigationBar
            ? TranslationManager.Translation.HideHoverNavigationBar
            : TranslationManager.Translation.ShowHoverNavigationBar;
    
        translationViewModel.IsUsingTouchpad.Value = Settings.Zoom.IsUsingTouchPad
            ? TranslationManager.Translation.UsingTouchpad
            : TranslationManager.Translation.UsingMouse;
    
        translationViewModel.ToggleFileHistory.Value = Settings.Navigation.IsFileHistoryEnabled
            ? TranslationManager.Translation.FileHistoryEnabled
            : TranslationManager.Translation.FileHistoryDisabled;
    }

    public static async Task UpdateLanguageAsync(TranslationViewModel translationViewModel, bool settingsExists)
    {
        await Task.Run(() =>
        {
#pragma warning disable MA0042
            UpdateLanguage(translationViewModel, settingsExists);
#pragma warning restore MA0042
        }).ConfigureAwait(false);
    }
}
