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
        ResizeTextBlock.Text = TranslationHelper.GetTranslation("BatchResize");

        // EffectsButton
        var effectsIconBrush = (SolidColorBrush)Resources["EffectsIconBrush"];
        SetButtonIconMouseOverAnimations(EffectsButton, EffectsButtonBrush, effectsIconBrush);
        EffectsButton.Click += delegate
        {
            UC.Close_UserControls();
            ConfigureWindows.EffectsWindow();
        };
        EffectsButton.ToolTip = TranslationHelper.GetTranslation("EffectsTooltip");
        EffectsTextBlock.Text = TranslationHelper.GetTranslation("Effects");

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
        ColorPickerButton.ToolTip = TranslationHelper.GetTranslation("ColorPickerToolTooltip");
        ColorPickerTextBlock.Text = TranslationHelper.GetTranslation("ColorPickerTool");

        // ImageInfoButton
        var infoIconBrush = (SolidColorBrush)Resources["InfoIconBrush"];
        SetButtonIconMouseOverAnimations(ImageInfoButton, ImageInfoBrush, infoIconBrush);
        ImageInfoButton.Click += delegate
        {
            UC.Close_UserControls();
            ConfigureWindows.ImageInfoWindow();
        };
        ImageInfoButton.ToolTip = TranslationHelper.GetTranslation("ShowImageInfo");
        ImageInfoTextBlock.Text = TranslationHelper.GetTranslation("ImageInfo");

        // OptimizeImageButton
        var optimizeIconBrush = (SolidColorBrush)Resources["OptimizeIconBrush"];
        SetButtonIconMouseOverAnimations(OptimizeImageButton, OptimizeImageBrush, optimizeIconBrush);
        OptimizeImageButton.Click += async (_, _) =>
            await ImageFunctions.OptimizeImageAsyncWithErrorChecking().ConfigureAwait(false);
        OptimizeImageTextBlock.Text = TranslationHelper.GetTranslation("OptimizeImage");

        // Change background
        var changeIconBrush = (SolidColorBrush)Resources["ChangeIconBrush"];
        SetButtonIconMouseOverAnimations(BgButton, BgBrush, changeIconBrush);
        OptimizeImageButton.Click += async (_, _) =>
            await ImageFunctions.OptimizeImageAsyncWithErrorChecking().ConfigureAwait(false);
        BgTextBlock.Text = TranslationHelper.GetTranslation("ChangeBackground");

        BgButton.Click += (_, _) => ConfigColors.ChangeBackground();
    }
}