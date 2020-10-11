using System;
using System.Globalization;
using System.Windows;

namespace PicView.Translations
{
    internal static class LoadLanguage
    {
        internal static void DetermineLanguage()
        {
            if (Properties.Settings.Default.CallUpgrade)
            {
                if (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName != "en")
                {
                    Load();
                }
            }

            else if (Properties.Settings.Default.UserLanguage != "en")
            {
                Load();
            }
        }

        internal static void Load()
        {
            try
            {
                Application.Current.Resources.MergedDictionaries[0] = new ResourceDictionary
                {
                    Source = new Uri(@"/PicView;component/Translations/" + Properties.Settings.Default.UserLanguage + ".xaml", UriKind.Relative)
                };
            }
            catch (Exception)
            {
                Application.Current.Resources.MergedDictionaries[0] = new ResourceDictionary
                {
                    Source = new Uri(@"/PicView;component/Translations/en.xaml", UriKind.Relative)
                };
            }
        }

        internal static void ChangeLanguage(int language)
        {
            var choice = (Languages)language;
            Properties.Settings.Default.UserLanguage = choice.ToString();
        }
    }
}
