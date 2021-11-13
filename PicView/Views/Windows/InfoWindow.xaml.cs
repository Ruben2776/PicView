using AutoUpdaterDotNET;
using PicView.Animations;
using PicView.UILogic.Sizing;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Navigation;

namespace PicView.Views.Windows
{
    public partial class InfoWindow : Window
    {
        double startheight, extendedheight;

        public InfoWindow()
        {
            InitializeComponent();
            startheight = Height;
            extendedheight = 750;

            // Get version
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            appVersion.Text += " " + fvi.FileVersion;

            ContentRendered += Window_ContentRendered;

            MaxWidth = MinWidth = 565 * WindowSizing.MonitorInfo.DpiScaling;
            if (double.IsNaN(Width)) // Fixes if user opens window when loading from startup
            {
                WindowSizing.MonitorInfo = SystemIntegration.MonitorSize.GetMonitorSize();
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
            Scroller.VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Visible;
            xGeo.Geometry = Geometry.Parse("F1 M512,512z M0,0z M414,321.94L274.22,158.82A24,24,0,0,0,237.78,158.82L98,321.94C84.66,337.51,95.72,361.56,116.22,361.56L395.82,361.56C416.32,361.56,427.38,337.51,414,321.94z");
        }

        private void Collaspse()
        {
            Scroller.ScrollToTop();
            Scroller.VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Hidden;
            xGeo.Geometry = Geometry.Parse("F1 M512,512z M0,0z M98,190.06L237.78,353.18A24,24,0,0,0,274.22,353.18L414,190.06C427.34,174.49,416.28,150.44,395.78,150.44L116.18,150.44C95.6799999999999,150.44,84.6199999999999,174.49,97.9999999999999,190.06z");
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            // ExpandButton
            ExpandButton.PreviewMouseLeftButtonDown += (_, _) => MouseOverAnimations.PreviewMouseButtonDownAnim(chevronDownBrush);
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

            KeyDown += (_, e) => Shortcuts.GenericWindowShortcuts.KeysDown(Scroller, e, this);
            Scroller.MouseWheel += (_, e) => Shortcuts.GenericWindowShortcuts.Window_MouseWheel(Scroller, e);
            Bar.MouseLeftButtonDown += (_, _) => DragMove();

            CloseButton.TheButton.Click += delegate { Hide(); };
            MinButton.TheButton.Click += delegate { SystemCommands.MinimizeWindow(this); };

            Iconic.MouseEnter += delegate { MouseOverAnimations.ButtonMouseOverAnim(IconicBrush); };
            Iconic.MouseLeave += delegate { MouseOverAnimations.ButtonMouseLeaveAnim(IconicBrush); };
            Iconic.PreviewMouseLeftButtonDown += delegate { MouseOverAnimations.PreviewMouseButtonDownAnim(IconicBrush); };

            Ionic.MouseEnter += delegate { MouseOverAnimations.ButtonMouseOverAnim(IonicBrush); };
            Ionic.MouseLeave += delegate { MouseOverAnimations.ButtonMouseLeaveAnim(IonicBrush); };
            Ionic.PreviewMouseLeftButtonDown += delegate { MouseOverAnimations.PreviewMouseButtonDownAnim(IonicBrush); };

            FontAwesome.MouseEnter += delegate { MouseOverAnimations.ButtonMouseOverAnim(FontAwesomeBrush); };
            FontAwesome.MouseLeave += delegate { MouseOverAnimations.ButtonMouseLeaveAnim(FontAwesomeBrush); };
            FontAwesome.PreviewMouseLeftButtonDown += delegate { MouseOverAnimations.PreviewMouseButtonDownAnim(FontAwesomeBrush); };

            GitHub.MouseEnter += delegate { MouseOverAnimations.ButtonMouseOverAnim(GitHubBrush); };
            GitHub.MouseLeave += delegate { MouseOverAnimations.ButtonMouseLeaveAnim(GitHubBrush); };
            GitHub.PreviewMouseLeftButtonDown += delegate { MouseOverAnimations.PreviewMouseButtonDownAnim(GitHubBrush); };

            License.MouseEnter += delegate { MouseOverAnimations.ButtonMouseOverAnim(LicenseBrush); };
            License.MouseLeave += delegate { MouseOverAnimations.ButtonMouseLeaveAnim(LicenseBrush); };
            License.PreviewMouseLeftButtonDown += delegate { MouseOverAnimations.PreviewMouseButtonDownAnim(LicenseBrush); };

            zondicons.MouseEnter += delegate { MouseOverAnimations.ButtonMouseOverAnim(zondiconsBrush); };
            zondicons.MouseLeave += delegate { MouseOverAnimations.ButtonMouseLeaveAnim(zondiconsBrush); };
            zondicons.PreviewMouseLeftButtonDown += delegate { MouseOverAnimations.PreviewMouseButtonDownAnim(zondiconsBrush); };

            freepik.MouseEnter += delegate { MouseOverAnimations.ButtonMouseOverAnim(FreepikBrush); };
            freepik.MouseLeave += delegate { MouseOverAnimations.ButtonMouseLeaveAnim(FreepikBrush); };
            freepik.PreviewMouseLeftButtonDown += delegate { MouseOverAnimations.PreviewMouseButtonDownAnim(FreepikBrush); };

            PicViewSite.MouseEnter += delegate { MouseOverAnimations.ButtonMouseOverAnim(PicViewBrush); };
            PicViewSite.MouseLeave += delegate { MouseOverAnimations.ButtonMouseLeaveAnim(PicViewBrush); };
            PicViewSite.PreviewMouseLeftButtonDown += delegate { MouseOverAnimations.PreviewMouseButtonDownAnim(PicViewBrush); };

            UpdateButton.MouseLeftButtonUp += delegate
            {
                AutoUpdater.ShowRemindLaterButton = false;
                AutoUpdater.ReportErrors = true;
                AutoUpdater.ShowSkipButton = false;
                AutoUpdater.Start("https://picview.org/update.xml");
            };

            UpdateButton.MouseEnter += delegate { MouseOverAnimations.ButtonMouseOverAnim(UpdateText); };
            UpdateButton.MouseEnter += delegate { AnimationHelper.MouseEnterBgTexColor(UpdateBrush); };
            UpdateButton.MouseLeave += delegate { MouseOverAnimations.ButtonMouseLeaveAnim(UpdateText); };
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