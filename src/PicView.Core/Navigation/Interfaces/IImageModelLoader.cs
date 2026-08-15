using PicView.Core.Models;

namespace PicView.Core.Navigation.Interfaces;

public interface IImageModelLoader
{
    ValueTask<ImageModel> GetImageModelAsync(FileInfo file, CancellationToken ct);
}