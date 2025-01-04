using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Threading;
using PicView.Avalonia.Crop;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views;
using PicView.Avalonia.Views.UC;
using PicView.Avalonia.Views.UC.Menus;
using PicView.Avalonia.Views.UC.PopUps;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Config;
using PicView.Core.Gallery;

namespace PicView.Avalonia.UI;
public static class UIHelper
{
    #region GetControls
    
    public static bool IsDialogOpen { get; set; }

    public static MainView? GetMainView { get; private set; }
    public static Control? GetTitlebar { get; private set; }
    public static EditableTitlebar? GetEditableTitlebar { get; private set; }
    public static GalleryAnimationControlView? GetGalleryView { get; private set; }

    public static BottomBar? GetBottomBar { get; private set; }
    
    public static ToolTipMessage? GetToolTipMessage { get; private set; }

    public static void SetControls(IClassicDesktopStyleApplicationLifetime desktop)
    {
        GetMainView = desktop.MainWindow.FindControl<MainView>("MainView");
        GetTitlebar = desktop.MainWindow.FindControl<Control>("Titlebar");
        GetEditableTitlebar = GetTitlebar.FindControl<EditableTitlebar>("EditableTitlebar");
        GetGalleryView = GetMainView.MainGrid.GetControl<GalleryAnimationControlView>("GalleryView");
        GetBottomBar = desktop.MainWindow.FindControl<BottomBar>("BottomBar");
        GetToolTipMessage = GetMainView.MainGrid.FindControl<ToolTipMessage>("ToolTipMessage");
    }

    #endregion

    #region Menus

    public static void AddMenus()
    {
        var mainView = GetMainView;
        var fileMenu = new FileMenu
        {
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 120, 0),
            IsVisible = false
        };

        mainView.MainGrid.Children.Add(fileMenu);

        var imageMenu = new ImageMenu
        {
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 63, 0),
            IsVisible = false
        };

        mainView.MainGrid.Children.Add(imageMenu);

        var settingsMenu = new SettingsMenu
        {
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, -75, 0),
            IsVisible = false
        };

        mainView.MainGrid.Children.Add(settingsMenu);

        var toolsMenu = new ToolsMenu
        {
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(80, 0, 0, 0),
            IsVisible = false
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

    public static bool IsAnyMenuOpen(MainViewModel vm)
    {
        return vm.IsFileMenuVisible || vm.IsImageMenuVisible || vm.IsSettingsMenuVisible || vm.IsToolsMenuVisible;
    }

    public static void ToggleFileMenu(MainViewModel vm)
    {
        if (IsDialogOpen)
        {
            return;
        }
        vm.IsFileMenuVisible = !vm.IsFileMenuVisible;
        vm.IsImageMenuVisible = false;
        vm.IsSettingsMenuVisible = false;
        vm.IsToolsMenuVisible = false;
    }

    public static void ToggleImageMenu(MainViewModel vm)
    {
        if (IsDialogOpen)
        {
            return;
        }
        vm.IsFileMenuVisible = false;
        vm.IsImageMenuVisible = !vm.IsImageMenuVisible;
        vm.IsSettingsMenuVisible = false;
        vm.IsToolsMenuVisible = false;
    }

    public static void ToggleSettingsMenu(MainViewModel vm)
    {
        if (IsDialogOpen)
        {
            return;
        }
        vm.IsFileMenuVisible = false;
        vm.IsImageMenuVisible = false;
        vm.IsSettingsMenuVisible = !vm.IsSettingsMenuVisible;
        vm.IsToolsMenuVisible = false;
    }

    public static void ToggleToolsMenu(MainViewModel vm)
    {
        if (IsDialogOpen)
        {
            return;
        }
        vm.IsFileMenuVisible = false;
        vm.IsImageMenuVisible = false;
        vm.IsSettingsMenuVisible = false;
        vm.IsToolsMenuVisible = !vm.IsToolsMenuVisible;
    }

    #endregion Menus

    #region Navigation

    public static async Task NavigateUp(MainViewModel? vm)
    {
        if (vm is null)
        {
            return;
        }

        if (GalleryFunctions.IsFullGalleryOpen)
        {
            GalleryNavigation.NavigateGallery(Direction.Up, vm);
            return;
        }

        if (vm.IsScrollingEnabled)
        {
            await Dispatcher.UIThread.InvokeAsync(() => { vm.ImageViewer.ImageScrollViewer.LineUp(); });
        }
        else
        {
            vm.ImageViewer.Rotate(true);
        }
    }

    public static async Task NavigateDown(MainViewModel? vm)
    {
        if (vm is null)
        {
            return;
        }

        if (GalleryFunctions.IsFullGalleryOpen)
        {
            GalleryNavigation.NavigateGallery(Direction.Down, vm);
            return;
        }

        if (vm.IsScrollingEnabled)
        {
            vm.ImageViewer.ImageScrollViewer.LineDown();
        }
        else
        {
            await Dispatcher.UIThread.InvokeAsync(() => { vm.ImageViewer.Rotate(false); });
        }
    }



    #endregion Navigation
    
    #region Dialogs
    
    public static async Task Close(MainViewModel vm)
    {
        if (IsAnyMenuOpen(vm))
        {
            CloseMenus(vm);
            return;
        }

        if (CropFunctions.IsCropping)
        {
            CropFunctions.CloseCropControl(vm);
            return;
        }

        if (Slideshow.IsRunning)
        {
            Slideshow.StopSlideshow(vm);
            return;
        }

        if (SettingsHelper.Settings.WindowProperties.Fullscreen)
        {
            await WindowFunctions.MaximizeRestore();
            return;
        }
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (SettingsHelper.Settings.UIProperties.ShowConfirmationOnEsc)
            {
                GetMainView.MainGrid.Children.Add(new CloseDialog());
            }
            else
            {
                desktop.MainWindow?.Close();
            }
        });
    } 

    #endregion


}
