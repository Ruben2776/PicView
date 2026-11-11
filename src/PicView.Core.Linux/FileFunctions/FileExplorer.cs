using System.Diagnostics;
using PicView.Core.DebugTools;

namespace PicView.Core.Linux.FileFunctions;

public static class FileExplorer
{
    private const int DbusTimeoutMs = 5000;

    /// <summary>
    /// Opens the file manager with the file selected, through the freedesktop
    /// <c>org.freedesktop.FileManager1.ShowItems</c> D-Bus call (Nautilus, Dolphin, Nemo, Caja, Thunar...).
    /// Falls back to opening the containing folder with <c>xdg-open</c>.
    /// </summary>
    /// <remarks>Blocks until the file manager answers, so call it off the UI thread.</remarks>
    public static void OpenFolderAndSelectFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var fullPath = Path.GetFullPath(path);
        var uri = new Uri(fullPath).AbsoluteUri;

        if (TryShowItems(uri))
        {
            return;
        }

        var folder = Directory.Exists(fullPath) ? fullPath : Path.GetDirectoryName(fullPath);
        if (string.IsNullOrEmpty(folder))
        {
            return;
        }

        Run("xdg-open", [folder], waitForExit: false);
    }

    private static bool TryShowItems(string uri)
    {
        // dbus-send ships with dbus itself, gdbus with glib; one of them is on practically every desktop
        return Run("dbus-send",
               [
                   "--session", "--print-reply", $"--reply-timeout={DbusTimeoutMs}",
                   "--dest=org.freedesktop.FileManager1",
                   "/org/freedesktop/FileManager1",
                   "org.freedesktop.FileManager1.ShowItems",
                   $"array:string:{uri}",
                   "string:"
               ], waitForExit: true)
               || Run("gdbus",
               [
                   "call", "--session", "--timeout", $"{DbusTimeoutMs / 1000}",
                   "--dest", "org.freedesktop.FileManager1",
                   "--object-path", "/org/freedesktop/FileManager1",
                   "--method", "org.freedesktop.FileManager1.ShowItems",
                   $"['{uri.Replace("'", "%27", StringComparison.Ordinal)}']", ""
               ], waitForExit: true);
    }

    private static bool Run(string fileName, string[] args, bool waitForExit)
    {
        try
        {
            var startInfo = new ProcessStartInfo(fileName)
            {
                UseShellExecute = false,
                RedirectStandardOutput = waitForExit,
                RedirectStandardError = waitForExit
            };
            foreach (var arg in args)
            {
                startInfo.ArgumentList.Add(arg);
            }

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return false;
            }

            if (!waitForExit)
            {
                return true;
            }

            if (process.WaitForExit(DbusTimeoutMs + 1000))
            {
                return process.ExitCode is 0;
            }

            process.Kill();
            return false;

        }
        catch (Exception ex)
        {
            // Not installed, or no session bus: the caller falls through to the next way
            DebugHelper.LogDebug(nameof(FileExplorer), fileName, ex);
            return false;
        }
    }
}
