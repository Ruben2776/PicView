using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Threading;
using PicView.Avalonia.ColorManagement;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Gallery;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.Sizing;

namespace PicView.Avalonia.SettingsManagement;
public static class SettingsUpdater
{
    public static void ValidateGallerySettings(MainViewModel vm, bool settingsExists)
    {
        if (vm.Gallery is not {} gallery)
        {
            return;
        }

        if (!settingsExists)
        {
            gallery.GalleryItem.BottomGalleryItemHeight.Value = GalleryDefaults.DefaultBottomGalleryHeight;
            gallery.GalleryItem.ExpandedGalleryItemHeight.Value = GalleryDefaults.DefaultFullGalleryHeight;
        }
        else
        {
            gallery.GalleryItem.ExpandedGalleryItemHeight.Value  = Settings.Gallery.ExpandedGalleryItemSize;
            gallery.GalleryItem.BottomGalleryItemHeight.Value = Settings.Gallery.BottomGalleryItemSize;
        }

        // Set default gallery sizes if they are out of range or upgrading from an old version
        if (gallery.GalleryItem.BottomGalleryItemHeight.CurrentValue is < GalleryDefaults.MinBottomGalleryItemHeight or > GalleryDefaults.MaxBottomGalleryItemHeight)
        {
            gallery.GalleryItem.BottomGalleryItemHeight.Value = GalleryDefaults.DefaultBottomGalleryHeight;
        }

        if (gallery.GalleryItem.ExpandedGalleryItemHeight.CurrentValue is < GalleryDefaults.MinFullGalleryItemHeight or > GalleryDefaults.MaxFullGalleryItemHeight)
        {
            gallery.GalleryItem.ExpandedGalleryItemHeight.Value = GalleryDefaults.DefaultFullGalleryHeight;
        }

        if (settingsExists)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(Settings.Gallery.BottomGalleryStretchMode))
        {
            Settings.Gallery.BottomGalleryStretchMode = "UniformToFill";
        }

        if (string.IsNullOrWhiteSpace(Settings.Gallery.FullGalleryStretchMode))
        {
            Settings.Gallery.FullGalleryStretchMode = "UniformToFill";
        }
    }



    public static void InitializeSettings(MainViewModel vm)
    {
        MainWindowViewModel.GetAndSetWindowMinSize(vm);
        
        // Set corner radius on macOS
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            vm.MainWindow.BottomCornerRadius.Value = new CornerRadius(0, 0, 8, 8);
        }
        
        vm.MainWindow.TitlebarHeight.Value = Settings.WindowProperties.Fullscreen
                                       || !Settings.UIProperties.ShowInterface
            ? 0
            : SizeDefaults.MainTitlebarHeight;
        vm.MainWindow.BottombarHeight.Value = Settings.WindowProperties.Fullscreen
                                              || !Settings.UIProperties.ShowInterface
            ? 0
            : SizeDefaults.BottombarHeight;
        vm.PicViewer.IsShowingSideBySide.Value = Settings.ImageScaling.ShowImageSideBySide;
        vm.MainWindow.IsUIShown.Value  = Settings.UIProperties.ShowInterface;
        vm.MainWindow.IsTopToolbarShown.Value  = Settings.UIProperties.ShowInterface;
        vm.MainWindow.IsBottomToolbarShown.Value   = Settings.UIProperties.ShowBottomNavBar &&
                                    Settings.UIProperties.ShowInterface;
        vm.MainWindow.IsFullscreen.Value  = Settings.WindowProperties.Fullscreen;
        vm.MainWindow.BackgroundChoice.Value = Settings.UIProperties.BgColorChoice;
    }
    
    public static async Task ResetSettings(MainViewModel vm)
    {
        vm.MainWindow.IsLoadingIndicatorShown.Value = true;

        try
        {
            ResetDefaults();

            ThemeManager.DetermineTheme(Application.Current, false);
        
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                TurnOffUsingTouchpad(vm);
            }
            else
            {
                TurnOffUsingTouchpad(vm);
            }

            if (vm.Gallery is not null)
            {
                vm.Gallery.GalleryItem.BottomGalleryItemHeight.Value = GalleryDefaults.DefaultBottomGalleryHeight;
                vm.Gallery.GalleryItem.ExpandedGalleryItemHeight.Value = GalleryDefaults.DefaultFullGalleryHeight;
            }
            
            if (string.IsNullOrWhiteSpace(Settings.Gallery.BottomGalleryStretchMode))
            {
                Settings.Gallery.BottomGalleryStretchMode = "UniformToFill";
            }

            if (string.IsNullOrWhiteSpace(Settings.Gallery.FullGalleryStretchMode))
            {
                Settings.Gallery.FullGalleryStretchMode = "UniformToFill";
            }
        
            await TurnOffSubdirectories(vm);
        
            TurnOffSideBySide(vm);
            TurnOffScroll(vm);
            TurnOffCtrlZoom(vm);
            TurnOffLooping(vm);
        
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                vm.PlatformService.StopTaskbarProgress();
                if (NavigationManager.CanNavigate(vm))
                {
                    vm.PlatformService.SetTaskbarProgress((ulong)NavigationManager.GetCurrentIndex,
                        (ulong)NavigationManager.GetCount);
                }
                WindowResizing.SetSize(vm);
            });
            
            await SaveSettingsAsync();
        }
        finally
        {
            TitleManager.SetTitle(vm);
            vm.MainWindow.IsLoadingIndicatorShown.Value = false;
        }
    }

    public static async Task ToggleUsingTouchpad(MainViewModel vm)
    {
        if (Settings.Zoom.IsUsingTouchPad)
        {
            TurnOffUsingTouchpad(vm);
        }
        else
        {
            TurnOnUsingTouchpad(vm);
        }
    
        await SaveSettingsAsync();
    }
    
    public static void TurnOffUsingTouchpad(MainViewModel vm)
    {
        Settings.Zoom.IsUsingTouchPad = false;
        vm.Translation.IsUsingTouchpad.Value = TranslationManager.Translation.UsingMouse;
        if (vm.SettingsViewModel is not null)
        {
            vm.SettingsViewModel.IsUsingTouchpad.Value = false;
        }
        
    }
    
    public static void TurnOnUsingTouchpad(MainViewModel vm)
    {
        Settings.Zoom.IsUsingTouchPad = true;
        vm.Translation.IsUsingTouchpad.Value = TranslationManager.Translation.UsingTouchpad;
        if (vm.SettingsViewModel is not null)
        {
            vm.SettingsViewModel.IsUsingTouchpad.Value = true;
        }
    }
    
    public static async Task ToggleSubdirectories(MainViewModel vm)
    {
        if (Settings.Sorting.IncludeSubDirectories)
        {
            await TurnOffSubdirectories(vm).ConfigureAwait(false);
        }
        else
        {
            await TurnOnSubdirectories(vm).ConfigureAwait(false);
        }
        await SaveSettingsAsync();
    }

    public static async Task ToggleStretch(MainViewModel vm)
    {
        if (Settings.ImageScaling.StretchImage)
        {
            Settings.ImageScaling.StretchImage = false;
        }
        else
        {
            Settings.ImageScaling.StretchImage = true;
        }
        await WindowResizing.SetSizeAsync(vm).ConfigureAwait(false);
        await SaveSettingsAsync();
    }
    
    public static async Task TurnOffSubdirectories(MainViewModel vm)
    {
        vm.GlobalSettings.IsIncludingSubdirectories.Value = false;
        Settings.Sorting.IncludeSubDirectories = false;

        if (!NavigationManager.CanNavigate(vm))
        {
            return;
        }
        
        await NavigationManager.ReloadFileListAsync().ConfigureAwait(false);
        TitleManager.SetTitle(vm);
    }
    
    public static async Task TurnOnSubdirectories(MainViewModel vm)
    {
        vm.GlobalSettings.IsIncludingSubdirectories.Value = true;
        Settings.Sorting.IncludeSubDirectories = true;
        
        if (!NavigationManager.CanNavigate(vm))
        {
            return;
        }
        
        await NavigationManager.ReloadFileListAsync().ConfigureAwait(false);
        TitleManager.SetTitle(vm);
    }
    
    public static async Task ToggleTaskbarProgress(MainViewModel vm)
    {
        if (Settings.UIProperties.IsTaskbarProgressEnabled)
        {
            Settings.UIProperties.IsTaskbarProgressEnabled = false;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                vm.PlatformService.StopTaskbarProgress();
            });
        }
        else
        {
            Settings.UIProperties.IsTaskbarProgressEnabled = true;
            if (NavigationManager.CanNavigate(vm))
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    vm.PlatformService.SetTaskbarProgress((ulong)NavigationManager.GetCurrentIndex,
                        (ulong)NavigationManager.GetCount);
                });
            }
        }

        await SaveSettingsAsync();
    }
    
    public static async Task ToggleConstrainBackgroundColor(MainViewModel vm)
    {
        if (Settings.UIProperties.IsConstrainBackgroundColorEnabled)
        {
            Settings.UIProperties.IsConstrainBackgroundColorEnabled = false;
            if (vm.SettingsViewModel is not null)
            {
                vm.SettingsViewModel.IsConstrainingBackgroundColor.Value = false;
            }
        }
        else
        {
            Settings.UIProperties.IsConstrainBackgroundColorEnabled = true;
            if (vm.SettingsViewModel is not null)
            {
                vm.SettingsViewModel.IsConstrainingBackgroundColor.Value = true;
            }
        }
        
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            BackgroundManager.SetBackground(vm);
        });
        await SaveSettingsAsync();
    }

    public static async Task ToggleOpeningInSameWindow(MainViewModel vm)
    {
        if (Settings.UIProperties.OpenInSameWindow)
        {
            IPC.StopListening();
            Settings.UIProperties.OpenInSameWindow = false;
        }
        else
        {
            _ = IPC.StartListeningForArguments(vm);
            Settings.UIProperties.OpenInSameWindow = true;
        }

        await SaveSettingsAsync();
    }

    public static async Task ToggleFileHistory(MainViewModel vm)
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
    
    #region Image settings

    public static async Task ToggleSideBySide(MainViewModel vm)
    {
        if (vm is null)
        {
            return;
        }
        
        if (Settings.ImageScaling.ShowImageSideBySide)
        {
            TurnOffSideBySide(vm);
        }
        else
        {
            await TurnOnSideBySide(vm);
        }

        await SaveSettingsAsync();
    }

    public static void TurnOffSideBySide(MainViewModel vm)
    {
        Settings.ImageScaling.ShowImageSideBySide = false;
        vm.PicViewer.IsShowingSideBySide.Value = false;
        vm.PicViewer.SecondaryImageSource.Value = null;
        WindowResizing.SetSize(vm);
        TitleManager.SetTitle(vm);
    }
    
    public static async Task TurnOnSideBySide(MainViewModel vm)
    {
        Settings.ImageScaling.ShowImageSideBySide = true;
        vm.PicViewer.IsShowingSideBySide.Value = true;
        if (NavigationManager.CanNavigate(vm))
        {
            var preloadValue = await NavigationManager.GetNextPreLoadValueAsync();
            if (preloadValue is null)
            {
#if DEBUG
                Console.WriteLine($"{nameof(TurnOnSideBySide)} {nameof(preloadValue)} is null");       
#endif
                return;
            }
            vm.PicViewer.SecondaryImageSource.Value = preloadValue.ImageModel.Image;
            var imageModel1 = new ImageModel
            {
                FileInfo = vm.PicViewer.FileInfo.CurrentValue,
                PixelWidth = (int)vm.PicViewer.ImageWidth.CurrentValue,
                PixelHeight = (int)vm.PicViewer.ImageHeight.CurrentValue,
                ImageType = vm.PicViewer.ImageType.CurrentValue,
                Image = vm.PicViewer.ImageSource,
                Orientation = vm.PicViewer.ExifOrientation.CurrentValue
            };
            var imageModel2 = new ImageModel
            {
                FileInfo = preloadValue.ImageModel.FileInfo,
                PixelWidth = preloadValue.ImageModel.PixelWidth,
                PixelHeight = preloadValue.ImageModel.PixelHeight,
                ImageType = preloadValue.ImageModel.ImageType,
                Image = preloadValue.ImageModel.Image,
                Orientation = preloadValue.ImageModel.Orientation
            };
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                WindowResizing.SetSize(vm.PicViewer.ImageWidth.CurrentValue, vm.PicViewer.ImageHeight.CurrentValue, preloadValue.ImageModel.PixelWidth, preloadValue.ImageModel.PixelHeight, vm);
                TitleManager.SetSideBySideTitle(vm, imageModel1, imageModel2);
            });
        }
    }
    
    public static async Task ToggleScroll(MainViewModel vm)
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
        
        WindowResizing.SetSize(vm);
        
        await SaveSettingsAsync();
    }
    
    public static void TurnOffScroll(MainViewModel vm)
    {
        vm.MainWindow.ToggleScrollBarVisibility.Value = ScrollBarVisibility.Disabled;
        vm.Translation.IsScrolling.Value = TranslationManager.Translation.ScrollingDisabled;
        vm.GlobalSettings.IsScrollingEnabled.Value = false;
        Settings.Zoom.ScrollEnabled = false;
        vm.MainWindow.RightControlOffSetMargin.Value = new Thickness(0);
    }
    
    public static void TurnOnScroll(MainViewModel vm)
    {
        vm.MainWindow.ToggleScrollBarVisibility.Value = ScrollBarVisibility.Visible;
        vm.Translation.IsScrolling.Value = TranslationManager.Translation.ScrollingEnabled;
        vm.GlobalSettings.IsScrollingEnabled.Value = true;
        Settings.Zoom.ScrollEnabled = true;
        vm.MainWindow.RightControlOffSetMargin.Value = new Thickness(0,0,30,0);
    }
    
    public static async Task ToggleCtrlZoom(MainViewModel vm)
    {
        if (vm is null)
        {
            return;
        }
        
        Settings.Zoom.CtrlZoom = !Settings.Zoom.CtrlZoom;
        vm.Translation.IsCtrlToZoom.Value = Settings.Zoom.CtrlZoom
            ? TranslationManager.Translation.CtrlToZoom
            : TranslationManager.Translation.ScrollToZoom;
        
        // Set source for ChangeCtrlZoomImage
        if (!Application.Current.TryGetResource("ScanEyeImage", Application.Current.RequestedThemeVariant, out var scanEyeImage ))
        {
            return;
        }
        if (!Application.Current.TryGetResource("LeftRightArrowsImage", Application.Current.RequestedThemeVariant, out var leftRightArrowsImage ))
        {
            return;
        }
        var isNavigatingWithCtrl = Settings.Zoom.CtrlZoom;
        vm.MainWindow.ChangeCtrlZoomImage.Value = isNavigatingWithCtrl ? leftRightArrowsImage as DrawingImage : scanEyeImage as DrawingImage;
        await SaveSettingsAsync().ConfigureAwait(false);
    }
    
    public static void TurnOffCtrlZoom(MainViewModel vm)
    {
        Settings.Zoom.CtrlZoom = false;
        vm.Translation.IsCtrlToZoom.Value = TranslationManager.Translation.ScrollToZoom;
        if (!Application.Current.TryGetResource("ScanEyeImage", Application.Current.RequestedThemeVariant, out var scanEyeImage ))
        {
            return;
        }
        vm.MainWindow.ChangeCtrlZoomImage.Value = scanEyeImage as DrawingImage;
    }
    
    public static async Task ToggleLooping(MainViewModel vm)
    {
        if (vm is null)
        {
            return;
        }
        
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

        await SaveSettingsAsync();
    }
    
    public static void TurnOffLooping(MainViewModel vm)
    {
        Settings.UIProperties.Looping = false;
        vm.Translation.IsLooping.Value = TranslationManager.Translation.LoopingDisabled;
        vm.GlobalSettings.IsLooping.Value = false;
    }
    
    #endregion
}
