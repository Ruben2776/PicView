using Avalonia.Controls;
using Avalonia.Interactivity;
using PicView.Avalonia.ColorManagement;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.ViewModels;
using PicView.Core.ColorHandling;
using PicView.Core.Config;

namespace PicView.Avalonia.Views;

public partial class AppearanceView : UserControl
{
    public AppearanceView()
    {
        InitializeComponent();
        Loaded += AppearanceView_Loaded;
    }

    private void AppearanceView_Loaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }
        GalleryStretchMode.DetermineStretchMode(vm);

        if (SettingsHelper.Settings.Theme.GlassTheme)
        {
            ThemeBox.SelectedItem = GlassThemeBox;
        }
        else
        {
            ThemeBox.SelectedItem = SettingsHelper.Settings.Theme.Dark ? DarkThemeBox : LightThemeBox;
        }
        ThemeBox.SelectionChanged += delegate
        {
            // Adjust based on which theme is selected
            if (Equals(ThemeBox.SelectedItem, GlassThemeBox))
            {
                SettingsHelper.Settings.Theme.GlassTheme = true;
            }
            else if (Equals(ThemeBox.SelectedItem, DarkThemeBox))
            {
                SettingsHelper.Settings.Theme.GlassTheme = false;
                SettingsHelper.Settings.Theme.Dark = true;
            }
            else
            {
                SettingsHelper.Settings.Theme.GlassTheme = false;
                SettingsHelper.Settings.Theme.Dark = false;
            }

            var selectedTheme = SettingsHelper.Settings.Theme.GlassTheme
                ? ThemeManager.Theme.Glass
                : SettingsHelper.Settings.Theme.Dark
                    ? ThemeManager.Theme.Dark
                    : ThemeManager.Theme.Light;

            ThemeManager.SetTheme(selectedTheme);
        };

        ClearColorButtonsActiveState();
        switch ((ColorOptions)SettingsHelper.Settings.Theme.ColorTheme)
        {
            case ColorOptions.Aqua:
                AquaButton.Classes.Add("active");
                break;
            case ColorOptions.Teal:
                TealButton.Classes.Add("active");
                break;
            case ColorOptions.Lime:
                LimeButton.Classes.Add("active");
                break;
            case ColorOptions.Golden:
                GoldButton.Classes.Add("active");
                break;
            case ColorOptions.Orange:
                OrangeButton.Classes.Add("active");
                break;
            case ColorOptions.Pink:
                PinkButton.Classes.Add("active");
                break;
            case ColorOptions.Purple:
                PurpleButton.Classes.Add("active");
                break;
            case ColorOptions.Red:
                RedButton.Classes.Add("active");
                break;
            case ColorOptions.Green:
                GreenButton.Classes.Add("active");
                break;
            case ColorOptions.Magenta:
                MagentaButton.Classes.Add("active");
                break;
            case ColorOptions.Blue:
                BlueButton.Classes.Add("active");
                break;
            case ColorOptions.Cyan:
                CyanButton.Classes.Add("active");
                break;
        }
        
        CheckerboardButton.Background = BackgroundManager.CreateCheckerboardBrush(default, default,10);
        CheckerboardAltButton.Background = BackgroundManager.CreateCheckerboardBrushAlt(25);

        switch (SettingsHelper.Settings.UIProperties.BgColorChoice)
        {
            default:
                TransparentBgButton.Classes.Add("active");
                break;
            case 1:
                NoiseTextureButton.Classes.Add("active");
                break;
            case 2:
                CheckerboardButton.Classes.Add("active");
                break;
            case 3:
                CheckerboardAltButton.Classes.Add("active");
                break;
            case 4:
                WhiteBgButton.Classes.Add("active");
                break;
            case 5:
                GrayBgButton.Classes.Add("active");
                break;
            case 6:
                DarkGrayBgButton.Classes.Add("active");
                break;
            case 7:
                DarkGraySemiTransparentBgButton.Classes.Add("active");
                break;
            case 8:
                DarkGraySemiTransparentAltBgButton.Classes.Add("active");
                break;
            case 9:
                BlackBgButton.Classes.Add("active");
                break;
        }
    }

    private void ClearColorButtonsActiveState()
    {
        var buttons = new List<Button>
        {
            BlueButton, CyanButton, GreenButton, MagentaButton, RedButton, AquaButton,
            TealButton, LimeButton, GoldButton, OrangeButton, PinkButton, PurpleButton
        };

        foreach (var button in buttons)
        {
            button.Classes.Remove("active");
        }
    }
    
    private void SetColorTheme(ColorOptions colorTheme)
    {
        ClearColorButtonsActiveState();
        switch (colorTheme)
        {
            default:
                BlueButton.Classes.Add("active");
                break;
            case ColorOptions.Pink:
                PinkButton.Classes.Add("active");
                break;
            case ColorOptions.Orange:
                OrangeButton.Classes.Add("active");
                break;
            case ColorOptions.Green:
                GreenButton.Classes.Add("active");
                break;
            case ColorOptions.Red:
                RedButton.Classes.Add("active");
                break;
            case ColorOptions.Teal:
                TealButton.Classes.Add("active");
                break;
            case ColorOptions.Aqua:
                AquaButton.Classes.Add("active");
                break;
            case ColorOptions.Golden:
                GoldButton.Classes.Add("active");
                break;
            case ColorOptions.Purple:
                PurpleButton.Classes.Add("active");
                break;
            case ColorOptions.Cyan:
                CyanButton.Classes.Add("active");
                break;
            case ColorOptions.Magenta:
                MagentaButton.Classes.Add("active");
                break;
            case ColorOptions.Lime:
                LimeButton.Classes.Add("active");
                break;
        }

        ColorManager.UpdateAccentColors((int)colorTheme);
    }

    private void ColorButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button clickedButton)
        {
            return;
        }

        // Map the button to the corresponding ColorOptions enum
        var selectedColor = clickedButton.Name switch
        {
            nameof(BlueButton) => ColorOptions.Blue,
            nameof(CyanButton) => ColorOptions.Cyan,
            nameof(GreenButton) => ColorOptions.Green,
            nameof(MagentaButton) => ColorOptions.Magenta,
            nameof(RedButton) => ColorOptions.Red,
            nameof(AquaButton) => ColorOptions.Aqua,
            nameof(TealButton) => ColorOptions.Teal,
            nameof(LimeButton) => ColorOptions.Lime,
            nameof(GoldButton) => ColorOptions.Golden,
            nameof(OrangeButton) => ColorOptions.Orange,
            nameof(PinkButton) => ColorOptions.Pink,
            nameof(PurpleButton) => ColorOptions.Purple,
            _ => ColorOptions.Blue
        };

        // Set the new active theme
        SetColorTheme(selectedColor);
    }
    
    private void BgButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button clickedButton)
        {
            return;
        }

        // Map the button to the corresponding ColorOptions enum
        var selectedBg = clickedButton.Name switch
        {
            nameof(TransparentBgButton) => 0,
            nameof(NoiseTextureButton) => 1,
            nameof(CheckerboardButton) => 2,
            nameof(CheckerboardAltButton) => 3,
            nameof(WhiteBgButton) => 4,
            nameof(GrayBgButton) => 5,
            nameof(DarkGrayBgButton) => 6,
            nameof(DarkGraySemiTransparentBgButton) => 7,
            nameof(DarkGraySemiTransparentAltBgButton) => 8,
            nameof(BlackBgButton) => 9,
            _ => 0
        };

        // Set the new active theme
        SetBackgroundTheme(selectedBg);
    }
    
    private void SetBackgroundTheme(int selectedBg)
    {
        ClearBackgroundButtonsActiveState();
        switch (selectedBg)
        {
            default:
                TransparentBgButton.Classes.Add("active");
                break;
            case 1:
                NoiseTextureButton.Classes.Add("active");
                break;
            case 2:
                CheckerboardButton.Classes.Add("active");
                break;
            case 3:
                CheckerboardAltButton.Classes.Add("active");
                break;
            case 4:
                WhiteBgButton.Classes.Add("active");
                break;
            case 5:
                GrayBgButton.Classes.Add("active");
                break;
            case 6:
                DarkGrayBgButton.Classes.Add("active");
                break;
            case 7:
                DarkGraySemiTransparentBgButton.Classes.Add("active");
                break;
            case 8:
                DarkGraySemiTransparentAltBgButton.Classes.Add("active");
                break;
            case 9:
                BlackBgButton.Classes.Add("active");
                break;
        }

        if (DataContext is not MainViewModel vm)
        {
            return;
        }
        
        BackgroundManager.SetBackground(vm, selectedBg);
    }

    private void ClearBackgroundButtonsActiveState()
    {
        var buttons = new List<Button>
        {
            TransparentBgButton, NoiseTextureButton, CheckerboardButton, CheckerboardAltButton,
            WhiteBgButton, GrayBgButton, DarkGrayBgButton, DarkGraySemiTransparentBgButton,
            DarkGraySemiTransparentAltBgButton, BlackBgButton
        };

        foreach (var button in buttons)
        {
            button.Classes.Remove("active");
        }
    }
}