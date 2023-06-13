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
        var source = isoLanguage switch
        {
            "da" => new Uri(@"/PicView;component/Translations/da.xaml", UriKind.Relative),
            "de" => new Uri(@"/PicView;component/Translations/de.xaml", UriKind.Relative),
            "es" => new Uri(@"/PicView;component/Translations/es.xaml", UriKind.Relative),
            "ko" => new Uri(@"/PicView;component/Translations/ko.xaml", UriKind.Relative),
            "zh_CN" => new Uri(@"/PicView;component/Translations/zh_CN.xaml", UriKind.Relative),
            "zh_TW" => new Uri(@"/PicView;component/Translations/zh_TW.xaml", UriKind.Relative),
            "pl" => new Uri(@"/PicView;component/Translations/pl.xaml", UriKind.Relative),
            "fr" => new Uri(@"/PicView;component/Translations/fr.xaml", UriKind.Relative),
            "it" => new Uri(@"/PicView;component/Translations/it.xaml", UriKind.Relative),
            "ru" => new Uri(@"/PicView;component/Translations/ru.xaml", UriKind.Relative),
            "ro" => new Uri(@"/PicView;component/Translations/ro.xaml", UriKind.Relative),
            _ => new Uri(@"/PicView;component/Translations/en.xaml", UriKind.Relative)
        };
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
        Settings.Default.UserLanguage = choice.ToString();

        Settings.Default.Save();
    }
}