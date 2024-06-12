using System.Windows.Media;
using PicView.Core.Config;
using PicView.WPF.ChangeImage;
using PicView.WPF.UILogic;
using PicView.WPF.Views.UserControls.Gallery;

namespace PicView.WPF.PicGallery;

internal static class GalleryStretch
{
    internal static void SetStretchMode()
    {
        if (Navigation.Pics?.Count < 0 || UC.GetPicGallery is null)
        {
            return;
        }

        var isBottomGallery = !GalleryFunctions.IsGalleryOpen;
        
        bool isSquare, isFillSquare;
        string stretchMode;
        if (isBottomGallery)
        {
            isSquare = SettingsHelper.Settings.Gallery.BottomGalleryStretchMode.Equals("Square",
                StringComparison.OrdinalIgnoreCase);
            isFillSquare = SettingsHelper.Settings.Gallery.BottomGalleryStretchMode.Equals("FillSquare",
                StringComparison.OrdinalIgnoreCase);
            stretchMode = SettingsHelper.Settings.Gallery.BottomGalleryStretchMode;
        }
        else
        {
            isSquare = SettingsHelper.Settings.Gallery.FullGalleryStretchMode.Equals("Square",
                StringComparison.OrdinalIgnoreCase);
            isFillSquare = SettingsHelper.Settings.Gallery.FullGalleryStretchMode.Equals("FillSquare",
                StringComparison.OrdinalIgnoreCase);
            stretchMode = SettingsHelper.Settings.Gallery.FullGalleryStretchMode;
        }
        
        var itemSize = isBottomGallery ? GalleryNavigation.PicGalleryItemSize : GalleryNavigation.PicGalleryItemSizeS;
        
        for (var i = 0; i < UC.GetPicGallery?.Container.Children.Count; i++)
        {
            var item = (PicGalleryItem) UC.GetPicGallery.Container.Children[i];
            if (isSquare)
            {
                item.ThumbImage.Stretch = Stretch.Uniform;
                item.InnerBorder.Height = item.InnerBorder.Width = itemSize;
                item.OuterBorder.Height = item.OuterBorder.Width = itemSize;
            }
            else if (isFillSquare)
            {
                item.ThumbImage.Stretch = Stretch.Fill;
                item.InnerBorder.Height = item.InnerBorder.Width = itemSize;
                item.OuterBorder.Height = item.OuterBorder.Width = GalleryNavigation.PicGalleryItemSize;
            }
            else if (Enum.TryParse<Stretch>(stretchMode, out var stretch))
            {
                item.ThumbImage.Stretch = stretch;
                item.InnerBorder.Height = itemSize;
                item.OuterBorder.Height = itemSize;
                item.InnerBorder.Width = double.NaN;
                item.OuterBorder.Width = double.NaN;
            }
        }
    }
    
    internal static void DetermineStretchMode()
    {
        if (ConfigureWindows.GetSettingsWindow is null)
        {
            return;
        }

        ConfigureWindows.GetSettingsWindow.GalleryStretchBoxUniform.IsSelected = false;
        ConfigureWindows.GetSettingsWindow.GalleryStretchBoxUniformToFill.IsSelected = false;
        ConfigureWindows.GetSettingsWindow.GalleryStretchBoxFill.IsSelected = false;
        ConfigureWindows.GetSettingsWindow.GalleryStretchBoxNone.IsSelected = false;

        ConfigureWindows.GetSettingsWindow.GalleryStretchBoxFillSquare.IsSelected =
            SettingsHelper.Settings.Gallery.FullGalleryStretchMode.Equals("FillSquare",
                StringComparison.OrdinalIgnoreCase);
        ConfigureWindows.GetSettingsWindow.GalleryStretchBoxSquare.IsSelected =
            SettingsHelper.Settings.Gallery.FullGalleryStretchMode.Equals("Square",
                StringComparison.OrdinalIgnoreCase);
        
        ConfigureWindows.GetSettingsWindow.BottomGalleryStretchBoxUniform.IsSelected = false;
        ConfigureWindows.GetSettingsWindow.BottomGalleryStretchBoxUniformToFill.IsSelected = false;
        ConfigureWindows.GetSettingsWindow.BottomGalleryStretchBoxFill.IsSelected = false;
        ConfigureWindows.GetSettingsWindow.BottomGalleryStretchBoxNone.IsSelected = false;
        
        ConfigureWindows.GetSettingsWindow.BottomGalleryStretchBoxFillSquare.IsSelected =
            SettingsHelper.Settings.Gallery.BottomGalleryStretchMode.Equals("FillSquare",
                StringComparison.OrdinalIgnoreCase);
        ConfigureWindows.GetSettingsWindow.BottomGalleryStretchBoxSquare.IsSelected =
            SettingsHelper.Settings.Gallery.BottomGalleryStretchMode.Equals("Square",
                StringComparison.OrdinalIgnoreCase);


        if (Enum.TryParse<Stretch>(SettingsHelper.Settings.Gallery.FullGalleryStretchMode, out var fullStretch))
        {
            switch (fullStretch)
            {
                case Stretch.Uniform:
                    ConfigureWindows.GetSettingsWindow.GalleryStretchBoxUniform.IsSelected = true;
                    break;
                case Stretch.UniformToFill:
                    ConfigureWindows.GetSettingsWindow.GalleryStretchBoxUniformToFill.IsSelected = true;
                    break;
                case Stretch.Fill:
                    ConfigureWindows.GetSettingsWindow.GalleryStretchBoxFill.IsSelected = true;
                    break;
                case Stretch.None:
                    ConfigureWindows.GetSettingsWindow.GalleryStretchBoxNone.IsSelected = true;
                    break;
                default:
                    ConfigureWindows.GetSettingsWindow.GalleryStretchBoxUniform.IsSelected = true;
                    break;
            }
        }

        if (Enum.TryParse<Stretch>(SettingsHelper.Settings.Gallery.BottomGalleryStretchMode, out var bottomStretch))
        {
            switch (bottomStretch)
            {
                case Stretch.Uniform:
                    ConfigureWindows.GetSettingsWindow.BottomGalleryStretchBoxUniform.IsSelected = true;
                    break;
                case Stretch.UniformToFill:
                    ConfigureWindows.GetSettingsWindow.BottomGalleryStretchBoxUniformToFill.IsSelected = true;
                    break;
                case Stretch.Fill:
                    ConfigureWindows.GetSettingsWindow.BottomGalleryStretchBoxFill.IsSelected = true;
                    break;
                case Stretch.None:
                    ConfigureWindows.GetSettingsWindow.BottomGalleryStretchBoxNone.IsSelected = true;
                    break;
                default:
                    ConfigureWindows.GetSettingsWindow.BottomGalleryStretchBoxUniform.IsSelected = true;
                    break;
            }
        }
    }
    
}