using System.Windows.Controls;
using System.Windows.Media;
using static PicView.Fields;
using static PicView.GoToLogic;
using static PicView.MouseOverAnimations;
using static PicView.UC;

namespace PicView.UserControls
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
            ToggleScroll.Click += (s, x) => Configs.SetScrolling(Properties.Settings.Default.ScrollEnabled);

            SettingsButton.Click += (s, x) => LoadWindows.AllSettingsWindow();

            ZoomButton.Click += delegate {
                Close_UserControls();
                Pan_and_Zoom.ResetZoom();
            };

            SettingsButton.Click += delegate {
                Close_UserControls();
                LoadWindows.AllSettingsWindow();
            };

            InfoButton.Click += delegate {
                Close_UserControls();
                LoadWindows.HelpWindow();
            };

            ToggleFill.IsChecked = Properties.Settings.Default.FillImage;
            ToggleFill.Click += Configs.SetAutoFill;

            ToggleLooping.IsChecked = Properties.Settings.Default.Looping;
            ToggleLooping.Click += Configs.SetLooping;

            SetFit.IsChecked = Properties.Settings.Default.WindowBehaviour;
            SetFit.Click += Configs.SetAutoFit;

            GoToPic.Click += GoToPicEvent;
            GoToPicBox.PreviewMouseLeftButtonDown += delegate {
                GoToPicBox.CaretBrush = new SolidColorBrush(mainColor);
            };
            GoToPicBox.PreviewKeyDown += GotoPicsShortcuts.GoToPicPreviewKeys;

            #region Animation events

            // SettingsButton
            SettingsButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(SettingsButtonBrush);
            SettingsButton.MouseEnter += (s, x) => ButtonMouseOverAnim(SettingsButtonBrush, true);
            SettingsButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(SettingsButtonBrush, false);


            // InfoButton
            InfoButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(InfoButtonBrush);
            InfoButton.MouseEnter += (s, x) => ButtonMouseOverAnim(InfoButtonBrush, true);
            InfoButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(InfoButtonBrush, false);


            // Zoom button
            ZoomButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(ZoomButtonBrush);
            ZoomButton.MouseEnter += (s, x) => ButtonMouseOverAnim(ZoomButtonBrush, true);
            ZoomButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(ZoomButtonBrush, false);


            // Toggle Scroll
            ToggleScroll.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(ToggleScrollBrush);
            ToggleScroll.MouseEnter += (s, x) => ButtonMouseOverAnim(ToggleScrollBrush, true);
            ToggleScroll.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(ToggleScrollBrush, false);

            // Toggle Loop
            ToggleLooping.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(ToggleLoopBrush);
            ToggleLooping.MouseEnter += (s, x) => ButtonMouseOverAnim(ToggleLoopBrush, true);
            ToggleLooping.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(ToggleLoopBrush, false);

            // BgButton
            BgButton.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(BgButtonBrush);
            BgButton.MouseEnter += (s, x) => ButtonMouseOverAnim(BgButtonBrush, true);
            BgButton.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(BgButtonBrush, false);
            BgButton.Click += Utilities.ChangeBackground;

            // Set Fit
            SetFit.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(SetFitBrush);
            SetFit.MouseEnter += (s, x) => ButtonMouseOverAnim(SetFitBrush, true);
            SetFit.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(SetFitBrush, false);

            // ToggleFill
            ToggleFill.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(ToggleFillBrush);
            ToggleFill.MouseEnter += (s, x) => ButtonMouseOverAnim(ToggleFillBrush, true);
            ToggleFill.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(ToggleFillBrush, false);

            // GoToPic
            GoToPic.PreviewMouseLeftButtonDown += (s, x) => PreviewMouseButtonDownAnim(GoToPicBrush);
            GoToPic.MouseEnter += (s, x) => ButtonMouseOverAnim(GoToPicBrush, true);
            GoToPic.MouseLeave += (s, x) => ButtonMouseLeaveAnimBgColor(GoToPicBrush, false);



            #endregion
        }

    }
}
