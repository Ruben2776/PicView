using PicView.Editing;
using PicView.Editing.Crop;
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
            ResizeButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(ResizeButtonBrush);
            ResizeButton.MouseEnter += (s, x) => ButtonMouseOverAnim(ResizeButtonBrush, true);
            ResizeButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(ResizeButtonBrush, false);
            ResizeButton.Click += delegate
            {
                UC.Close_UserControls();
                LoadWindows.ResizeAndOptimizeWindow();
                Batch_Resize.UpdateValues();
            };

            // EffectsButton
            EffectsButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(EffectsButtonBrush);
            EffectsButton.MouseEnter += (s, x) => ButtonMouseOverAnim(EffectsButtonBrush, true);
            EffectsButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(EffectsButtonBrush, false);
            EffectsButton.Click += delegate
            {
                UC.Close_UserControls();
                LoadWindows.EffectsWindow();
            };

            // CropButton
            CropButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(CropButtonBrush);
            CropButton.MouseEnter += (s, x) => ButtonMouseOverAnim(CropButtonBrush, true);
            CropButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(CropButtonBrush, false);
            CropButton.Click += delegate
            {
                UC.Close_UserControls();
                CropFunctions.StartCrop();
            };

            // ColorPickerButton
            ColorPickerButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(ColorPickerButtonBrush);
            ColorPickerButton.MouseEnter += (s, x) => ButtonMouseOverAnim(ColorPickerButtonBrush, true);
            ColorPickerButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(ColorPickerButtonBrush, false);
            ColorPickerButton.Click += delegate
            {
                UC.Close_UserControls();
                Color_Picking.IsRunning = true;
                Color_Picking.Start();
            };

        }
    }
}