using Avalonia.Controls;
using Avalonia.Threading;
using PicView.Avalonia.Animations;
using PicView.Avalonia.ViewModels;
using PicView.Core.Calculations;
using PicView.Core.Config;

namespace PicView.Avalonia.UI;

public static class HideInterfaceLogic
{
    #region Toggle UI
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
                vm.BottombarHeight = SizeDefaults.BottombarHeight;
            }
            SettingsHelper.Settings.UIProperties.ShowInterface = true;
            vm.TitlebarHeight = SizeDefaults.TitlebarHeight;
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
    
    #endregion

    #region ClickArrows
    
    public static void AddClickArrowEvents(Control parent, Control polyButton, MainViewModel vm)
    {
        polyButton.PointerEntered += delegate
        {
            if (vm.ImageIterator is null)
            {
                parent.Opacity = 0;
                polyButton.Opacity = 0;
                return;
            }

            if (vm.ImageIterator.Pics?.Count <= 1)
            {
                parent.Opacity = 0;
                polyButton.Opacity = 0;
                return;
            }
            parent.Opacity = 1;
            polyButton.Opacity = 1;
        };
        parent.PointerEntered += async delegate
        {
            await DoClickArrowAnimation(isShown:true, parent, polyButton, vm);
        };
        parent.PointerExited += async delegate
        {
            await DoClickArrowAnimation(isShown: false, parent, polyButton, vm);
        };
        UIHelper.GetMainView.PointerExited += async delegate
        {
            await DoClickArrowAnimation(isShown: false, parent, polyButton, vm);
        };
    }
    
    private static bool _isClickArrowAnimationRunning;
    private static async Task DoClickArrowAnimation(bool isShown, Control parent, Control polyButton, MainViewModel vm)
    {
        if (_isClickArrowAnimationRunning)
        {
            return;
        }

        if (vm.ImageIterator is null)
        {
            parent.Opacity = 0;
            polyButton.Opacity = 0;
            return;
        }

        if (vm.ImageIterator.Pics?.Count <= 1)
        {
            parent.Opacity = 0;
            polyButton.Opacity = 0;
            return;
        }
        _isClickArrowAnimationRunning = true;
        var from = isShown ? 0 : 1;
        var to = isShown ? 1 : 0;
        var speed = isShown ? 0.3 : 0.45;
        var anim = AnimationsHelper.OpacityAnimation(from, to, speed);
        await anim.RunAsync(polyButton);
        _isClickArrowAnimationRunning = false;
    }
    
    #endregion
}
