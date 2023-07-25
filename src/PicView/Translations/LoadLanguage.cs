using PicView.Properties;
using System.Globalization;
using System.Windows;

namespace PicView.Translations;

internal static class LoadLanguage
{
    /// <summary>
    /// Determines the language to use for the application based on the user's culture or the user's preferred language setting.
    /// </summary>
    /// <param name="isFromCulture">If true, the language will be determined based on the user's culture. Otherwise, it will be determined based on the user's preferred language setting.</param>
    internal static void DetermineLanguage(bool isFromCulture)
    {
        var isoLanguage = isFromCulture ? CultureInfo.CurrentCulture.TwoLetterISOLanguageName : Settings.Default.UserLanguage;
        Uri? source;
        switch (isoLanguage)
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
                source = new Uri(@"/PicView;component/Translations/ko.xaml", UriKind.Relative);
                break;

            case "zh":
            case "zh-CN":
            case "zh_CN":
                source = new Uri(@"/PicView;component/Translations/zh-CN.xaml", UriKind.Relative);
                break;

            case "zh_TW":
            case "zh-TW":
                source = new Uri(@"/PicView;component/Translations/zh-TW.xaml", UriKind.Relative);
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