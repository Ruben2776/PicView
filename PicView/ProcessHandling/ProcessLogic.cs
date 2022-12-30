using PicView.ChangeImage;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace PicView.ProcessHandling
{
    internal static class ProcessLogic
    {
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
        internal static string? GetPathToProcess()
        {
            var GetAppPath = Environment.ProcessPath;

            if (Path.GetExtension(GetAppPath) == ".dll")
            {
                GetAppPath = GetAppPath?.Replace(".dll", ".exe", StringComparison.InvariantCultureIgnoreCase);
            }
            return GetAppPath;
        }

        internal static void RestartApp()
        {
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

        internal static void StartNewProcess()
        {
            var pathToExe = GetPathToProcess();

            Process process = new()
            {
                StartInfo =
                        {
                            FileName = pathToExe,
                        }
            };
            process.Start();
        }
    }
}
