using ImageMagick;
using PicView.Avalonia.ImageHandling;
using PicView.Core.Gallery;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Navigation.Services;

public class AvaloniaThumbnailLoader : IThumbnailLoader
{
    public async ValueTask<object?> GetThumbnailAsync(FileInfo file, CoreViewModel core)
    {
        var defaultItemHeight = core.GallerySettings.ItemHeight.Value > 0
            ? core.GallerySettings.ItemHeight.Value
            : GalleryDefaults.DefaultExpandedGalleryHeight;
        
        return await GetThumbnails.GetThumbAsync(file, (uint)defaultItemHeight, core).ConfigureAwait(false);
    }

    public async ValueTask<object?> GetThumbnailAsync(FileInfo file, uint size, CoreViewModel? core = null, MagickImage? magickImage = null)
    {
        return await GetThumbnails.GetThumbAsync(file, size, core, magickImage).ConfigureAwait(false);
    }

    public object? GetExifThumbnail(FileInfo file) =>
        GetThumbnails.GetExifThumb(file.FullName);

    public object? GetThumbQuick(FileInfo file, CoreViewModel? core = null) =>
        GetThumbnails.GetThumbQuick(file, core);
}
