namespace PicView.Core.IPlatform;

public interface ICropService
{
    bool IsCropping { get; }
    Task StartCropControlAsync();
    Task SaveCropAsync();
    ValueTask CopyCroppedImageAsync();
    void CloseCropControl();
    
    object? GetCroppedImage();
    
}
