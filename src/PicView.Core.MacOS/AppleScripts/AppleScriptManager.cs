using System.Diagnostics;
using PicView.Core.DebugTools;

namespace PicView.Core.MacOS.AppleScripts;

/// <summary>
///     Provides methods for executing AppleScript code asynchronously via temporary files and the <c>osascript</c>
///     command-line tool.
/// </summary>
public static class AppleScriptManager
{
    /// <summary>
    ///     Executes the specified AppleScript asynchronously and returns whether it succeeded.
    ///     <para>
    ///         Success is determined by the script's exit code and output. If the script outputs "true" or "1" on the last
    ///         line, it is considered successful.
    ///     </para>
    /// </summary>
    /// <param name="appleScript">The AppleScript code to execute.</param>
    /// <returns>
    ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The result is <c>true</c> if the script
    ///     succeeded, otherwise <c>false</c>.
    /// </returns>
    public static async Task<bool> ExecuteAppleScriptAsync(string appleScript)
    {
        var scriptPath = await WriteTempScriptAsync(appleScript).ConfigureAwait(false);
        try
        {
            var (exitCode, output, errors) = await RunAppleScriptProcessAsync(scriptPath, true).ConfigureAwait(false);

            if (exitCode != 0)
            {
                DebugHelper.LogDebug(nameof(AppleScriptManager), nameof(ExecuteAppleScriptAsync),
                    $"AppleScript execution failed with code {exitCode}");
                foreach (var error in errors)
                {
                    DebugHelper.LogDebug(nameof(AppleScriptManager), nameof(ExecuteAppleScriptAsync),
                        $"Error: {error}");
                }

                return false;
            }

            if (output.Count == 0)
            {
                return true;
            }

            var lastOutput = output.Last().Trim().ToLowerInvariant();
            return lastOutput is "true" or "1";
        }
        finally
        {
            DeleteTempScript(scriptPath);
        }
    }

    /// <summary>
    ///     Executes the specified AppleScript asynchronously and returns the script's output if it succeeded.
    /// </summary>
    /// <param name="appleScript">The AppleScript code to execute.</param>
    /// <returns>
    ///     A <see cref="Task{TResult}" /> representing the asynchronous operation.
    ///     The result is the output from the script if it succeeded; otherwise, <c>null</c>.
    /// </returns>
    public static async Task<string?> ExecuteAppleScriptWithResultAsync(string appleScript)
    {
        var scriptPath = await WriteTempScriptAsync(appleScript).ConfigureAwait(false);
        try
        {
            var (exitCode, output, _) = await RunAppleScriptProcessAsync(scriptPath, false).ConfigureAwait(false);

            return exitCode == 0 ? output.FirstOrDefault()?.Trim() : null;
        }
        finally
        {
            DeleteTempScript(scriptPath);
        }
    }

    /// <summary>
    ///     Writes the specified AppleScript code to a temporary file.
    /// </summary>
    /// <param name="appleScript">The AppleScript code to write.</param>
    /// <returns>
    ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The result is the path to the temporary
    ///     script file.
    /// </returns>
    private static async Task<string> WriteTempScriptAsync(string appleScript)
    {
        var scriptPath = Path.Combine(Path.GetTempPath(), $"picview_script_{Guid.NewGuid():N}.scpt");
        await File.WriteAllTextAsync(scriptPath, appleScript).ConfigureAwait(false);
        return scriptPath;
    }

    /// <summary>
    ///     Deletes the specified temporary script file, logging any errors encountered.
    /// </summary>
    /// <param name="scriptPath">The path to the temporary script file.</param>
    private static void DeleteTempScript(string scriptPath)
    {
        try
        {
            File.Delete(scriptPath);
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(AppleScriptManager), nameof(DeleteTempScript), ex);
        }
    }

    /// <summary>
    ///     Runs the AppleScript process for the given script file and returns its result.
    /// </summary>
    /// <param name="scriptPath">The path to the temporary AppleScript file.</param>
    /// <param name="captureOutput">
    ///     If <c>true</c>, captures both standard output and error as line collections; otherwise, reads standard output as a
    ///     single string.
    /// </param>
    /// <returns>
    ///     A <see cref="Task{TResult}" /> representing the asynchronous operation.
    ///     The result is a tuple containing the exit code, output lines, and error lines.
    /// </returns>
    private static async Task<(int exitCode, List<string> output, List<string> errors)> RunAppleScriptProcessAsync(
        string scriptPath, bool captureOutput)
    {
        var output = new List<string>();
        var errors = new List<string>();

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "osascript",
                Arguments = scriptPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        if (captureOutput)
        {
            process.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    output.Add(e.Data);
                }
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    errors.Add(e.Data);
                }
            };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }
        else
        {
            process.Start();
            var stdOutput = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(stdOutput))
            {
                output.Add(stdOutput);
            }
        }

        await process.WaitForExitAsync().ConfigureAwait(false);

        return (process.ExitCode, output, errors);
    }
}