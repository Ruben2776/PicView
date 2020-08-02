using PicView.Editing;
using PicView.Editing.Crop;
using PicView.UILogic.Animations;
using PicView.UILogic.Loading;
using System.Windows.Controls;
using static PicView.UILogic.Animations.MouseOverAnimations;

namespace PicView.UILogic.UserControls
{
    /// <summary>
    /// Menu to open functions
    /// </summary>
    public partial class ToolsAndEffectsMenu : UserControl
    {
        public ToolsAndEffectsMenu()
        {
            InitializeComponent();

            // ResizeButton
            ResizeButton.MouseEnter += delegate
            {
                ButtonMouseOverAnim(ResizeText);
                ButtonMouseOverAnim(ResizeFill);
                AnimationHelper.MouseEnterBgTexColor(ResizeButtonBrush);
            };
            ResizeButton.MouseLeave += delegate
            {
                ButtonMouseLeaveAnim(ResizeText);
                ButtonMouseLeaveAnim(ResizeFill);
                AnimationHelper.MouseLeaveBgTexColor(ResizeButtonBrush);
            };
            ResizeButton.Click += delegate
            {
                UC.Close_UserControls();
                LoadWindows.ResizeAndOptimizeWindow();
                Batch_Resize.UpdateValues();
            };

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
                LoadWindows.EffectsWindow();
            };

            // CropButton
            CropButton.PreviewMouseLeftButtonDown += delegate
            {
                PreviewMouseButtonDownAnim(CropButtonBrush);
            };
            CropButton.MouseEnter += delegate
            {
                ButtonMouseOverAnim(CropText);
                ButtonMouseOverAnim(CropFill);
                AnimationHelper.MouseEnterBgTexColor(CropButtonBrush);
            };
            CropButton.MouseLeave += delegate
            {
                ButtonMouseLeaveAnim(CropText);
                ButtonMouseLeaveAnim(CropFill);
                AnimationHelper.MouseLeaveBgTexColor(CropButtonBrush);
            };
            CropButton.Click += delegate
            {
                UC.Close_UserControls();
                CropFunctions.StartCrop();
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
                LoadWindows.ImageInfoWindow();
            };
        }
    }
}