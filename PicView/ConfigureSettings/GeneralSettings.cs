using PicView.ChangeImage;
using PicView.Translations;
using System.Diagnostics;
using System.IO;
using System.Security.Permissions;
using System.Windows;

namespace PicView.ConfigureSettings
{
    internal static class GeneralSettings
    {
        internal static void ChangeLanguage(int language)
        {
            var choice = (Languages)language;
            Properties.Settings.Default.UserLanguage = choice.ToString();
        }

        internal static void RestartApp()
        {
            Properties.Settings.Default.Save();

            var GetAppPath = Application.ResourceAssembly.Location;

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
        public static Process ElevateProcess(Process source)
        {
            using var target = new Process
            {
                StartInfo = source.StartInfo
            };
            target.StartInfo.FileName = source.MainModule.FileName;
            target.StartInfo.WorkingDirectory = Path.GetDirectoryName(source.MainModule.FileName);

            //Required for UAC to work
            target.StartInfo.UseShellExecute = true;
            target.StartInfo.Verb = "runas";

            try
            {
                if (!target.Start())
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }

            return target;
        }
    }
}