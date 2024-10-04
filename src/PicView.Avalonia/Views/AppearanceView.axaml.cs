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
        
        ThemeBox.SelectedItem = SettingsHelper.Settings.Theme.Dark ? DarkThemeBox : LightThemeBox;
        ThemeBox.SelectionChanged += delegate
        {
            ThemeManager.SetTheme(ThemeBox.SelectedIndex == 0);
        };

        ClearButtonsActiveState();
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
    }

    private void ClearButtonsActiveState()
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
        ClearButtonsActiveState();
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
}