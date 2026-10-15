namespace PicView.Core.IPlatform;

public interface ICropService
{
    bool IsCropping { get; }
    Task StartCropControlAsync();
    /// <summary>
    /// Asks where to save the cropped image, then saves it there.
    /// </summary>
    Task SaveCropAsAsync();
    ValueTask CopyCroppedImageAsync();
    void CloseCropControl();
    
    object? GetCroppedImage();
    
}
