using PicView.ChangeImage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PicView.ProcessHandling
{
    internal static class ProcessLogic
    {
        internal static string GetPathToProcess()
        {
            var GetAppPath = System.Environment.ProcessPath;

            if (Path.GetExtension(GetAppPath) == ".dll")
            {
                GetAppPath = GetAppPath.Replace(".dll", ".exe", System.StringComparison.InvariantCultureIgnoreCase);
            }
            return GetAppPath;
        }

        internal static void RestartApp()
        {
            Properties.Settings.Default.Save();

            var GetAppPath = ProcessLogic.GetPathToProcess();

            string args;
            if (Navigation.Pics.Count > Navigation.FolderIndex)
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
            var pathToExe = ProcessLogic.GetPathToProcess();

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

#pragma warning disable SYSLIB0003 // Type or member is obsolete
        [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
#pragma warning restore SYSLIB0003 // Type or member is obsolete
        public static void ElevateProcess(string args)
        {
            var GetAppPath = System.Environment.ProcessPath;

            if (Path.GetExtension(GetAppPath) == ".dll")
            {
                GetAppPath = GetAppPath.Replace(".dll", ".exe", System.StringComparison.InvariantCultureIgnoreCase);
            }

            using var target = new Process
            {
                StartInfo = new ProcessStartInfo(GetAppPath, args)
            };

            //Required for UAC to work
            target.StartInfo.UseShellExecute = true;
            target.StartInfo.Verb = "runas";

            target.Start();
        }
    }
}
