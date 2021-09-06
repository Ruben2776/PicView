using PicView.UILogic;
using PicView.UILogic.Animations;
using System;
using System.Windows;
using System.Windows.Controls;
using static PicView.UILogic.Animations.MouseOverAnimations;
using static PicView.UILogic.UC;

namespace PicView.Views.UserControls
{
    /// <summary>
    /// Interaction logic for QuickSettingsMenu.xaml
    /// </summary>
    public partial class QuickSettingsMenu : UserControl
    {
        public QuickSettingsMenu()
        {
            InitializeComponent();

            ToggleScroll.IsChecked = Properties.Settings.Default.ScrollEnabled;
            ToggleScroll.Click += (s, x) => ConfigureSettings.UpdateUIValues.SetScrolling(Properties.Settings.Default.ScrollEnabled);

            SettingsButton.TheButton.Click += delegate
            {
                ConfigureWindows.AllSettingsWindow();
                Close_UserControls();
            };

            ToggleFill.IsChecked = Properties.Settings.Default.FillImage;
            ToggleFill.Click += ConfigureSettings.UpdateUIValues.SetAutoFill;

            ToggleLooping.IsChecked = Properties.Settings.Default.Looping;
            ToggleLooping.Click += async (_,_) => await ConfigureSettings.UpdateUIValues.SetLooping().ConfigureAwait(false);

            SetFit.IsChecked = Properties.Settings.Default.AutoFitWindow;
            SetFit.Click += ConfigureSettings.UpdateUIValues.SetAutoFit;


            // CropButton
            CropButton.PreviewMouseLeftButtonDown += delegate
            {
                PreviewMouseButtonDownAnim(CropButtonBrush);
            };

            CropButton.Click += delegate
            {
                UC.Close_UserControls();
                Editing.Crop.CropFunctions.StartCrop();
            };


            #region Animation events

            // Toggle Scroll
            ToggleScroll.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(ToggleScrollFill); };
            ToggleScroll.MouseEnter += delegate { ButtonMouseOverAnim(ToggleScrollFill); };
            ToggleScroll.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(ToggleScrollBrush); };
            ToggleScroll.MouseLeave += delegate { ButtonMouseLeaveAnim(ToggleScrollFill); };
            ToggleScroll.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(ToggleScrollBrush); };

            // Toggle Loop
            ToggleLooping.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(ToggleLoopFill); };
            ToggleLooping.MouseEnter += delegate { ButtonMouseOverAnim(ToggleLoopFill); };
            ToggleLooping.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(ToggleLoopBrush); };
            ToggleLooping.MouseLeave += delegate { ButtonMouseLeaveAnim(ToggleLoopFill); };
            ToggleLooping.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(ToggleLoopBrush); };

            // Set Fit
            SetFit.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(SetFitFill); };
            SetFit.MouseEnter += delegate { ButtonMouseOverAnim(SetFitFill); };
            SetFit.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(SetFitBrush); };
            SetFit.MouseLeave += delegate { ButtonMouseLeaveAnim(SetFitFill); };
            SetFit.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(SetFitBrush); };

            // ToggleFill
            ToggleFill.PreviewMouseLeftButtonDown += delegate { PreviewMouseButtonDownAnim(ToggleFillFill); };
            ToggleFill.MouseEnter += delegate { ButtonMouseOverAnim(ToggleFillFill); };
            ToggleFill.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(ToggleFillBrush); };
            ToggleFill.MouseLeave += delegate { ButtonMouseLeaveAnim(ToggleFillFill); };
            ToggleFill.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(ToggleFillBrush); };

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