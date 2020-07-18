using PicView.UILogic.Animations;
using PicView.UILogic.Loading;
using System;
using System.Windows;
using System.Windows.Controls;
using static PicView.UILogic.Animations.MouseOverAnimations;
using static PicView.UILogic.UserControls.UC;

namespace PicView.UILogic.UserControls
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
            ToggleScroll.Click += (s, x) => UpdateUIValues.SetScrolling(Properties.Settings.Default.ScrollEnabled);

            SettingsButton.Click += (s, x) => LoadWindows.AllSettingsWindow();

            SettingsButton.Click += delegate
            {
                Close_UserControls();
                LoadWindows.AllSettingsWindow();
            };

            InfoButton.Click += delegate
            {
                Close_UserControls();
                LoadWindows.InfoDialogWindow();
            };

            ToggleFill.IsChecked = Properties.Settings.Default.FillImage;
            ToggleFill.Click += UpdateUIValues.SetAutoFill;

            ToggleLooping.IsChecked = Properties.Settings.Default.Looping;
            ToggleLooping.Click += UpdateUIValues.SetLooping;

            SetFit.IsChecked = Properties.Settings.Default.AutoFitWindow;
            SetFit.Click += UpdateUIValues.SetAutoFit;


            ZoomButton.Click += delegate
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

            ZoomSlider.ValueChanged += delegate { TransformImage.ZoomLogic.Zoom(ZoomSlider.Value); };

            #region Animation events

            // SettingsButton
            SettingsButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(SettingsButtonBrush);
            SettingsButton.MouseEnter += (s, x) => ButtonMouseOverAnim(SettingsButtonBrush, true);
            SettingsButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(SettingsButtonBrush, false);

            // InfoButton
            InfoButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(InfoButtonBrush);
            InfoButton.MouseEnter += (s, x) => ButtonMouseOverAnim(InfoButtonBrush, true);
            InfoButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(InfoButtonBrush, false);

            // Toggle Scroll
            ToggleScroll.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(ToggleScrollBrush);
            ToggleScroll.MouseEnter += (s, x) => ButtonMouseOverAnim(ToggleScrollBrush, true);
            ToggleScroll.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(ToggleScrollBrush, false);

            // Toggle Loop
            ToggleLooping.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(ToggleLoopBrush);
            ToggleLooping.MouseEnter += (s, x) => ButtonMouseOverAnim(ToggleLoopBrush, true);
            ToggleLooping.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(ToggleLoopBrush, false);

            // Set Fit
            SetFit.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(SetFitBrush);
            SetFit.MouseEnter += (s, x) => ButtonMouseOverAnim(SetFitBrush, true);
            SetFit.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(SetFitBrush, false);

            // ToggleFill
            ToggleFill.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(ToggleFillBrush);
            ToggleFill.MouseEnter += (s, x) => ButtonMouseOverAnim(ToggleFillBrush, true);
            ToggleFill.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(ToggleFillBrush, false);

            // ZoomButton
            ZoomButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(ZoomButtonBrush);
            ZoomButton.MouseEnter += (s, x) => ButtonMouseOverAnim(ZoomButtonBrush, true);
            ZoomButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(ZoomButtonBrush, false);

            #endregion Animation events
        }
    }
}