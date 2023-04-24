using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using AutoUpdaterDotNET;
using PicView.Animations;
using PicView.ConfigureSettings;
using PicView.Properties;
using PicView.Shortcuts;
using PicView.SystemIntegration;
using PicView.UILogic;
using PicView.UILogic.Sizing;

namespace PicView.Views.Windows
{
    public partial class InfoWindow : Window
    {
        private double startheight, extendedheight;

        public InfoWindow()
        {
            InitializeComponent();

            extendedheight = 750;
            startheight = Height;

            // Get version
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            appVersion.Text += " " + fvi.FileVersion;

            ContentRendered += Window_ContentRendered;

            MaxWidth = MinWidth = 565 * WindowSizing.MonitorInfo.DpiScaling;
            if (double.IsNaN(Width)) // Fixes if user opens window when loading from startup
            {
                WindowSizing.MonitorInfo = MonitorSize.GetMonitorSize();
                MaxHeight = WindowSizing.MonitorInfo.WorkArea.Height;
                Width *= WindowSizing.MonitorInfo.DpiScaling;
                MaxWidth = MinWidth = 565 * WindowSizing.MonitorInfo.DpiScaling;
            }
        }

        public void ChangeColor()
        {
            Logo.ChangeColor();
            AccentBrush.Brush = new SolidColorBrush(ConfigColors.GetSecondaryAccentColor);
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            WindowBlur.EnableBlur(this);
            ChangeColor();

            MouseLeftButtonDown += (_, e) =>
            { if (e.LeftButton == MouseButtonState.Pressed) { DragMove(); } };

            // ExpandButton
            ExpandButton.MouseEnter += (_, _) => MouseOverAnimations.ButtonMouseOverAnim(chevronDownBrush);
            ExpandButton.MouseEnter += (_, _) => AnimationHelper.MouseEnterBgTexColor(ExpandButtonBg);
            ExpandButton.MouseLeave += (_, _) => MouseOverAnimations.ButtonMouseLeaveAnim(chevronDownBrush);
            ExpandButton.MouseLeave += (_, _) => AnimationHelper.MouseLeaveBgTexColor(ExpandButtonBg);

            ExpandButton.Click += (_, _) => UIHelper.ExtendOrCollapse(Height, startheight, extendedheight, this, Scroller, xGeo);

            PreviewMouseWheel += (_, e) => // Collapse when scrolling down
            {
                if (e.Delta < 0 && Height == startheight)
                {
                    UIHelper.ExtendOrCollapse(Height, startheight, extendedheight, this, Scroller, xGeo);
                }
            };

            KeyDown += (_, e) => GenericWindowShortcuts.KeysDown(Scroller, e, this);
            Scroller.MouseWheel += (_, e) => GenericWindowShortcuts.Window_MouseWheel(Scroller, e);
            TitleBar.MouseLeftButtonDown += (_, _) => DragMove();

            CloseButton.TheButton.Click += delegate { Hide(); };
            MinButton.TheButton.Click += delegate { SystemCommands.MinimizeWindow(this); };

            var color = Settings.Default.DarkTheme ? Colors.White : (Color) Application.Current.Resources["MainColor"];

            Iconic.MouseEnter += delegate { AnimationHelper.MouseOverColorEvent(color.A, color.R, color.G, color.B, IconicBrush); };
            Iconic.MouseLeave += delegate { AnimationHelper.MouseLeaveColorEvent(color.A, color.R, color.G, color.B, IconicBrush); };

            Ionic.MouseEnter += delegate { AnimationHelper.MouseOverColorEvent(color.A, color.R, color.G, color.B, IonicBrush); };
            Ionic.MouseLeave += delegate { AnimationHelper.MouseLeaveColorEvent(color.A, color.R, color.G, color.B, IonicBrush); };

            FontAwesome.MouseEnter += delegate { AnimationHelper.MouseOverColorEvent(color.A, color.R, color.G, color.B, FontAwesomeBrush); };
            FontAwesome.MouseLeave += delegate { AnimationHelper.MouseLeaveColorEvent(color.A, color.R, color.G, color.B, FontAwesomeBrush); };

            GitHub.MouseEnter += delegate { AnimationHelper.MouseOverColorEvent(color.A, color.R, color.G, color.B, GitHubBrush); };
            GitHub.MouseLeave += delegate { AnimationHelper.MouseLeaveColorEvent(color.A, color.R, color.G, color.B, GitHubBrush); };

            License.MouseEnter += delegate { AnimationHelper.MouseOverColorEvent(color.A, color.R, color.G, color.B, LicenseBrush); };
            License.MouseLeave += delegate { AnimationHelper.MouseLeaveColorEvent(color.A, color.R, color.G, color.B, LicenseBrush); };

            zondicons.MouseEnter += delegate { AnimationHelper.MouseOverColorEvent(color.A, color.R, color.G, color.B, zondiconsBrush); };
            zondicons.MouseLeave += delegate { AnimationHelper.MouseLeaveColorEvent(color.A, color.R, color.G, color.B, zondiconsBrush); };

            freepik.MouseEnter += delegate { AnimationHelper.MouseOverColorEvent(color.A, color.R, color.G, color.B, FreepikBrush); };
            freepik.MouseLeave += delegate { AnimationHelper.MouseLeaveColorEvent(color.A, color.R, color.G, color.B, FreepikBrush); };

            PicViewSite.MouseEnter += delegate { AnimationHelper.MouseOverColorEvent(color.A, color.R, color.G, color.B, PicViewBrush); };
            PicViewSite.MouseLeave += delegate { AnimationHelper.MouseLeaveColorEvent(color.A, color.R, color.G, color.B, PicViewBrush); };

            UpdateButton.MouseEnter += delegate { AnimationHelper.MouseOverColorEvent(color.A, color.R, color.G, color.B, UpdateText); };
            UpdateButton.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(UpdateBrush); };
            UpdateButton.MouseLeave += delegate { AnimationHelper.MouseLeaveColorEvent(color.A, color.R, color.G, color.B, UpdateText); };
            UpdateButton.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(UpdateBrush); };


            UpdateButton.MouseLeftButtonDown += delegate
            {
                AutoUpdater.ShowRemindLaterButton = false;
                AutoUpdater.ReportErrors = true;
                AutoUpdater.ShowSkipButton = false;
                AutoUpdater.Start("https://picview.org/update.xml");
            };
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            var ps = new ProcessStartInfo(e.Uri.AbsoluteUri)
            {
                UseShellExecute = true,
                Verb = "open"
            };
            Process.Start(ps);
        }
    }
}