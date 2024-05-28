using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Threading;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.Helpers;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;
using PicView.Core.Localization;

namespace PicView.Avalonia.Views;

public partial class AppearanceView : UserControl
{
    public AppearanceView()
    {
        InitializeComponent();
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            TaskBarToggleButton.IsVisible = false;
        }
        Loaded += AppearanceView_Loaded;
    }

    private void AppearanceView_Loaded(object? sender, RoutedEventArgs e)
    {
        try
        {
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
        }
        catch (Exception exception)
        {
#if DEBUG
            Trace.WriteLine($"{nameof(AppearanceView)} Add language caught exception: \n {exception}");
#endif
        }
    }

    private void RangeBase_OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (DataContext is not MainViewModel vm ||
            Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            !GalleryFunctions.IsBottomGalleryOpen)
        {
            return;
        }
        WindowHelper.SetSize(vm);
        var mainView = desktop.MainWindow.GetControl<MainView>("MainView");
        var gallery = mainView.GalleryView;
        gallery.Height = vm.GalleryHeight;
        // Binding to height depends on timing of the update. Maybe find a cleaner mvvm solution one day

        // Maybe save this on close or some other way
        SettingsHelper.Settings.Gallery.BottomGalleryItemSize = e.NewValue;
        _ = SettingsHelper.SaveSettingsAsync();
    }
}