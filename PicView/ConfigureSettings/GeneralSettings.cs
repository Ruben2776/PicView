using PicView.Translations;
using System.Globalization;
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
    }
}
