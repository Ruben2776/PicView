using Avalonia.Input.Platform;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Navigation;
using PicView.Core.DebugTools;
using PicView.Core.FileHandling;
using PicView.Core.ImageDecoding;
using PicView.Core.IPlatform;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.Svg;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Clipboard;

public class ClipboardPasteService(
    IClipboard? clipboard,
    MainWindow? mainWindow = null,
    CoreViewModel? core = null) : IPasteClipboard
{
    private const char ByteOrderMark = (char)0xFEFF;

    public async ValueTask<bool> PasteAsync(MainWindowViewModel vm)
    {
        if (clipboard is null)
        {
            return false;
        }

        var tabs = vm.WindowTabs;
        var tab = tabs.ActiveTab.CurrentValue;
        var ticket = tab.SetLoading();

        var pasted = false;
        try
        {
            // 1. Try to paste files
            var files = await clipboard.TryGetFilesAsync().ConfigureAwait(false);
            if (files is { Length: > 0 })
            {
                var paths = new List<string>(files.Length);
                foreach (var file in files)
                {
                    paths.Add(file.Path.LocalPath);
                    file.Dispose();
                }

                pasted = await PasteFilesAsync(paths, vm).ConfigureAwait(false);
                return pasted;
            }

            // 2. Try to paste text (URLs, file paths, base64, SVG code)
            var text = await clipboard.TryGetTextAsync().ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(text))
            {
                if (await PasteTextAsync(text, vm).ConfigureAwait(false))
                {
                    pasted = true;
                    return true;
                }
            }

            // 3. Try to paste image data
            pasted = await PasteImageAsync(vm).ConfigureAwait(false);
            return pasted;
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(ClipboardPasteService), nameof(PasteAsync), ex);
            return false;
        }
        finally
        {
            if (!pasted)
            {
                // Nothing was shown, so the titles stop saying Loading
                tab.EndLoading(ticket);
            }
        }
    }

    public async ValueTask<bool> PasteFilesAsync(IReadOnlyList<string> filePaths, MainWindowViewModel vm)
    {
        if (filePaths.Count is 0 || mainWindow is null)
        {
            return false;
        }

        var activeCore = core ?? vm.Core;
        if (activeCore is null)
        {
            return false;
        }

        var firstItem = filePaths[0];

        await SourceOpener.OpenAsync(mainWindow, activeCore, firstItem).ConfigureAwait(false);

        if (vm.WindowTabs.ActiveTab.CurrentValue.Gallery.IsDocked.Value)
        {
            return true;
        }

        // Open consecutive files in new tabs
        foreach (var path in filePaths.Skip(1))
        {
            var tab = vm.WindowTabs.CreateTab();
            _ = Task.Run(() => SourceOpener.OpenAsync(mainWindow, activeCore, path, tab: tab).AsTask());
        }

        return true;
    }

    public async ValueTask<bool> PasteTextAsync(string text, MainWindowViewModel vm)
    {
        if (string.IsNullOrWhiteSpace(text) || mainWindow == null)
        {
            return false;
        }

        var tab = vm.WindowTabs.ActiveTab.CurrentValue;

        if (Base64Decoder.IsBase64String(text, out var base64))
        {
            await UpdateImage.SetSingeBase64ImageAsync(base64, vm, tab.GetTabCancellation().Token).ConfigureAwait(false);
            return true;
        }

        if (await PasteSvgCodeAsync(text, vm, tab).ConfigureAwait(false))
        {
            return true;
        }

        var activeCore = core ?? vm.Core;
        if (activeCore is null)
        {
            return false;
        }

        return (await SourceOpener.OpenAsync(mainWindow, activeCore, text).ConfigureAwait(false)).IsLoaded;
    }

    /// <summary>
    /// Shows <paramref name="text"/> as an image when it is SVG code that can be drawn. The code goes through a
    /// temp file, so it is read and drawn exactly as an SVG file is, and shows as a Single Image.
    /// </summary>
    private static async ValueTask<bool> PasteSvgCodeAsync(string text, MainWindowViewModel vm, TabViewModel tab)
    {
        if (!SvgFileReader.LooksLikeSvg(text))
        {
            return false;
        }

        try
        {
            // Deleted with the other temp files when the app closes
            var path = TempFileManager.GetNewTempFilePath("clipboard.svg");
            // An XML declaration must be the very first thing in the file, and copied text often starts with a
            // line break or a byte order mark
            var code = WithoutXmlDeclaration(text.Trim().TrimStart(ByteOrderMark).TrimStart());
            await File.WriteAllTextAsync(path, code).ConfigureAwait(false);

            // Into the tab the paste started in, which may no longer be the active one when the file has loaded
            return await TempFileSingleImage
                .ShowAsync(vm, new FileInfo(path), TranslationManager.Translation?.SvgImage ?? "SVG", tab)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(ClipboardPasteService), nameof(PasteSvgCodeAsync), ex);
            return false;
        }
    }

    /// <summary>
    /// The code without its leading XML declaration. The declaration names the encoding of the file the code was
    /// copied from, such as UTF-16, which is not the encoding the temp file is written in, and a file that
    /// contradicts its own declaration is rejected. The declaration is optional, and without it the file is UTF-8.
    /// </summary>
    internal static string WithoutXmlDeclaration(string code)
    {
        const string start = "<?xml";
        const string end = "?>";
        if (!code.StartsWith(start, StringComparison.OrdinalIgnoreCase) || code.Length == start.Length ||
            !char.IsWhiteSpace(code[start.Length]))
        {
            return code;
        }

        var endIndex = code.IndexOf(end, StringComparison.Ordinal);
        return endIndex < 0 ? code : code[(endIndex + end.Length)..].TrimStart();
    }

    public async ValueTask<bool> PasteImageAsync(MainWindowViewModel vm)
    {
        if (clipboard is null || mainWindow is null)
        {
            return false;
        }

        try
        {
            var bitmap = await clipboard.TryGetBitmapAsync().ConfigureAwait(false);
            if (bitmap is null)
            {
                return false;
            }

            UpdateImage.SetSingleImage(vm, bitmap, SingleImageType.Clipboard, TranslationManager.Translation?.ClipboardImage);
            return true;
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(ClipboardPasteService), nameof(PasteImageAsync), ex);
            return false;
        }
    }
}
