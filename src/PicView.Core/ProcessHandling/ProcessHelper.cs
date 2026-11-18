using System.Diagnostics;
using PicView.Core.DebugTools;

namespace PicView.Core.ProcessHandling;

/// <summary>
///     Provides helper methods for process-related operations.
/// </summary>
public static class ProcessHelper
{
    /// <summary>
    ///     Gets the path to the current process.
    /// </summary>
    /// <returns>The path to the current process.</returns>
    public static string? GetPathToProcess()
    {
        var getAppPath = Environment.ProcessPath;

        if (getAppPath is not null &&
            Path.GetExtension(getAppPath.AsSpan()).Equals(".dll", StringComparison.InvariantCultureIgnoreCase))
        {
            getAppPath = getAppPath.Replace(".dll", ".exe", StringComparison.InvariantCultureIgnoreCase);
        }

        return getAppPath;
    }

    /// <summary>
    /// Starts a new process with elevated permissions using the specified command-line arguments.
    /// </summary>
    /// <param name="arguments">The command-line arguments to pass to the process.</param>
    /// <returns>A task that represents the asynchronous operation. The result is true if the process was started successfully, otherwise false.</returns>
    public static async Task<bool> StartProcessWithElevatedPermissionAsync(string arguments)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = Process.GetCurrentProcess().MainModule?.FileName,
                Arguments = arguments,
                UseShellExecute = true,
                Verb = "runas"
            };

            process.Start();
            await process.WaitForExitAsync().ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            // User declined the UAC prompt or other error
            DebugHelper.LogDebug(nameof(ProcessHelper), nameof(StartProcessWithElevatedPermissionAsync), ex);
            return false;
        }
    }

    /// <summary>
    /// Starts the current application, in another process.
    /// </summary>
    /// <param name="args">The command line arguments to pass.</param>
    public static void StartCurrentAppWithArgs(string? args)
    {
        var getAppPath = GetPathToProcess();

        Process.Start(new ProcessStartInfo(getAppPath, args));
    }

    /// <summary>
    /// Starts a new PicView process.
    /// </summary>
    /// <param name="source">The Source it opens, or null for none.</param>
    /// <param name="ownProcess">Whether it runs its own App Start whatever else is running, as a restarted PicView
    /// does. Otherwise it hands its Source over to a PicView that is running, as any start does (ADR-0037).</param>
    public static void StartNewProcess(string? source = null, bool ownProcess = false)
    {
        using Process process = new();
        process.StartInfo = StartInfoFor(GetPathToProcess(), source, ownProcess);
        process.Start();
    }

    /// <summary>
    /// How <see cref="StartNewProcess"/> starts PicView. The Source is one argument exactly as given, whatever spaces,
    /// quotes or backslashes it has.
    /// </summary>
    internal static ProcessStartInfo StartInfoFor(string? pathToExe, string? source, bool ownProcess)
    {
        var info = new ProcessStartInfo { FileName = pathToExe };
        if (ownProcess)
        {
            info.ArgumentList.Add(SingleProcess.OwnProcessArgument);
        }

        if (source is not null)
        {
            info.ArgumentList.Add(source);
        }

        return info;
    }

    /// <summary>
    ///     Opens a file using the Windows "Open with" dialog, allowing the user to select a program to open the file.
    /// </summary>
    /// <param name="path">The path to the file to be opened.</param>
    public static void OpenWith(string path)
    {
        try
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
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(ProcessHelper), nameof(OpenWith), e);
        }
    }

    /// <summary>
    ///     Opens a URL or file path in the default associated application.
    /// </summary>
    /// <param name="link">The URL or file path to open.</param>
    public static void OpenLink(string link)
    {
        if (string.IsNullOrWhiteSpace(link))
        {
            return;
        }

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo(link)
        {
            UseShellExecute = true,
            Verb = "open"
        };
        process.Start();
    }
}