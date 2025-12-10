using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using PicView.Avalonia.Crop;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views.UC.PopUps;

namespace PicView.Avalonia.UI;

public static class DialogManager
{
    public static bool IsDialogOpen { get; set; }
    
    /// <summary>
    /// Handles close action based on current application state
    /// </summary>
    public static async Task HandleShouldClosing(MainViewModel vm)
    {
        // Handle open menus
        if (MenuManager.IsAnyMenuOpen(vm))
        {
            MenuManager.CloseMenus(vm);
            return;
        }

        // Handle cropping mode
        if (CropFunctions.IsCropping)
        {
            CropFunctions.CloseCropControl(vm);
            return;
        }

        // Handle slideshow
        if (Slideshow.IsRunning)
        {
            Slideshow.StopSlideshow(vm);
            return;
        }
        
        // Handle window close
        await Dispatcher.UIThread.InvokeAsync(CloseWithOptionalDialog);
    }

    public static void CloseWithOptionalDialog()
    {
        if (Settings.UIProperties.ShowConfirmationOnEsc)
        {
            UIHelper.GetMainView?.MainGrid.Children.Add(new CloseDialog());
        }
        else
        {
            Close();
        }
    }

    public static void Close()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow?.Close();
        }
    }

    public static void AddFileSearchDialog()
    {
        if (!NavigationManager.CanNavigate(UIHelper.GetMainView.DataContext as MainViewModel))
        {
            return;
        }
        if (UIHelper.GetMainView.MainGrid.Children.OfType<FileSearchDialog>().Any())
        {
            return;
        }

        MenuManager.CloseMenus(UIHelper.GetMainView.DataContext as MainViewModel);
        UIHelper.GetMainView.MainGrid.Children.Add(new FileSearchDialog());
    }

    public static void AddNavigationDialog()
    {
        if (UIHelper.GetMainView.MainGrid.Children.OfType<NavigationDialog>().Any())
        {
            return;
        }

        MenuManager.CloseMenus(UIHelper.GetMainView.DataContext as MainViewModel);
        UIHelper.GetMainView.MainGrid.Children.Add(new NavigationDialog());
    }
}
