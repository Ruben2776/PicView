using PicView.Properties;
using System.Globalization;
using System.Windows;

namespace PicView.Translations;

internal static class LoadLanguage
{
    internal static void DetermineLanguage(bool check)
    {
        if (check)
        {
            TrySetSource(new Uri(@"/PicView;component/Translations/" + Settings.Default.UserLanguage + ".xaml", UriKind.Relative));
            return;
        }

        var source = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName switch
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