using System;
using System.Globalization;
using System.Windows;

namespace PicView.Translations
{
    internal static class LoadLanguage
    {
        internal static void DetermineLanguage()
        {
            if (Properties.Settings.Default.UserLanguage != "en")
            {
                Application.Current.Resources.MergedDictionaries[0] = new ResourceDictionary
                {
                    Source = new Uri(@"/PicView;component/Translations/" + Properties.Settings.Default.UserLanguage + ".xaml", UriKind.Relative)
                };
                return;
            }

            Uri source;
            switch (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName)
            {
                case "da":
                    source = new Uri(@"/PicView;component/Translations/de.xaml", UriKind.Relative);
                    break;

                case "de":
                    source = new Uri(@"/PicView;component/Translations/de.xaml", UriKind.Relative);
                    break;

                case "es":
                    source = new Uri(@"/PicView;component/Translations/es.xaml", UriKind.Relative);
                    break;

                case "ko":
                    source = new Uri(@"/PicView;component/Translations/es.xaml", UriKind.Relative);
                    break;

                case "zh":
                    source = new Uri(@"/PicView;component/Translations/zh_CN.xaml", UriKind.Relative);
                    break;

                case "pl":
                    source = new Uri(@"/PicView;component/Translations/pl.xaml", UriKind.Relative);
                    break;

                case "fr":
                    source = new Uri(@"/PicView;component/Translations/fr.xaml", UriKind.Relative);
                    break;

                case "it":
                    source = new Uri(@"/PicView;component/Translations/it.xaml", UriKind.Relative);
                    break;
                case "ru":
                    source = new Uri(@"/PicView;component/Translations/ru.xaml", UriKind.Relative);
                    break;
                default:
                case "en":
                    source = new Uri(@"/PicView;component/Translations/en.xaml", UriKind.Relative);
                    break;
            }
            try
            {
                Application.Current.Resources.MergedDictionaries[0] = new ResourceDictionary
                {
                    Source = source
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

            Properties.Settings.Default.Save();
        }
    }
}