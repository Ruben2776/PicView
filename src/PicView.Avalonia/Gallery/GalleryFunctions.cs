using System.Diagnostics;
using Avalonia.Controls;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;
using PicView.Core.FileHandling;
using PicView.Core.Gallery;

namespace PicView.Avalonia.Gallery
{
    public static class GalleryFunctions
    {
        public static void RecycleItem(object sender, MainViewModel vm)
        {
#if DEBUG
            Debug.Assert(sender != null, nameof(sender) + " != null");
#endif
            var menuItem = (MenuItem)sender;
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (menuItem is null) { return; }
#if DEBUG
            Debug.Assert(menuItem != null, nameof(menuItem) + " != null");
            Debug.Assert(menuItem.DataContext != null, "menuItem.DataContext != null");
#endif
            var galleryItem = (GalleryThumbInfo.GalleryThumbHolder)menuItem.DataContext;
            FileDeletionHelper.DeleteFileWithErrorMsg(galleryItem.FileLocation, recycle: true);

            vm.GalleryItems.Remove(galleryItem); // TODO: rewrite file system watcher to delete gallery items
        }
        
        public static async Task ToggleGallery(MainViewModel vm)
        {
            if (vm is null)
            {
                return;
            }
            vm.IsGalleryOpen = !vm.IsGalleryOpen;
            SettingsHelper.Settings.Gallery.IsBottomGalleryShown = false;
            if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
            {
                // TODO: Change to bottom gallery view
            }

            vm.CloseMenuCommand.Execute(null);
            if (vm.IsGalleryOpen)
            {
                if (!NavigationHelper.CanNavigate(vm))
                {
                    return;
                }
                _ = Task.Run(() => GalleryLoad.LoadGallery(vm, Path.GetDirectoryName(vm.ImageIterator.Pics[0])));
            }
            //WindowHelper.SetSize(this);
            await SettingsHelper.SaveSettingsAsync();
        }
        
        public static async Task ToggleBottomGallery(MainViewModel vm)
        {
            if (vm is null)
            {
                return;
            }
            if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
            {
                vm.IsGalleryOpen = false;
                SettingsHelper.Settings.Gallery.IsBottomGalleryShown = false;
            }
            else
            {
                vm.IsGalleryOpen = true;
                SettingsHelper.Settings.Gallery.IsBottomGalleryShown = true;
                if (!NavigationHelper.CanNavigate(vm))
                {
                    return;
                }
                _ = Task.Run(() => GalleryLoad.LoadGallery(vm, Path.GetDirectoryName(vm.ImageIterator.Pics[0])));
            }
            vm.CloseMenuCommand.Execute(null);
            //WindowHelper.SetSize(this);
            await SettingsHelper.SaveSettingsAsync();
        }
    }
}