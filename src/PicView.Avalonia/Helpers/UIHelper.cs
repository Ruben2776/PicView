using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views;
using PicView.Core.Config;
using PicView.Core.Localization;

namespace PicView.Avalonia.Helpers;

public class UIHelper
{
    #region Menus
    
    public static void AddMenus(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var mainView = desktop.MainWindow.FindControl<MainView>("MainView");    
        var fileMenu = new Views.UC.Menus.FileMenu
        {
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 120, 0),
        };

        mainView.MainGrid.Children.Add(fileMenu);

        var imageMenu = new Views.UC.Menus.ImageMenu
        {
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 63, 0),
        };

        mainView.MainGrid.Children.Add(imageMenu);

        var settingsMenu = new Views.UC.Menus.SettingsMenu
        {
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, -75, 0),
        };

        mainView.MainGrid.Children.Add(settingsMenu);

        var toolsMenu = new Views.UC.Menus.ToolsMenu
        {
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(80, 0, 0, 0),
        };

        mainView.MainGrid.Children.Add(toolsMenu);
    }

    public static void CloseMenus(MainViewModel vm)
    {
        vm.IsFileMenuVisible = false;
        vm.IsImageMenuVisible = false;
        vm.IsSettingsMenuVisible = false;
        vm.IsToolsMenuVisible = false;
    }

    public static void ToggleFileMenu(MainViewModel vm)
    {
        vm.IsFileMenuVisible = !vm.IsFileMenuVisible;
        vm.IsImageMenuVisible = false;
        vm.IsSettingsMenuVisible = false;
        vm.IsToolsMenuVisible = false;
    }

    public static void ToggleImageMenu(MainViewModel vm)
    {
        vm.IsFileMenuVisible = false;
        vm.IsImageMenuVisible = !vm.IsImageMenuVisible;
        vm.IsSettingsMenuVisible = false;
        vm.IsToolsMenuVisible = false;
    }

    public static void ToggleSettingsMenu(MainViewModel vm)
    {
        vm.IsFileMenuVisible = false;
        vm.IsImageMenuVisible = false;
        vm.IsSettingsMenuVisible = !vm.IsSettingsMenuVisible;
        vm.IsToolsMenuVisible = false;
    }

    public static void ToggleToolsMenu(MainViewModel vm)
    {
        vm.IsFileMenuVisible = false;
        vm.IsImageMenuVisible = false;
        vm.IsSettingsMenuVisible = false;
        vm.IsToolsMenuVisible = !vm.IsToolsMenuVisible;
    }

    #endregion Menus

    #region Settings
    
    public static void ToggleScroll(MainViewModel vm)
    {
        if (SettingsHelper.Settings.Zoom.ScrollEnabled)
        {
            vm.ToggleScrollBarVisibility = ScrollBarVisibility.Disabled;
            vm.GetScrolling = TranslationHelper.GetTranslation("ScrollingDisabled");
            vm.IsScrollingEnabled = false;
            SettingsHelper.Settings.Zoom.ScrollEnabled = false;
        }
        else
        {
            vm.ToggleScrollBarVisibility = ScrollBarVisibility.Visible;
            vm.GetScrolling = TranslationHelper.GetTranslation("ScrollingEnabled");
            vm.IsScrollingEnabled = true;
            SettingsHelper.Settings.Zoom.ScrollEnabled = true;
        }
        WindowHelper.SetSize(vm);
    }

    public static async Task Flip(MainViewModel vm)
    {
        if (!NavigationHelper.CanNavigate(vm))
        {
            return;
        }

        if (vm.ScaleX == 1)
        {
            vm.ScaleX = -1;
            vm.GetFlipped = vm.UnFlip;
        }
        else
        {
            vm.ScaleX = 1;
            vm.GetFlipped = vm.Flip;
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            vm.ImageViewer.Flip(animate: true);
        });
    }

    #region Set Gallery Stretch

    public static void SetStretchMode(MainViewModel vm)
    {
        // Reset all boolean properties
        vm.IsUniformChecked = false;
        vm.IsUniformToFillChecked = false;
        vm.IsFillChecked = false;
        vm.IsNoneChecked = false;
        vm.IsSquareChecked = false;
        vm.IsFillSquareChecked = false;
        
        if (GalleryFunctions.IsFullGalleryOpen)
        {
            if (SettingsHelper.Settings.Gallery.FullGalleryStretchMode.Equals("Square", StringComparison.OrdinalIgnoreCase))
            {
                vm.GalleryBottomItemStretch = Stretch.Uniform;
                vm.GetGalleryItemWidth = vm.GetGalleryItemHeight;
                vm.IsSquareChecked = true;
            }
            else if (SettingsHelper.Settings.Gallery.FullGalleryStretchMode.Equals("FillSquare", StringComparison.OrdinalIgnoreCase))
            {
                vm.GalleryBottomItemStretch = Stretch.Fill;
                vm.GetGalleryItemWidth = vm.GetGalleryItemHeight;
                vm.IsFillSquareChecked = true;
            }
            else if (Enum.TryParse<Stretch>(SettingsHelper.Settings.Gallery.FullGalleryStretchMode, out var stretchMode))
            {
                vm.GalleryBottomItemStretch = stretchMode;
                vm.GetGalleryItemWidth = double.NaN;
                SetStretchIsChecked(stretchMode);
            }
            else
            {
                vm.GalleryBottomItemStretch = Stretch.Uniform;
                vm.GetGalleryItemWidth = double.NaN;
                vm.IsUniformChecked = true;
            }
        }
        else
        {
            if (SettingsHelper.Settings.Gallery.BottomGalleryStretchMode.Equals("Square", StringComparison.OrdinalIgnoreCase))
            {
                vm.GalleryFullItemStretch = Stretch.Uniform;
                vm.GetGalleryItemWidth = vm.GetGalleryItemHeight;
                vm.IsSquareChecked = true;
            }
            else if (SettingsHelper.Settings.Gallery.BottomGalleryStretchMode.Equals("FillSquare", StringComparison.OrdinalIgnoreCase))
            {
                vm.GalleryFullItemStretch = Stretch.Fill;
                vm.GetGalleryItemWidth = vm.GetGalleryItemHeight;
                vm.IsFillSquareChecked = true;
            }
            else if (Enum.TryParse<Stretch>(SettingsHelper.Settings.Gallery.BottomGalleryStretchMode, out var stretchMode))
            {
                vm.GalleryFullItemStretch = stretchMode;
                vm.GetGalleryItemWidth = double.NaN;
                SetStretchIsChecked(stretchMode);
            }
            else
            {
                vm.GalleryFullItemStretch = Stretch.Uniform;
                vm.GetGalleryItemWidth = double.NaN;
                vm.IsUniformChecked = true;
            }
        }

        
        return;

        void SetStretchIsChecked(Stretch stretchMode)
        {
            switch (stretchMode)
            {
                case Stretch.Uniform:
                    vm.IsUniformChecked = true;
                    break;
                case Stretch.UniformToFill:
                    vm.IsUniformToFillChecked = true;
                    break;
                case Stretch.Fill:
                    vm.IsFillChecked = true;
                    break;
                case Stretch.None:
                    vm.IsNoneChecked = true;
                    break;
            }
        }
    }
    
    private static void SetGalleryStretch(MainViewModel vm, Stretch stretch)
    {
        vm.GetGalleryItemWidth = double.NaN;
        vm.GalleryBottomItemStretch = stretch;
        vm.GalleryStretch = stretch;
    }

    private static void SetSquareStretch(MainViewModel vm)
    {
        vm.GetGalleryItemWidth = vm.GetGalleryItemHeight;
        vm.GalleryBottomItemStretch = Stretch.Uniform;
        vm.GalleryStretch = Stretch.Uniform;
    }
    
    private static void SetSquareFillStretch(MainViewModel vm)
    {
        vm.GetGalleryItemWidth = vm.GetGalleryItemHeight;
        vm.GalleryBottomItemStretch = Stretch.Fill;
        vm.GalleryStretch = Stretch.Fill;
    }

    public static async Task ChangeBottomGalleryItemStretch(MainViewModel vm, Stretch stretch)
    {
        SetGalleryStretch(vm, stretch);
        
        SettingsHelper.Settings.Gallery.BottomGalleryStretchMode = stretch.ToString();
        await SettingsHelper.SaveSettingsAsync();
        GalleryNavigation.CenterScrollToSelectedItem(vm);
    }
    
    public static async Task ChangeFullGalleryItemStretch(MainViewModel vm, Stretch stretch)
    {
        SetGalleryStretch(vm, stretch);
        
        SettingsHelper.Settings.Gallery.FullGalleryStretchMode = stretch.ToString();
        await SettingsHelper.SaveSettingsAsync();
        GalleryNavigation.CenterScrollToSelectedItem(vm);
    }
    
    public static async Task ChangeBottomGalleryStretchSquare(MainViewModel vm)
    {
        SetSquareStretch(vm);
        
        SettingsHelper.Settings.Gallery.BottomGalleryStretchMode = "Square";
        await SettingsHelper.SaveSettingsAsync();
        GalleryNavigation.CenterScrollToSelectedItem(vm);
    }
    
    public static async Task ChangeBottomGalleryStretchSquareFill(MainViewModel vm)
    {
        SetSquareFillStretch(vm);
        
        SettingsHelper.Settings.Gallery.BottomGalleryStretchMode = "FillSquare";
        await SettingsHelper.SaveSettingsAsync();
        GalleryNavigation.CenterScrollToSelectedItem(vm);
    }

    public static async Task ChangeFullGalleryStretchSquare(MainViewModel vm)
    {
        SetSquareStretch(vm);
        
        SettingsHelper.Settings.Gallery.FullGalleryStretchMode = "Square";
        await SettingsHelper.SaveSettingsAsync();
        GalleryNavigation.CenterScrollToSelectedItem(vm);
    }
    
    public static async Task ChangeFullGalleryStretchSquareFill(MainViewModel vm)
    {
        SetSquareFillStretch(vm);
        
        SettingsHelper.Settings.Gallery.FullGalleryStretchMode = "FillSquare";
        await SettingsHelper.SaveSettingsAsync();
        GalleryNavigation.CenterScrollToSelectedItem(vm);
    }
    #endregion
    
    #endregion
}
