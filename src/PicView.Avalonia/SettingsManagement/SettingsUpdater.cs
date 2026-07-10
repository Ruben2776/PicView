using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using PicView.Avalonia.ColorManagement;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Functions;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.UI;
using PicView.Avalonia.Views.UC;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.ColorHandling;
using PicView.Core.DebugTools;
using PicView.Core.Localization;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.SettingsManagement;
public static class SettingsUpdater
{
    public static void InitializeSettings(MainWindowViewModel vm, bool settingsExists)
    {
        Task.Run(() =>
        {
            _ = LanguageUpdater.UpdateLanguageAsync(vm.Translation, settingsExists);
            vm.TitlebarHeight.Value = Settings.WindowProperties.Fullscreen
                                           || !Settings.UIProperties.ShowInterface
                ? 0
                : SizeDefaults.MainTitlebarHeight;
            vm.BottombarHeight.Value = Settings.WindowProperties.Fullscreen
                                                  || !Settings.UIProperties.ShowInterface
                ? 0
                : SizeDefaults.BottombarHeight;
            vm.IsSideBySide.Value = Settings.ImageScaling.ShowImageSideBySide;
            vm.IsUIShown.Value  = Settings.UIProperties.ShowInterface;
            vm.IsTopToolbarShown.Value  = Settings.UIProperties.ShowInterface;
            vm.IsBottomToolbarShown.Value   = Settings.UIProperties.ShowBottomNavBar &&
                                        Settings.UIProperties.ShowInterface;
            vm.IsFullscreen.Value  = Settings.WindowProperties.Fullscreen;
            vm.GlobalSettings.BackgroundChoice.Value = Settings.UIProperties.BgColorChoice;
        });
        

    }
    
    public static async Task ResetSettings()
    {
        var core = Application.Current.DataContext as CoreViewModel;
        if (File.Exists(CurrentSettingsPath))
        {
            try
            {
                File.Delete(CurrentSettingsPath);
            }
            catch (Exception e)
            {
                DebugHelper.LogDebug(nameof(SettingsUpdater), nameof(ResetSettings), e);
                await SetAndSave();
            }
        }
        else
        {
            await SetAndSave();
        }

        AppFunctions.Restart(core?.MainWindows.ActiveWindow.Value.WindowTabs.ActiveTab?.Value);
        
        return;

        async Task SetAndSave()
        {
            SetDefaults();
            await SaveSettingsAsync();
        }
    }
    
    public static async ValueTask ToggleZoomToFit(MainWindowViewModel vm, MainWindow mainWindow)
    {
        if (Settings.ImageScaling.ZoomToFit)
        {
            Settings.ImageScaling.ZoomToFit = false;
            vm.IsZoomedToFit.Value = false;
        }
        else
        {
            Settings.ImageScaling.ZoomToFit = true;
            vm.IsZoomedToFit.Value = true;
        }

        WindowResizing.SetSize(mainWindow, WindowResizeReason.Layout);

        var tabViewModel = vm.WindowTabs.ActiveTab.CurrentValue;
        tabViewModel.ZoomLevel.Value = Convert.ToInt32(tabViewModel.InitialZoom.CurrentValue * 100);;
        tabViewModel.UpdateTabTitle();
        
        await SaveSettingsAsync().ConfigureAwait(false);
    }
    
    public static async ValueTask ToggleSubdirectories(MainWindowViewModel vm)
    {
        if (Settings.Sorting.IncludeSubDirectories)
        {
            TurnOffSubdirectories(vm);
        }
        else
        {
            TurnOnSubdirectories(vm);
        }

        var windowTabs = vm.WindowTabs;
        var tab = windowTabs.ActiveTab.CurrentValue;
        if (tab.ImageIterator?.Files.Count > 0)
        {
            var ct = tab.GetTabCancellation();
            await windowTabs.SharedNavigation.RepopulateIterator(tab.FileInfo.CurrentValue, tab, ct).ConfigureAwait(false);
            await tab.ImageIterator.ReloadAsync().ConfigureAwait(false);
            tab.UpdateTabTitle();
        }
        
        await SaveSettingsAsync();
    }
    
    public static void TurnOffSubdirectories(MainWindowViewModel vm)
    {
        vm.GlobalSettings.IsIncludingSubdirectories.Value = false;
        Settings.Sorting.IncludeSubDirectories = false;
    }
    
    public static void TurnOnSubdirectories(MainWindowViewModel vm)
    {
        vm.GlobalSettings.IsIncludingSubdirectories.Value = true;
        Settings.Sorting.IncludeSubDirectories = true;
    }
    
    public static async ValueTask ToggleTaskbarProgress(MainWindowViewModel vm)
    {
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }
        if (Settings.UIProperties.IsTaskbarProgressEnabled)
        {
            Settings.UIProperties.IsTaskbarProgressEnabled = false;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                core.PlatformService.StopTaskbarProgress();
            });
        }
        else
        {
            Settings.UIProperties.IsTaskbarProgressEnabled = true;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                core.PlatformService.SetTaskbarProgress(
                    (ulong)vm.WindowTabs.ActiveTab.CurrentValue.ImageIterator.CurrentIndex,
                    (ulong)vm.WindowTabs.ActiveTab.CurrentValue.ImageIterator.Files.Count);
            });
        }

        await SaveSettingsAsync();
    }
    
    public static async Task ToggleConstrainBackgroundColor()
    {
        Settings.UIProperties.IsConstrainBackgroundColorEnabled =
            !Settings.UIProperties.IsConstrainBackgroundColorEnabled;
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }

        var brush = BackgroundManager.GetBackgroundBrush((BackgroundType)Settings.UIProperties.BgColorChoice);
        var globalSettings = core.GlobalSettings;
                 
        if (Settings.UIProperties.IsConstrainBackgroundColorEnabled)
        {
            globalSettings.ImageBackground.Value = new SolidColorBrush(Colors.Transparent);
            globalSettings.ConstrainedImageBackground.Value = brush;
        }
        else
        {
            globalSettings.ImageBackground.Value = brush;
            globalSettings.ConstrainedImageBackground.Value = new SolidColorBrush(Colors.Transparent);
        }
                 
        globalSettings.BackgroundChoice.Value = Settings.UIProperties.BgColorChoice;
        await SaveSettingsAsync();
    }

    public static async Task ToggleOpeningInSameWindow()
    {
        if (Settings.UIProperties.OpenInSameWindow)
        {
            IPC.StopListening();
            Settings.UIProperties.OpenInSameWindow = false;
        }
        else
        {
            _ = IPC.StartListeningForArguments();
            Settings.UIProperties.OpenInSameWindow = true;
        }

        await SaveSettingsAsync();
    }

    public static async Task ToggleFileHistory(MainWindowViewModel vm)
    {
        if (Settings.Navigation.IsFileHistoryEnabled)
        {
            vm.GlobalSettings.IsFileHistoryEnabled.Value = false;
            Settings.Navigation.IsFileHistoryEnabled = false;
            
            vm.Translation.ToggleFileHistory.Value = TranslationManager.Translation.FileHistoryDisabled;
        }
        else
        {
            vm.GlobalSettings.IsFileHistoryEnabled.Value = true;
            Settings.Navigation.IsFileHistoryEnabled = true;
            
            vm.Translation.ToggleFileHistory.Value = TranslationManager.Translation.FileHistoryEnabled;
        }
        
        await SaveSettingsAsync();
    }
    
    public static async Task ToggleShowFullPathInTitleBar(MainWindowViewModel vm)
    {
        if (Settings.UIProperties.ShowFullPathInTitleBar)
        {
            Settings.UIProperties.ShowFullPathInTitleBar = false;
        }
        else
        {
            Settings.UIProperties.ShowFullPathInTitleBar = true;
        }
        vm.WindowTabs.ActiveTab.CurrentValue.UpdateTabTitle();
        await SaveSettingsAsync();
    }
    
    public static async Task ToggleSideBySide(MainWindow mainWindow)
    {
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }
        var vm = core.MainWindows.ActiveWindow.Value;
        var tab = vm.WindowTabs.ActiveTab.Value;
        
        var showSideBySide = !Settings.ImageScaling.ShowImageSideBySide;
        if (showSideBySide)
        {
            Settings.ImageScaling.ShowImageSideBySide = true;
            vm.IsSideBySide.Value = true;
            if (tab.CurrentView.CurrentValue is ImageViewer imageViewer)
            {
                await tab.ImageIterator.ReloadAsync(false).ConfigureAwait(false);
                var imageModel = await core.SharedCache.LoadAsync(tab.Id, tab.ImageIterator.SecondaryCurrentIndex, tab.ImageIterator.Files);
                imageViewer.SecondaryImage.Source = imageModel.Image;
            }
        }
        else
        {
            Settings.ImageScaling.ShowImageSideBySide = false;
            vm.IsSideBySide.Value = false;
            if (tab.CurrentView.CurrentValue is ImageViewer imageViewer)
            {
                imageViewer.SecondaryImage.Source = null;
            }
        }
        
        WindowResizing.SetSize(mainWindow, WindowResizeReason.Application);
        tab.UpdateTabTitle();
        await SaveSettingsAsync();
    }
    
    public static async Task ToggleScroll(MainWindowViewModel vm, MainWindow mainWindow)
    {
        if (vm is null)
        {
            return;
        }
        
        if (Settings.Zoom.ScrollEnabled)
        {
            TurnOffScroll(vm);
        }
        else
        {
            TurnOnScroll(vm);
        }
        
        WindowResizing.SetSize(mainWindow, WindowResizeReason.Application);
        
        await SaveSettingsAsync();
    }
    
    public static void TurnOffScroll(MainWindowViewModel vm)
    {
        vm.Translation.IsScrolling.Value = TranslationManager.Translation.ScrollingDisabled;
        vm.IsScrollingEnabled.Value = false;
        Settings.Zoom.ScrollEnabled = false;
    }
    
    public static void TurnOnScroll(MainWindowViewModel vm)
    {
        vm.Translation.IsScrolling.Value = TranslationManager.Translation.ScrollingEnabled;
        vm.IsScrollingEnabled.Value = true;
        Settings.Zoom.ScrollEnabled = true;
    }
    
    public static async Task ToggleCtrlZoom(MainWindowViewModel vm)
    {
        Settings.Zoom.CtrlZoom = !Settings.Zoom.CtrlZoom;
        vm.Translation.IsCtrlToZoom.Value = Settings.Zoom.CtrlZoom
            ? TranslationManager.Translation.CtrlToZoom
            : TranslationManager.Translation.ScrollToZoom;
        
        UIHelper.SetCtrlToZoomImage(vm);
        await SaveSettingsAsync().ConfigureAwait(false);
    }
    
    public static async Task ToggleLooping(MainWindowViewModel vm)
    {
        var value = !Settings.UIProperties.Looping;
        Settings.UIProperties.Looping = value;
        vm.Translation.IsLooping.Value = value
            ? TranslationManager.Translation.LoopingEnabled
            : TranslationManager.Translation.LoopingDisabled;
        vm.GlobalSettings.IsLooping.Value = value;

        var msg = value
            ? TranslationManager.Translation.LoopingEnabled
            : TranslationManager.Translation.LoopingDisabled;
        TooltipHelper.ShowTooltipMessage(msg);

        var windowTabs = vm.WindowTabs;
        var tab = windowTabs.ActiveTab.CurrentValue;
        if (tab.ImageIterator?.Files.Count > 0)
        {
            var isLooping = Settings.UIProperties.Looping;
            var index = tab.ImageIterator.CurrentIndex;
            var count = tab.ImageIterator.Files.Count;
            if (Settings.ImageScaling.ShowImageSideBySide)
            {
                tab.CanNavigateForwards.Value = isLooping || index < count - 2;
            }
            else
            {
                tab.CanNavigateForwards.Value = isLooping || index < count - 1;
            }

            tab.CanNavigateBackwards.Value = isLooping || index > 0;
        }
        
        await SaveSettingsAsync();
    }
    
}
