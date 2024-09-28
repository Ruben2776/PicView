using System.Globalization;
using Avalonia.Controls;
using Avalonia.Threading;
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
                    vm.UpdateLanguage();

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

                // Find the ComboBoxItem whose Tag matches UserLanguage
                for (var i = 0; i < LanguageBox.Items.Count; i++)
                {
                    if (LanguageBox.Items[i] is ComboBoxItem { Tag: string tag } && 
                        tag == SettingsHelper.Settings.UIProperties.UserLanguage)
                    {
                        LanguageBox.SelectedIndex = i;
                        break;
                    }
                }
            };
    }
}