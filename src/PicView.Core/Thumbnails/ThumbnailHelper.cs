using PicView.Core.ViewModels;

namespace PicView.Core.Thumbnails;

public static class ThumbnailHelper
{
    public static async Task<object?> GetThumbnailAsync(string file, MainWindowViewModel vm, uint size = 150)
    {
        var fileInfo = new FileInfo(file);
        if (!fileInfo.Exists)
        {
            return null;
        }
        return await GetThumbnailAsync(fileInfo, vm, size).ConfigureAwait(false);
    }
    
    public static async Task<object?> GetThumbnailAsync(FileInfo fileInfo, MainWindowViewModel vm, uint size = 150)
    {
        var tabs = vm.WindowTabs;
        var cache = tabs.SharedCache;
        var thumbnailLoader = tabs.SharedThumbnailLoader;
        if (cache.TryGet(fileInfo.FullName, out var cached) && cached is not null)
        {
            return cached.ImageModel.Image;
        }

        var thumb = await thumbnailLoader.GetThumbnailAsync(fileInfo, size)
            .ConfigureAwait(false);
        return thumb;
    }
}