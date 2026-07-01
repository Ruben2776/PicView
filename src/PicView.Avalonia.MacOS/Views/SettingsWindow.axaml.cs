using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Input;
using PicView.Avalonia.UI;
using PicView.Core.Config;
using PicView.Core.Extensions;
using PicView.Core.FileAssociations;
using PicView.Core.Localization;
using PicView.Core.MacOS.FileAssociation;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.MacOS.Views;

public partial class SettingsWindow : GenericWindow
{
    private readonly SettingsWindowConfig _config;
    private readonly IDisposable? _disposable;
    
    public SettingsWindow(SettingsWindowConfig config)
    {
        _config = config;
        InitializeComponent();

        if (_config.WindowProperties.Maximized)
        {
            WindowState = WindowState.Maximized;
        }
        else
        {
            var left = _config.WindowProperties.Left;
            var top = _config.WindowProperties.Top;
            if (left.HasValue && top.HasValue)
            {
                Position = new PixelPoint(left.Value, top.Value);
            }

            var width = _config.WindowProperties.Width;
            var height = _config.WindowProperties.Height;
            if (width.HasValue && height.HasValue)
            {
                Width = width.Value;
                Height = height.Value;
            }
        }
        if (!Settings.Theme.Dark || Settings.Theme.GlassTheme)
        {
            TitleText.Background = Brushes.Transparent;
            SettingsView.Background = Brushes.Transparent;
            SettingsButton.Background = Brushes.Transparent;
            
            HomeButton.Classes.Remove("noBorderHover");
            HomeButton.Classes.Add("hover");
            GoBackButton.Classes.Remove("noBorderHover");
            GoBackButton.Classes.Add("hover");
            GoForwardButton.Classes.Remove("noBorderHover");
            GoForwardButton.Classes.Add("hover");
        }

        if (!Settings.Theme.Dark)
        {
            MainBorder.Background = UIHelper.GetMenuBackgroundColor();
        }
        Loaded += delegate
        {
            if (DataContext is not CoreViewModel core)
            {
                return;
            }
            core.SettingsViewModel.RestoreLastTab(_config.WindowProperties.LastTab);

            Title = StringExtensions.CombineWithAppName(TranslationManager.Translation.Settings);
            SettingsView.Focus();

            GoForwardButton.Command = core.SettingsViewModel?.GoForwardCommand;
            GoBackButton.Command = core.SettingsViewModel?.GoBackCommand;
            HomeButton.Command = core.SettingsViewModel?.GoHomeCommand;
        };
        KeyDown += (_, e) =>
        {
            var ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta);
            switch (e.Key)
            {
                case Key.Escape:
                    MainKeyboardShortcuts.IsEscKeyEnabled = false;
                    Close();
                    break;
                case Key.F when ctrl:
                    break;
            }
        };

        Closing += async delegate
        {
            Hide();
            if (DataContext is CoreViewModel vm)
            {
                _config.WindowProperties.LastTab = vm.SettingsViewModel.GetLastTabId();
            }
            await _config.SaveAsync();
            await SaveSettingsAsync();
            _disposable?.Dispose();
        };

        _disposable = ClientSizeProperty.Changed.ToObservable()
            .Subscribe(UpdateWindowSizeAndPosition);

        InitializeFileAssociationManager();
    }
    
    private void UpdateWindowSizeAndPosition(AvaloniaPropertyChangedEventArgs<Size> size)
    {
        _config.WindowProperties.Left = Position.X;
        _config.WindowProperties.Top = Position.Y;

        _config.WindowProperties.Width = Bounds.Width;
        _config.WindowProperties.Height = Bounds.Height;
    }
    
    private static void InitializeFileAssociationManager()
    {
        var iIFileAssociationService = new MacFileAssociationService();
        FileAssociationManager.Initialize(iIFileAssociationService);
    }
}