using Avalonia.Layout;
using PicView.Avalonia.Helpers;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;
using PicView.Core.Localization;

namespace PicView.Avalonia.Gallery;
public static class GalleryFunctions
{
    public static bool IsFullGalleryOpen { get; private set; }
    public static bool IsBottomGalleryOpen { get; private set; }

    public static async Task ToggleGallery(MainViewModel vm)
    {
        if (vm is null || !NavigationHelper.CanNavigate(vm))
        {
            return;
        }
        
        UIHelper.CloseMenus(vm);
        if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
        {
            // Showing bottom gallery is enabled
            IsBottomGalleryOpen = true;
            if (IsFullGalleryOpen)
            {
                // Switch to bottom gallery
                IsFullGalleryOpen = false;
                vm.IsGalleryOpen = false;
                vm.GalleryMode = GalleryMode.FullToBottom;
                vm.GetGalleryItemHeight = vm.GetBottomGalleryItemHeight;
            }
            else
            {
                // Switch to full gallery
                IsFullGalleryOpen = true;
                vm.IsGalleryOpen = true;
                vm.GalleryMode = GalleryMode.BottomToFull;
                vm.GetGalleryItemHeight = vm.GetExpandedGalleryItemHeight;
            }
        }
        else
        {
            IsBottomGalleryOpen = false;
            if (IsFullGalleryOpen)
            {
                // close full gallery
                IsFullGalleryOpen = false;
                vm.IsGalleryOpen = false;
                vm.GalleryMode = GalleryMode.FullToClosed;
            }
            else
            {
                // open full gallery
                IsFullGalleryOpen = true;
                vm.IsGalleryOpen = true;
                vm.GalleryMode = GalleryMode.ClosedToFull;
                vm.GetGalleryItemHeight = vm.GetExpandedGalleryItemHeight;
            }
        }
        _ = Task.Run(() => GalleryLoad.LoadGallery(vm, Path.GetDirectoryName(vm.ImageIterator.Pics[0])));
        await SettingsHelper.SaveSettingsAsync();
    }

    public static async Task ToggleBottomGallery(MainViewModel vm)
    {
        SettingsHelper.Settings.Gallery.IsBottomGalleryShown = !SettingsHelper.Settings.Gallery.IsBottomGalleryShown;
        await OpenCloseBottomGallery(vm);
    }

    public static async Task OpenCloseBottomGallery(MainViewModel vm)
    {
        if (vm is null)
        {
            return;
        }
        UIHelper.CloseMenus(vm);
        
        if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
        {
            SettingsHelper.Settings.Gallery.IsBottomGalleryShown = false;
            IsFullGalleryOpen = false;
            vm.IsGalleryOpen = false;
            IsBottomGalleryOpen = false;
            vm.GalleryMode = GalleryMode.BottomToClosed;
            vm.GetBottomGallery = TranslationHelper.GetTranslation("ShowBottomGallery");
            await SettingsHelper.SaveSettingsAsync();
            return;
        }

        IsBottomGalleryOpen = true;
        IsFullGalleryOpen = false;
        vm.IsGalleryOpen = true;
        SettingsHelper.Settings.Gallery.IsBottomGalleryShown = true;
        vm.GalleryMode = GalleryMode.ClosedToBottom;
        vm.GetBottomGallery = TranslationHelper.GetTranslation("HideBottomGallery");
        await SettingsHelper.SaveSettingsAsync();
        if (!NavigationHelper.CanNavigate(vm))
        {
            return;
        }
        await Task.Run(() => GalleryLoad.LoadGallery(vm, Path.GetDirectoryName(vm.ImageIterator.Pics[0])));
    }

    public static void OpenBottomGallery(MainViewModel vm)
    {
        IsBottomGalleryOpen = true;
        vm.GalleryMode = GalleryMode.ClosedToBottom;
        vm.GalleryVerticalAlignment = VerticalAlignment.Bottom;
    }
    
    public static async Task CloseGallery(MainViewModel vm)
    {
        if (IsBottomGalleryOpen && !IsFullGalleryOpen)
        {
            SettingsHelper.Settings.Gallery.IsBottomGalleryShown = false;
            IsFullGalleryOpen = false;
            vm.IsGalleryOpen = false;
            IsBottomGalleryOpen = false;
            vm.GalleryMode = GalleryMode.BottomToClosed;
            vm.GetBottomGallery = TranslationHelper.GetTranslation("ShowBottomGallery");
            await SettingsHelper.SaveSettingsAsync();
            return;
        }

        await ToggleGallery(vm);
    }
}
