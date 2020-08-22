using PicView.UILogic.Animations;
using PicView.UILogic.Sizing;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;
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
            appVersion.Content += fvi.FileVersion;

            ContentRendered += Window_ContentRendered;

            if (Properties.Settings.Default.DarkTheme)
            {
                var Source = new BitmapImage();
                Source.BeginInit();
                Source.UriSource = new Uri("pack://application:,,,/PicView;component/Themes/Resources/img/icon-raster.180l.png");
                Source.EndInit();

                Logo.Source = Source;
                Logo.Width = 180;
                Logo.Height = 133;
            }

            MaxWidth = MinWidth = 565 * WindowLogic.MonitorInfo.DpiScaling;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            // Center vertically
            Top = ((WindowLogic.MonitorInfo.WorkArea.Height * WindowLogic.MonitorInfo.DpiScaling) - ActualHeight) / 2 + WindowLogic.MonitorInfo.WorkArea.Top;

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