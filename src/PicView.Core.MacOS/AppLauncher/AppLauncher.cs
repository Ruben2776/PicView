using System.Diagnostics;

namespace PicView.Core.MacOS.AppLauncher;

public static class AppLauncher
{
    /// <summary>
    /// Launches the specified application with the given file
    /// </summary>
    /// <param name="appPath">Full path to the application</param>
    /// <param name="filePath">Path to the file to open</param>
    /// <returns>True if the app was launched successfully</returns>
    public static async Task<bool> LaunchAppWithFileAsync(string appPath, string filePath)
    {
        try
        {
            if (string.IsNullOrEmpty(appPath) || string.IsNullOrEmpty(filePath))
                return false;

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "open",
                    Arguments = $"-a \"{appPath}\" \"{filePath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();
            
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
#if DEBUG
            Console.WriteLine($"Error launching app {appPath} with file {filePath}: {ex.Message}");
#endif
            return false;
        }
    }
}