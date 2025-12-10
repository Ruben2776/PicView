using Avalonia.Threading;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Gallery;
using PicView.Core.Localization;
using PicView.Core.Sizing;

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
        if (Settings.UIProperties.ShowInterface)
        {
            vm.MainWindow.IsUIShown.Value = false;
            Settings.UIProperties.ShowInterface = false;
            vm.MainWindow.IsTopToolbarShown.Value = false;
            vm.MainWindow.IsBottomToolbarShown.Value = false;
            vm.Translation.IsShowingUI.Value = TranslationManager.Translation.ShowUI;
            vm.HoverbarViewModel.IsHoverbarVisible.Value = Settings.UIProperties.ShowHoverNavigationBar;
            if (!GalleryFunctions.IsFullGalleryOpen)
            {
                if (!Settings.Gallery.ShowBottomGalleryInHiddenUI)
                {
                    vm.Gallery.GalleryMode.Value = GalleryMode.Closed;
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (UIHelper.GetGalleryView.Bounds.Height > 0)
                        {
                            vm.Gallery.GalleryMode.Value = GalleryMode.BottomToClosed;
                        }
                    });
                    vm.Gallery.IsBottomGalleryShown.Value = false;
                }
                else
                {
                    vm.Gallery.IsBottomGalleryShown.Value = Settings.Gallery.ShowBottomGalleryInHiddenUI;
                }
            }
        }
        else
        {
            vm.MainWindow.IsUIShown.Value = true;
            vm.MainWindow.IsTopToolbarShown.Value = true;
            vm.Translation.IsShowingUI.Value = TranslationManager.Translation.HideUI;
            if (Settings.UIProperties.ShowBottomNavBar)
            {
                vm.MainWindow.IsBottomToolbarShown.Value = true;
                vm.MainWindow.BottombarHeight.Value = SizeDefaults.BottombarHeight;
                vm.HoverbarViewModel.IsHoverbarVisible.Value = false;
            }
            else
            {
                vm.HoverbarViewModel.IsHoverbarVisible.Value = Settings.UIProperties.ShowHoverNavigationBar;
            }
            Settings.UIProperties.ShowInterface = true;
            vm.MainWindow.TitlebarHeight.Value = SizeDefaults.MainTitlebarHeight;
            if (!GalleryFunctions.IsFullGalleryOpen)
            {
                if (Settings.Gallery.IsBottomGalleryShown)
                {
                    if (NavigationManager.CanNavigate(vm))
                    {
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            if (UIHelper.GetGalleryView.Bounds.Height <= 0)
                            {
                                vm.Gallery.GalleryMode.Value = GalleryMode.Closed;
                                GalleryFunctions.OpenBottomGallery(vm);
                            }
                        });
                        _ = GalleryLoad.LoadGallery(vm, vm.PicViewer.FileInfo.CurrentValue.DirectoryName);
                    }

                    vm.Gallery.IsBottomGalleryShown.Value = true;
                }
                else
                {
                    vm.Gallery.IsBottomGalleryShown.Value = false;
                }
            }
        }
        
        MenuManager.CloseMenus(vm);
        await WindowResizing.SetSizeAsync(vm);

        await SaveSettingsAsync();
    }
    
    /// <summary>
    /// Toggle between showing the bottom toolbar and hiding it
    /// </summary>
    /// <param name="vm">The view model. </param>
    public static async Task ToggleBottomToolbar(MainViewModel vm)
    {
        if (Settings.UIProperties.ShowBottomNavBar)
        {
            vm.MainWindow.IsBottomToolbarShown.Value = false;
            Settings.UIProperties.ShowBottomNavBar = false;
            vm.Translation.IsShowingBottomToolbar.Value = TranslationManager.Translation.ShowBottomToolbar;
            vm.HoverbarViewModel.IsHoverbarVisible.Value = Settings.UIProperties.ShowHoverNavigationBar;
        }
        else
        {
            vm.MainWindow.IsBottomToolbarShown.Value = true;
            Settings.UIProperties.ShowBottomNavBar = true;
            vm.MainWindow.BottombarHeight.Value = SizeDefaults.BottombarHeight;
            vm.Translation.IsShowingBottomToolbar.Value = TranslationManager.Translation.HideBottomToolbar;
            vm.HoverbarViewModel.IsHoverbarVisible.Value = false;
        }
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            WindowResizing.SetSize(vm);
        });
        
        await SaveSettingsAsync();
    }
    
    #endregion

    public static async Task ToggleBottomGalleryShownInHiddenUI(MainViewModel vm)
    {
        Settings.Gallery.ShowBottomGalleryInHiddenUI = !Settings.Gallery
            .ShowBottomGalleryInHiddenUI;
        if (vm.Gallery is not { } gallery)
        {
            return;
        }
        gallery.IsBottomGalleryShownInHiddenUI.Value = Settings.Gallery.ShowBottomGalleryInHiddenUI;

        if (!Settings.UIProperties.ShowInterface && !Settings.Gallery
                .ShowBottomGalleryInHiddenUI)
        {
            gallery.IsBottomGalleryShown.Value = false;
        }
        else
        {
            gallery.IsBottomGalleryShown.Value = Settings.Gallery.IsBottomGalleryShown;
        }
        
        await WindowResizing.SetSizeAsync(vm);
        if (gallery.IsBottomGalleryShown.Value)
        {
            gallery.GalleryMode.Value = GalleryMode.ClosedToBottom;
        }
        
        await SaveSettingsAsync();
    }

    public static async Task ToggleFadeInButtonsOnHover(MainViewModel vm)
    {
        Settings.UIProperties.ShowAltInterfaceButtons = !Settings
            .UIProperties.ShowAltInterfaceButtons;
        
        vm.Translation.IsShowingFadingUIButtons.Value = Settings.UIProperties.ShowAltInterfaceButtons
            ? TranslationManager.Translation.DisableFadeInButtonsOnHover
            : TranslationManager.Translation.ShowFadeInButtonsOnHover;
        
        await SaveSettingsAsync();
    }

    public static async Task ToggleHoverNavigationBar(MainViewModel vm)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (!UIHelper.GetMainView.MainGrid.Children.Contains(UIHelper.GetHoverBar))
            {
                UIHelper.AddHoverBar(vm);
            }
        });
        
        Settings.UIProperties.ShowHoverNavigationBar = !Settings
            .UIProperties.ShowHoverNavigationBar;

        vm.Translation.IsShowingHoverNavigationBar.Value = Settings.UIProperties.ShowHoverNavigationBar
            ? TranslationManager.Translation.HideHoverNavigationBar
            : TranslationManager.Translation.ShowHoverNavigationBar;

        vm.HoverbarViewModel.IsHoverbarVisible.Value = Settings.UIProperties.ShowHoverNavigationBar;

        await SaveSettingsAsync();
    }
}
