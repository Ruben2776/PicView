using PicView.ConfigureSettings;
using PicView.Editing;
using PicView.ImageHandling;
using PicView.Properties;
using PicView.UILogic;
using System.Windows.Controls;
using System.Windows.Media;
using static PicView.Animations.MouseOverAnimations;

namespace PicView.Views.UserControls.Menus;

/// <summary>
/// Menu to open functions
/// </summary>
public partial class ToolsAndEffectsMenu : UserControl
{
    public ToolsAndEffectsMenu()
    {
        InitializeComponent();

        switch (Settings.Default.UserLanguage)
        {
            case "ru":
            case "pl":
            case "es":
                ResizeButton.FontSize = EffectsButton.FontSize =
                    ImageInfoButton.FontSize = ColorPickerButton.FontSize =
                        OptimizeImageButton.FontSize = BgButton.FontSize = 12;
                break;
        }

        // ResizeButton
        var resizeIconBrush = (SolidColorBrush)Resources["ResizeIconBrush"];
        SetButtonIconMouseOverAnimations(ResizeButton, ResizeButtonBrush, resizeIconBrush);
        ResizeButton.Click += (_, _) => ConfigureWindows.ResizeWindow();

        // EffectsButton
        var effectsIconBrush = (SolidColorBrush)Resources["EffectsIconBrush"];
        SetButtonIconMouseOverAnimations(EffectsButton, EffectsButtonBrush, effectsIconBrush);
        EffectsButton.Click += delegate
        {
            UC.Close_UserControls();
            ConfigureWindows.EffectsWindow();
        };

        // ColorPickerButton
        var colorIconBrush = (SolidColorBrush)Resources["ColorIconBrush"];
        SetButtonIconMouseOverAnimations(ColorPickerButton, ColorPickerBrush, colorIconBrush);
        ColorPickerButton.Click += delegate
        {
            if (ConfigureWindows.GetMainWindow.MainImage.Source is null)
            {
                return;
            }

            UC.Close_UserControls();
            ColorPicking.IsRunning = true;
            ColorPicking.Start();
        };

        // ImageInfoButton
        var infoIconBrush = (SolidColorBrush)Resources["InfoIconBrush"];
        SetButtonIconMouseOverAnimations(ImageInfoButton, ImageInfoBrush, infoIconBrush);
        ImageInfoButton.Click += delegate
        {
            UC.Close_UserControls();
            ConfigureWindows.ImageInfoWindow();
        };

        // OptimizeImageButton
        var optimizeIconBrush = (SolidColorBrush)Resources["OptimizeIconBrush"];
        SetButtonIconMouseOverAnimations(OptimizeImageButton, OptimizeImageBrush, optimizeIconBrush);
        OptimizeImageButton.Click += async (_, _) => await ImageFunctions.OptimizeImageAsyncWithErrorChecking().ConfigureAwait(false);

        // Change background
        var changeIconBrush = (SolidColorBrush)Resources["ChangeIconBrush"];
        SetButtonIconMouseOverAnimations(BgButton, BgBrush, changeIconBrush);
        OptimizeImageButton.Click += async (_, _) => await ImageFunctions.OptimizeImageAsyncWithErrorChecking().ConfigureAwait(false);

        BgButton.Click += (_, _) => ConfigColors.ChangeBackground();
    }
}