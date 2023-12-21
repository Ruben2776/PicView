using PicView.Core.Config;
using PicView.WPF.ChangeImage;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;

namespace PicView.WPF.ProcessHandling;

/// <summary>
/// Contains the logic related to processing.
/// </summary>
internal static class ProcessLogic
{
    /// <summary>
    /// Restarts the current application.
    /// </summary>
    internal static void RestartApp()
    {
        SettingsHelper.SaveSettingsAsync();

        string args;
        if (Navigation.Pics?.Count > Navigation.FolderIndex)
        {
            args = Navigation.Pics[Navigation.FolderIndex];

            // Add double qoutations to support file paths with spaces
            args = args.Insert(0, @"""");
            args = args.Insert(args.Length - 1, @"""");
        }
        else
        {
            args = string.Empty;
        }
        Core.ProcessHandling.ProcessHelper.RestartApp(args);
        Application.Current.Shutdown();
    }

    internal static bool RunElevated(string fileName, string args)
    {
        var processInfo = new ProcessStartInfo
        {
            Verb = "runas",
            UseShellExecute = true,
            FileName = fileName,
            Arguments = args,
            WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
        };
        try
        {
            Process.Start(processInfo);
            return true;
        }
        catch (Win32Exception)
        {
            // Do nothing. Probably the user canceled the UAC window
        }

        return false;
    }
}