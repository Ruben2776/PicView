using PicView.Animations;
using PicView.ConfigureSettings;
using PicView.Editing.Crop;
using PicView.Properties;
using PicView.UILogic;
using System.Windows.Controls;
using static PicView.Animations.MouseOverAnimations;
using static PicView.UILogic.UC;

namespace PicView.Views.UserControls.Menus
{
    /// <summary>
    /// Interaction logic for QuickSettingsMenu.xaml
    /// </summary>
    public partial class QuickSettingsMenu : UserControl
    {
        public QuickSettingsMenu()
        {
            InitializeComponent();

            SettingsButton.TheButton.Click += delegate
            {
                ConfigureWindows.AllSettingsWindow();
                Close_UserControls();
            };

            ToggleScroll.IsChecked = Settings.Default.ScrollEnabled;
            ToggleScroll.Click += (s, x) => UpdateUIValues.SetScrolling(Settings.Default.ScrollEnabled);
            ToggleScrollBorder.MouseLeftButtonDown += (_, _) => UpdateUIValues.SetScrolling(Settings.Default.ScrollEnabled);

            ToggleFill.IsChecked = Settings.Default.FillImage;
            ToggleFill.Click += UpdateUIValues.SetAutoFill;
            ToggleFillBorder.MouseLeftButtonDown += UpdateUIValues.SetAutoFill;

            ToggleLooping.IsChecked = Settings.Default.Looping;
            ToggleLooping.Click += (_, _) => UpdateUIValues.SetLooping();
            ToggleLoopingBorder.MouseLeftButtonDown += (_, _) => UpdateUIValues.SetLooping();

            SetFit.IsChecked = Settings.Default.AutoFitWindow;
            SetFit.Click += UpdateUIValues.SetAutoFit;
            SetFitBorder.MouseLeftButtonDown += UpdateUIValues.SetAutoFit;

            // CropButton
            CropButton.PreviewMouseLeftButtonDown += delegate
            {
                ButtonMouseOverAnim(CropFill, false, true);
            };

            CropButton.Click += delegate
            {
                Close_UserControls();
                CropFunctions.StartCrop();
            };

            #region Animation events

            // Toggle Scroll
            ToggleScrollBorder.MouseEnter += delegate { ButtonMouseOverAnim(ToggleScrollFill); };
            ToggleScrollBorder.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(ToggleScrollBrush); };
            ToggleScrollBorder.MouseLeave += delegate { ButtonMouseLeaveAnim(ToggleScrollFill); };
            ToggleScrollBorder.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(ToggleScrollBrush); };

            // Toggle Loop
            ToggleLoopingBorder.MouseEnter += delegate { ButtonMouseOverAnim(ToggleLoopFill); };
            ToggleLoopingBorder.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(ToggleLoopBrush); };
            ToggleLoopingBorder.MouseLeave += delegate { ButtonMouseLeaveAnim(ToggleLoopFill); };
            ToggleLoopingBorder.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(ToggleLoopBrush); };

            // Set Fit
            SetFitBorder.MouseEnter += delegate { ButtonMouseOverAnim(SetFitFill); };
            SetFitBorder.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(SetFitBrush); };
            SetFitBorder.MouseLeave += delegate { ButtonMouseLeaveAnim(SetFitFill); };
            SetFitBorder.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(SetFitBrush); };

            // ToggleFill
            ToggleFillBorder.MouseEnter += delegate { ButtonMouseOverAnim(ToggleFillFill); };
            ToggleFillBorder.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(ToggleFillBrush); };
            ToggleFillBorder.MouseLeave += delegate { ButtonMouseLeaveAnim(ToggleFillFill); };
            ToggleFillBorder.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(ToggleFillBrush); };

            // CropButton
            CropButton.MouseEnter += delegate
            {
                ButtonMouseOverAnim(CropFill);
                AnimationHelper.MouseEnterBgTexColor(CropButtonBrush);
            };
            CropButton.MouseLeave += delegate
            {
                ButtonMouseLeaveAnim(CropFill);
                AnimationHelper.MouseLeaveBgTexColor(CropButtonBrush);
            };

            #endregion Animation events
        }
    }
}