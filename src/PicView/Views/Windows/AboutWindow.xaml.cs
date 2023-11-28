using AutoUpdaterDotNET;
using PicView.Animations;
using PicView.ConfigureSettings;
using PicView.Properties;
using PicView.Shortcuts;
using PicView.SystemIntegration;
using PicView.UILogic;
using PicView.UILogic.Sizing;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;

namespace PicView.Views.Windows
{
    public partial class AboutWindow
    {
        private readonly double startHeight;
        private readonly double extendedHeight;

        public AboutWindow()
        {
            InitializeComponent();

            extendedHeight = 750;
            startHeight = Height;

            // Get version
            var assembly = Assembly.GetExecutingAssembly();
            var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            appVersion.Text += " " + fvi.FileVersion;

            ContentRendered += Window_ContentRendered;

            MaxWidth = MinWidth = 565 * WindowSizing.MonitorInfo.DpiScaling;
            if (!double.IsNaN(Width)) return; // Fixes if user opens window when loading from startup
            WindowSizing.MonitorInfo = MonitorSize.GetMonitorSize();
            MaxHeight = WindowSizing.MonitorInfo.WorkArea.Height;
            Width *= WindowSizing.MonitorInfo.DpiScaling;
            MaxWidth = MinWidth = 565 * WindowSizing.MonitorInfo.DpiScaling;
        }

        private void Window_ContentRendered(object? sender, EventArgs e)
        {
            WindowBlur.EnableBlur(this);

            Deactivated += (_, _) => ConfigColors.WindowUnfocusOrFocus(TitleBar, TitleText, ExpandBorder, false);
            Activated += (_, _) => ConfigColors.WindowUnfocusOrFocus(TitleBar, TitleText, ExpandBorder, true);

            MouseLeftButtonDown += (_, mouseButtonEventArgs) =>
            {
                if (mouseButtonEventArgs.LeftButton == MouseButtonState.Pressed) DragMove();
            };

            // ExpandButton
            ExpandButton.MouseEnter += (_, _) => MouseOverAnimations.ButtonMouseOverAnim(chevronDownBrush);
            ExpandButton.MouseEnter += (_, _) => AnimationHelper.MouseEnterBgTexColor(ExpandButtonBg);
            ExpandButton.MouseLeave += (_, _) => MouseOverAnimations.ButtonMouseLeaveAnim(chevronDownBrush);
            ExpandButton.MouseLeave += (_, _) => AnimationHelper.MouseLeaveBgTexColor(ExpandButtonBg);

            ExpandButton.Click += (_, _) =>
                UIHelper.ExtendOrCollapse(Height, startHeight, extendedHeight, this, Scroller, xGeo);

            PreviewMouseWheel += (_, mouseWheelEventArgs) => // Collapse when scrolling down
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (mouseWheelEventArgs.Delta < 0 && Height == startHeight)
                {
                    UIHelper.ExtendOrCollapse(Height, startHeight, extendedHeight, this, Scroller, xGeo);
                }
            };

            KeyDown += (_, e) => GenericWindowShortcuts.KeysDown(Scroller, e, this);
            Scroller.MouseWheel += (_, e) => GenericWindowShortcuts.Window_MouseWheel(Scroller, e);
            TitleBar.MouseLeftButtonDown += (_, _) => DragMove();

            CloseButton.TheButton.Click += delegate { Hide(); };
            MinButton.TheButton.Click += delegate { SystemCommands.MinimizeWindow(this); };

            var color = Settings.Default.DarkTheme ? Colors.White : (Color)Application.Current.Resources["MainColor"];

            Iconic.MouseEnter += delegate
            {
                AnimationHelper.MouseOverColorEvent(color.A, color.R, color.G, color.B, IconicBrush);
            };
            Iconic.MouseLeave += delegate
            {
                AnimationHelper.MouseLeaveColorEvent(color.A, color.R, color.G, color.B, IconicBrush);
            };

            Ionic.MouseEnter += delegate
            {
                AnimationHelper.MouseOverColorEvent(color.A, color.R, color.G, color.B, IonicBrush);
            };
            Ionic.MouseLeave += delegate
            {
                AnimationHelper.MouseLeaveColorEvent(color.A, color.R, color.G, color.B, IonicBrush);
            };

            FontAwesome.MouseEnter += delegate
            {
                AnimationHelper.MouseOverColorEvent(color.A, color.R, color.G, color.B, FontAwesomeBrush);
            };
            FontAwesome.MouseLeave += delegate
            {
                AnimationHelper.MouseLeaveColorEvent(color.A, color.R, color.G, color.B, FontAwesomeBrush);
            };

            GitHub.MouseEnter += delegate
            {
                AnimationHelper.MouseOverColorEvent(color.A, color.R, color.G, color.B, GitHubBrush);
            };
            GitHub.MouseLeave += delegate
            {
                AnimationHelper.MouseLeaveColorEvent(color.A, color.R, color.G, color.B, GitHubBrush);
            };

            License.MouseEnter += delegate
            {
                AnimationHelper.MouseOverColorEvent(color.A, color.R, color.G, color.B, LicenseBrush);
            };
            License.MouseLeave += delegate
            {
                AnimationHelper.MouseLeaveColorEvent(color.A, color.R, color.G, color.B, LicenseBrush);
            };

            zondicons.MouseEnter += delegate
            {
                AnimationHelper.MouseOverColorEvent(color.A, color.R, color.G, color.B, zondiconsBrush);
            };
            zondicons.MouseLeave += delegate
            {
                AnimationHelper.MouseLeaveColorEvent(color.A, color.R, color.G, color.B, zondiconsBrush);
            };

            freepik.MouseEnter += delegate
            {
                AnimationHelper.MouseOverColorEvent(color.A, color.R, color.G, color.B, FreepikBrush);
            };
            freepik.MouseLeave += delegate
            {
                AnimationHelper.MouseLeaveColorEvent(color.A, color.R, color.G, color.B, FreepikBrush);
            };

            PicViewSite.MouseEnter += delegate
            {
                AnimationHelper.MouseOverColorEvent(color.A, color.R, color.G, color.B, PicViewBrush);
            };
            PicViewSite.MouseLeave += delegate
            {
                AnimationHelper.MouseLeaveColorEvent(color.A, color.R, color.G, color.B, PicViewBrush);
            };

            UpdateButton.MouseEnter += delegate
            {
                AnimationHelper.MouseOverColorEvent(color.A, color.R, color.G, color.B, UpdateText);
            };
            UpdateButton.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(UpdateBrush); };
            UpdateButton.MouseLeave += delegate
            {
                AnimationHelper.MouseLeaveColorEvent(color.A, color.R, color.G, color.B, UpdateText);
            };
            UpdateButton.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(UpdateBrush); };

            UpdateButton.MouseLeftButtonDown += delegate
            {
                var currentDirectory = new DirectoryInfo(Application.Current.StartupUri.AbsolutePath);
                if (currentDirectory.Parent != null)
                {
                    AutoUpdater.InstallationPath = currentDirectory.Parent.FullName;
                }
                AutoUpdater.ShowRemindLaterButton = false;
                AutoUpdater.ReportErrors = true;
                AutoUpdater.ShowSkipButton = false;
                AutoUpdater.SetOwner(this);
                AutoUpdater.RunUpdateAsAdmin = true;
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