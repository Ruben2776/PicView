using PicView.ChangeImage;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace PicView.ProcessHandling
{
    internal static class ProcessLogic
    {
        internal static string? GetPathToProcess()
        {
            var GetAppPath = System.Environment.ProcessPath;

            if (Path.GetExtension(GetAppPath) == ".dll")
            {
                GetAppPath = GetAppPath?.Replace(".dll", ".exe", System.StringComparison.InvariantCultureIgnoreCase);
            }
            return GetAppPath;
        }

        internal static void RestartApp()
        {
            Properties.Settings.Default.Save();

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
    }
}
