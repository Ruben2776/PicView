using Avalonia.Media;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;

namespace PicView.Avalonia.Gallery;
public static class GalleryStretchMode
{
    public static void DetermineStretchMode(MainViewModel vm)
    {
        // Reset all boolean properties
        vm.IsUniformMenuChecked = false;
        vm.IsUniformBottomChecked = false;
        vm.IsUniformFullChecked = false;
        
        vm.IsUniformToFillMenuChecked = false;
        vm.IsUniformToFillBottomChecked = false;
        vm.IsUniformToFillFullChecked = false;
        
        vm.IsFillMenuChecked = false;
        vm.IsFillBottomChecked = false;
        vm.IsFillFullChecked = false;
        
        vm.IsNoneMenuChecked = false;
        vm.IsNoneBottomChecked = false;
        vm.IsNoneFullChecked = false;
        
        vm.IsSquareMenuChecked = false;
        vm.IsSquareBottomChecked = false;
        vm.IsSquareFullChecked = false;
        
        vm.IsFillSquareMenuChecked = false;
        vm.IsFillSquareBottomChecked = false;
        vm.IsFillSquareFullChecked = false;

        if (SettingsHelper.Settings.Gallery.FullGalleryStretchMode.Equals("Square", StringComparison.OrdinalIgnoreCase))
        {
            vm.GetGalleryItemWidth = vm.GetGalleryItemHeight;
            if (GalleryFunctions.IsFullGalleryOpen)
            {
                vm.IsSquareMenuChecked = true;
            }
            vm.IsSquareFullChecked = true;
        }
        else if (SettingsHelper.Settings.Gallery.FullGalleryStretchMode.Equals("FillSquare", StringComparison.OrdinalIgnoreCase))
        {
            vm.GetGalleryItemWidth = vm.GetGalleryItemHeight;
            if (GalleryFunctions.IsFullGalleryOpen)
            {
                vm.IsFillSquareMenuChecked = true;
            }
            vm.IsFillSquareFullChecked = true;
        }
        else if (Enum.TryParse<Stretch>(SettingsHelper.Settings.Gallery.FullGalleryStretchMode, out var stretchMode))
        {
            vm.GetGalleryItemWidth = double.NaN;
            SetStretchIsChecked(stretchMode);
        }
        else
        {
            vm.GetGalleryItemWidth = double.NaN;
            if (GalleryFunctions.IsFullGalleryOpen)
            {
                vm.IsUniformMenuChecked = true;
            }
            vm.IsUniformFullChecked = true;
        }
        

        if (SettingsHelper.Settings.Gallery.BottomGalleryStretchMode.Equals("Square", StringComparison.OrdinalIgnoreCase))
        {
            vm.GetGalleryItemWidth = vm.GetGalleryItemHeight;
            if (!GalleryFunctions.IsFullGalleryOpen)
            {
                vm.IsSquareMenuChecked = true;
            }
        }
        else if (SettingsHelper.Settings.Gallery.BottomGalleryStretchMode.Equals("FillSquare", StringComparison.OrdinalIgnoreCase))
        {
            vm.GetGalleryItemWidth = vm.GetGalleryItemHeight;
            if (!GalleryFunctions.IsFullGalleryOpen)
            {
                vm.IsFillSquareMenuChecked = true;
            }
        }
        else if (Enum.TryParse<Stretch>(SettingsHelper.Settings.Gallery.BottomGalleryStretchMode, out var stretchMode))
        {
            vm.GetGalleryItemWidth = double.NaN;
            SetStretchIsChecked(stretchMode);
        }
        else
        {
            vm.GetGalleryItemWidth = double.NaN;
            if (!GalleryFunctions.IsFullGalleryOpen)
            {
                vm.IsUniformMenuChecked = true;
            }
            vm.IsUniformBottomChecked = true;
        }
    
        
        return;

        void SetStretchIsChecked(Stretch stretchMode)
        {
            switch (stretchMode)
            {
                case Stretch.Uniform:
                    
                    if (GalleryFunctions.IsFullGalleryOpen)
                    {
                        vm.IsUniformFullChecked = true;
                    }
                    else
                    {
                        vm.IsUniformBottomChecked = true;
                        vm.IsUniformMenuChecked = true;
                    }
                    break;
                case Stretch.UniformToFill:
                    
                    if (GalleryFunctions.IsFullGalleryOpen)
                    {
                        vm.IsUniformToFillFullChecked = true;
                    }
                    else
                    {
                        vm.IsUniformToFillBottomChecked = true;
                        vm.IsUniformToFillMenuChecked = true;
                    }
                    break;
                case Stretch.Fill:
                    
                    if (GalleryFunctions.IsFullGalleryOpen)
                    {
                        vm.IsFillFullChecked = true;
                    }
                    else
                    {
                        vm.IsFillBottomChecked = true;
                        vm.IsFillMenuChecked = true;
                    }
                    break;
                case Stretch.None:
                    
                    if (GalleryFunctions.IsFullGalleryOpen)
                    {
                        vm.IsNoneFullChecked = true;
                    }
                    else
                    {
                        vm.IsNoneBottomChecked = true;
                        vm.IsNoneMenuChecked = true;
                    }
                    break;
                default:
                    if (!GalleryFunctions.IsFullGalleryOpen)
                    {
                        vm.IsUniformMenuChecked = true;
                    }
                    vm.IsUniformFullChecked = true;
                    vm.IsUniformBottomChecked = true;
                    break;
            }
        }
    }
    
    public static void SetGalleryStretch(MainViewModel vm, Stretch stretch)
    {
        vm.GetGalleryItemWidth = double.NaN;
        vm.GalleryStretch = stretch;
    }

    public static void SetSquareStretch(MainViewModel vm)
    {
        vm.GetGalleryItemWidth = vm.GetGalleryItemHeight;
        vm.GalleryStretch = Stretch.Uniform;
    }
    
    public static void SetSquareFillStretch(MainViewModel vm)
    {
        vm.GetGalleryItemWidth = vm.GetGalleryItemHeight;
        vm.GalleryStretch = Stretch.Fill;
    }

    public static async Task ChangeBottomGalleryItemStretch(MainViewModel vm, Stretch stretch)
    {
        SetGalleryStretch(vm, stretch);
        
        SettingsHelper.Settings.Gallery.BottomGalleryStretchMode = stretch.ToString();
        await SettingsHelper.SaveSettingsAsync();
    }
    
    public static async Task ChangeFullGalleryItemStretch(MainViewModel vm, Stretch stretch)
    {
        SetGalleryStretch(vm, stretch);
        
        SettingsHelper.Settings.Gallery.FullGalleryStretchMode = stretch.ToString();
        await SettingsHelper.SaveSettingsAsync();
    }
    
    public static async Task ChangeBottomGalleryStretchSquare(MainViewModel vm)
    {
        SetSquareStretch(vm);
        
        SettingsHelper.Settings.Gallery.BottomGalleryStretchMode = "Square";
        await SettingsHelper.SaveSettingsAsync();
    }
    
    public static async Task ChangeBottomGalleryStretchSquareFill(MainViewModel vm)
    {
        SetSquareFillStretch(vm);
        
        SettingsHelper.Settings.Gallery.BottomGalleryStretchMode = "FillSquare";
        await SettingsHelper.SaveSettingsAsync();
    }

    public static async Task ChangeFullGalleryStretchSquare(MainViewModel vm)
    {
        SetSquareStretch(vm);
        
        SettingsHelper.Settings.Gallery.FullGalleryStretchMode = "Square";
        await SettingsHelper.SaveSettingsAsync();
    }
    
    public static async Task ChangeFullGalleryStretchSquareFill(MainViewModel vm)
    {
        SetSquareFillStretch(vm);
        
        SettingsHelper.Settings.Gallery.FullGalleryStretchMode = "FillSquare";
        await SettingsHelper.SaveSettingsAsync();
    }
}
