using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Navigation.Services;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Gallery;
using PicView.Core.Localization;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.UI;

public static class ToggleUIVisibility
{
    public static async ValueTask ToggleBottomBar(MainWindowViewModel vm, MainWindow mainWindow)
    {
        if (Settings.UIProperties.ShowBottomNavBar)
        {
            vm.IsBottomToolbarShown.Value = false;
            Settings.UIProperties.ShowBottomNavBar = false;
            vm.Translation.IsShowingBottomToolbar.Value = TranslationManager.Translation.ShowBottomToolbar;
            vm.WindowTabs.ActiveTab.CurrentValue.Hoverbar.IsHoverbarVisible.Value =
                Settings.UIProperties.ShowHoverNavigationBar;

            WindowResizing.SetSize(mainWindow, WindowResizeReason.Layout);
        }
        else
        {
            vm.IsBottomToolbarShown.Value = true;
            Settings.UIProperties.ShowBottomNavBar = true;
            vm.BottombarHeight.Value = SizeDefaults.BottombarHeight;
            vm.Translation.IsShowingBottomToolbar.Value = TranslationManager.Translation.HideBottomToolbar;
            vm.WindowTabs.ActiveTab.CurrentValue.Hoverbar.IsHoverbarVisible.Value = false;

            WindowResizing.SetSize(mainWindow, WindowResizeReason.Layout);

            Dispatcher.UIThread.Post(() => mainWindow.SharedBottomBar.ResponsiveNavigationBtnSize(mainWindow.Bounds.Width));
        }

        await SaveSettingsAsync();
    }

    public static async ValueTask ToggleInterface(MainWindowViewModel vm, MainWindow mainWindow)
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
            if (!Settings.Gallery.ShowDockedGalleryInHiddenUI)
            {
                // Hide gallery if not enabled
                tab.Gallery.ActiveGalleryMode.Value = GalleryMode.Closed;
                tab.Gallery.IsGalleryDocked.Value = false;
            }
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
            if (Settings.Gallery.IsGalleryDocked)
            {
                if (tab.ImageIterator.Files.Count > 0)
                {
                    if (Application.Current.DataContext is not CoreViewModel core)
                    {
                        return;

                    }

                    if (tab.Gallery.LoadingState is GalleryLoadingState.NotLoaded)
                    {
                        _ = GalleryLoader.LoadGalleryAsync(
                                core.MainWindows.ActiveWindow.Value.WindowTabs.ActiveTab.Value,
                                core.MainWindows.ActiveWindow.Value.WindowTabs.ActiveTab.Value.ImageIterator.Files,
                                ServiceHelper.ThumbLoader,
                                core.SharedThumbnailCache,
                                core.MainWindows.ActiveWindow.Value.WindowTabs.ActiveTab.Value.GetTabCancellation()
                                    .Token)
                            .ConfigureAwait(false);
                    }
                }

                tab.Gallery.IsDockedGalleryVisible.Value = true;
            }
            else
            {
                tab.Gallery.IsGalleryDocked.Value = false;
            }
        }

        vm.TopTitlebarViewModel.CloseDropDownMenu();
        Dispatcher.UIThread.Post(() =>
        {
            WindowResizing.SetSize(mainWindow, WindowResizeReason.Layout);
            mainWindow.SharedBottomBar.ResponsiveNavigationBtnSize(mainWindow.Bounds.Width);
        }, DispatcherPriority.Render);

        await SaveSettingsAsync();
    }

    public static async ValueTask ToggleHoverBar(MainWindowViewModel vm)
    {
        var shouldShow = !Settings.UIProperties.ShowHoverNavigationBar;
        
        Settings.UIProperties.ShowHoverNavigationBar = shouldShow;
        if (shouldShow && !vm.IsBottomToolbarShown.CurrentValue || shouldShow && vm.IsFullscreen.CurrentValue)
        {
            vm.WindowTabs.ActiveTab.CurrentValue.Hoverbar.IsHoverbarVisible.Value = true;
            vm.Translation.IsShowingHoverNavigationBar.Value = TranslationManager.Translation.HideHoverNavigationBar;
        }
        else
        {
            vm.WindowTabs.ActiveTab.CurrentValue.Hoverbar.IsHoverbarVisible.Value = false;
            vm.Translation.IsShowingHoverNavigationBar.Value = TranslationManager.Translation.ShowHoverNavigationBar;
        }
        
        await SaveSettingsAsync();
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
    
    
    public static void HideInterface(MainWindowViewModel vm)
    {
        vm.IsBottomToolbarShown.Value = false;
        vm.IsTopToolbarShown.Value = false;
        vm.IsUIShown.Value = false;
    }
}