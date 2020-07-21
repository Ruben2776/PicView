using PicView.ChangeImage;
using PicView.Translations;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace PicView.ConfigureSettings
{
    internal static class GeneralSettings
    {
        internal static void ChangeLanguage(int language)
        {
            var choice = (Languages)language;
            Properties.Settings.Default.UserCulture = choice.ToString();
        }

        internal static void RestartApp()
        {
            Properties.Settings.Default.Save();

            var GetAppPath = Application.ResourceAssembly.Location;

            if (Path.GetExtension(GetAppPath) == ".dll")
            {
                GetAppPath = GetAppPath.Replace(".dll", ".exe", System.StringComparison.InvariantCultureIgnoreCase);
            }

            Process.Start(new ProcessStartInfo(GetAppPath, Navigation.Pics[Navigation.FolderIndex]));
            Application.Current.Shutdown();
        }
    }
}
