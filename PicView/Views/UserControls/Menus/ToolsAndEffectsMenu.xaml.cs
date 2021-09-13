using PicView.ChangeImage;
using PicView.Editing;
using PicView.Editing.Crop;
using PicView.ImageHandling;
using PicView.UILogic;
using PicView.UILogic.Animations;
using System.Windows;
using System.Windows.Controls;
using static PicView.UILogic.Animations.MouseOverAnimations;

namespace PicView.Views.UserControls
{
    /// <summary>
    /// Menu to open functions
    /// </summary>
    public partial class ToolsAndEffectsMenu : UserControl
    {
        public ToolsAndEffectsMenu()
        {
            InitializeComponent();

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


            // InfoButton
            InfoButton.TheButton.Click += delegate
            {
                UC.Close_UserControls();
                ConfigureWindows.InfoWindow();
            };

            // ColorPickerButton
            ColorPickerButton.PreviewMouseLeftButtonDown += delegate
            {
                PreviewMouseButtonDownAnim(ColorPickerBrush);
            };
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
                if (Navigation.FolderIndex < Navigation.Pics.Count || Navigation.Pics.Count == 0)
                {
                    return;
                }
                UC.Close_UserControls();
                Color_Picking.IsRunning = true;
                Color_Picking.Start();
            };

            // ImageInfoButton
            ImageInfoButton.PreviewMouseLeftButtonDown += delegate
            {
                PreviewMouseButtonDownAnim(ImageInfoBrush);
            };
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
            OptimizeImageButton.PreviewMouseLeftButtonDown += delegate
            {
                PreviewMouseButtonDownAnim(OptimizeImageBrush);
            };
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
            OptimizeImageButton.Click += async delegate
            {
                if (Navigation.FolderIndex < Navigation.Pics.Count || Navigation.Pics.Count == 0)
                {
                    return;
                }
                await Tooltip.ShowTooltipMessage(Application.Current.Resources["Applying"] as string, true).ConfigureAwait(false);
                var success = await ImageFunctions.OptimizeImageAsync(Navigation.Pics[Navigation.FolderIndex]).ConfigureAwait(false);
                if (success)
                {
                    await Error_Handling.ReloadAsync().ConfigureAwait(false);
                }
                else
                {
                    await Tooltip.ShowTooltipMessage(Application.Current.Resources["UnexpectedError"] as string, true).ConfigureAwait(false);
                }
            };
        }
    }
}