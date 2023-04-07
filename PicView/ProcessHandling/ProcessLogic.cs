using PicView.ChangeImage;
using PicView.Properties;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace PicView.ProcessHandling
{
    /// <summary>
    /// Contains the logic related to processing.
    /// </summary>
    internal static class ProcessLogic
    {
        /// <summary>
        /// Launches the URL provided in a web browser.
        /// </summary>
        /// <param name="url">The URL to be launched in a web browser.</param>
        internal static void Hyperlink_RequestNavigate(string url)
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
        internal static string? GetPathToProcess()
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
        internal static void RestartApp()
        {
            Settings.Default.Save();

            var GetAppPath = GetPathToProcess();

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

            Process.Start(new ProcessStartInfo(GetAppPath, args));
            Application.Current.Shutdown();
        }

        /// <summary>
        /// Starts a new instance of the current process with the provided file argument.
        /// </summary>
        /// <param name="argument">The file argument to be passed to the new instance of the current process.</param>
        internal static void StartProcessWithFileArgument(string argument)
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
        internal static void StartNewProcess()
        {
            var pathToExe = GetPathToProcess();

            Process process = new()
            {
                StartInfo = { FileName = pathToExe }
            };
            process.Start();
        }
    }

}