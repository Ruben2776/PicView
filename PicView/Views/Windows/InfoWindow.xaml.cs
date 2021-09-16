using AutoUpdaterDotNET;
using PicView.UILogic.Animations;
using PicView.UILogic.Sizing;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;

namespace PicView.Views.Windows
{
    public partial class InfoWindow : Window
    {
        public InfoWindow()
        {
            InitializeComponent();

            // Get version
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            appVersion.Text += " " + fvi.FileVersion;

            ContentRendered += Window_ContentRendered;

            MaxWidth = MinWidth = 565 * WindowSizing.MonitorInfo.DpiScaling;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            // Center vertically
            Top = ((WindowSizing.MonitorInfo.WorkArea.Height * WindowSizing.MonitorInfo.DpiScaling) - ActualHeight) / 2 + WindowSizing.MonitorInfo.WorkArea.Top;

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