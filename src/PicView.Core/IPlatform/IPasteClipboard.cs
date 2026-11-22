using PicView.Core.ViewModels;

namespace PicView.Core.IPlatform;

public interface IPasteClipboard
{
    ValueTask<bool> PasteAsync(MainWindowViewModel vm);
    /// <param name="tab">The tab the first file opens in; the active one when null.</param>
    ValueTask<bool> PasteFilesAsync(IReadOnlyList<string> filePaths, MainWindowViewModel vm, TabViewModel? tab = null);

    /// <param name="tab">The tab it shows in; the active one when null.</param>
    ValueTask<bool> PasteTextAsync(string text, MainWindowViewModel vm, TabViewModel? tab = null);

    /// <param name="tab">The tab it shows in; the active one when null.</param>
    ValueTask<bool> PasteImageAsync(MainWindowViewModel vm, TabViewModel? tab = null);
}
