using PicView.WPF.ConfigureSettings;
using PicView.WPF.Editing;
using PicView.WPF.ImageHandling;
using PicView.WPF.UILogic;
using System.Windows.Media;
using PicView.Core.Localization;
using static PicView.WPF.Animations.MouseOverAnimations;

namespace PicView.WPF.Views.UserControls.Menus;

public partial class ToolsAndEffectsMenu
{
    public ToolsAndEffectsMenu()
    {
        InitializeComponent();

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
        OptimizeImageButton.Click += async (_, _) =>
            await ImageFunctions.OptimizeImageAsyncWithErrorChecking().ConfigureAwait(false);

        // Change background
        var changeIconBrush = (SolidColorBrush)Resources["ChangeIconBrush"];
        SetButtonIconMouseOverAnimations(BgButton, BgBrush, changeIconBrush);

        BgButton.Click += (_, _) => ConfigColors.ChangeBackground();

        UpdateLanguage();
    }

    internal void UpdateLanguage()
    {
        ResizeTextBlock.Text = TranslationHelper.GetTranslation("BatchResize");
        EffectsTextBlock.Text = TranslationHelper.GetTranslation("Effects");
        ColorPickerTextBlock.Text = TranslationHelper.GetTranslation("ColorPickerTool");
        ImageInfoTextBlock.Text = TranslationHelper.GetTranslation("ImageInfo");
        OptimizeImageTextBlock.Text = TranslationHelper.GetTranslation("OptimizeImage");
        BgTextBlock.Text = TranslationHelper.GetTranslation("ChangeBackground");
        ImageInfoButton.ToolTip = TranslationHelper.GetTranslation("ShowImageInfo");
        ColorPickerButton.ToolTip = TranslationHelper.GetTranslation("ColorPickerToolTooltip");
        EffectsButton.ToolTip = TranslationHelper.GetTranslation("EffectsTooltip");
    }
}