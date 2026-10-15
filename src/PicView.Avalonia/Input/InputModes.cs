using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.Views.UC;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Input;

/// <summary>
/// Works out a window's <see cref="InputMode"/> from what is open in it and in its active tab.
/// Nothing is stored, so the mode cannot go stale. Call on the UI thread.
/// </summary>
public static class InputModes
{
    public static InputMode Of(MainWindow window) => StateOf(window).Resolve();

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
            IsSlideshowRunning: Slideshow.IsRunning);
    }

    /// <summary>
    /// A pop-up stays in the main panel while it animates closed, and some are hidden instead of removed.
    /// </summary>
    private static bool IsPopUpOpen(MainWindow window) =>
        window.UIHelper.GetMainView?.MainPanel.Children.OfType<AnimatedPopUp>().Any(popUp => popUp.IsVisible) ?? false;

    private static bool IsMenuOpen(MainWindowViewModel vm) =>
        (vm.TopTitlebarViewModel.DropDownMenu?.IsDropDownMenuVisible.CurrentValue ?? false) ||
        (vm.IsUIShown.CurrentValue && vm.TopTitlebarViewModel.IsMainMenuVisible.CurrentValue);
}
