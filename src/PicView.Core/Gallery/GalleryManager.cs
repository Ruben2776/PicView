using PicView.Core.ViewModels;

namespace PicView.Core.Gallery;

public static class GalleryManager
{
    public static async ValueTask CloseDockedGalleryAsync(CancellationToken ct)
    {
        Settings.Gallery.IsGalleryDocked = false;
        // Wait for animation to finish
        await Task.Delay(TimeSpan.FromSeconds(GalleryDefaults.VeryFastAnimationSpeed), ct);
        Settings.Gallery.DockPosition = GalleryDockPosition.Closed;
        await SaveSettingsAsync();
    }
    
    public static void ToggleGallery(GalleryViewModel galleryViewModel)
    {
        if (Settings.Gallery.IsGalleryDocked && galleryViewModel.IsGalleryExpanded.CurrentValue)
        {
            galleryViewModel.ActiveGalleryMode.Value = GalleryMode.Docked;
        }
        else if (galleryViewModel.IsGalleryExpanded.CurrentValue)
        {
            galleryViewModel.ActiveGalleryMode.Value = GalleryMode.Closed;
        }
        else
        {
            galleryViewModel.ActiveGalleryMode.Value = GalleryMode.Expanded;
        }
    }
    
    public static void OpenOrCloseGallery(GalleryViewModel galleryViewModel)
    {
        if (Settings.Gallery.IsGalleryDocked)
        {
            galleryViewModel.ActiveGalleryMode.Value = GalleryMode.Closed;
            Settings.Gallery.IsGalleryDocked = false;
        }
        else
        {
            if (Settings.Gallery.DockPosition is GalleryDockPosition.Closed)
            {
                Settings.Gallery.DockPosition = GalleryDockPosition.Bottom;
            }
            Settings.Gallery.IsGalleryDocked = true;
            galleryViewModel.IsGalleryDocked.Value = true;
        }
    }
}