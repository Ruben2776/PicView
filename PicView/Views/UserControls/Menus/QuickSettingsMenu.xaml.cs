using PicView.UILogic.Animations;
using PicView.UILogic.Loading;
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
                LoadWindows.AllSettingsWindow();
                Close_UserControls();
            };

            InfoButton.TheButton.Click += delegate
            {
                Close_UserControls();
                LoadWindows.InfoWindow();
            };

            ToggleFill.IsChecked = Properties.Settings.Default.FillImage;
            ToggleFill.Click += ConfigureSettings.UpdateUIValues.SetAutoFill;

            ToggleLooping.IsChecked = Properties.Settings.Default.Looping;
            ToggleLooping.Click += ConfigureSettings.UpdateUIValues.SetLooping;

            SetFit.IsChecked = Properties.Settings.Default.AutoFitWindow;
            SetFit.Click += ConfigureSettings.UpdateUIValues.SetAutoFit;

            ZoomButton.TheButton.Click += delegate
            {
                if (ZoomSliderParent.Visibility == Visibility.Collapsed || ZoomSliderParent.Opacity == 0)
                {
                    ZoomSliderParent.Visibility = Visibility.Visible;
                    AnimationHelper.Fade(ZoomSliderParent, 1, TimeSpan.FromSeconds(.4));
                }
                else
                {
                    AnimationHelper.Fade(ZoomSliderParent, 0, TimeSpan.FromSeconds(.3));
                }
            };

            ZoomSlider.ValueChanged += delegate { UILogic.TransformImage.ZoomLogic.Zoom(ZoomSlider.Value); };

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

            #endregion Animation events
        }
    }
}