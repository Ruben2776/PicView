using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.UI;

public static class ToggleUIVisibility
{
    public static void ToggleBottomBar(MainWindowViewModel vm, MainWindow mainWindow)
    {
        if (vm.GlobalSettings.ShowBottomBar.Value)
        {
            vm.IsBottomToolbarShown.Value = false;
            vm.GlobalSettings.ShowBottomBar.Value = false;
            vm.WindowTabs.ActiveTab.CurrentValue.Hoverbar.IsHoverbarVisible.Value =
                vm.GlobalSettings.ShowHoverBar.Value;
        }
        else
        {
            vm.IsBottomToolbarShown.Value = true;
            vm.GlobalSettings.ShowBottomBar.Value = true;
            vm.BottombarHeight.Value = SizeDefaults.BottombarHeight;
            vm.WindowTabs.ActiveTab.CurrentValue.Hoverbar.IsHoverbarVisible.Value = false;

            Dispatcher.UIThread.Post(() => mainWindow.SharedBottomBar.ResponsiveNavigationBtnSize(mainWindow.Bounds.Width));
        }
    }

    public static void ToggleInterface(MainWindowViewModel vm, MainWindow mainWindow)
    {
        var tab = vm.WindowTabs.ActiveTab.CurrentValue;
        if (vm.GlobalSettings.ShowInterface.Value)
        {
            // Hide Interface

            vm.IsUIShown.Value = false;
            vm.GlobalSettings.ShowInterface.Value = false;
            vm.IsTopToolbarShown.Value = false;
            vm.IsBottomToolbarShown.Value = false;
            tab.Hoverbar.IsHoverbarVisible.Value = vm.GlobalSettings.ShowHoverBar.Value;
        }
        else
        {
            vm.IsUIShown.Value = true;
            vm.IsTopToolbarShown.Value = true;
            if (vm.GlobalSettings.ShowBottomBar.Value)
            {
                vm.IsBottomToolbarShown.Value = true;
                vm.BottombarHeight.Value = SizeDefaults.BottombarHeight;
                tab.Hoverbar.IsHoverbarVisible.Value = false;
            }
            else
            {
                tab.Hoverbar.IsHoverbarVisible.Value = vm.GlobalSettings.ShowHoverBar.Value;
            }

            vm.GlobalSettings.ShowInterface.Value = true;
            vm.TitlebarHeight.Value = SizeDefaults.MainTitlebarHeight;
        }

        vm.TopTitlebarViewModel.CloseDropDownMenu();
        // The window's fitter fits it to the bars it now shows, with the gallery they show or hide along with them
        Dispatcher.UIThread.Post(() =>
            mainWindow.SharedBottomBar.ResponsiveNavigationBtnSize(mainWindow.Bounds.Width), DispatcherPriority.Render);
    }

    public static void ToggleHoverBar(MainWindowViewModel vm)
    {
        var shouldShow = !vm.GlobalSettings.ShowHoverBar.Value;
        vm.GlobalSettings.ShowHoverBar.Value = shouldShow;
        if (shouldShow && !vm.IsBottomToolbarShown.CurrentValue || shouldShow && vm.IsFullscreen.Value)
        {
            ShowHoverBar(vm);
        }
        else
        {
            HideHoverBar(vm);
        }
    }
    
    private static void ShowHoverBar(MainWindowViewModel vm)
    {
        vm.WindowTabs.ActiveTab.CurrentValue.Hoverbar.IsHoverbarVisible.Value = true;
    }
    
    private static void HideHoverBar(MainWindowViewModel vm)
    {
        vm.WindowTabs.ActiveTab.CurrentValue.Hoverbar.IsHoverbarVisible.Value = false;
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