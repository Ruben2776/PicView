using PicView.Core.ViewModels;

namespace PicView.Core.IPlatform;

public interface ICopyClipboard
{
    Task<bool> CopyTextAsync(string? text);
    Task<bool> CopyFileAsync(string? filePath);
    Task<bool> CutFileAsync(string? filePath);
    Task<bool> CopyImageAsync(object? image);

    /// <summary>
    /// Copies the image as <paramref name="tab"/> shows it: its shown bitmap, which is the Effects Render when there is
    /// one, turned and flipped by the tab's Orientation.
    /// </summary>
    Task<bool> CopyImageAsync(TabViewModel tab);

    Task<bool> CopyBase64Async(string? path);
}
