using AutoUpdaterDotNET;
using Microsoft.VisualBasic.Logging;
using PicView.Animations;
using PicView.ConfigureSettings;
using PicView.Shortcuts;
using PicView.SystemIntegration;
using PicView.UILogic.Sizing;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;

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

        private void ExtendOrCollopase()
        {
            double from, to;
            bool expanded;
            if (Height == startheight)
            {
                from = startheight;
                to = extendedheight;
                expanded = false;
            }
            else
            {
                to = startheight;
                from = extendedheight;
                expanded = true;
            }

            AnimationHelper.HeightAnimation(this, from, to, expanded);

            if (expanded)
            {
                Collaspse();
            }
            else
            {
                Extend();
            }
        }

        private void Extend()
        {
            Scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
            xGeo.Geometry = Geometry.Parse("F1 M512,512z M0,0z M414,321.94L274.22,158.82A24,24,0,0,0,237.78,158.82L98,321.94C84.66,337.51,95.72,361.56,116.22,361.56L395.82,361.56C416.32,361.56,427.38,337.51,414,321.94z");
        }

        private void Collaspse()
        {
            Scroller.ScrollToTop();
            Scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
            xGeo.Geometry = Geometry.Parse("F1 M512,512z M0,0z M98,190.06L237.78,353.18A24,24,0,0,0,274.22,353.18L414,190.06C427.34,174.49,416.28,150.44,395.78,150.44L116.18,150.44C95.6799999999999,150.44,84.6199999999999,174.49,97.9999999999999,190.06z");
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

            ExpandButton.Click += (_, _) => ExtendOrCollopase();

            PreviewMouseWheel += (_, e) => // Collapse when scrolling down
            {
                if (e.Delta < 0 && Height == startheight)
                {
                    ExtendOrCollopase();
                }
            };

            KeyDown += (_, e) => GenericWindowShortcuts.KeysDown(Scroller, e, this);
            Scroller.MouseWheel += (_, e) => GenericWindowShortcuts.Window_MouseWheel(Scroller, e);
            TitleBar.MouseLeftButtonDown += (_, _) => DragMove();

            CloseButton.TheButton.Click += delegate { Hide(); };
            MinButton.TheButton.Click += delegate { SystemCommands.MinimizeWindow(this); };

            Iconic.MouseEnter += delegate { AnimationHelper.MouseOverColorEvent(255, 255, 255, 255, IconicBrush, false); };
            Iconic.MouseLeave += delegate { AnimationHelper.MouseLeaveColorEvent(255, 255, 255, 255, IconicBrush, false); };

            Ionic.MouseEnter += delegate { AnimationHelper.MouseOverColorEvent(255, 255, 255, 255, IonicBrush, false); };
            Ionic.MouseLeave += delegate { AnimationHelper.MouseLeaveColorEvent(255, 255, 255, 255, IonicBrush, false); };

            FontAwesome.MouseEnter += delegate { AnimationHelper.MouseOverColorEvent(255, 255, 255, 255, FontAwesomeBrush, false); };
            FontAwesome.MouseLeave += delegate { AnimationHelper.MouseLeaveColorEvent(255, 255, 255, 255, FontAwesomeBrush, false); };

            GitHub.MouseEnter += delegate { AnimationHelper.MouseOverColorEvent(255, 255, 255, 255, GitHubBrush, false); };
            GitHub.MouseLeave += delegate { AnimationHelper.MouseLeaveColorEvent(255, 255, 255, 255, GitHubBrush, false); };

            License.MouseEnter += delegate { AnimationHelper.MouseOverColorEvent(255, 255, 255, 255, LicenseBrush, false); };
            License.MouseLeave += delegate { AnimationHelper.MouseLeaveColorEvent(255, 255, 255, 255, LicenseBrush, false); };

            zondicons.MouseEnter += delegate { AnimationHelper.MouseOverColorEvent(255, 255, 255, 255, zondiconsBrush, false); };
            zondicons.MouseLeave += delegate { AnimationHelper.MouseLeaveColorEvent(255, 255, 255, 255, zondiconsBrush, false); };

            freepik.MouseEnter += delegate { AnimationHelper.MouseOverColorEvent(255, 255, 255, 255, FreepikBrush, false); };
            freepik.MouseLeave += delegate { AnimationHelper.MouseLeaveColorEvent(255, 255, 255, 255, FreepikBrush, false); };

            PicViewSite.MouseEnter += delegate { AnimationHelper.MouseOverColorEvent(255, 255, 255, 255, PicViewBrush, false); };
            PicViewSite.MouseLeave += delegate { AnimationHelper.MouseLeaveColorEvent(255, 255, 255, 255, PicViewBrush, false); };

            UpdateButton.MouseLeftButtonDown += delegate
            {
                AutoUpdater.ShowRemindLaterButton = false;
                AutoUpdater.ReportErrors = true;
                AutoUpdater.ShowSkipButton = false;
                AutoUpdater.Start("https://picview.org/update.xml");
            };

            UpdateButton.MouseEnter += delegate { AnimationHelper.MouseOverColorEvent(255, 255, 255, 255, UpdateText, false); };
            UpdateButton.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(UpdateBrush); };
            UpdateButton.MouseLeave += delegate { AnimationHelper.MouseLeaveColorEvent(255, 255, 255, 255, UpdateText, false); };
            UpdateButton.MouseLeave += delegate { AnimationHelper.MouseLeaveBgTexColor(UpdateBrush); };
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