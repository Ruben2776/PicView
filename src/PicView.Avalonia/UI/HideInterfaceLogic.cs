using PicView.Avalonia.ViewModels;
using PicView.Core.Config;

namespace PicView.Avalonia.UI;

public static class HideInterfaceLogic
{
    /// <summary>
    /// Toggle between showing the full interface and hiding it
    /// </summary>
    /// <param name="vm">The view model. </param>
    public static async Task ToggleUI(MainViewModel vm)
    {
        if (SettingsHelper.Settings.UIProperties.ShowInterface)
        {
            vm.IsInterfaceShown = false;
            SettingsHelper.Settings.UIProperties.ShowInterface = false;
            vm.IsTopToolbarShown = false;
            vm.IsBottomToolbarShown = false;
        }
        else
        {
            vm.IsInterfaceShown = true;
            vm.IsTopToolbarShown = true;
            if (SettingsHelper.Settings.UIProperties.ShowBottomNavBar)
            {
                vm.IsBottomToolbarShown = true;
            }
            SettingsHelper.Settings.UIProperties.ShowInterface = true;
        }
        
        WindowHelper.SetSize(vm);
        await FunctionsHelper.CloseMenus();
        await SettingsHelper.SaveSettingsAsync().ConfigureAwait(false);
    }
    
    /// <summary>
    /// Toggle between showing the bottom toolbar and hiding it
    /// </summary>
    /// <param name="vm">The view model. </param>
    public static async Task ToggleBottomToolbar(MainViewModel vm)
    {
        if (SettingsHelper.Settings.UIProperties.ShowBottomNavBar)
        {
            vm.IsBottomToolbarShown = false;
            SettingsHelper.Settings.UIProperties.ShowBottomNavBar = false;
        }
        else
        {
            vm.IsBottomToolbarShown = true;
            SettingsHelper.Settings.UIProperties.ShowBottomNavBar = true;
        }
        WindowHelper.SetSize(vm);
        await SettingsHelper.SaveSettingsAsync().ConfigureAwait(false);
    }
}
