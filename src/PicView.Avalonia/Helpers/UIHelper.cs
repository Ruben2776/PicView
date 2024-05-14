using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Threading;
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
            Margin = new Thickness(0, 0, 168, 0),
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
            Margin = new Thickness(127, 0, 0, 0),
        };

        mainView.MainGrid.Children.Add(settingsMenu);

        var toolsMenu = new Views.UC.Menus.ToolsMenu
        {
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(120, 0, 0, 0),
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
    
    #endregion
}
