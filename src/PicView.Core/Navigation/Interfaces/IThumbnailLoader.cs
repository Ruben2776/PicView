using ImageMagick;
using PicView.Core.ViewModels;

namespace PicView.Core.Navigation.Interfaces;

public interface IThumbnailLoader
{
    ValueTask<object?> GetThumbnailAsync(FileInfo file, CoreViewModel core);
    ValueTask<object?> GetThumbnailAsync(FileInfo file, uint size, CoreViewModel? core = null, MagickImage? magick = null);
    object? GetExifThumbnail(FileInfo file);
    object? GetThumbQuick(FileInfo file, CoreViewModel? core = null);
}
