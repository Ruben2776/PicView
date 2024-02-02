using System.Diagnostics;

namespace PicView.Core.ProcessHandling;

public static class ProcessHelper
{
    /// <summary>
    /// Launches the URL provided in a web browser.
    /// </summary>
    /// <param name="url">The URL to be launched in a web browser.</param>
    public static void Hyperlink_RequestNavigate(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        var ps = new ProcessStartInfo(url)
        {
            UseShellExecute = true,
            Verb = "open"
        };
        Process.Start(ps);
    }

    /// <summary>
    /// Gets the path to the current process.
    /// </summary>
    /// <returns>The path to the current process.</returns>
    public static string? GetPathToProcess()
    {
        var GetAppPath = Environment.ProcessPath;

        if (GetAppPath is not null && Path.GetExtension(GetAppPath) == ".dll")
        {
            GetAppPath = GetAppPath.Replace(".dll", ".exe", StringComparison.InvariantCultureIgnoreCase);
        }

        return GetAppPath;
    }

    /// <summary>
    /// Restarts the current application.
    /// </summary>
    public static void RestartApp(string? args)
    {
        var GetAppPath = GetPathToProcess();

        Process.Start(new ProcessStartInfo(GetAppPath, args));
    }

    /// <summary>
    /// Starts a new instance of the current process with the provided file argument.
    /// </summary>
    /// <param name="argument">The file argument to be passed to the new instance of the current process.</param>
    public static void StartNewProcess(string argument)
    {
        var pathToExe = GetPathToProcess();

        // Sanitize file name
        var args = argument.Replace(@"\\", @"\");
        args = args.Insert(0, @"""");
        args = args.Insert(args.Length - 1, @"""");

        Process process = new()
        {
            StartInfo =
            {
                FileName = pathToExe,
                Arguments = args
            }
        };
        process.Start();
    }

    /// <summary>
    /// Starts a new instance of the current process.
    /// </summary>
    public static void StartNewProcess()
    {
        var pathToExe = GetPathToProcess();

        Process process = new()
        {
            StartInfo = { FileName = pathToExe }
        };
        process.Start();
    }

    public static bool CheckIfAnotherInstanceIsRunning()
    {
        try
        {
            // Get the current process name without the extension
            var currentProcessName = Process.GetCurrentProcess().ProcessName;

            // Check for other processes with the same name
            var processes = Process.GetProcessesByName(currentProcessName);

            // If there is more than one process, another instance is running
            return processes.Length > 1;
        }
        catch (Exception exception)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(CheckIfAnotherInstanceIsRunning)}: caught exception:\n{exception.Message}");
#endif
            return false;
        }
    }

    public static void OpenWith(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }
        using var process = new Process();
        process.StartInfo.FileName = "openwith";
        process.StartInfo.Arguments = $"\"{path}\"";
        process.StartInfo.ErrorDialog = true;

        process.Start();
    }

    public static void Print(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo(path)
        {
            Verb = "print",
            UseShellExecute = true,
        };
        process.Start();
    }
}