using Avalonia.Controls;
using Avalonia.Threading;
using PicView.Avalonia.Animations;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.ViewModels;
using PicView.Core.Calculations;
using PicView.Core.Config;
using PicView.Core.Gallery;

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
            if (!GalleryFunctions.IsFullGalleryOpen)
            {
                if (!SettingsHelper.Settings.Gallery.ShowBottomGalleryInHiddenUI)
                {
                    vm.GalleryMode = GalleryMode.Closed;
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (UIHelper.GetGalleryView.Bounds.Height > 0)
                        {
                            vm.GalleryMode = GalleryMode.BottomToClosed;
                        }
                    });
                    vm.IsGalleryShown = false;
                }
                else
                {
                    vm.IsGalleryShown = SettingsHelper.Settings.Gallery.ShowBottomGalleryInHiddenUI;
                }
            }
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
            if (!GalleryFunctions.IsFullGalleryOpen)
            {
                if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
                {
                    if (NavigationHelper.CanNavigate(vm))
                    {
                        Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            if (UIHelper.GetGalleryView.Bounds.Height <= 0)
                            {
                                vm.GalleryMode = GalleryMode.Closed;
                                GalleryFunctions.OpenBottomGallery(vm);
                            }
                        });
                        _ = GalleryLoad.LoadGallery(vm, vm.FileInfo.DirectoryName);
                    }

                    vm.IsGalleryShown = true;
                }
                else
                {
                    vm.IsGalleryShown = false;
                }
            }
        }
        
        WindowHelper.SetSize(vm);
        await FunctionsHelper.CloseMenus();
        await SettingsHelper.SaveSettingsAsync();
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
            vm.IsBottomToolbarShownSetting = false;
        }
        else
        {
            vm.IsBottomToolbarShown = true;
            SettingsHelper.Settings.UIProperties.ShowBottomNavBar = true;
            vm.IsBottomToolbarShownSetting = true;
            vm.BottombarHeight = SizeDefaults.BottombarHeight;
        }
        WindowHelper.SetSize(vm);
        await SettingsHelper.SaveSettingsAsync();
    }
    
    #endregion

    #region ClickArrows
    
    public static void AddHoverButtonEvents(Control parent, Control childControl, MainViewModel vm)
    {
        childControl.PointerEntered += delegate
        {
            if (vm.ImageIterator is null)
            {
                parent.Opacity = 0;
                childControl.Opacity = 0;
                return;
            }

            if (vm.ImageIterator.Pics?.Count <= 1)
            {
                parent.Opacity = 0;
                childControl.Opacity = 0;
                return;
            }

            if (childControl.IsPointerOver)
            {
                parent.Opacity = 1;
                childControl.Opacity = 1;
            }
        };
        parent.PointerEntered += async delegate
        {
            await DoClickArrowAnimation(isShown:true, parent, childControl, vm);
        };
        parent.PointerExited += async delegate
        {
            await DoClickArrowAnimation(isShown: false, parent, childControl, vm);
        };
        UIHelper.GetMainView.PointerExited += async delegate
        {
            await DoClickArrowAnimation(isShown: false, parent, childControl, vm);
        };
    }
    
    private static bool _isClickArrowAnimationRunning;
    private static async Task DoClickArrowAnimation(bool isShown, Control parent, Control childControl, MainViewModel vm)
    {
        if (_isClickArrowAnimationRunning)
        {
            return;
        }

        if (vm.ImageIterator is null)
        {
            parent.Opacity = 0;
            childControl.Opacity = 0;
            return;
        }

        if (vm.ImageIterator.Pics?.Count <= 1)
        {
            parent.Opacity = 0;
            childControl.Opacity = 0;
            return;
        }
        _isClickArrowAnimationRunning = true;
        var from = isShown ? 0 : 1;
        var to = isShown ? 1 : 0;
        var speed = isShown ? 0.3 : 0.45;
        var anim = AnimationsHelper.OpacityAnimation(from, to, speed);
        await anim.RunAsync(childControl);
        _isClickArrowAnimationRunning = false;
    }
    
    #endregion

    public static async Task ToggleBottomGalleryShownInHiddenUI(MainViewModel vm)
    {
        SettingsHelper.Settings.Gallery.ShowBottomGalleryInHiddenUI = !SettingsHelper.Settings.Gallery
            .ShowBottomGalleryInHiddenUI;
        vm.IsBottomGalleryShownInHiddenUI = SettingsHelper.Settings.Gallery.ShowBottomGalleryInHiddenUI;

        if (!GalleryFunctions.IsFullGalleryOpen)
        {
            if (!SettingsHelper.Settings.UIProperties.ShowInterface && !SettingsHelper.Settings.Gallery
                    .ShowBottomGalleryInHiddenUI)
            {
                vm.IsGalleryShown = false;
            }
            else
            {
                vm.IsGalleryShown = SettingsHelper.Settings.Gallery.IsBottomGalleryShown;
            }
        }
        
        await SettingsHelper.SaveSettingsAsync();
    }
}
