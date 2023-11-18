using PicView.Properties;
using System.Globalization;
using System.Windows;

namespace PicView.Translations
{
    internal static class LoadLanguage
    {
        /// <summary>
        /// Determines the language to use for the application based on the user's culture or the user's preferred language setting.
        /// </summary>
        /// <param name="isFromCulture">If true, the language will be determined based on the user's culture. Otherwise, it will be determined based on the user's preferred language setting.</param>
        internal static void DetermineLanguage(bool isFromCulture)
        {
            var isoLanguage = isFromCulture
                ? CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                : Settings.Default.UserLanguage;
            Uri? source;
            switch (isoLanguage)
            {
                case "da":
                case "da-DK":
                    source = new Uri(@"/PicView;component/Translations/da.xaml", UriKind.Relative);
                    break;

                case "de":
                case "de-DE":
                case "de-CH":
                case "de-AT":
                case "de-LU":
                case "de-LI":
                    source = new Uri(@"/PicView;component/Translations/de.xaml", UriKind.Relative);
                    break;

                case "es":
                case "es-ES":
                case "es-GT":
                case "es-CR":
                case "es-MX":
                case "es-PA":
                case "es-DO":
                case "es-VE":
                case "es-CO":
                case "es-PE":
                case "es-AR":
                case "es-CL":
                case "es-EC":
                case "es-UY":
                case "es-PY":
                case "es-BO":
                case "es-HN":
                case "es-NI":
                case "es-PR":
                    source = new Uri(@"/PicView;component/Translations/es.xaml", UriKind.Relative);
                    break;

                case "ko":
                case "ko-KR":
                    source = new Uri(@"/PicView;component/Translations/ko.xaml", UriKind.Relative);
                    break;

                case "zh":
                case "zh-CN":
                    source = new Uri(@"/PicView;component/Translations/zh-CN.xaml", UriKind.Relative);
                    break;

                case "zh-TW":
                    source = new Uri(@"/PicView;component/Translations/zh-TW.xaml", UriKind.Relative);
                    break;

                case "pl":
                case "pl-PL":
                    source = new Uri(@"/PicView;component/Translations/pl.xaml", UriKind.Relative);
                    break;

                case "fr":
                case "fr-FR":
                    source = new Uri(@"/PicView;component/Translations/fr.xaml", UriKind.Relative);
                    break;

                case "it":
                case "it-IT":
                case "it-CH":
                    source = new Uri(@"/PicView;component/Translations/it.xaml", UriKind.Relative);
                    break;

                case "ru":
                case "ru-RU":
                    source = new Uri(@"/PicView;component/Translations/ru.xaml", UriKind.Relative);
                    break;

                case "ro":
                case "ro-RO":
                    source = new Uri(@"/PicView;component/Translations/ro.xaml", UriKind.Relative);
                    break;

                default:
                    source = new Uri(@"/PicView;component/Translations/en.xaml", UriKind.Relative);
                    break;
            }

            TrySetSource(source);
        }

        /// <summary>
        /// Tries to set the source of the application's resources to the specified URI. If an exception occurs, falls back to the English translation.
        /// </summary>
        /// <param name="source">The URI of the resource dictionary to use.</param>
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
            Settings.Default.UserLanguage = choice.ToString().Replace('_', '-');

            Settings.Default.Save();
        }
    }
}