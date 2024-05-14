using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;

namespace PicView.Avalonia.Helpers;
    public static class ClipboardHelper
    {
        public static async Task CopyTextToClipboard(string text)
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            {
                return;
            }
            await desktop.MainWindow.Clipboard.SetTextAsync(text);
        }

        public static async Task CopyFileToClipboard(string? file)
        {            
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            {
                return;
            }
            var dataObject = new DataObject();
            dataObject.Set(DataFormats.Files, new[] { file });
            await desktop.MainWindow.Clipboard.SetDataObjectAsync(dataObject);
            // Doesn't work, TODO figure out how to add a file to the clipboard
        }
    }
