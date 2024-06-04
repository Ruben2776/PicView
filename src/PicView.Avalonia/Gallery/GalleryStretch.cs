using Avalonia.Media;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;

namespace PicView.Avalonia.Gallery;
public static class GalleryStretchMode
{
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
                default:
                    vm.IsUniformChecked = true;
                    break;
            }
        }
    }
    
    public static void SetGalleryStretch(MainViewModel vm, Stretch stretch)
    {
        vm.GetGalleryItemWidth = double.NaN;
        vm.GalleryBottomItemStretch = stretch;
        vm.GalleryStretch = stretch;
    }

    public static void SetSquareStretch(MainViewModel vm)
    {
        vm.GetGalleryItemWidth = vm.GetGalleryItemHeight;
        vm.GalleryBottomItemStretch = Stretch.Uniform;
        vm.GalleryStretch = Stretch.Uniform;
    }
    
    public static void SetSquareFillStretch(MainViewModel vm)
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
}
