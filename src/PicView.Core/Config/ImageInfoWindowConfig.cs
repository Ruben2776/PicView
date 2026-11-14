namespace PicView.Core.Config;

public sealed class ImageInfoWindowConfig()
    : RememberedWindowConfig<ImageInfoWindowConfig.ImageInfoWindowProperties>("ImageInfoWindow.json")
{
    public class ImageInfoWindowProperties : RememberedWindowProperties
    {
        public ImageInfoWindowProperties()
        {
            Width = 850;
            Height = 495;
        }
    }
}
