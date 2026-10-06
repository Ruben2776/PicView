using PicView.Core.ViewModels;

namespace PicView.Core.IPlatform;

public interface IPasteClipboard
{
    ValueTask<bool> PasteAsync(MainWindowViewModel vm);
    ValueTask<bool> PasteFilesAsync(IReadOnlyList<string> filePaths, MainWindowViewModel vm);
    ValueTask<bool> PasteTextAsync(string text, MainWindowViewModel vm);
    ValueTask<bool> PasteImageAsync(MainWindowViewModel vm);
}
