using PicView.Avalonia.ViewModels;
using PicView.Core.Config;
using PicView.Core.Localization;

namespace PicView.Avalonia.SettingsManagement;

public static class LanguageUpdater
{
    public static void UpdateLanguage(MainViewModel vm)
    {
        vm.UpdateLanguage();

        vm.GetIsFlippedTranslation = vm.ScaleX == 1 ? vm.Flip : vm.UnFlip;
        
        vm.GetIsShowingUITranslation = !SettingsHelper.Settings.UIProperties.ShowInterface ? vm.ShowUI : vm.HideUI;
        
        vm.GetIsScrollingTranslation = SettingsHelper.Settings.Zoom.ScrollEnabled ?
            TranslationHelper.Translation.ScrollingEnabled : TranslationHelper.Translation.ScrollingDisabled;
        
        vm.GetIsShowingBottomGalleryTranslation = vm.IsGalleryShown ?
            TranslationHelper.Translation.HideBottomGallery :
            TranslationHelper.Translation.ShowBottomGallery;
        
        vm.GetIsLoopingTranslation = SettingsHelper.Settings.UIProperties.Looping
            ? TranslationHelper.Translation.LoopingEnabled
            : TranslationHelper.Translation.LoopingDisabled;
        
        vm.GetIsCtrlZoomTranslation = SettingsHelper.Settings.Zoom.CtrlZoom
            ? TranslationHelper.Translation.CtrlToZoom
            : TranslationHelper.Translation.ScrollToZoom;
        
        vm.GetIsShowingBottomToolbarTranslation = SettingsHelper.Settings.UIProperties.ShowBottomNavBar
            ? TranslationHelper.Translation.HideBottomToolbar
            : TranslationHelper.Translation.ShowBottomToolbar;
    }
}
