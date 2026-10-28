using PicView.Core.ViewModels;

namespace PicView.Core.Gallery;

public static class GallerySettingsConverter
{
    public static void UpdateDockPositionProperties(GalleryViewModel gallery)
    {
        var pos = Settings.Gallery.DockPosition;
        gallery.IsTopDocked.Value = pos == GalleryDockPosition.Top;
        gallery.IsBottomDocked.Value = pos == GalleryDockPosition.Bottom;
        gallery.IsLeftDocked.Value = pos == GalleryDockPosition.Left;
        gallery.IsRightDocked.Value = pos == GalleryDockPosition.Right;
    }

    public static void UpdateDockedGalleryStretchMode(GallerySharedSettingsViewModel gallerySettings, GalleryStretchMode mode)
    {
        switch (mode)
        {
            case GalleryStretchMode.Uniform:
                gallerySettings.IsDockedStretchUniform.Value = true;
                gallerySettings.IsDockedStretchUniformToFill.Value = false;
                gallerySettings.IsDockedStretchSquare.Value = false;
                gallerySettings.IsDockedStretchSquareFill.Value = false;
                break;
            case GalleryStretchMode.UniformToFill:
                gallerySettings.IsDockedStretchUniform.Value = false;
                gallerySettings.IsDockedStretchUniformToFill.Value = true;
                gallerySettings.IsDockedStretchSquare.Value = false;
                gallerySettings.IsDockedStretchSquareFill.Value = false;
                break;
            case GalleryStretchMode.Square:
                gallerySettings.IsDockedStretchUniform.Value = false;
                gallerySettings.IsDockedStretchUniformToFill.Value = false;
                gallerySettings.IsDockedStretchSquare.Value = true;
                gallerySettings.IsDockedStretchSquareFill.Value = false;
                break;
            case GalleryStretchMode.FillSquare:
                gallerySettings.IsDockedStretchUniform.Value = false;
                gallerySettings.IsDockedStretchUniformToFill.Value = false;
                gallerySettings.IsDockedStretchSquare.Value = false;
                gallerySettings.IsDockedStretchSquareFill.Value = true;
                break;
        }

        Settings.Gallery.DockedGalleryStretchMode = mode;
        gallerySettings.DockedGalleryStretchMode.Value = mode;
    }

    public static void UpdateExpandedGalleryStretchMode(GallerySharedSettingsViewModel gallerySettings, GalleryStretchMode mode)
    {
        switch (mode)
        {
            case GalleryStretchMode.Uniform:
                gallerySettings.IsExpandedStretchUniform.Value = true;
                gallerySettings.IsExpandedStretchUniformToFill.Value = false;
                gallerySettings.IsExpandedStretchSquare.Value = false;
                gallerySettings.IsExpandedStretchSquareFill.Value = false;
                break;
            case GalleryStretchMode.UniformToFill:
                gallerySettings.IsExpandedStretchUniform.Value = false;
                gallerySettings.IsExpandedStretchUniformToFill.Value = true;
                gallerySettings.IsExpandedStretchSquare.Value = false;
                gallerySettings.IsExpandedStretchSquareFill.Value = false;
                break;
            case GalleryStretchMode.Square:
                gallerySettings.IsExpandedStretchUniform.Value = false;
                gallerySettings.IsExpandedStretchUniformToFill.Value = false;
                gallerySettings.IsExpandedStretchSquare.Value = true;
                gallerySettings.IsExpandedStretchSquareFill.Value = false;
                break;
            case GalleryStretchMode.FillSquare:
                gallerySettings.IsExpandedStretchUniform.Value = false;
                gallerySettings.IsExpandedStretchUniformToFill.Value = false;
                gallerySettings.IsExpandedStretchSquare.Value = false;
                gallerySettings.IsExpandedStretchSquareFill.Value = true;
                break;
        }
        Settings.Gallery.ExpandedGalleryStretchMode = mode;
        gallerySettings.ExpandedGalleryStretchMode.Value = mode;
    }
}