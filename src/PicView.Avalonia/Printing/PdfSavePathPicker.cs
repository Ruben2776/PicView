using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using PicView.Avalonia.FileSystem;
using PicView.Core.Extensions;
using PicView.Core.Localization;

namespace PicView.Avalonia.Printing;

/// <summary>
/// Asks where to save a PDF, with the file picker of <paramref name="owner"/>: the Print window when Save as PDF is
/// sent from it, else the Main Window that asked.
/// </summary>
internal sealed class PdfSavePathPicker(TopLevel owner) : ISavePathPicker
{
    public async Task<string?> PickSavePathAsync(string? suggestedName, string? ext = null)
    {
        var file = await Dispatcher.UIThread.InvokeAsync(() => owner.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = StringExtensions.CombineWithAppName(TranslationManager.Translation.SaveAs),
                SuggestedFileName = suggestedName,
                FileTypeChoices = [new FilePickerFileType("PDF") { Patterns = ["*.pdf"] }]
            })).ConfigureAwait(false);
        return file?.Path.LocalPath;
    }
}
