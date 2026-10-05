using PicView.Avalonia.ImageHandling;
using PicView.Core.Models;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Navigation.Services;

public class AvaloniaImageModelLoader(CoreViewModel core) : IImageModelLoader
{
    public async ValueTask<ImageModel?> GetImageModelAsync(FileInfo file, CancellationToken ct) => 
        await GetImageModel.GetImageModelAsync(file, null, core, ct).ConfigureAwait(false);
    
    public async ValueTask<ImageModel?> GetBase64ImageAsync(FileInfo file, CancellationToken ct) => 
        await GetImageModel.GetBase64ImageModelAsync(file, ct).ConfigureAwait(false);
    
    public async ValueTask<ImageModel?> GetBase64ImageAsync(string base64String, CancellationToken ct) => 
        await GetImageModel.GetBase64ImageModelAsync(base64String, ct).ConfigureAwait(false);
}