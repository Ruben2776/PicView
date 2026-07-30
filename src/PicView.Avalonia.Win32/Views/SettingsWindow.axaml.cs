using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Input;
using PicView.Avalonia.UI;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Config;
using PicView.Core.Extensions;
using PicView.Core.ViewModels;
using PicView.Core.FileAssociations;
using PicView.Core.Localization;
using PicView.Core.WindowsNT.FileAssociation;
using R3;

namespace PicView.Avalonia.Win32.Views;

public partial class SettingsWindow : GenericWindow
{
    
    public SettingsWindow(SettingsWindowConfig config)
    {
        InitializeComponent();
        GenericWindowHelper.GenericWindowInitialize(this, TranslationManager.Translation.Settings, false, config.WindowProperties);
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
            SettingsView.Focus();
            if (DataContext is not CoreViewModel core)
            {
                return;
            }
            
            core.SettingsViewModel.RestoreLastTab(config.WindowProperties.LastTab);

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
                    FocusFilterBox();
                    break;
            }
        };

        InitializeFileAssociationManager();
    }

    private void FocusFilterBox()
    {
        var filterBox = SettingsView.FindControl<Control>("FilterBox");
        var isFilterBoxEffectivelyVisible = filterBox?.Bounds is { Width: > 0, Height: > 0 };
        if (isFilterBoxEffectivelyVisible)
        {
            filterBox?.Focus();
        }
    }

    private static void InitializeFileAssociationManager()
    {
        var iIFileAssociationService = new WindowsFileAssociationService();
        FileAssociationManager.Initialize(iIFileAssociationService);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (DataContext is not CoreViewModel vm)
        {
            return;
        }

        var properties = e.GetCurrentPoint(this).Properties;
        switch (properties.PointerUpdateKind)
        {
            case PointerUpdateKind.XButton1Pressed:
            {
                if (vm.SettingsViewModel.GoBackCommand.CanExecute())
                {
                    vm.SettingsViewModel.GoBackCommand.Execute(Unit.Default);
                }

                break;
            }
            case PointerUpdateKind.XButton2Pressed:
            {
                if (vm.SettingsViewModel.GoForwardCommand.CanExecute())
                {
                    vm.SettingsViewModel.GoForwardCommand.Execute(Unit.Default);
                }

                break;
            }
        }
    }
}