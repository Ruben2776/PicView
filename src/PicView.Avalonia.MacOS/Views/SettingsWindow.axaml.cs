using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Input;
using PicView.Avalonia.UI;
using PicView.Core.Config;
using PicView.Core.Localization;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.MacOS.Views;

public partial class SettingsWindow : GenericWindow
{
    public SettingsWindow(SettingsWindowConfig config)
    {
        InitializeComponent();

        GenericWindowHelper.GenericWindowInitialize(this, TranslationManager.Translation.Settings, false, config.WindowProperties);
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