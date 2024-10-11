using System.Globalization;
using Avalonia.Controls;
using Avalonia.Threading;
using PicView.Avalonia.SettingsManagement;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;
using PicView.Core.Localization;

namespace PicView.Avalonia.Views;

public partial class LanguageView : UserControl
{
    public LanguageView()
    {
        InitializeComponent();

        var languages = TranslationHelper.GetLanguages().OrderBy(x => x);
        foreach (var language in languages)
        {
            var lang = Path.GetFileNameWithoutExtension(language);
            var isSelected = lang.Length switch
            {
                >= 4 => lang[^2..] == SettingsHelper.Settings.UIProperties.UserLanguage[^2..],
                2 => lang[..2] == SettingsHelper.Settings.UIProperties.UserLanguage[..2],
                _ => lang == SettingsHelper.Settings.UIProperties.UserLanguage
            };

            var comboBoxItem = new ComboBoxItem
            {
                Content = new CultureInfo(lang).DisplayName,
                IsSelected = isSelected,
                Tag = lang
            };

            LanguageBox.Items.Add(comboBoxItem);
            if (isSelected)
            {
                LanguageBox.SelectedItem = comboBoxItem;
            }
        }

        LanguageBox.SelectionChanged += async delegate
        {
            if (LanguageBox.SelectedItem is not ComboBoxItem comboBoxItem)
            {
                return;
            }

            var language = Path.GetFileNameWithoutExtension(comboBoxItem.Tag as string ?? string.Empty);
            if (string.IsNullOrEmpty(language))
            {
                return;
            }

            if (language == SettingsHelper.Settings.UIProperties.UserLanguage)
            {
                return;
            }

            SettingsHelper.Settings.UIProperties.UserLanguage = language;

            await TranslationHelper.LoadLanguage(language).ConfigureAwait(false);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (DataContext is not MainViewModel vm)
                {
                    return;
                }

                LanguageUpdater.UpdateLanguage(vm);

                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel is not Window window)
                {
                    return;
                }

                window.Close();
            });

            await FunctionsHelper.SettingsWindow();
            await SettingsHelper.SaveSettingsAsync();
        };
        LanguageBox.DropDownOpened += delegate
        {
            if (LanguageBox.SelectedIndex != -1)
            {
                return;
            }

            // Find the ComboBoxItem whose Tag matches the two-letter or culture-specific language
            for (var i = 0; i < LanguageBox.Items.Count; i++)
            {
                if (LanguageBox.Items[i] is not ComboBoxItem { Tag: string tag })
                {
                    continue;
                }

                // Check if the selected language exactly matches, including culture
                if (tag.Equals(SettingsHelper.Settings.UIProperties.UserLanguage, StringComparison.OrdinalIgnoreCase))
                {
                    LanguageBox.SelectedIndex = i;
                    break;
                }

                // If the language tag starts with the two-letter ISO code and contains a culture (e.g., "zh" and "zh-CN")
                if (tag.StartsWith(SettingsHelper.Settings.UIProperties.UserLanguage[..2],
                        StringComparison.OrdinalIgnoreCase))
                {
                    // Check if the user's selected language contains a culture (like "zh-CN")
                    if (SettingsHelper.Settings.UIProperties.UserLanguage.Length > 2)
                    {
                        // Select the specific culture version if the tag matches up to the dash (e.g., "zh-CN")
                        if (tag.StartsWith(SettingsHelper.Settings.UIProperties.UserLanguage,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            LanguageBox.SelectedIndex = i;
                            break;
                        }
                    }
                    else
                    {
                        // Select the first matching two-letter language code (e.g., "zh")
                        LanguageBox.SelectedIndex = i;
                        break;
                    }
                }
            }
        };
    }
}