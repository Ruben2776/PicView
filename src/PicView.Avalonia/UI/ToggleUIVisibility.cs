using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Core.Localization;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.UI;

public static class ToggleUIVisibility
{
    public static void ToggleBottomBar(MainWindowViewModel vm, MainWindow mainWindow)
    {
        if (Settings.UIProperties.ShowBottomNavBar)
        {
            vm.IsBottomToolbarShown.Value = false;
            Settings.UIProperties.ShowBottomNavBar = false;
            vm.Translation.IsShowingBottomToolbar.Value = TranslationManager.Translation.ShowBottomToolbar;
            vm.WindowTabs.ActiveTab.CurrentValue.Hoverbar.IsHoverbarVisible.Value =
                Settings.UIProperties.ShowHoverNavigationBar;
        }
        else
        {
            vm.IsBottomToolbarShown.Value = true;
            Settings.UIProperties.ShowBottomNavBar = true;
            vm.BottombarHeight.Value = SizeDefaults.BottombarHeight;
            vm.Translation.IsShowingBottomToolbar.Value = TranslationManager.Translation.HideBottomToolbar;
            vm.WindowTabs.ActiveTab.CurrentValue.Hoverbar.IsHoverbarVisible.Value = false;

            Dispatcher.UIThread.Post(() => mainWindow.SharedBottomBar.ResponsiveNavigationBtnSize(mainWindow.Bounds.Width));
        }
    }

    public static void ToggleInterface(MainWindowViewModel vm, MainWindow mainWindow)
    {
        var tab = vm.WindowTabs.ActiveTab.CurrentValue;
        if (Settings.UIProperties.ShowInterface)
        {
            // Hide Interface

            vm.IsUIShown.Value = false;
            Settings.UIProperties.ShowInterface = false;
            vm.IsTopToolbarShown.Value = false;
            vm.IsBottomToolbarShown.Value = false;
            vm.Translation.IsShowingUI.Value = TranslationManager.Translation.ShowUI;
            tab.Hoverbar.IsHoverbarVisible.Value = Settings.UIProperties.ShowHoverNavigationBar;
        }
        else
        {
            vm.IsUIShown.Value = true;
            vm.IsTopToolbarShown.Value = true;
            vm.Translation.IsShowingUI.Value = TranslationManager.Translation.HideUI;
            if (Settings.UIProperties.ShowBottomNavBar)
            {
                vm.IsBottomToolbarShown.Value = true;
                vm.BottombarHeight.Value = SizeDefaults.BottombarHeight;
                tab.Hoverbar.IsHoverbarVisible.Value = false;
            }
            else
            {
                tab.Hoverbar.IsHoverbarVisible.Value = Settings.UIProperties.ShowHoverNavigationBar;
            }

            Settings.UIProperties.ShowInterface = true;
            vm.TitlebarHeight.Value = SizeDefaults.MainTitlebarHeight;
        }

        vm.TopTitlebarViewModel.CloseDropDownMenu();
        // The window's fitter fits it to the bars it now shows, with the gallery they show or hide along with them
        Dispatcher.UIThread.Post(() =>
            mainWindow.SharedBottomBar.ResponsiveNavigationBtnSize(mainWindow.Bounds.Width), DispatcherPriority.Render);
    }

    public static async ValueTask ToggleHoverBar(MainWindowViewModel vm)
    {
        var shouldShow = !Settings.UIProperties.ShowHoverNavigationBar;
        
        Settings.UIProperties.ShowHoverNavigationBar = shouldShow;
        if (shouldShow && !vm.IsBottomToolbarShown.CurrentValue || shouldShow && vm.IsFullscreen.Value)
        {
            ShowHoverBar(vm);
        }
        else
        {
            HideHoverBar(vm);
        }
        
        await SaveSettingsAsync().ConfigureAwait(false);
    }
    
    private static void ShowHoverBar(MainWindowViewModel vm)
    {
        vm.WindowTabs.ActiveTab.CurrentValue.Hoverbar.IsHoverbarVisible.Value = true;
        vm.Translation.IsShowingHoverNavigationBar.Value = TranslationManager.Translation.HideHoverNavigationBar;
    }
    
    private static void HideHoverBar(MainWindowViewModel vm)
    {
        vm.WindowTabs.ActiveTab.CurrentValue.Hoverbar.IsHoverbarVisible.Value = false;
        vm.Translation.IsShowingHoverNavigationBar.Value = TranslationManager.Translation.ShowHoverNavigationBar;
    }

    public static void RestoreInterface(MainWindowViewModel vm)
    {
        vm.IsUIShown.Value = Settings.UIProperties.ShowInterface;
        
        if (!Settings.UIProperties.ShowInterface)
        {
            return;
        }
        
        vm.IsTopToolbarShown.Value = true;
        vm.TitlebarHeight.Value = SizeDefaults.MainTitlebarHeight;
        
        if (!Settings.UIProperties.ShowBottomNavBar)
        {
            return;
        }
        
        vm.IsBottomToolbarShown.Value = true;
        vm.BottombarHeight.Value = SizeDefaults.BottombarHeight;
    }
    
    
    public static void FullscreenHideInterface(MainWindowViewModel vm)
    {
        vm.IsBottomToolbarShown.Value = false;
        vm.IsTopToolbarShown.Value = false;
        vm.IsUIShown.Value = false;

        vm.WindowTabs.ActiveTab.CurrentValue.Hoverbar.IsHoverbarVisible.Value = Settings.UIProperties.ShowHoverNavigationBar;
        vm.BottombarHeight.Value = 0;
    }
}