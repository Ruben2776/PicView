using PicView.Properties;
using System.Globalization;
using System.Windows;

namespace PicView.Translations
{
    internal static class LoadLanguage
    {
        internal static void DetermineLanguage()
        {
            if (Settings.Default.UserLanguage != "en")
            {
                TrySetSource(new Uri(@"/PicView;component/Translations/" + Settings.Default.UserLanguage + ".xaml", UriKind.Relative));
                return;
            }

            Uri source;
            switch (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName)
            {
                case "da":
                    source = new Uri(@"/PicView;component/Translations/da.xaml", UriKind.Relative);
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

                case "zh_CN":
                    source = new Uri(@"/PicView;component/Translations/zh_CN.xaml", UriKind.Relative);
                    break;

                case "zh_TW":
                    source = new Uri(@"/PicView;component/Translations/zh_TW.xaml", UriKind.Relative);
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

                case "ro":
                    source = new Uri(@"/PicView;component/Translations/ro.xaml", UriKind.Relative);
                    break;

                default:
                    source = new Uri(@"/PicView;component/Translations/en.xaml", UriKind.Relative);
                    break;
            }
            TrySetSource(source);
        }

        private static void TrySetSource(Uri source)
        {
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
            Settings.Default.UserLanguage = choice.ToString();

            Settings.Default.Save();
        }
    }
}