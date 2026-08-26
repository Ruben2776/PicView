using Avalonia;
using ImageMagick;
using PicView.Avalonia.ImageHandling;
using PicView.Core.Gallery;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Navigation.Services;

public class AvaloniaThumbnailLoader : IThumbnailLoader
{
    public async ValueTask<object?> GetThumbnailAsync(FileInfo file)
    {
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return null;
        }

        var defaultItemHeight = core.GallerySettings.ItemHeight.Value > 0
            ? core.GallerySettings.ItemHeight.Value
            : GalleryDefaults.DefaultExpandedGalleryHeight;
        
        return await GetThumbnails.GetThumbAsync(file, (uint)defaultItemHeight).ConfigureAwait(false);
    }

    public async ValueTask<object?> GetThumbnailAsync(FileInfo file, uint size, MagickImage? magickImage = null)
    {
        return await GetThumbnails.GetThumbAsync(file, size, magickImage).ConfigureAwait(false);
    }

    public object? GetExifThumbnail(FileInfo file) =>
        GetThumbnails.GetExifThumb(file.FullName);

    public object? GetThumbQuick(FileInfo file) =>
        GetThumbnails.GetThumbQuick(file);
}
