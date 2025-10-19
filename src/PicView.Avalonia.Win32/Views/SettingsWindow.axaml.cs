using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using PicView.Avalonia.Input;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;
using PicView.Core.FileAssociations;
using PicView.Core.Localization;
using PicView.Core.WindowsNT.FileAssociation;

namespace PicView.Avalonia.Win32.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsWindowConfig _config;
    public SettingsWindow(SettingsWindowConfig config)
    {
        _config = config;
        InitializeComponent();
        Task.Run(async () =>
        {
            await _config.LoadAsync();
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
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
                }
            });
        });
        ParentBorder.Height = ScreenHelper.GetWindowMaxHeight();
        if (Settings.Theme.GlassTheme)
        {
            SettingsView.Background = Brushes.Transparent;
            
            LogoBorder.Background = Brushes.Transparent;
            LogoBorder.BorderThickness = new Thickness(0);

            SettingsButton.Background = Brushes.Transparent;
            SettingsButton.BorderThickness = new Thickness(0);

            CloseButton.Background = Brushes.Transparent;
            CloseButton.BorderThickness = new Thickness(0);

            MinimizeButton.Background = Brushes.Transparent;
            MinimizeButton.BorderThickness = new Thickness(0);

            GoBackBorder.Background = Brushes.Transparent;
            GoBackBorder.BorderThickness = new Thickness(0);
            GoBackButton.Background = Brushes.Transparent;
            GoBackButton.BorderThickness = new Thickness(0);

            GoForwardBorder.Background = Brushes.Transparent;
            GoForwardBorder.BorderThickness = new Thickness(0);
            GoForwardButton.Background = Brushes.Transparent;
            GoForwardButton.BorderThickness = new Thickness(0);

            TitleText.Background = Brushes.Transparent;

            SettingsButton.Background = Brushes.Transparent;
            SettingsButton.BorderThickness = new Thickness(0);

            if (SettingsButton.Content is Button settingsIcon)
            {
                settingsIcon.Background = Brushes.Transparent;
                settingsIcon.BorderThickness = new Thickness(0);
            }

            if (!Application.Current.TryGetResource("SecondaryTextColor",
                    Application.Current.RequestedThemeVariant, out var textColor))
            {
                return;
            }

            if (textColor is not Color color)
            {
                return;
            }

            TitleText.Foreground = new SolidColorBrush(color);
            MinimizeButton.Foreground = new SolidColorBrush(color);
            CloseButton.Foreground = new SolidColorBrush(color);
        }
        else if (!Settings.Theme.Dark)
        {
            SettingsButton.Background = Brushes.White;
            if (!Application.Current.TryGetResource("MainBorderColor",
                    Application.Current.RequestedThemeVariant, out var mbColor))
            {
                return;
            }

            if (mbColor is Color color)
            {
                SettingsButton.BorderThickness = new Thickness(1, 0, 0, 0);
                SettingsButton.BorderBrush = new SolidColorBrush(color);
            }
        }

        Loaded += delegate
        {
            MinWidth = Width;
            Title = TranslationManager.GetTranslation("Settings") + " - PicView";
            if (DataContext is not MainViewModel vm)
            {
                return;
            }

            GoForwardButton.Command = vm.SettingsViewModel.GoForwardCommand;
            GoBackButton.Command = vm.SettingsViewModel.GoBackCommand;
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
                    FocusFilterBox();
                    break;
            }
        };

        Closing += async delegate
        {
            Hide();
            await _config.SaveAsync();
            await SaveSettingsAsync();
        };

        InitializeFileAssociationManager();
    }

    private void FocusFilterBox()
    {
        var fileAssociationsView = SettingsView.FindControl<Control>("FileAssociationsView");
        var filterBox = fileAssociationsView?.FindControl<Control>("FilterBox");
        var isFilterBoxEffectivelyVisible = filterBox?.Bounds is { Width: > 0, Height: > 0 };
        if (isFilterBoxEffectivelyVisible)
        {
            filterBox?.Focus();
        }
    }

    private void MoveWindow(object? sender, PointerPressedEventArgs e)
    {
        if (VisualRoot is null)
        {
            return;
        }

        var hostWindow = (Window)VisualRoot;
        hostWindow?.BeginMoveDrag(e);
    }
    
    private void UpdateWindowPosition(object? sender, PointerReleasedEventArgs e)
    {
        _config.WindowProperties.Left = Position.X;
        _config.WindowProperties.Top = Position.Y;
    }

    private void Close(object? sender, RoutedEventArgs e) => Close();

    private void Minimize(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private static void InitializeFileAssociationManager()
    {
        var iIFileAssociationService = new WindowsFileAssociationService();
        FileAssociationManager.Initialize(iIFileAssociationService);
    }


}