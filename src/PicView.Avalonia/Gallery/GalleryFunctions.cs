using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using PicView.Avalonia.Helpers;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;
using PicView.Core.FileHandling;
using PicView.Core.Gallery;
using PicView.Core.Localization;

namespace PicView.Avalonia.Gallery
{
    public static class GalleryFunctions
    {
        public static bool IsFullGalleryOpen { get; private set; }
        public static bool IsBottomGalleryOpen { get; private set; }
        public static bool IsAnyGalleryOpen => IsFullGalleryOpen || IsBottomGalleryOpen;

        public static void RecycleItem(object sender, MainViewModel vm)
        {
            // if (vm.GalleryItems.Count <= vm.ImageIterator.Index)
            // {
            //     return;
            // }
            //
            // var galleryItem = vm.GalleryItems[vm.ImageIterator.Index];
            // FileDeletionHelper.DeleteFileWithErrorMsg(galleryItem.FileLocation, recycle: true);
            //
            // vm.GalleryItems.Remove(galleryItem); // TODO: rewrite file system watcher to delete gallery items
        }

        public static void OpenWithItem(object sender, MainViewModel vm)
        {
            // if (vm.GalleryItems.Count <= vm.ImageIterator.Index)
            // {
            //     return;
            // }
            //
            // var galleryItem = vm.GalleryItems[vm.ImageIterator.Index];
            // vm.PlatformService.OpenWith(galleryItem.FileLocation);
        }

        public static async Task ToggleGallery(MainViewModel vm)
        {
            if (vm is null)
            {
                return;
            }

            UIHelper.CloseMenus(vm);

            if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
            {
                if (IsFullGalleryOpen)
                {
                    OpenBottomGallery(vm);
                }
                else
                {
                    OpenFullGallery(vm);
                }
                
                //SetGalleryItemSize(vm);
                return;
            }
            
            if (IsFullGalleryOpen)
            {
                CloseGallery(vm);
            }
            else
            {
                if (!NavigationHelper.CanNavigate(vm))
                {
                    return;
                }
                _ = Task.Run(() => GalleryLoad.LoadGallery(vm, Path.GetDirectoryName(vm.ImageIterator.Pics[0])));
                OpenFullGallery(vm);
            }

            await SettingsHelper.SaveSettingsAsync();
        }

        public static async Task ToggleBottomGallery(MainViewModel vm)
        {
            if (vm is null)
            {
                return;
            }
            UIHelper.CloseMenus(vm);
            
            if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
            {
                OpenBottomGallery(vm);
                SetGalleryItemSize(vm);
            }
            else
            {
                SettingsHelper.Settings.Gallery.IsBottomGalleryShown = true;
                if (!NavigationHelper.CanNavigate(vm))
                {
                    return;
                }
                _ = Task.Run(() => GalleryLoad.LoadGallery(vm, Path.GetDirectoryName(vm.ImageIterator.Pics[0])));
            }
            
            await SettingsHelper.SaveSettingsAsync();
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
                CloseGallery(vm);
                SettingsHelper.Settings.Gallery.IsBottomGalleryShown = false;
                vm.GetBottomGallery = TranslationHelper.GetTranslation("ShowBottomGallery");
                await SettingsHelper.SaveSettingsAsync();
                return;
            }

            OpenBottomGallery(vm);
            SettingsHelper.Settings.Gallery.IsBottomGalleryShown = true;
            await SettingsHelper.SaveSettingsAsync();
            if (!NavigationHelper.CanNavigate(vm))
            {
                return;
            }
            await Task.Run(() => GalleryLoad.LoadGallery(vm, Path.GetDirectoryName(vm.ImageIterator.Pics[0])));
        }

        public static void OpenBottomGallery(MainViewModel vm)
        {
            vm.GalleryVerticalAlignment = VerticalAlignment.Bottom;
            vm.GalleryOrientation = Orientation.Horizontal;
            vm.IsGalleryCloseIconVisible = false;
            IsBottomGalleryOpen = true;
            IsFullGalleryOpen = false;
            vm.IsGalleryOpen = true;
            SettingsHelper.Settings.Gallery.IsBottomGalleryShown = true;
            WindowHelper.SetSize(vm);
            vm.GetBottomGallery = TranslationHelper.GetTranslation("HideBottomGallery");
            vm.GalleryStretch = Stretch.UniformToFill;
        }

        private static void OpenFullGallery(MainViewModel vm)
        {
            vm.GalleryVerticalAlignment = VerticalAlignment.Stretch;
            vm.GalleryOrientation = Orientation.Vertical;
            vm.IsGalleryCloseIconVisible = true;
            IsBottomGalleryOpen = false;
            IsFullGalleryOpen = true;
            vm.IsGalleryOpen = true;
            vm.GalleryStretch = Stretch.Uniform;
        }

        private static void CloseGallery(MainViewModel vm)
        {
            IsBottomGalleryOpen = false;
            IsFullGalleryOpen = false;
            vm.IsGalleryOpen = false;
            WindowHelper.SetSize(vm);
        }

        public static void SetGalleryItemSize(MainViewModel vm)
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            {
                return;
            }

            var screen = ScreenHelper.GetScreen(desktop.MainWindow);
            var size = Math.Min(SettingsHelper.Settings.Gallery.BottomGalleryItemSize,
                SettingsHelper.Settings.Gallery.ExpandedGalleryItemSize);
            vm.GalleryItemSize = screen.WorkingArea.Height / size;
        }
    }
}