using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.Views.UC;
using PicView.Core.ImageDecoding;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Input;

/// <summary>
/// Works out a window's <see cref="InputMode"/> from what is open in it and in its active tab.
/// Nothing is stored, so the mode cannot go stale. Call on the UI thread.
/// </summary>
public static class InputModes
{
    public static InputModeState StateOf(MainWindow window)
    {
        if (window.DataContext is not MainWindowViewModel vm)
        {
            return new InputModeState(IsDialogOpen: IsPopUpOpen(window));
        }

        var activeTab = vm.WindowTabs.ActiveTab.CurrentValue;
        return new InputModeState(
            IsDialogOpen: IsPopUpOpen(window) || IsMenuOpen(vm),
            IsRenaming: vm.IsEditableTitlebarOpen.CurrentValue,
            IsCropping: activeTab?.CropService?.IsCropping ?? false,
            IsMotionPhotoActive: activeTab?.CurrentView.CurrentValue is ImageViewer { IsMotionPhotoActive: true },
            IsSlideshowRunning: Slideshow.IsRunning,
            IsAnimatedImageActive: activeTab?.Model?.ImageType is
                ImageType.AnimatedGif or ImageType.AnimatedWebp or ImageType.AnimatedAvif);
    }

    /// <summary>
    /// A pop-up stays in the main panel while it animates closed, and some are hidden instead of removed.
    /// </summary>
    private static bool IsPopUpOpen(MainWindow window) =>
        ContextualActionHandler.TopPopUp(window.UIHelper.GetMainView?.MainPanel) is not null;

    private static bool IsMenuOpen(MainWindowViewModel vm) =>
        (vm.TopTitlebarViewModel.DropDownMenu?.IsDropDownMenuVisible.CurrentValue ?? false) ||
        (vm.IsUIShown.CurrentValue && vm.TopTitlebarViewModel.IsMainMenuVisible.CurrentValue);
}
