namespace PicView.Core.Navigation;

public interface IImageLoading
{
    Task LoadPicFromBase64Async(string base64);
    
    Task LoadPicFromUrlAsync(string url);

    Task LoadPicFromArchiveAsync(string path);
    
    Task LoadPicFromDirectoryAsync(string folderName);
    
    Task LoadPicFromStringAsync(string path);
    
    Task LoadPicFromFileAsync(string fileName);
    
    Task LoadPicAtIndexAsync(int index);

    Task Unload();

}
