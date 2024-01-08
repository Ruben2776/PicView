using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using PicView.Core.Config;
using PicView.Core.Localization;
using ReactiveUI;
using System.Windows.Input;
using PicView.Avalonia.Helpers;

namespace PicView.Avalonia.ViewModels;

public class MainViewModel : ViewModelBase
{
    #region Localization

    private string? _currentLanguageKey;

    public string CurrentLanguageValue
    {
        get => SettingsHelper.Settings.UIProperties.UserLanguage;
    }

    public string? CurrentLanguageKey
    {
        get => _currentLanguageKey ?? "en";
        set
        {
            if (_currentLanguageKey == value)
            {
                return;
            }

            _currentLanguageKey = value;
            this.RaisePropertyChanged(nameof(CurrentLanguageValue));
            this.RaisePropertyChanged(nameof(CurrentLanguageValue));
            this.RaisePropertyChanged(nameof(SelectFile));
            this.RaisePropertyChanged(nameof(OpenLastFile));
            this.RaisePropertyChanged(nameof(Paste));
        }
    }

    public string SelectFile => TranslationHelper.GetTranslation("OpenFileDialog");

    public string OpenLastFile => TranslationHelper.GetTranslation("OpenLastFile");
    public string Paste => TranslationHelper.GetTranslation("FilePaste");

    #endregion Localization

    public ICommand? ExitCommand { get; }
    public ICommand? MinimizeCommand { get; }
    public ICommand? MaximizeCommand { get; }

    public MainViewModel()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        WindowHelper.InitializeWindowSizeAndPosition(desktop);

        ExitCommand = ReactiveCommand.Create(desktop.MainWindow.Close);
        MinimizeCommand = ReactiveCommand.Create(() =>
            desktop.MainWindow.WindowState = WindowState.Minimized);
        MaximizeCommand = ReactiveCommand.Create(() =>
        {
            desktop.MainWindow.WindowState = desktop.MainWindow.WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal;
        });
    }
}