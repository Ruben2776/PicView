using PicView.Core.ViewModels;

namespace PicView.Core.IPlatform;

public interface ICopyClipboard
{
    Task<bool> CopyTextAsync(string? text);
    Task<bool> CopyFileAsync(string? filePath);
    Task<bool> CutFileAsync(string? filePath);
    Task<bool> CopyImageAsync(object? image);
    Task<bool> CopyBase64Async(string? path);
    Task DuplicateFileAsync(string? sourcePath, string? currentActiveFilePath, MainWindowViewModel vm);
}