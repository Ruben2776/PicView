using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views.UC.Menus;

namespace PicView.Avalonia.UI;

public static class MenuManager
{
    /// <summary>
    /// Adds menu controls to the main view
    /// </summary>
    public static void AddMenus()
    {
        var mainView = UIHelper.GetMainView;
        if (mainView?.MainGrid == null)
        {
            return;
        }

        mainView.MainGrid.Children.Add(CreateMenu<FileMenu>(new Thickness(0, 0, 147, 0)));
        mainView.MainGrid.Children.Add(CreateMenu<ImageMenu>(new Thickness(0, 0, 140, 0)));
        mainView.MainGrid.Children.Add(CreateMenu<SettingsMenu>(new Thickness(0, 0, -102, 0)));
        mainView.MainGrid.Children.Add(CreateMenu<ToolsMenu>(new Thickness(95, 0, 0, 0)));
    }

    private static T CreateMenu<T>(Thickness margin) where T : Control, new()
    {
        return new T
        {
            Name = typeof(T).Name,
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = margin,
            IsVisible = false
        };
    }

    /// <summary>
    /// Closes all menus
    /// </summary>
    public static void CloseMenus(MainViewModel vm)
    {
        vm.MainWindow.IsFileMenuVisible.Value = false;
        vm.MainWindow.IsImageMenuVisible.Value = false;
        vm.MainWindow.IsSettingsMenuVisible.Value = false;
        vm.MainWindow.IsToolsMenuVisible.Value = false;
    }

    /// <summary>
    /// Checks if any menu is open
    /// </summary>
    public static bool IsAnyMenuOpen(MainViewModel vm)
    {
        return vm.MainWindow.IsFileMenuVisible.CurrentValue ||
               vm.MainWindow.IsImageMenuVisible.CurrentValue ||
               vm.MainWindow.IsSettingsMenuVisible.CurrentValue ||
               vm.MainWindow.IsToolsMenuVisible.CurrentValue;
    }

    /// <summary>
    /// Toggles the file menu
    /// </summary>
    public static void ToggleFileMenu(MainViewModel vm) => ToggleMenu(vm, MenuType.File);

    /// <summary>
    /// Toggles the image menu
    /// </summary>
    public static void ToggleImageMenu(MainViewModel vm) => ToggleMenu(vm, MenuType.Image);

    /// <summary>
    /// Toggles the settings menu
    /// </summary>
    public static void ToggleSettingsMenu(MainViewModel vm) => ToggleMenu(vm, MenuType.Settings);

    /// <summary>
    /// Toggles the tools menu
    /// </summary>
    public static void ToggleToolsMenu(MainViewModel vm) => ToggleMenu(vm, MenuType.Tools);

    private static void ToggleMenu(MainViewModel vm, MenuType menuType)
    {
        if (DialogManager.IsDialogOpen)
        {
            return;
        }

        // Get the current state of the menu being toggled
        var currentState = GetMenuState(vm, menuType);

        // Close all menus
        CloseMenus(vm);

        // Only open the menu if it wasn't already open (toggle behavior)
        if (!currentState)
        {
            SetMenuState(vm, menuType, true);
        }
        // If it was already open, it remains closed after CloseMenus()
    }

    private static bool GetMenuState(MainViewModel vm, MenuType menuType)
    {
        return menuType switch
        {
            MenuType.File => vm.MainWindow.IsFileMenuVisible.CurrentValue,
            MenuType.Image => vm.MainWindow.IsImageMenuVisible.CurrentValue,
            MenuType.Settings => vm.MainWindow.IsSettingsMenuVisible.CurrentValue,
            MenuType.Tools => vm.MainWindow.IsToolsMenuVisible.CurrentValue,
            _ => false
        };
    }

    private static void SetMenuState(MainViewModel vm, MenuType menuType, bool state)
    {
        switch (menuType)
        {
            case MenuType.File:
                vm.MainWindow.IsFileMenuVisible.Value = state;
                break;
            case MenuType.Image:
                vm.MainWindow.IsImageMenuVisible.Value = state;
                break;
            case MenuType.Settings:
                vm.MainWindow.IsSettingsMenuVisible.Value = state;
                break;
            case MenuType.Tools:
                vm.MainWindow.IsToolsMenuVisible.Value = state;
                break;
        }
    }

    private enum MenuType
    {
        File,
        Image,
        Settings,
        Tools
    }
}