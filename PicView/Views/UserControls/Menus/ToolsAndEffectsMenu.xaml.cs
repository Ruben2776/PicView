using PicView.Animations;
using PicView.ConfigureSettings;
using PicView.Editing;
using PicView.ImageHandling;
using PicView.Properties;
using PicView.UILogic;
using System.Windows.Controls;
using System.Windows.Media;
using static PicView.Animations.MouseOverAnimations;

namespace PicView.Views.UserControls.Menus
{
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
            var iconBrush = (SolidColorBrush)Resources["IconBrush"];
            ResizeButton.MouseEnter += delegate
            {
                ButtonMouseOverAnim(ResizeText);
                ButtonMouseOverAnim(iconBrush);
                AnimationHelper.MouseEnterBgTexColor(ResizeButtonBrush);
            };
            ResizeButton.MouseLeave += delegate
            {
                ButtonMouseLeaveAnim(ResizeText);
                ButtonMouseLeaveAnim(iconBrush);
                AnimationHelper.MouseLeaveBgTexColor(ResizeButtonBrush);
            };

            ResizeButton.Click += (_, _) => ConfigureWindows.ResizeWindow();

            // EffectsButton
            EffectsButton.MouseEnter += delegate
            {
                ButtonMouseOverAnim(EffectsText);
                ButtonMouseOverAnim(EffectsFill);
                AnimationHelper.MouseEnterBgTexColor(EffectsButtonBrush);
            };
            EffectsButton.MouseLeave += delegate
            {
                ButtonMouseLeaveAnim(EffectsText);
                ButtonMouseLeaveAnim(EffectsFill);
                AnimationHelper.MouseLeaveBgTexColor(EffectsButtonBrush);
            };
            EffectsButton.Click += delegate
            {
                UC.Close_UserControls();
                ConfigureWindows.EffectsWindow();
            };

            // ColorPickerButton
            ColorPickerButton.MouseEnter += delegate
            {
                ButtonMouseOverAnim(ColorPickerText);
                ButtonMouseOverAnim(ColorPickerFill);
                AnimationHelper.MouseEnterBgTexColor(ColorPickerBrush);
            };
            ColorPickerButton.MouseLeave += delegate
            {
                ButtonMouseLeaveAnim(ColorPickerText);
                ButtonMouseLeaveAnim(ColorPickerFill);
                AnimationHelper.MouseLeaveBgTexColor(ColorPickerBrush);
            };
            ColorPickerButton.Click += delegate
            {
                if (ConfigureWindows.GetMainWindow.MainImage.Source is null)
                {
                    return;
                }

                UC.Close_UserControls();
                Color_Picking.IsRunning = true;
                Color_Picking.Start();
            };

            // ImageInfoButton
            ImageInfoButton.MouseEnter += delegate
            {
                ButtonMouseOverAnim(ImageInfoText);
                ButtonMouseOverAnim(ImageInfoFill);
                AnimationHelper.MouseEnterBgTexColor(ImageInfoBrush);
            };
            ImageInfoButton.MouseLeave += delegate
            {
                ButtonMouseLeaveAnim(ImageInfoText);
                ButtonMouseLeaveAnim(ImageInfoFill);
                AnimationHelper.MouseLeaveBgTexColor(ImageInfoBrush);
            };
            ImageInfoButton.Click += delegate
            {
                UC.Close_UserControls();
                ConfigureWindows.ImageInfoWindow();
            };

            // OptimizeImageButton
            OptimizeImageButton.MouseEnter += delegate
            {
                ButtonMouseOverAnim(OptimizeImageText);
                ButtonMouseOverAnim(OptimizeImageFill1);
                ButtonMouseOverAnim(OptimizeImageFill2);
                AnimationHelper.MouseEnterBgTexColor(OptimizeImageBrush);
            };
            OptimizeImageButton.MouseLeave += delegate
            {
                ButtonMouseLeaveAnim(OptimizeImageText);
                ButtonMouseLeaveAnim(OptimizeImageFill1);
                ButtonMouseLeaveAnim(OptimizeImageFill2);
                AnimationHelper.MouseLeaveBgTexColor(OptimizeImageBrush);
            };
            OptimizeImageButton.Click += async (_, _) => await ImageFunctions.OptimizeImageAsyncWithErrorChecking().ConfigureAwait(false);

            BgBorder.MouseEnter += delegate
            {
                ButtonMouseOverAnim(IconBrush1);
                ButtonMouseOverAnim(IconBrush2);
                ButtonMouseOverAnim(IconBrush3);
                ButtonMouseOverAnim(IconBrush4);
                ButtonMouseOverAnim(IconBrush5);
                ButtonMouseOverAnim(BgText);
                AnimationHelper.MouseEnterBgTexColor(BgBrush);
            };

            BgBorder.MouseLeave += delegate
            {
                ButtonMouseLeaveAnim(IconBrush1);
                ButtonMouseLeaveAnim(IconBrush2);
                ButtonMouseLeaveAnim(IconBrush3);
                ButtonMouseLeaveAnim(IconBrush4);
                ButtonMouseLeaveAnim(IconBrush5);
                ButtonMouseLeaveAnim(BgText);
                AnimationHelper.MouseLeaveBgTexColor(BgBrush);
            };

            BgButton.Click += (_, _) => ConfigColors.ChangeBackground();
        }
    }
}