using PicView.Avalonia.ImageHandling;

namespace PicView.Avalonia.Preloading;
public class PreLoadValue(ImageModel? imageModel)
{
    public ImageModel? ImageModel { get; set; } = imageModel;

    public bool IsLoading = true;
}