using PicView.ChangeImage;
using System.Diagnostics;
using System.IO;
using System.Security.Permissions;
using System.Windows;

namespace PicView.ConfigureSettings
{
    internal static class GeneralSettings
    {
        internal static void RestartApp()
        {
            Properties.Settings.Default.Save();

            var GetAppPath = Process.GetCurrentProcess().MainModule.FileName;

            if (Path.GetExtension(GetAppPath) == ".dll")
            {
                GetAppPath = GetAppPath.Replace(".dll", ".exe", System.StringComparison.InvariantCultureIgnoreCase);
            }

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

        [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
        public static void ElevateProcess(string args)
        {
            var GetAppPath = Process.GetCurrentProcess().MainModule.FileName;

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