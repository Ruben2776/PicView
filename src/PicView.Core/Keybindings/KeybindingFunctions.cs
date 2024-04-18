﻿namespace PicView.Core.Keybindings;

public static class KeybindingFunctions
{
    public static async Task SaveKeyBindingsFile(string json)
    {
        try
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/keybindings.json");
            await using var writer = new StreamWriter(path);
            await writer.WriteAsync(json).ConfigureAwait(false);
        }
        catch (Exception)
        {
            var newPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ruben2776/PicView/Config/keybindings.json");
            if (!File.Exists(newPath))
            {
                var fileInfo = new FileInfo(newPath);
                fileInfo.Directory?.Create();
            }
            await using var newWriter = new StreamWriter(newPath);
            await newWriter.WriteAsync(json).ConfigureAwait(false);
        }
    }

    public static async Task<string> LoadKeyBindingsFile()
    {
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/keybindings.json");
        if (File.Exists(path))
        {
            var text = await File.ReadAllTextAsync(path).ConfigureAwait(false);
            return text;
        }

        var newPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ruben2776/PicView/Config/keybindings.json");
        if (File.Exists(newPath))
        {
            var text = await File.ReadAllTextAsync(newPath).ConfigureAwait(false);
            return text;
        }

        throw new FileNotFoundException();
    }
}