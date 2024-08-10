using System.Globalization;
using Avalonia.Controls;
using Avalonia.Threading;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;
using PicView.Core.Localization;

namespace PicView.Avalonia.Views;

public partial class GeneralSettingsView : UserControl
{
    public GeneralSettingsView()
    {
        InitializeComponent();
        Loaded += delegate
        {
            ApplicationStartupBox.SelectionChanged += async delegate
            {
                SettingsHelper.Settings.StartUp.OpenLastFile = ApplicationStartupBox.SelectedIndex == 1;
                await SettingsHelper.SaveSettingsAsync();
            };
            MouseWheelBox.SelectionChanged += async delegate
            {
                SettingsHelper.Settings.Zoom.CtrlZoom = MouseWheelBox.SelectedIndex == 0;
                await SettingsHelper.SaveSettingsAsync();
            };
            ScrollDirectionBox.SelectionChanged += async delegate
            {
                SettingsHelper.Settings.Zoom.HorizontalReverseScroll = ScrollDirectionBox.SelectedIndex == 0;
                await SettingsHelper.SaveSettingsAsync();
            };
            MouseWheelBox.SelectedIndex = SettingsHelper.Settings.Zoom.CtrlZoom ? 0 : 1;
            ScrollDirectionBox.SelectedIndex = SettingsHelper.Settings.Zoom.HorizontalReverseScroll ? 0 : 1;
            ApplicationStartupBox.SelectedIndex = SettingsHelper.Settings.StartUp.OpenLastFile ? 1 : 0;
            
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
        };
    }
}