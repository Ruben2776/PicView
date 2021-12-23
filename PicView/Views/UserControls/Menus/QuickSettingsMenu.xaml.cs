using PicView.Animations;
using PicView.UILogic;
using System.Windows.Controls;
using static PicView.Animations.MouseOverAnimations;
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

            SettingsButton.TheButton.Click += delegate
            {
                ConfigureWindows.AllSettingsWindow();
                Close_UserControls();
            };

            ToggleScroll.IsChecked = Properties.Settings.Default.ScrollEnabled;
            ToggleScroll.Click += (s, x) => ConfigureSettings.UpdateUIValues.SetScrolling(Properties.Settings.Default.ScrollEnabled);
            ToggleScrollBorder.MouseLeftButtonDown += (s, x) => ConfigureSettings.UpdateUIValues.SetScrolling(Properties.Settings.Default.ScrollEnabled);

            ToggleFill.IsChecked = Properties.Settings.Default.FillImage;
            ToggleFill.Click += async (s, e) => await ConfigureSettings.UpdateUIValues.SetAutoFillAsync(s, e).ConfigureAwait(false);
            ToggleFillBorder.MouseLeftButtonDown += async (s, e) => await ConfigureSettings.UpdateUIValues.SetAutoFillAsync(s, e).ConfigureAwait(false);

            ToggleLooping.IsChecked = Properties.Settings.Default.Looping;
            ToggleLooping.Click += (_, _) => ConfigureSettings.UpdateUIValues.SetLooping();
            ToggleLoopingBorder.MouseLeftButtonDown += (_, _) => ConfigureSettings.UpdateUIValues.SetLooping();

            SetFit.IsChecked = Properties.Settings.Default.AutoFitWindow;
            SetFit.Click += async (s, e) => await ConfigureSettings.UpdateUIValues.SetAutoFitAsync(s, e).ConfigureAwait(false);
            SetFitBorder.MouseLeftButtonDown += async (s, e) => await ConfigureSettings.UpdateUIValues.SetAutoFitAsync(s, e).ConfigureAwait(false);

            // CropButton
            CropButton.PreviewMouseLeftButtonDown += delegate
            {
                ButtonMouseOverAnim(CropFill, false, true);
            };

            CropButton.Click += delegate
            {
                UC.Close_UserControls();
                Editing.Crop.CropFunctions.StartCrop();
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