using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using PicView.Avalonia.Crop;
using PicView.Avalonia.FileSystem;
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

        if (vm.PicViewer.HasChanges.Value && vm.GlobalSettings.ShowPromptToSaveChanges.Value)
        {
            await FileSaverHelper.PromptSaveChangesAsync().ConfigureAwait(false);
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

    public static void AddMessageDialog(string title, string message)
    {
        if (UIHelper.GetMainView.MainGrid.Children.OfType<MessageDialog>().Any())
        {
            return;
        }

        MenuManager.CloseMenus(UIHelper.GetMainView.DataContext as MainViewModel);
        UIHelper.GetMainView.MainGrid.Children.Add(new MessageDialog(title, message));
    }

    public static Task<bool> AddSaveDialog(string title, string message)
    {
        // If a SaveDialog is already open, just return its task
        var existing = UIHelper.GetMainView.MainGrid.Children
            .OfType<SaveDialog>()
            .FirstOrDefault();

        if (existing != null)
        {
            return existing.CloseTask;
        }

        var dialog = new SaveDialog(title, message);
        UIHelper.GetMainView.MainGrid.Children.Add(dialog);

        return dialog.CloseTask;
    }

}
